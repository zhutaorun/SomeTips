using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;

public class MyAssetHandler : MonoBehaviour {

	[OnOpenAssetAttribute(1)]
	public static bool step1(int instanceID,int line)
	{
		return false;
	}

	[OnOpenAssetAttribute(2)]
	public static bool step2(int instanceID,int line)
	{
		string path = AssetDatabase.GetAssetPath (EditorUtility.InstanceIDToObject (instanceID));
		string name = Application.dataPath + "/" + path.Replace ("Assets/","");

		if (name.EndsWith (".xx")) //以什么结尾的文件
		{
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
			startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			startInfo.FileName= "D:/Program Files/Sublime Text 3/sublime_text.exe";//使用外部可识别的工具
			startInfo.Arguments= name;
			process.StartInfo= startInfo;
			process.Start();
			return true;
		}

		return false;
	}
}
