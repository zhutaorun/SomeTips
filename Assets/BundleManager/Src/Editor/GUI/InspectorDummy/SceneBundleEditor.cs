using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SceneBundleInpectorObj))]
public class SceneBundleEditor : Editor 
{
#if !(UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
	public override bool UseDefaultMargins()
	{
		return false;
	}
#endif

	public override void OnInspectorGUI()
	{
		BundleEditorDrawer.DrawInspector();
	}
	
	void OnEnable()
	{
		BundleEditorDrawer.CurrentBundleEditor = this;
	}
}
