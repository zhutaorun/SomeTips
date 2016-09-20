using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;


public enum BundleType
{
    Normal,
    Scene,
    Text,
}

public class BundleVersionInfo
{
    public string name = "";
    public string requestString = "";
    public string parent = "";
    public uint crc = 0;
    public long size = -1;
    public int priority;
}

public class BundleDownloadInfo
{
    public string name = "";
    public string url = "";
    public int version = 0;
    public bool localBundle = false;
    public bool needDownload = true;
    public BundleVersionInfo versionInfo;
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
    public Dictionary<string, string> copyFolders; 
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
        copyFolders = new Dictionary<string, string>()
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
