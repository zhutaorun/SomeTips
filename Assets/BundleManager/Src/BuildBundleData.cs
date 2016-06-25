using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;

/**
 * All informations of a bundle
 * Use the BundleManager APIs to change the bundle content, don't change the members of this class directly.
 */ 
public class BundleData
{
	/**
	 * Name of the bundle. The name should be uniqle in all bundles
	 */ 
	public string		name = "";
	
	/**
	 * List of paths included. The path can be directories.
	 */ 
	public List<string>	includs = new List<string>();

	/**
	 * List of paths currently depended. One asset can be depended but not included in the Includes list
	 */ 
	public List<string> dependAssets = new List<string>();

	public List<string> includeGUIDs = new List<string>();

	public List<string> dependGUIDs = new List<string>();
	
	/**
	 * Is this bundle a scene bundle?
	 */ 
	public bool			sceneBundle = false;

	/**
	 * Default download priority of this bundle.
	 */ 
	public int			priority = 0;
	
	/**
	 * Parent name of this bundle.
	 */ 
	public string		parent = "";
	
	/**
	 * Childrens' name of this bundle
	 */ 
	public List<string>	children = new List<string>();
}

public class BundleBuildState
{
	public string		bundleName = "";
	public int			version = -1;
	public uint			crc = 0;
	public long			size = -1;
	public long			changeTime = -1;
	public string[]		lastBuildDependencies = null;
}

public class BMConfiger
{
	public bool				compress = true;
	public bool				deterministicBundle = false;
	public string			bundleSuffix = "assetBundle";
	public string			buildOutputPath = "";
	
	public bool				useCache = true;
	public bool				useCRC = false;
	public int				downloadThreadsCount = 1;
	public int				downloadRetryTime = 2;

	public int				bmVersion = 0;
}

public class BMUrls
{
	public Dictionary<string, string> downloadUrls;
	public Dictionary<string, string> outputs;
	public BuildPlatform bundleTarget = BuildPlatform.Standalones;
	public bool useEditorTarget = false;
	public bool downloadFromOutput = false;
	public bool offlineCache = false;
	
	public BMUrls()
	{
		downloadUrls = new Dictionary<string, string>()
		{
			{"WebPlayer", ""},
			{"Standalones", ""},
			{"IOS", ""},
			{"Android", ""},
			{"WP8", ""}
		};
		outputs = new Dictionary<string, string>()
		{
			{"WebPlayer", ""},
			{"Standalones", ""},
			{"IOS", ""},
			{"Android", ""},
			{"WP8", ""}
		};
	}
	
	public string GetInterpretedDownloadUrl(BuildPlatform platform)
	{
		return BMUtility.InterpretPath(downloadUrls[platform.ToString()], platform);
	}
	
	public string GetInterpretedOutputPath(BuildPlatform platform)
	{
		return BMUtility.InterpretPath(outputs[platform.ToString()], platform);
	}

	public static string SerializeToString(BMUrls urls)
	{
		return JsonMapper.ToJson(urls);
	}
}

public enum BuildPlatform
{
	WebPlayer,
	Standalones,
	IOS,
	Android,
	WP8,
}
