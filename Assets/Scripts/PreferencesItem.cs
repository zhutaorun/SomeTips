//using UnityEngine;
//using System.Collections;
//using UnityEditor;
//public class PreferencesItem:Editor{
//	[PreferenceItem("BCB")]//Preference扩展功能
//	public static void PreferencesGUI()
//	{
//		_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
//
//		GUILayout.Beginvertical();
//		{
//			GUILayout.Label("Testing",EditorStyles.boldLabel);
//			GUILayout.Space(5f);
//
//			if(!demoMode)
//			{
//				skipMenu = GUILayout.Toggle(skipMenu,"Skip Menu");
//				if(skipMenu)
//				{
//					EditorGUILayout.PropertyField(goToLevelProperty,new GUIContent("Level:"));
//					GUILayou.Space(5f);
//				}
//			}
//		}
//
//	}
//}
