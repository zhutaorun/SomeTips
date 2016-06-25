using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable 0618

/**
 * Build helper contains APIs for bundle building.
 * You can use this to custom your own build progress.
 */ 
public class BuildHelper 
{
	/**
	 * Copy the configeration files to target directory.
	 */ 
	public static void ExportBMDatasToOutput()
	{
		string exportPath = BuildConfiger.InterpretedOutputPath;
		if(!Directory.Exists(exportPath))
			Directory.CreateDirectory(exportPath);

		uint crc = 0;
		if(!BuildAssetBundle(new string[]{BMDataAccessor.BundleDataPath, BMDataAccessor.BundleBuildStatePath, BMDataAccessor.BMConfigerPath}, Path.Combine( exportPath, "BM.data" ), out crc))
			Debug.LogError("Failed to build bundle of config files.");

		BuildHelper.ExportBundleDataFileToOutput();
		BuildHelper.ExportBundleBuildDataFileToOutput();
		BuildHelper.ExportBMConfigerFileToOutput();
	}

	/**
	 * Copy the bundle datas to target directory.
	 */ 
	public static void ExportBundleDataFileToOutput()
	{
		string exportPath = BuildConfiger.InterpretedOutputPath;
		if(!Directory.Exists(exportPath))
			Directory.CreateDirectory(exportPath);

		File.Copy( 	BMDataAccessor.BundleDataPath, 
		          	Path.Combine( exportPath, Path.GetFileName(BMDataAccessor.BundleDataPath) ), 
					true );
	}
	
	/**
	 * Copy the bundle build states to target directory.
	 */ 
	public static void ExportBundleBuildDataFileToOutput()
	{
		string exportPath = BuildConfiger.InterpretedOutputPath;
		if(!Directory.Exists(exportPath))
			Directory.CreateDirectory(exportPath);

		File.Copy( 	BMDataAccessor.BundleBuildStatePath, 
		          	Path.Combine( exportPath, Path.GetFileName(BMDataAccessor.BundleBuildStatePath) ), 
					true );
	}
	
	/**
	 * Copy the bundle manager configeration file to target directory.
	 */ 
	public static void ExportBMConfigerFileToOutput()
	{
		string exportPath = BuildConfiger.InterpretedOutputPath;
		if(!Directory.Exists(exportPath))
			Directory.CreateDirectory(exportPath);

		File.Copy( 	BMDataAccessor.BMConfigerPath, 
		          	Path.Combine( exportPath, Path.GetFileName(BMDataAccessor.BMConfigerPath) ), 
					true );
	}
	
	/**
	 * Detect if the bundle need update.
	 */ 
	public static bool IsBundleNeedBunild(BundleData bundle)
	{	
		string outputPath = GenerateOutputPathForBundle(bundle.name);
		if(!File.Exists(outputPath))
			return true;
		
		BundleBuildState bundleBuildState = BundleManager.GetBuildStateOfBundle(bundle.name);
		DateTime lastBuildTime = File.GetLastWriteTime(outputPath);
		DateTime bundleChangeTime = bundleBuildState.changeTime == -1 ? DateTime.MaxValue : DateTime.FromBinary(bundleBuildState.changeTime);
		if( System.DateTime.Compare(lastBuildTime, bundleChangeTime) < 0 )
		{
			return true;
		}
		
		string[] assetPaths = GetAssetsFromPaths(BundleManager.GUIDsToPaths(bundle.includeGUIDs.ToArray()), bundle.sceneBundle);
		string[] dependencies = AssetDatabase.GetDependencies(assetPaths);
		if( !EqualStrArray(dependencies, bundleBuildState.lastBuildDependencies) )
			return true; // Build depenedencies list changed.
		
		foreach(string file in dependencies)
		{
			if(DateTime.Compare(lastBuildTime, File.GetLastWriteTime(file)) < 0)
				return true;
		}
		
		if(bundle.parent != "")
		{
			BundleData parentBundle = BundleManager.GetBundleData(bundle.parent);
			if(parentBundle != null)
			{
				if(IsBundleNeedBunild(parentBundle))
					return true;
			}
			else
			{
				Debug.LogError("Cannot find bundle");
			}
		}
		
		return false;
	}
	
	/**
	 * Build all bundles.
	 */
	public static void BuildAll()
	{	
		BuildBundles(BundleManager.bundles.Select(bundle=>bundle.name).ToArray());
	}
	
	/**
	 * Force rebuild all bundles.
	 */
	public static void RebuildAll()
	{
		foreach(BundleBuildState bundle in BundleManager.buildStates)
			bundle.lastBuildDependencies = null;
		
		BuildAll();
	}
	
