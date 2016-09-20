using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

internal class BMGUIStyles
{
	Dictionary<string, GUIStyle> styleDict = new Dictionary<string, GUIStyle>();
	Dictionary<string, Texture2D> iconDict = new Dictionary<string, Texture2D>();
	
	private static BMGUIStyles instance = null;
	private static BMGUIStyles getInstance()
	{
		if(instance == null)
		{
			// Instancial
			instance = new BMGUIStyles();
			GUIStyleSet styleSet = (GUIStyleSet)AssetDatabase.LoadAssetAtPath("Assets/BundleManager/Editor/customStyles.asset", typeof(GUIStyleSet));
			var styles = EditorGUIUtility.isProSkin ? styleSet.styles : styleSet.freeStyles;
			foreach(GUIStyle style in styles)
			{
				if(instance.styleDict.ContainsKey(style.name))
					Debug.LogError("Duplicated GUIStyle " + style.name);
				else
					instance.styleDict.Add(style.name, style);
			}
			
			foreach(Texture2D icon in styleSet.icons)
			{
				if(instance.iconDict.ContainsKey(icon.name))
					Debug.LogError("Duplicated icon " + icon.name);
				else
					instance.iconDict.Add(icon.name, icon);
			}
		}
			
		return instance;
	}
	
	public static GUIStyle GetStyle(string name)
	{
		if( !getInstance().styleDict.ContainsKey(name) )
			getInstance().styleDict.Add(name, new GUIStyle(name));

		return getInstance().styleDict[name];
	}
	
	public static Texture2D GetIcon(string name)
	{
		if( !getInstance().iconDict.ContainsKey(name) )
			return null;
		else
			return getInstance().iconDict[name];
	}
}
