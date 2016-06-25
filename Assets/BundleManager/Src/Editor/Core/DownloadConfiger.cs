using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;

/**
 * Settings for download.
 */
public class DownloadConfiger
{
	/**
	 * Use unity cache system.
	 */
	static public bool useCache
	{
		get{return BMDataAccessor.BMConfiger.useCache;}
		set{BMDataAccessor.BMConfiger.useCache = value;}
	}

	static public bool offlineCache
	{
		get{return BMDataAccessor.Urls.offlineCache;}
		set{BMDataAccessor.Urls.offlineCache = value;}
	}

	/**
	 * Use crc when download bundles
	 */
	static public bool useCRC
	{
		get{return BMDataAccessor.BMConfiger.useCRC;}
		set{BMDataAccessor.BMConfiger.useCRC = value;}
	}
	
	/**
	 * How many www requests will be started at the same time.
	 */
	static public int downloadThreadsCount
	{
		get{return BMDataAccessor.BMConfiger.downloadThreadsCount;}
		set{BMDataAccessor.BMConfiger.downloadThreadsCount = value;}
	}
	
	/**
	 * WWW request retry time when error ocurred.
	 */
	static public int retryTime
	{
		get{return BMDataAccessor.BMConfiger.downloadRetryTime;}
		set{BMDataAccessor.BMConfiger.downloadRetryTime = value;}
	}
	
	/**
	 * The root url of assets.
	 */
	static public string downloadUrl
	{
		get
		{
			return BMDataAccessor.Urls.downloadUrls[BMDataAccessor.Urls.bundleTarget.ToString()];
		}
		set
		{
			var urls = BMDataAccessor.Urls.downloadUrls;
			string platformStr = BMDataAccessor.Urls.bundleTarget.ToString();
			urls[platformStr] = value;
		}
	}
	
	/**
	 * Test option to force application to download from output path
	 */
	static public bool downloadFromOutput
	{
		get{return BMDataAccessor.Urls.downloadFromOutput;}
		set{BMDataAccessor.Urls.downloadFromOutput = value;}
	}
}