	/**
	 * Build bundles.
	 */
	public static void BuildBundles(string[] bundles)
	{
		Dictionary<string, List<string>> buildingRoutes = new Dictionary<string, List<string>>();
		foreach(string bundle in bundles)
			AddBundleToBuildList(bundle, ref buildingRoutes);
		
		foreach(var buildRoute in buildingRoutes)
		{
			BundleData bundle = BundleManager.GetBundleData( buildRoute.Key );
			if(bundle != null)
				BuildBundleTree(bundle, buildRoute.Value);
		}
	}
	
	internal static void AddBundleToBuildList(string bundleName, ref Dictionary<string, List<string>> buildingRoutes)	
	{
		BundleData bundle = BundleManager.GetBundleData(bundleName);
		if(bundle == null)
		{
			Debug.LogError("Cannot find bundle " + bundleName);
			return;
		}
			
		if( BuildHelper.IsBundleNeedBunild(bundle) )
		{
			string rootName = BundleManager.GetRootOf(bundle.name);
			if(buildingRoutes.ContainsKey(rootName))
			{
				if(!buildingRoutes[rootName].Contains(bundle.name))
					buildingRoutes[rootName].Add(bundle.name);
				else
					Debug.LogError("Bundle name duplicated: " + bundle.name);
			}
			else
			{
				List<string> buildingList = new List<string>();
				buildingList.Add(bundle.name);
				buildingRoutes.Add(rootName, buildingList);
			}
		}
		else
		{
			Debug.Log("Bundle " + bundle.name + " skiped.");
		}
	}
	
	internal static bool BuildBundleTree(BundleData bundle, List<string> requiredBuildList)
	{
		BuildPipeline.PushAssetDependencies();
		
		bool succ = BuildSingleBundle(bundle);
		if(!succ)
		{
			Debug.LogError(bundle.name + " build failed.");
			BuildPipeline.PopAssetDependencies();
			return false;
		}
		else
		{
			Debug.Log(bundle.name + " build succeed.");
		}
		
		foreach(string childName in bundle.children)
		{
			BundleData child = BundleManager.GetBundleData(childName);
			if(child == null)
			{
				Debug.LogError("Cannnot find bundle [" + childName + "]. Sth wrong with the bundle config data.");
				BuildPipeline.PopAssetDependencies();
				return false;
			}
			
			bool isDependingBundle = false;
			foreach(string requiredBundle in requiredBuildList)
			{
				if(BundleManager.IsBundleDependOn(requiredBundle, childName))
				{
					isDependingBundle = true;
					break;
				}
			}
			
			if(isDependingBundle || !BuildConfiger.DeterministicBundle)
			{
				succ = BuildBundleTree(child, requiredBuildList);
				if(!succ)
				{
					BuildPipeline.PopAssetDependencies();
					return false;
				}
			}
		}
		
		BuildPipeline.PopAssetDependencies();
		return true;
	}
	
	// Get scene or plain assets from include paths
	internal static string[] GetAssetsFromPaths(string[] includeList, bool onlySceneFiles)
	{
		// Get all the includes file's paths
		List<string> files = new List<string>();
		foreach(string includPath in includeList)
		{
			files.AddRange(GetAssetsFromPath(includPath, onlySceneFiles));
		}
		
		return files.ToArray();
	}

	// Get scene or plain assets from path
	internal static string[] GetAssetsFromPath(string path, bool onlySceneFiles)
	{
		if(!File.Exists(path) && !Directory.Exists(path))
			return new string[]{};
		
		bool isDir = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
		bool isSceneFile = Path.GetExtension(path) == ".unity";
		if(!isDir)
		{
			if(onlySceneFiles && !isSceneFile)
				// If onlySceneFiles is true, we can't add file without "unity" extension
				return new string[]{};
			
			return new string[]{path};
		}
		else
		{
			string[] subFiles = null;
			if(onlySceneFiles)
				subFiles = FindSceneFileInDir(path, SearchOption.AllDirectories);
			else
				subFiles = FindAssetsInDir(path, SearchOption.AllDirectories);
			
			return subFiles;
		}
	}
	
	private static string[] FindSceneFileInDir(string dir, SearchOption option)
	{
		return Directory.GetFiles(dir, "*.unity", option);
	}
	
