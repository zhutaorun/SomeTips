using System.CodeDom;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;

internal class BMDataAccessor
{
	static public List<BundleData> Bundles
	{
		get
		{
			if(m_Bundles == null)
				m_Bundles = loadObjectFromJsonFile< List<BundleData> >(BundleDataPath);
			
			if(m_Bundles == null)
				m_Bundles = new List<BundleData>();
			
			return m_Bundles;
		}
	}
	
	static public List<BundleBuildState> BuildStates
	{
		get
		{
			if(m_BuildStates == null)
				m_BuildStates = loadObjectFromJsonFile< List<BundleBuildState> >(BundleBuildStatePath);
			
			if(m_BuildStates == null)
				m_BuildStates = new List<BundleBuildState>();
			
			return m_BuildStates;
		}
	}

    public static int BuildVersion
    {
        get
        {
            if (m_BuildVersion < 0)
            {
                if (File.Exists(BundleBuildVersionPath))
                {
                    var lines = File.ReadAllLines(BundleBuildVersionPath);
                    if (lines.Length > 0) int.TryParse(lines[0], out m_BuildVersion);
                }
                if (m_BuildVersion < 0)
                {
                    m_BuildVersion = 0;
                }
            }
            return m_BuildVersion;
        }
        set
        {
            m_BuildVersion = value;
        }
    }

    public static Dictionary<string, AssetState> AssetStates
    {
        get
        {
            if(m_AssetStates==null)
                m_AssetStates = new Dictionary<string, AssetState>();
            return m_AssetStates;
        }
    }

    public static Dictionary<string, BundleExtraData> BundleExtraDatas
    {
        get
        {
            if(m_BundleExtraDatas==null)
                m_BundleExtraDatas = new Dictionary<string, BundleExtraData>();
            return m_BundleExtraDatas;
        }
    }

    static public BMConfiger BMConfiger
	{
		get
		{
			if(m_BMConfier == null)
				m_BMConfier = loadObjectFromJsonFile<BMConfiger>(BMConfigerPath);
			
			if(m_BMConfier == null)
				m_BMConfier = new BMConfiger();
			
			return m_BMConfier;
		}
	}
	
	static public BMUrls Urls
	{
		get
		{
			if(m_Urls == null)
				m_Urls = loadObjectFromJsonFile<BMUrls>(UrlDataPath);
			
			if(m_Urls == null)
				m_Urls = new BMUrls();
			
			return m_Urls;
		}
	}

    public static bool DependencyUpdated = false;
    public static bool ShouldSaveBundleData = false;
    public static bool ShouldSaveBundleStates = false;

	static public void Refresh()
	{
		m_Bundles = null;
		m_BuildStates = null;
		m_BMConfier = null;
		m_Urls = null;
	}
	
	static public void SaveBMConfiger()
	{
		saveObjectToJsonFile(BMConfiger, BMConfigerPath);
	}
	
	static public void SaveBundleData()
	{
		foreach(BundleData bundle in Bundles)
		{
			bundle.includeGUIDs.Sort(guidComp);
		}
		saveObjectToJsonFile(Bundles, BundleDataPath);
        Debug.LogError("BuildleData Saved.");
	}
	
	static public void SaveBundleBuildeStates()
	{
	    m_BuildStates = BundleManager.BuildStatesToList();
		saveObjectToJsonFile(BuildStates, BundleBuildStatePath);
        Debug.Log("BuildStates Saved.");
	}

    public static void SaveBundleBuildVersion()
    {
        if (m_BuildVersion > 0)
        {
            File.WriteAllText(BundleBuildVersionPath,m_BuildVersion.ToString());
        }
    }

    public static void SaveBundleVersionInfo()
    {
        var bundleVersionInfos = BMDataAccessor.Bundles.Select(data =>
        {
            var state = BundleManager.GetBuildStateOfBundle(data.name);
            return new BundleVersionInfo()
            {
                name = data.name,
                parent = data.parent,
                requestString = state.requestString,
                crc = state.crc,
                size = state.size,
                priority = data.priority,
            };
        }).ToList();
        saveObjectToJsonFile(bundleVersionInfos,BundleVersionInfoPath);
    }

    static public void SaveUrls()
	{
		saveObjectToJsonFile(Urls, UrlDataPath);
	}
	
		
	static private T loadObjectFromJsonFile<T>(string path)
	{
		TextReader reader = new StreamReader(path);
		if(reader == null)
		{
			Debug.LogError("Cannot find " + path);
			reader.Close();
			return default(T);
		}
		
		T data = JsonMapper.ToObject<T>(reader.ReadToEnd());
		if(data == null)
		{
			Debug.LogError("Cannot read data from " + path);
		}
		
		reader.Close();
		return data;
	}
	
	static private void saveObjectToJsonFile<T>(T data, string path)
	{
		TextWriter tw = new StreamWriter(path);
		if(tw == null)
		{
			Debug.LogError("Cannot write to " + path);
			return;
		}
		
		string jsonStr = JsonFormatter.PrettyPrint( JsonMapper.ToJson(data) );
		
		tw.Write(jsonStr);
		tw.Flush();
		tw.Close();

		BMDataWatcher.MarkChangeDate(path);
	}

	static private int guidComp(string guid1, string guid2)
	{
		string fileName1 = Path.GetFileNameWithoutExtension( AssetDatabase.GUIDToAssetPath(guid1) );
		string fileName2 = Path.GetFileNameWithoutExtension( AssetDatabase.GUIDToAssetPath(guid2) );
		int ret = fileName1.CompareTo(fileName2);

		if(ret == 0)
			return guid1.CompareTo(guid2);
		else
			return ret;
	}
	
	static private List<BundleData> m_Bundles = null;
	static private List<BundleBuildState> m_BuildStates = null;
    private static int m_BuildVersion = -1;
    private static Dictionary<string, AssetState> m_AssetStates = null;
    private static Dictionary<string, BundleExtraData> m_BundleExtraDatas = null;
	static private BMConfiger m_BMConfier = null;
	static private BMUrls m_Urls = null;
		
	public const string BundleDataPath = "Assets/BundleManager/Editor/BundleData.txt";
    public const string BMConfigerPath = "Assets/BundleManager/Editor/BMConfiger.txt";
    public const string UrlDataPath = "Assets/BundleManager/Resources/Urls.txt";
    public const string BundleVersionInfoPath = "Assets/BundleManager/Editor/BundleVersionInfo.txt";

    public static string BundleBuildStatePath { get { return string.Format(_BundleBuildStatePath_Format,BuildConfiger.AutoBundleBuildtarget);}}
    public const string _BundleBuildStatePath_Format = "Assets/BundleManager/Editor/BuildStates_{0}.txt";

    public static string BundleBuildVersionPath {get { return string.Format(BundleBuildVersionPath_Format, BuildConfiger.AutoBundleBuildtarget); }}
    public const string BundleBuildVersionPath_Format = "Assets/BundleManager/Editor/BMDateVersion_{0}.txt";
}
