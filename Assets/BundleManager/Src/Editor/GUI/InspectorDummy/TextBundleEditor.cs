using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(TextBundleInspectorObj))]
public class TextBundleEditor : Editor 
{

#if!(UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
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
