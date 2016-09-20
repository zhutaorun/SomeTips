using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
public class BuildAssetBundleInOneClickWindow : EditorWindow
{

    [MenuItem("Window/打开一键打包窗口")]
    private static void ShowWindow()
    {
        var w = EditorWindow.GetWindow<BuildAssetBundleInOneClickWindow>();
        w.title = "一键打包";
        w.Show();
    }

    private float colorAnimationValue = 0;
    private bool buttonPressed = false;

    private void OnGUI()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        var c1 = 0;
        var c2 = 0.7f + 0.3f*colorAnimationValue;
        GUI.color = new Color(c1,c2,c1);
        if (GUILayout.Button("一键生成AssetBundle", EditorCNStyles.largeButton, GUILayout.MinHeight(120),
            GUILayout.MinWidth(400)))
        {
            buttonPressed = true;
        }
        GUI.color = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新BundleManager列表"))
        {
            RefreshBundlesForUI();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void Update()
    {
        if (buttonPressed)
        {
            buttonPressed = false;
            RefreshBundlesForUI();
            BuildHelper.BuildAll();
        }
        else
        {
            colorAnimationValue = Mathf.Sin(Time.realtimeSinceStartup*10)*0.5f + 0.5f;
            Repaint();
        }
    }

    public static void RefreshBundlesForUI()
    {
        const string STANDLONE_BUNDLE_NAME="UIStandalone";    

        //Check Error
        var standaloneBundle = BundleManager.GetBundleData(STANDLONE_BUNDLE_NAME);
        if (standaloneBundle == null)
        {
            Debug.LogError("Cannot find parent bundle:"+STANDLONE_BUNDLE_NAME);
            return;
        }

        //standalone UI Bundles
        var groups = new Dictionary<string, string>();
        foreach (var filePath in Directory.GetFiles("Assets/_Prefab/Edit/UI/Windwos", ".prefab", SearchOption.TopDirectoryOnly))
        {
            var groupName = "UI/" + Path.GetFileNameWithoutExtension(filePath);
            groups.Add(groupName,filePath);
        }
        foreach (var filePath in Directory.GetFiles("Assets/_Prefab/Edit/UI/NewUI",".prefab",SearchOption.TopDirectoryOnly))
        {
            var groupName = "UI/" + Path.GetFileNameWithoutExtension(filePath);
            groups.Add(groupName,filePath);
        }

        var oldGroups = BundleManager.GetBundleData(STANDLONE_BUNDLE_NAME).GetChildren().ToArray();
        foreach (var oldGroup in oldGroups)
        {
            if (!groups.ContainsKey(oldGroup))
            {
                BundleManager.RemoveBundle(oldGroup);
            }
        }

        foreach (var pair in groups)
        {
            var groupName = pair.Key;
            var bundle = BundleManager.GetBundleData(groupName);
            if (bundle == null)
            {
                BundleManager.CreateNewBundle(groupName, STANDLONE_BUNDLE_NAME, BundleType.Normal);
                bundle = BundleManager.GetBundleData(groupName);
                BundleManager.AddPathToBundle(pair.Value,groupName);
            }
            else
            {
                var path = pair.Value;
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (bundle.includeGUIDs[0] != guid)
                {
                    BundleManager.RemoveAssetFromBundle(bundle.includeGUIDs[0],groupName);
                    BundleManager.AddPathToBundle(path,groupName);
                }
            }
        }
    }
}
