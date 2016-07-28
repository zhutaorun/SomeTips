using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/**
 * All informations of a bundle
 * Use the BundleManager APIs to change the bundle content, don't change the members of this class directly.
 */
public class BundleData
{
    /**
     * Name of the bundle. The name should be uniqle in all bundles
     */
    public string name = "";

    /**
     * List of paths included. The path can be directories.
     */


    public List<string> includeGUIDs = new List<string>();



    public BundleType bundleType;

    /**
     * Default download priority of this bundle.
     */
    public int priority = 0;

    /**
     * Parent name of this bundle.
     */
    public string parent = "";

    /**
     * Childrens' name of this bundle
     */

    public List<string> GetChildren()
    {
        return GetExtraData().children;
    }

    public BundleExtraData GetExtraData()
    {
        var dic = BMDataAccessor.BundleExtraDatas;
        if (!dic.ContainsKey(name))
        {
            dic[name] = new BundleExtraData();
        }
        return dic[name];
    }
}


public class BundleExtraData
{
    public string name;
    public bool needBuild;
    public List<string> includePaths = new List<string>();
    public List<string> includeAssetPaths = new List<string>();
    public List<string> includeAssetGUIDs = new List<string>();
    public List<string> dependPaths = new List<string>();
    public List<string> dependGUIDs = new List<string>();
    public List<string> children = new List<string>();
}

public class BundleBuildState
{
    public string bundleName = "";
    public string requestString = "";
    public int version = -1;
    public uint crc = 0;
    public long size = -1;
    public bool changed = true;
    public string assetListMd5 = null;
    public Dictionary<string, AssetState> assetStates = new Dictionary<string, AssetState>();
}

public class AssetState
{
    public string guid;
    public long lastModifyTime = 0;
    public long fileLength = 0;
    public long metaLength = 0;
    public string fileMd5 = null;
    public string metaMd5 = null;
}