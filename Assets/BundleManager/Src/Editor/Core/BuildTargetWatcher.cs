using UnityEngine;
using UnityEditor;


[InitializeOnLoad]
public class BuildTargetWatcher
{
	static BuildTargetWatcher()
	{
		OnBuildTargetChanged();

		EditorUserBuildSettings.activeBuildTargetChanged += OnBuildTargetChanged;
	}

	static void OnBuildTargetChanged()
	{
		if(BuildConfiger.UseEditorTarget)
		{
			BuildConfiger.UnityBuildTarget = EditorUserBuildSettings.activeBuildTarget;
			BMDataAccessor.SaveUrls();
		}
	}
}
