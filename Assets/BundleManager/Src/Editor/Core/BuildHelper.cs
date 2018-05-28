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
    public static void RefreshBundleChanged(BundleData bundle)
    {
        var state = BundleManager.GetBuildStateOfBundle(bundle.name);
        if (!state.changed)
        {
            if (IsBundleChanged(bundle))
            {
                state.changed = true;
                BundleManager.MarkParentsNeedBuild(bundle);
                BMDataAccessor.ShouldSaveBundleStates = true;
            }
        }
    }

    /**
	 * Copy the configeration files to target directory.
	 */ 
    //public static void ExportBMDatasToOutput()
    //{
    //    string exportPath = BuildConfiger.InterpretedOutputPath;
    //    if(!Directory.Exists(exportPath))
    //        Directory.CreateDirectory(exportPath);

    //    uint crc = 0;
    //    if(!BuildAssetBundle(new string[]{BMDataAccessor.BundleDataPath, BMDataAccessor.BundleBuildStatePath, BMDataAccessor.BMConfigerPath}, Path.Combine( exportPath, "BM.data" ), out crc))
    //        Debug.LogError("Failed to build bundle of config files.");

    //    BuildHelper.ExportBundleDataFileToOutput();
    //    BuildHelper.ExportBundleBuildDataFileToOutput();
    //    BuildHelper.ExportBMConfigerFileToOutput();
    //}

	/**
	 * Copy the bundle datas to target directory.
	 */ 
	
    public static void BuildSelections(string[] selections)
    {
        BMDataAccessor.AssetStates.Clear();
        foreach (string bundleName in selections)
        {
            var bundle = BundleManager.GetBundleData(bundleName);
            BundleManager.RefreshBundleDependencies(bundle);
            BuildHelper.RefreshBundleChanged(bundle);
        }
        BuildBundles(selections);
    }

    public static void RebuildSelection(string[] selections)
    {
        foreach (string bundleName in selections)
        {
            var bundle = BundleManager.GetBundleData(bundleName);
            BundleManager.RefreshBundleDependencies(bundle);
            BundleManager.GetBuildStateOfBundle(bundleName).changed = true;
        }
        BuildBundles(selections);
    }

    /**
	 * Build all bundles.
	 */
	public static void BuildAll()
	{
	    BMDataAccessor.AssetStates.Clear();
	    foreach (var bundle in BundleManager.bundles)
	    {
	        BundleManager.RefreshBundleDependencies(bundle);
	        BuildHelper.RefreshBundleChanged(bundle);
	    }
	}
	
	/**
	 * Force rebuild all bundles.
	 */
	public static void RebuildAll()
	{
	    foreach (var  bundle in BMDataAccessor.Bundles)
	    {
            BundleManager.RefreshBundleDependencies(bundle);
	        BundleManager.GetBuildStateOfBundle(bundle.name).changed = true;
	    }
	    BMDataAccessor.DependencyUpdated = true;
        DirectBuildAll();
	}


    public static void DirectBuildAll()
    {
        BuildBundles(BundleManager.bundles.Select(bundle => bundle.name).ToArray());
    }

    /**
	 * Build bundles.
	 */
	public static void BuildBundles(string[] bundles)
	{
		Dictionary<string, List<string>> buildingRoutes = new Dictionary<string, List<string>>();
		foreach(string bundle in bundles)
			AddBundleToBuildList(bundle, ref buildingRoutes);
	    m_BuiltCount = 0;
	    var startTime = Time.realtimeSinceStartup;
		foreach(var buildRoute in buildingRoutes)
		{
			BundleData bundle = BundleManager.GetBundleData( buildRoute.Key );
			if(bundle != null)
				BuildBundleTree(bundle, buildRoute.Value);
		}

	    BMDataAccessor.BuildVersion++;
        BMDataAccessor.SaveBundleBuildVersion();
        BMDataAccessor.SaveBundleVersionInfo();

	    string exportpath = BuildConfiger.InterpretedOutputPath;
	    if (!Directory.Exists(exportpath))
	        Directory.CreateDirectory(exportpath);

        BundleManager.UpdateAllBundlesNeedBuild();

	    uint crc = 0;
	    if (!BuildAssetBundle(new string[] {BMDataAccessor.BundleBuildVersionPath, BMDataAccessor.BMConfigerPath},Path.Combine(exportpath, "BM.data"), out crc))
            //存入数据，三个path指向三个txt文件，将三个txt文件导入BM.data
            Debug.LogError("Failed to build bundle of config files.");
        Debug.Log("Build bundles:"+m_BuiltCount+"| AssetBundleVersion:" + BMDataAccessor.BuildVersion+"| Time Consumed"+(Time.realtimeSinceStartup- startTime));

        File.WriteAllText(Path.Combine(exportpath,"BMDataVersion.txt"),BMDataAccessor.BuildVersion.ToString());

	}


    public static void CopyFiles(string srcFolder, string destFolder)
    {
        var title = "Copy Asset Bundles";
        EditorUtility.DisplayProgressBar(title,"Starting...",0);
        CreateFoldersRecursivly(srcFolder, destFolder);
        var files = Directory.GetFiles(srcFolder, "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            var filePath = files[i];
            var destPath = filePath.Replace(srcFolder, destFolder);

            var progress = (i + 1)/(float) filePath.Length;
            EditorUtility.DisplayProgressBar(title,"Copying "+ filePath.Replace(srcFolder,"").Substring(1),progress);
            File.Copy(filePath,destPath);
        }
        EditorUtility.ClearProgressBar();
    }


    private static void CreateFoldersRecursivly(string srcFolder, string destFolder)
    {
        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);
        foreach (var subFolder in Directory.GetDirectories(srcFolder))
        {
            var destPath = subFolder.Replace(srcFolder, destFolder);
            CreateFoldersRecursivly(subFolder,destPath);
        }
    }

    internal static void AddBundleToBuildList(string bundleName, ref Dictionary<string, List<string>> buildingRoutes)	
	{
		BundleData bundle = BundleManager.GetBundleData(bundleName);
	    var state = BundleManager.GetBuildStateOfBundle(bundleName);
		if(bundle == null)
		{
			Debug.LogError("Cannot find bundle " + bundleName);
			return;
		}
			
		//if( BuildHelper.IsBundleNeedBunild(bundle) )
        if(state.changed)
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
	}
	
	internal static bool BuildBundleTree(BundleData bundle, List<string> requiredBuildList)
	{
		BuildPipeline.PushAssetDependencies();
		
		bool succ = BuildSingleBundle(bundle);
		if(!succ)
		{
			Debug.LogError(bundle.name + " build failed.");
            //为了实现能够跳过build错误的bundle继续进行打包而注释
			//BuildPipeline.PopAssetDependencies();
			//return false;
		}
		else
		{
		    var buildState = BundleManager.GetBuildStateOfBundle(bundle.name);
			Debug.Log(bundle.name + " build succeed.");
		}
	    if (succ)
	    {
	        foreach (string childName in bundle.GetChildren())
	        {
	            BundleData child = BundleManager.GetBundleData(childName);
	            if (child == null)
	            {
	                Debug.LogError("Cannnot find bundle [" + childName + "]. Sth wrong with the bundle config data.");
	                BuildPipeline.PopAssetDependencies();
	                return false;
	            }

	            bool isDependingBundle = false;
	            foreach (string requiredBundle in requiredBuildList)
	            {
	                if (BundleManager.IsBundleDependOn(requiredBundle, childName))
	                {
	                    isDependingBundle = true;
	                    break;
	                }
	            }

	            if (isDependingBundle || !BuildConfiger.DeterministicBundle)
	            {
	                succ = BuildBundleTree(child, requiredBuildList);
	                if (!succ)
	                {
	                    BuildPipeline.PopAssetDependencies();
	                    return false;
	                }
	            }
	        }
	    }
	    BuildPipeline.PopAssetDependencies();
		return true;
	}
	
	// Get scene or plain assets from include paths
	internal static string[] GetAssetsFromPaths(string[] includeList, BundleType bundleType)
	{
		// Get all the includes file's paths
		List<string> files = new List<string>();
		foreach(string includPath in includeList)
		{
            files.AddRange(GetAssetsFromPath(includPath, bundleType));
		}
		
		return files.ToArray();
	}

	// Get scene or plain assets from path
    internal static string[] GetAssetsFromPath(string path, BundleType bundleType)
	{
		if(!File.Exists(path) && !Directory.Exists(path))
			return new string[]{};
		
		bool isDir = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
		bool isSceneFile = Path.GetExtension(path) == ".unity";
		if(!isDir)
		{
            if (bundleType==BundleType.Scene && !isSceneFile)
				// If onlySceneFiles is true, we can't add file without "unity" extension
				return new string[]{};
			
			return new string[]{path};
		}
		else
		{
			string[] subFiles = null;
		    switch (bundleType)
		    {
                case BundleType.Normal:
                    subFiles = FindAssetsInDir(path, SearchOption.AllDirectories);
                    break;
                case BundleType.Scene:
                     subFiles = FindSceneFileInDir(path, SearchOption.AllDirectories);
                    break;
                case BundleType.Text:
                     subFiles = FindTextAssetsInDir(path, SearchOption.AllDirectories);
                    break;
                default:
		            throw new System.NotFiniteNumberException();

		    }
			return subFiles;
		}
	}
	
	private static string[] FindSceneFileInDir(string dir, SearchOption option)
	{
		return Directory.GetFiles(dir, "*.unity", option);
	}
    private static string[] FindTextAssetsInDir(string dir, SearchOption option)
    {
        return Directory.GetFiles(dir, "*.bytes", option);
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
	    var extra = bundle.GetExtraData();
        
		// Prepare bundle output dictionary
		string outputPath = GenerateOutputPathForBundle(bundle.name);
		string bundleStoreDir = Path.GetDirectoryName(outputPath);
		if(!Directory.Exists(bundleStoreDir))
			Directory.CreateDirectory(bundleStoreDir);

	    if (extra.includeAssetPaths.Count == 0)
	    {
            BundleManager.RefreshBundleDependencies(bundle);
	    }

	    // Start build
	    string[] assetPaths = extra.includeAssetPaths.ToArray();
		bool succeed = false;
		uint crc = 0;
	    switch (bundle.bundleType)
	    {
	        case BundleType.Normal:
                succeed = BuildSceneBundle(assetPaths, outputPath, out crc);
                break;
            case BundleType.Scene:
	            succeed = BuildSceneBundle(assetPaths, outputPath, out crc);
                break;
            case BundleType.Text:
	            succeed = BuildSceneBundle(assetPaths, outputPath, out crc);
                break;
            default:
                throw new NotImplementedException();
	    }

		// Remember the assets for next time build test
		BundleBuildState buildState = BundleManager.GetBuildStateOfBundle(bundle.name);
		if(succeed)
		{
		    foreach (var guid in extra.includeAssetGUIDs)
		    {
		        buildState.assetStates[guid] = BundleManager.GetAssetState(guid);
		    }
		    foreach (var guid in extra.dependGUIDs)
		    {
		        buildState.assetStates[guid] = BundleManager.GetAssetState(guid);
		    }

		    buildState.assetListMd5 = GetBundleAssetsListMD5(bundle);

			buildState.crc = crc;
		    buildState.changed = true;
		    buildState.requestString = bundle.name + "." + BuildConfiger.BundleSuffix;
			FileInfo bundleFileInfo = new FileInfo(outputPath);
			buildState.size = bundleFileInfo.Length;
		    extra.needBuild = false;
		    m_BuiltCount++;

		    BMDataAccessor.ShouldSaveBundleStates = true;
		}
		return succeed;
	}
	
    //private static bool EqualStrArray(string[] strList1, string[] strList2)
    //{
    //    if(strList1 == null || strList2 == null)
    //        return false;
		
    //    if(strList1.Length != strList2.Length)
    //        return false;
		
    //    for(int i = 0; i < strList1.Length; ++i)
    //    {
    //        if(strList1[i] != strList2[i])
    //            return false;
    //    }
		
    //    return true;
    //}
	
	private static string GenerateOutputPathForBundle(string bundleName)
	{
		return Path.Combine(BuildConfiger.InterpretedOutputPath, bundleName + "." + BuildConfiger.BundleSuffix);
	}

    private static bool IsBundleChanged(BundleData bundle)
    {
        var state = BundleManager.GetBuildStateOfBundle(bundle.name);
        if (state.changed) return true;
        var extra = bundle.GetExtraData();

        if (bundle.bundleType == BundleType.Text)
        {
            //HACK:文本bundles中往往包含大量文件，所以永远重新打包
            return true;
        }

        if (GetBundleAssetsListMD5(bundle) != state.assetListMd5)
        {
            return true;
        }

        //判断所有涉及的文件是否有变化，如有，则需要重新打包
        foreach (var guid in extra.includeAssetGUIDs)
        {
            if (!state.assetStates.ContainsKey(guid))
            {
                return true;
            }
            else if (BundleManager.CheckAssetModified(state.assetStates[guid]))
            {
                return true;
            }
        }
        foreach (var guid in extra.dependGUIDs)
        {
            if (!state.assetStates.ContainsKey(guid))
            {
                return true;
            }
            else if(BundleManager.CheckAssetModified(state.assetStates[guid]))
            {
                return true;
            }
        }
        return false;
    }


    private static string GetBundleAssetsListMD5(BundleData bundle)
    {
        var str = string.Join(string.Empty, bundle.includeGUIDs.ToArray()) +
                  string.Join(string.Empty, bundle.GetExtraData().dependGUIDs.ToArray());
        return ZipFile.md5.getMd5Hash(System.Text.Encoding.ASCII.GetBytes(str));
    }

    private static int m_BuiltCount;
}
