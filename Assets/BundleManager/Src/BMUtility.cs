using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class BMUtility
{
	public static void Swap<T>(ref T a, ref T b)
	{
		T temp = a;
		a = b;
		b = temp;
	}

	public static string InterpretPath(string origPath, BuildPlatform platform)
	{
		var matches = Regex.Matches(origPath, @"\$\((\w+)\)");
		foreach(Match match in matches)
		{
			string var = match.Groups[1].Value;
			origPath = origPath.Replace(@"$(" + var + ")", EnvVarToString(var, platform));
		}
		
		return origPath;
	}

	public static int[] long2doubleInt(long a) 
	{
		int a1 = (int)(a & uint.MaxValue);
		int a2 = (int)(a >> 32);
		return new int[] { a1, a2 };
	}
	
	public static long doubleInt2long(int a1, int a2)
	{
		long b = a2;
		b = b << 32;
		b = b | (uint)a1;
		return b;
	}
	
	private static string EnvVarToString(string varString, BuildPlatform platform)
	{
		switch(varString)
		{
		case "DataPath":
			return Application.dataPath;
		case "PersistentDataPath":
			return Application.persistentDataPath;
		case "StreamingAssetsPath":
			return Application.streamingAssetsPath;
		case "Platform":
			return platform.ToString();
		default:
			Debug.LogError("Cannot solve enviroment var " + varString);
			return "";
		}
	}
}
