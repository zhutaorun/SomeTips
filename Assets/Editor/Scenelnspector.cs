using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(UnityEditor.AssetDeleteResult))]
public class CustomInspector : Editor {

	public override void OnInspectorGUI()
	{
		string path = AssetDatabase.GetAssetPath (target);

		GUI.enabled = true;
		if (path.EndsWith (".unity")) 
		{
			GUILayout.Button("我是场景");
		}
		else if(path.EndsWith(""))
		{
			GUILayout.Button("我是文件夹");
		}
	}
}
