using UnityEngine;
using System.Collections;
using UnityEditor;

public class EditorTools : EditorWindow
{

    string path;
    Rect rect;
    string strForShader = "";
    [MenuItem("Window/TestDrag")]
    static void Init()
    {
        EditorTools wind = (EditorTools)EditorWindow.GetWindow(typeof(EditorTools));
        wind.position = new Rect(300,300,860,550);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10,10,600,520));
        //EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("路径");
        
        //获得一个长300的框
        rect = EditorGUILayout.GetControlRect(GUILayout.Width(300));
        //将上面的框作为文本输入框
        path = EditorGUI.TextField(rect, path);

        //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内
        if ((Event.current.type == EventType.DragUpdated
          || Event.current.type == EventType.DragExited)
          && rect.Contains(Event.current.mousePosition))
        {
            //改变鼠标的外表
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                path = DragAndDrop.paths[0];
            }
        }
        EditorGUILayout.BeginHorizontal();
        
        strForShader = GUILayout.TextArea(strForShader, GUIStyle.none, GUILayout.Width(450), GUILayout.Height(20));
        if (GUILayout.Button("粘贴"))
        {
            TextEditor te = new TextEditor();
            te.Paste();
            strForShader = te.content.text;
        }
      //  GUILayout.HorizontalScrollbar(1f,)
        EditorGUILayout.EndHorizontal();
        //EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