	private static string[] FindAssetsInDir(string dir, SearchOption option)
	{
		List<string> files = new List<string>( Directory.GetFiles(dir, "*.*", option) );
		files.RemoveAll(x => x.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase) || x.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase));
		return files.ToArray();
	}
	
	private static bool BuildAssetBundle(string[] assetsList, string outputPath, out uint crc)
	{
		crc = 0;

		// Load all of assets in this bundle
		List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
		foreach(string assetPath in assetsList)
		{
			UnityEngine.Object[] assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			if(assetsAtPath != null || assetsAtPath.Length != 0)
				assets.AddRange(assetsAtPath);
			else
				Debug.LogError("Cannnot load [" + assetPath + "] as asset object");
		}

		// Build bundle
#if UNITY_4_2 || UNITY_4_1 || UNITY_4_0
		bool succeed = BuildPipeline.BuildAssetBundle(	null, 
		                                              	assets.ToArray(), 
														outputPath, 
		                                              	CurrentBuildAssetOpts,
														BuildConfiger.UnityBuildTarget);
#else
		bool succeed = BuildPipeline.BuildAssetBundle(	null, 
		                                              	assets.ToArray(), 
		                                              	outputPath,
		                                              	out crc,
		                                              	CurrentBuildAssetOpts,
		                                              	BuildConfiger.UnityBuildTarget);
#endif
		return succeed;
	}

	private static BuildAssetBundleOptions CurrentBuildAssetOpts
	{
		get
		{
			return	(BMDataAccessor.BMConfiger.compress ? 0 : BuildAssetBundleOptions.UncompressedAssetBundle) |
					(BMDataAccessor.BMConfiger.deterministicBundle ? 0 : BuildAssetBundleOptions.DeterministicAssetBundle) |
					BuildAssetBundleOptions.CollectDependencies;
		}
	}
	
	private static bool BuildSceneBundle(string[] sceneList, string outputPath, out uint crc)
	{
		crc = 0;

		if(sceneList.Length == 0)
		{
			Debug.LogError("No scenes were provided for the scene bundle");
			return false;
		}

#if UNITY_4_2 || UNITY_4_1 || UNITY_4_0
		string error = BuildPipeline.BuildPlayer (sceneList, outputPath, BuildConfiger.UnityBuildTarget, BuildOptions.BuildAdditionalStreamedScenes | CurrentBuildSceneOpts);
#else
		string error = BuildPipeline.BuildStreamedSceneAssetBundle(sceneList, outputPath, BuildConfiger.UnityBuildTarget, out crc, CurrentBuildSceneOpts);
#endif
		return error == "";
	}

	private static BuildOptions CurrentBuildSceneOpts
	{
		get
		{
			return	BMDataAccessor.BMConfiger.compress ? 0 : BuildOptions.UncompressedAssetBundle;
		}
	}
	
	private static bool BuildSingleBundle(BundleData bundle)
	{
		// Prepare bundle output dictionary
		string outputPath = GenerateOutputPathForBundle(bundle.name);
		string bundleStoreDir = Path.GetDirectoryName(outputPath);
		if(!Directory.Exists(bundleStoreDir))
			Directory.CreateDirectory(bundleStoreDir);
		
		// Start build
		string[] assetPaths = GetAssetsFromPaths(BundleManager.GUIDsToPaths(bundle.includeGUIDs.ToArray()), bundle.sceneBundle);
		bool succeed = false;
		uint crc = 0;
		if(bundle.sceneBundle)
			succeed = BuildSceneBundle(assetPaths, outputPath, out crc);
		else
			succeed = BuildAssetBundle(assetPaths, outputPath, out crc);
		
		// Remember the assets for next time build test
		BundleBuildState buildState = BundleManager.GetBuildStateOfBundle(bundle.name);
		if(succeed)
		{
			buildState.lastBuildDependencies = AssetDatabase.GetDependencies(assetPaths);
			buildState.version++;
			if(buildState.version == int.MaxValue)
				buildState.version = 0;

			buildState.crc = crc;
			FileInfo bundleFileInfo = new FileInfo(outputPath);
			buildState.size = bundleFileInfo.Length;
		}
		else
		{
			buildState.lastBuildDependencies = null;
		}
		
		BMDataAccessor.SaveBundleBuildeStates();
		return succeed;
	}
	
	private static bool EqualStrArray(string[] strList1, string[] strList2)
	{
		if(strList1 == null || strList2 == null)
			return false;
		
		if(strList1.Length != strList2.Length)
			return false;
		
		for(int i = 0; i < strList1.Length; ++i)
		{
			if(strList1[i] != strList2[i])
				return false;
		}
		
		return true;
	}
	
	private static string GenerateOutputPathForBundle(string bundleName)
	{
		return Path.Combine(BuildConfiger.InterpretedOutputPath, bundleName + "." + BuildConfiger.BundleSuffix);
	}
}
