using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class BMDataWatcher : AssetPostprocessor 
{
	public static bool Active = true;

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		foreach(string asset in importedAssets)
		{
			if(asset == BMDataAccessor.BundleDataPath || asset == BMDataAccessor.BundleBuildStatePath)
			{
				if(isDateChangedManually(asset))
					BundleManager.RefreshAll();
			}
			else if(asset == BMDataAccessor.BMConfigerPath || asset == BMDataAccessor.UrlDataPath)
			{
				if(isDateChangedManually(asset))
					BMDataAccessor.Refresh();
			}
		}
	}

	public static void MarkChangeDate(string path)
	{
		int[] date = BMUtility.long2doubleInt(File.GetLastWriteTime(path).ToBinary());
		PlayerPrefs.SetInt("BMChangeDate0", date[0]);
		PlayerPrefs.SetInt("BMChangeDate1", date[1]);
	}

	static bool isDateChangedManually(string asset)
	{
		if(!PlayerPrefs.HasKey("BMChangeDate0") || !PlayerPrefs.HasKey("BMChangeDate1"))
			return false;

		long assetChangeTime = File.GetLastWriteTime(asset).ToBinary();
		long markedChangeTime = BMUtility.doubleInt2long(PlayerPrefs.GetInt("BMChangeDate0"), PlayerPrefs.GetInt("BMChangeDate1"));
		return assetChangeTime != markedChangeTime;
	}
}
