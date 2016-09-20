using System;
using UnityEditor;
using UnityEngine;
public class EditorCNStyles
{
    public static GUIStyle button;
    public static GUIStyle largeButton;

    static EditorCNStyles()
    {
        button = new GUIStyle("button");
        //button.font = AssetDatabase.LoadAllAssetsAtPath("Assets/_Resource/Font/msyh.ttf", typeof(Font)) as Font;
        button.fontSize = 12;

        largeButton = new GUIStyle(button);
        largeButton.fontSize = 24;
    }

}
