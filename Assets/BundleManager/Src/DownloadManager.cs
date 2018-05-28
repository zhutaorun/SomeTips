#define ALWAYS_UPDATE_LOCAL

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Uri = System.Uri;
using LitJson;

/**
 * DownloadManager is a runtime class for asset steaming and WWW management.
 */ 
public class DownloadManager : MonoBehaviour 
{
    //HACK: Ϊ���ñ༭��������ȷ����������������޷����ظ��°������Ҫ��������汾���֣�ǿ�ƿͻ������Cache�����°�

    private const int FORCE_DOWNLOAD_VERSION = 17;

    public const string FORCE_DOWNLOAD_PREFSKEY = "HACK_ForceDownloadVersion";
    public const string BMDATA_VERSION_PREFSKEY = "BMDateVersion";
    public const string BMDATA_NAME = "BM.date";
    public const string DOWNLOAD_INFO_CACHE_NAME = "BMDownloadInfoCache.json";
    public const string CONFIGER_CACHE_NAME = "BMConfigerCache.json";

    /*
     * Get the bundle info of a specified name
     */

    public BundleDownloadInfo GetBundleInfo(string bundleName)
    {
        if (bundleDict.ContainsKey(bundleName)) return bundleDict[bundleName];
        return null;
    }

    /**
	 * Get the error string of WWW request.
	 * @return The error string of WWW. Return null if WWW request succeed or still in processing.
	 */ 
	public string GetError(string bundleName)
	{
		if(!ConfigLoaded)
			return null;

        if (failedRequest.ContainsKey(bundleName))
            return failedRequest[bundleName].www.error;
		else
			return null;
	}
	
	/**
	 * Test if the url is already requested.
	 */
	public bool IsUrlRequested(string bundleName)
	{
		if(!ConfigLoaded)
		{
            return isInBeforeInitList(bundleName);
		}
		else
		{
            bool isRequested = isInWaitingList(bundleName) || processingRequest.ContainsKey(bundleName) || 
                succeedRequest.ContainsKey(bundleName) || failedRequest.ContainsKey(bundleName);
			return isRequested;
		}
	}

    /**
	 * Get WWW instance of the url.
	 * @return Return null if the WWW request haven't succeed.
	 */ 
    //public WWW GetWWW(string url)
    //{
    //    if(!ConfigLoaded)
    //        return null;
		
    //    url = formatUrl(url);
		
    //    if(succeedRequest.ContainsKey(url))
    //    {
    //        WWWRequest request = succeedRequest[url];
    //        prepareDependBundles( stripBundleSuffix(request.requestString) );
    //        return request.www;
    //    }
    //    else
    //        return null;
    //}    
   
    public AssetBundle GetAssetBundle(string bundleName)
    {
        if (!ConfigLoaded)
            return null;
        if (succeedRequest.ContainsKey(bundleName))
        {
            WWWRequest request = succeedRequest[bundleName];
            return request.assetBundle;
        }
        else
            return null;
	}

    public IEnumerator CheckUpdate(string downloadPath)
    {
        while (!ConfigLoaded) yield return null;

        var downloadPathStr = BMUtility.InterpretPath(downloadPath, curPlatform);
        if (string.IsNullOrEmpty(downloadPathStr))
        {
            CheckUpdateError = "{BM} Invalid update download path"+downloadPathStr;
            yield break;
        }

        remoteRootUrl = new Uri(downloadPathStr).AbsoluteUri;

        //Try to get Url redirect file
        var redirectUrl = formatUrl(remoteRootUrl, "BMRedirect.txt");
        if (!string.IsNullOrEmpty(redirectUrl))
        {
            WWW redirectWWW = new WWW(redirectUrl);
            yield return redirectWWW;

            if (redirectWWW.error == null)
            {
                //Redirect download
                var redirectedDownloadPath = redirectWWW.text;
                downloadPathStr = BMUtility.InterpretPath(redirectedDownloadPath, curPlatform);
                if (string.IsNullOrEmpty(downloadPathStr))
                {
                    Debug.LogError("{BM} Invalid redirected download Path :" + downloadPathStr+"(Retrived from url:"+redirectUrl+")."+
                        "Using the original download path"+remoteRootUrl);
                }
                else
                {
                    remoteRootUrl = new Uri(downloadPathStr).AbsoluteUri;
                }
            }
            redirectWWW.Dispose();
        }

        var versionInfo = new VersionInfo(remoteRootUrl);
        yield return StartCoroutine(downloadVersionInfoCo(versionInfo));
        CheckUpdateError = versionInfo.error;

        if (versionInfo.isValue)
        {
            UpdateTotalSize = 0;
            UpdateVersion = versionInfo.listVersion;

            mergeVersion(versionInfo);
            refreshBundleDict();
        }

        updateList = new List<BundleDownloadInfo>();
        foreach (var bundleDownloadInfo in bundles)
        {
            if (bundleDownloadInfo.needDownload)
            {
                if (!updateList.Contains(bundleDownloadInfo))
                {
                    updateList.Add(bundleDownloadInfo);
                    UpdateTotalSize += bundleDownloadInfo.versionInfo.size;
                }
            }
        }
    }

    public IEnumerator UpdateBundles()
    {
        if(!initializationFinished) yield break;
        initializationFinished = false;
        StopAll();

        foreach (var req in succeedRequest.Values)
        {
            var ab = req.assetBundle;
            if(ab) ab.Unload(false);
        }

        string[]  disposeRequestArray = new string[succeedRequest.Count+failedRequest.Count];
        succeedRequest.Keys.CopyTo(disposeRequestArray,0);
        failedRequest.Keys.CopyTo(disposeRequestArray,succeedRequest.Count);
        foreach (var req in disposeRequestArray)
        {
            DisposeBundle(req);
        }

        long downloadFileSize = 0;
        for (int i = 0; i < updateList.Count; ++i)
        {
            var info = updateList[i];
            UpdateFinishFielCount = i + 1;
            var www = WWW.LoadFromCacheOrDownload(info.url, info.version);
            do
            {
                yield return null;
                UpdateFinishSize = downloadFileSize + (long) (www.progress*info.versionInfo.size);
            } while (!www.isDone);
            www.Dispose();
            downloadFileSize += info.versionInfo.size;
            UpdateFinishSize = downloadFileSize;
            info.needDownload = false;
            BMUtility.SaveToPersistentData(bundles,DOWNLOAD_INFO_CACHE_NAME);
        }
        CurrentVesion = UpdateVersion;
        PlayerPrefs.SetInt(BMDATA_VERSION_PREFSKEY,CurrentVesion);
        initializationFinished = true;
    }



    public IEnumerator WaitDownload(string url)
	{
		yield return StartCoroutine( WaitDownload(url, -1) );
	}
	
	/**
	 * Coroutine for download waiting. 
	 * You should use it like this,
	 * yield return StartCoroutine(DownloadManager.Instance.WaitDownload("bundle1.assetbundle"));
	 * If this url didn't been requested, the coroutine will start a new request.
	 */ 
	public IEnumerator WaitDownload(string bundleName, int priority)
	{
		while(!ConfigLoaded)
			yield return null;
		
		download(bundleName);

        while (isDownloadingWWW(bundleName))
			yield return null;
	}
	
	public void StartDownload(string url)
	{
		StartDownload(url, -1);
	}
	
	/**
	 * Start a new download request.
	 * @param url The url for download. Can be a absolute or relative url.
	 * @param priority Priority for this request.
	 */ 
	public void StartDownload(string bundleName, int priority)
	{
		if(!ConfigLoaded)
		{
		    if (!isInBeforeInitList(bundleName))
		        requestedBeforeInit.Add(bundleName);
		}
		else
            download(bundleName);
	}
	
	/**
	 * Stop a request.
	 */ 
	public void StopDownload(string bundleName)
	{
		if(!ConfigLoaded)
		{
            requestedBeforeInit.RemoveAll(x => x == bundleName);
		}
		else
		{
            waitingRequests.RemoveAll(x => x.bundleName == bundleName);

            if (processingRequest.ContainsKey(bundleName))
			{
                processingRequest[bundleName].DisposeWWW();
                processingRequest.Remove(bundleName);
			}
		}
	}
	
	/**
	 * Dispose a finished WWW request.
	 */ 
    //public void DisposeWWW(string url)
    //{
    //    url = formatUrl(url);
    //    StopDownload(url);
		
    //    if(succeedRequest.ContainsKey(url))
    //    {
    //        var r = succeedRequest[url];
    //        var ab = r.www.assetBundle;
    //        if (dbgDownloadBundles.Contains(ab)) dbgDownloadBundles.Remove(ab);
    //        ab.Unload(false);

    //        succeedRequest[url].www.Dispose();
    //        succeedRequest.Remove(url);
    //    }
		
    //    if(failedRequest.ContainsKey(url))
    //    {
    //        failedRequest[url].www.Dispose();
    //        failedRequest.Remove(url);
    //    }
    //}


    /**
     * Dispose a finished request
     */

    public void DisposeBundle(string bundleName)
    {
        StopDownload(bundleName);

        if (succeedRequest.ContainsKey(bundleName))
        {
            var r = succeedRequest[bundleName];
            var ab = r.assetBundle;
            if (ab != null) ab.Unload(false);
            if (dbgDownloadBundles.Contains(ab)) dbgDownloadBundles.Remove(ab);
            succeedRequest.Remove(bundleName);
        }

        if (failedRequest.ContainsKey(bundleName))
        {
            failedRequest.Remove(bundleName);
        }
    }

    /**
	 * This function will stop all request in processing.
	 */ 
	public void StopAll()
	{
		requestedBeforeInit.Clear();
		waitingRequests.Clear();
		
		foreach(WWWRequest request in processingRequest.Values)
			request.DisposeWWW();
		
		processingRequest.Clear();
	}

    public float ProgressOfBundle(string bundleName)
    {
        if (!ConfigLoaded) return 0f;
        if (processingRequest.ContainsKey(bundleName))
            return processingRequest[bundleName].progress;

        if (succeedRequest.ContainsKey(bundleName))
            return 1;
        return 0;
    }

    /**
	 * Get download progress of bundles.����bundle����
	 * All bundle dependencies will be counted too.
	 * This method can only used on self built bundles.
	 */ 
	public float ProgressOfBundles(string[] bundleNames)
	{
		if(!ConfigLoaded)
			return 0f;
		
		List<string> bundles = new List<string>();
        foreach (string bundleName in bundleNames)
		{
            if (!bundleDict.ContainsKey(bundleName))
			{
                Debug.LogWarning("ProgressOfBundles only accept bundle files. " + bundleName + " is not a bundle file.");
				continue;
			}
            bundles.Add(bundleName);
		}
		
		HashSet<string> allInludeBundles = new HashSet<string>();
		foreach(string bundle in bundles)
		{
			foreach(string depend in getDependList(bundle))
			{
				if(!allInludeBundles.Contains(depend))
					allInludeBundles.Add(depend);
			}
			
			if(!allInludeBundles.Contains(bundle))
				allInludeBundles.Add(bundle);
		}
		
		long currentSize = 0;
		long totalSize = 0;
		foreach(string bundleName in allInludeBundles)
		{
			if(!bundleDict.ContainsKey(bundleName))
			{
				Debug.LogError("Cannot get progress of [" + bundleName + "]. It's not such bundle in bundle build states list.");		
				continue;
			}

            long bundleSize = bundleDict[bundleName].versionInfo.size;
			totalSize += bundleSize;

            if (processingRequest.ContainsKey(bundleName))
                currentSize += (long)(processingRequest[bundleName].progress * bundleSize);

            if (succeedRequest.ContainsKey(bundleName))
				currentSize += bundleSize;
		}
		
		if(totalSize == 0)
			return 0;
		else
			return ((float)currentSize)/totalSize;
	}

    public IEnumerable GetFinishedRequests()
    {
        foreach (var bundleName in succeedRequest.Keys)
        {
            yield return bundleName;
        }
        foreach (var bundleName in failedRequest.Keys)
        {
            yield return bundleName;
        }
    }

    public void ResetRession()
    {
        sessionUrls.Clear();
    }

    public float GetSessionPorgress()
    {
        return ProgressOfBundles(sessionUrls.ToArray());
    }

    /**
	 * Check if the config files downloading finished.
	 */
	public bool ConfigLoaded
	{
		get
		{
			return bundles != null && bundles != null && bmConfiger != null;
		}
	}

	/**
	 * Get list of the built bundles. 
	 * Before use this, please make sure ConfigLoaded is true.
	 */ 
	public BundleDownloadInfo[] BuiltBundles
	{
		get
		{
			if(bundles == null)
				return null;
			else
				return bundles.ToArray();
		}
	}

    public int CurrentVesion { get; private set; }

    public bool NeedUpdate {get { return UpdateTotalSize > 0; }}

    public string CheckUpdateError { get; private set; }

    public int UpdateVersion { get;private set; }

    public long UpdateTotalSize { get; private set; }

    public long UpdateFinishSize { get; private set; }

    public int UpdateTotalFileCount { get { return updateList.Count; }}

    public int UpdateFinishFielCount { get; private set; }
    // Privats
	IEnumerator Start() 
	{
	    if (instance != this)
	    {
	        Destroy(gameObject);
	        yield break;
	    }
#if UNITY_EDITOR
        //HACK:Ϊ���ñ༭������������������
        //����������޷����ظ��°������Ҫ��������汾���֣�ǿ�ƿͻ������Cache���ذ�
	    if (!PlayerPrefs.HasKey(FORCE_DOWNLOAD_PREFSKEY) ||
	        PlayerPrefs.GetInt(FORCE_DOWNLOAD_PREFSKEY) < FORCE_DOWNLOAD_VERSION)
	    {
	        CleanCache();
	        PlayerPrefs.SetInt(FORCE_DOWNLOAD_PREFSKEY,FORCE_DOWNLOAD_VERSION);
	    }

#endif   
	    // Initial download urls
		initRootUrl();

        //�ӱ�������������ж�ȡ����
	    int lastBMDataVersion = 0;
	    if (PlayerPrefs.HasKey(BMDATA_VERSION_PREFSKEY))
	    {
	        lastBMDataVersion = PlayerPrefs.GetInt(BMDATA_VERSION_PREFSKEY);
#if UNITY_EDITOR && ALWAYS_UPDATE_LOCAL
            //�ڱ༭��ģʽ�¿��ܿ�������Ҳ�����������ᵼ�¿��������б�������İ��汾���ֺ����ɵİ汾���ֲ�����ͻ
            //���Ա༭��ģʽ�����Ӱ汾���ֻ���
            lastBMDataVersion =0;
#endif
        }
	    CurrentVesion = lastBMDataVersion;

	    if (BMUtility.IsPersistentDataExists(DOWNLOAD_INFO_CACHE_NAME) &&
	        BMUtility.IsPersistentDataExists(CONFIGER_CACHE_NAME))
	    {
	        bundles = BMUtility.LoadFromPersistentData<List<BundleDownloadInfo>>(DOWNLOAD_INFO_CACHE_NAME);
	        bmConfiger = BMUtility.LoadFromPersistentData<BMConfiger>(CONFIGER_CACHE_NAME);
	    }




        //�����ȡ�����г������⣬��������л��棬֮����Դ�LoadRootUrl������������
	    if (bundles == null || bmConfiger == null)
	    {
	        DownloadManager.CleanCache();
	        CurrentVesion = 0;
	        bundles = new List<BundleDownloadInfo>();
	        bmConfiger = new BMConfiger();
	    }
	    refreshBundleDict();

        //���Դ�LoadDownloadUrl���ذ汾��Ϣ
        //��Ҹ��¿ͻ��˺����LoadDownLoadUrl�а汾���Ϊ���°汾���������ͬ����ҪmergeVersion�ʹӱ��ظ�������
	    if (string.IsNullOrEmpty(localRootUrl))
	    {

	    }
	    else
	    {
	        var localVersionInfo = new VersionInfo(localRootUrl);
	        yield return StartCoroutine(downloadVersionInfoCo(localVersionInfo));

	        if (localVersionInfo.isValue)
	        {
	            CleanCache();
	            bundles = new List<BundleDownloadInfo>();
	            mergeVersion(localVersionInfo);
                refreshBundleDict();
	            CurrentVesion = localVersionInfo.listVersion;
                PlayerPrefs.SetInt(BMDATA_VERSION_PREFSKEY,CurrentVesion);
	        }
		}

        //Validation
	    if (bundles == null)
	    {
            Debug.LogError("{BM} Cannot get bundle list.");
            yield break;
	    }
	    if (bmConfiger == null)
	    {
            Debug.LogError("{BM} Fail to load BMConfiger.");
	    }

	    initializationFinished = true;
        
        //Start download for requests before init
	    foreach (var request in requestedBeforeInit)
	    {
	        download(request);
	    }
	}

	void Update () 
	{
		if(!ConfigLoaded)
			return;
		
		// Check if any WWW is finished or errored
		List<string> newFinisheds = new List<string>();
		List<string> newFaileds = new List<string>();
		foreach(WWWRequest request in processingRequest.Values)
		{
			if(request.error != null)
			{
				if(request.triedTimes - 1 < bmConfiger.downloadRetryTime)
				{
					// Retry download
					request.CreatWWW();
				}
				else
				{
				    request.DisposeWWW();
					newFaileds.Add(request.bundleName);
					Debug.LogError("{BM}Download Bundle " + request.bundleName +"URL:"+request.info.url+ "failed for " + request.triedTimes + " times.\nError: " + request.error);
				}
			}
			else if(request.isDone)
			{
                request.DisposeWWW();
				newFinisheds.Add( request.bundleName );
			}
		}
		
		// Move complete bundles out of downloading list
		foreach(string finishedUrl in newFinisheds)
		{
		    var req = processingRequest[finishedUrl];
		    if (req.info.needDownload)
		    {
		        req.info.needDownload = false;
		    }
		    req.assetBundle.name = req.bundleName;
            dbgDownloadBundles.Add(req.assetBundle);
            succeedRequest.Add(finishedUrl,req);
		    processingRequest.Remove(finishedUrl);
		}
		
		// Move failed bundles out of downloading list
		foreach(string finishedUrl in newFaileds)
		{
			if(!failedRequest.ContainsKey(finishedUrl))
				failedRequest.Add(finishedUrl, processingRequest[finishedUrl]);
			processingRequest.Remove(finishedUrl);
		}
		
		// Start download new bundles
		int waitingIndex = 0;
		while( processingRequest.Count < bmConfiger.downloadThreadsCount && 
			   waitingIndex < waitingRequests.Count)
		{
			WWWRequest curRequest = waitingRequests[waitingIndex++];
			
			bool canStartDownload = curRequest.info == null || isBundleDependenciesReady( curRequest.info.name );
			if(canStartDownload)
			{
				waitingRequests.Remove(curRequest);
				curRequest.CreatWWW();
				processingRequest.Add(curRequest.bundleName, curRequest);
			}
		}

	    if (needSaveDownloadInfoCache && processingRequest.Count == 0 && waitingRequests.Count == 0)
	    {
	        needSaveDownloadInfoCache = false;
            BMUtility.SaveToPersistentData(bundles,DOWNLOAD_INFO_CACHE_NAME);
	    }
	}//update end
	
	bool isBundleDependenciesReady(string bundleName)
	{
		List<string> dependencies = getDependList(bundleName);
        foreach (string dependBundleNames in dependencies)
		{
			if(!succeedRequest.ContainsKey(dependBundleNames))
				return false;
		}
		
		return true;
	}

    //void prepareDependBundles(string bundleName)
    //{
    //    List<string> dependencies = getDependList(bundleName);
    //    foreach(string dependBundle in dependencies)
    //    {
    //        string dependUrl = formatUrl(dependBundle + "." + bmConfiger.bundleSuffix);
    //        if(succeedRequest.ContainsKey(dependUrl))
    //        {
    //            #pragma warning disable 0168
    //            var assetBundle = succeedRequest[dependUrl].www.assetBundle;
    //            #pragma warning restore 0168
    //        }
    //    }
    //}
	
	// This private method should be called after init
	void download(string bundleName)
	{
	    if (!bundleDict.ContainsKey(bundleName))
	    {
#if UNITY_EDITOR
            Debug.LogError("{BM} �б��в�����Bundle["+bundleName+"],�޷�����.");
#else
            Debug.LogError("{BM} Cannot download bundle["+bundlename+"],It's not in the bundle config.");
#endif
	        return;
	    }
        if (isDownloadingWWW(bundleName) || succeedRequest.ContainsKey(bundleName))
			return;

	    var bundleDownloadInfo = bundleDict[bundleName];
        var request = createWWWRequest(bundleDownloadInfo);
		//if(isBundleUrl(request.url))
		{
			List<string> dependlist = getDependList(bundleName);
			foreach(string dependBundleName in dependlist)
			{
				if(!processingRequest.ContainsKey(dependBundleName) && !succeedRequest.ContainsKey(dependBundleName))
				{
					var dependRequest = createWWWRequest(bundleDict[dependBundleName]);
					addRequestToWaitingList(dependRequest);
				}
			}
			
			request.info = bundleDict[bundleName];
			if(request.priority == -1)
				request.priority = request.info.versionInfo.priority;  // User didn't change the default priority
			addRequestToWaitingList(request);
		}
        //else
        //{
        //    if(request.priority == -1)
        //        request.priority = 0; // User didn't give the priority
        //    addRequestToWaitingList(request);
        //}
	}
	
	bool isInWaitingList(string bundleName)
	{
		foreach(WWWRequest request in waitingRequests)
			if(request.bundleName == bundleName)
				return true;
		
		return false;
	}
	
	void addRequestToWaitingList(WWWRequest request)
	{
		if(succeedRequest.ContainsKey(request.bundleName) || isInWaitingList(request.bundleName))
			return;
		
		int insertPos = waitingRequests.FindIndex(x => x.priority < request.priority);
		insertPos = insertPos == -1 ? waitingRequests.Count : insertPos;
		waitingRequests.Insert(insertPos, request);
	}
	
	bool isDownloadingWWW(string bundleName)
	{
		foreach(WWWRequest request in waitingRequests)
			if(request.info.name == bundleName)
				return true;
		
		return processingRequest.ContainsKey(bundleName);
	}
	
	bool isInBeforeInitList(string bundleName)
	{
		foreach(var request in requestedBeforeInit)
		{
			if(request == bundleName)
				return true;
		}

		return false;
	}

	List<string> getDependList(string bundle)
	{
		if(!ConfigLoaded)
		{
			Debug.LogError("getDependList() should be call after download manger inited");
			return null;
		}
		
		List<string> res = new List<string>();
		
		if(!bundleDict.ContainsKey(bundle))
		{
			Debug.LogError("Cannot find parent bundle [" + bundle + "], Please check your bundle config.");
			return res;
		}
			
		while(bundleDict[bundle].versionInfo.parent != "")
		{
			bundle = bundleDict[bundle].versionInfo.parent;
			if(bundleDict.ContainsKey(bundle))
			{
				res.Add(bundle);
			}
			else
			{
				Debug.LogError("Cannot find parent bundle [" + bundle + "], Please check your bundle config.");
				break;
			}
		}
		
		res.Reverse();
		return res;
	}
	
	BuildPlatform getRuntimePlatform()
	{
		if(	Application.platform == RuntimePlatform.WindowsPlayer ||
			Application.platform == RuntimePlatform.OSXPlayer )
		{
			return BuildPlatform.Standalones;
		}
		else if(Application.platform == RuntimePlatform.OSXWebPlayer ||
				Application.platform == RuntimePlatform.WindowsWebPlayer)
		{
			return BuildPlatform.WebPlayer;
		}
		else if(Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return BuildPlatform.IOS;
		}
		else if(Application.platform == RuntimePlatform.Android)
		{
			return BuildPlatform.Android;
		}
		else
		{
			Debug.LogError("Platform " + Application.platform + " is not supported by BundleManager.");
			return BuildPlatform.Standalones;
		}
	}
	
	void initRootUrl()
	{
		TextAsset downloadUrlText = (TextAsset)Resources.Load("Urls");
		bmUrl = JsonMapper.ToObject<BMUrls>(downloadUrlText.text);

		if( Application.platform == RuntimePlatform.WindowsEditor ||
		   Application.platform == RuntimePlatform.OSXEditor )
		{
#if UNITY_EDITOR
		    switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
		    {
                case UnityEditor.BuildTarget.Android:
		            curPlatform = BuildPlatform.Android;
		            break;
                case UnityEditor.BuildTarget.iOS:
		            curPlatform = BuildPlatform.IOS;
                    break;
                default:
		            curPlatform = BuildPlatform.Standalones;
                    break;
		    }
#else
			curPlatform = bmUrl.bundleTarget;
#endif
        }
		else
		{
			curPlatform = getRuntimePlatform();

		}
	    string downloadPathStr = string.Empty;
		if(manualUrl == "")
		{
			
			if(bmUrl.downloadFromOutput)
				downloadPathStr = bmUrl.GetInterpretedOutputPath(curPlatform);
			else
				downloadPathStr = bmUrl.GetInterpretedDownloadUrl(curPlatform);
		}
		else
		{
			downloadPathStr = BMUtility.InterpretPath(manualUrl, curPlatform);
		}
	    if (!string.IsNullOrEmpty(downloadPathStr))
	    {
	        localRootUrl = new Uri(downloadPathStr).AbsoluteUri;
	    }
	}


    /// <summary>
    /// ��ָ����Url ���ذ汾������Ϣ��������°汾�����������Bundle�б��BMConfiger.
    /// </summary>
    /// <param name="versionInfo"> ���ڴ������ص�URl�Լ��������صõ�������</param>
    private IEnumerator downloadVersionInfoCo(VersionInfo versionInfo)
    {
        if (string.IsNullOrEmpty(versionInfo.rootUrl))
            yield break;

        versionInfo.listVersion = 0;//ֵΪ0����versionΪ��Ч״̬
        versionInfo.bundles = null;
        versionInfo.bmConfiger = null;

        //Download new BMData version
        var serverBMDataVersion = 0;

        var versionWWW = new WWW(formatUrl(versionInfo.rootUrl,"BMDataVersion.txt"));
        yield return versionWWW;

        if (versionWWW.error == null)
        {
            //update BMData Version
            int.TryParse(versionWWW.text, out serverBMDataVersion);
        }
        else
        {
            versionInfo.error = "{BM} BMDataVersion error:" + versionWWW.error;
        }

        versionWWW.Dispose();

        //�������
        if(serverBMDataVersion>0&& serverBMDataVersion<=CurrentVesion) yield break;
        if(!string.IsNullOrEmpty(versionInfo.error)) yield break;

        TextAsset ta = null;

        //Download the initial data bundle from url
        WWW initDataWWW = null;
        var initBMDataUrl = formatUrl(versionInfo.rootUrl, BMDATA_NAME);
        initDataWWW = new WWW(initBMDataUrl);

        yield return initDataWWW;
        if (initDataWWW.error != null)
        {
            initDataWWW.Dispose();
            initDataWWW = null;
            versionInfo.error = "{BM}BMData error:" + initDataWWW.error;
            Debug.LogError(versionInfo.error);
        }
        if (initDataWWW != null)
        {
            //bundle Data
            var bundleDataName = "BundleVersionInfo";
#if UNITY_5
            ta = initDataWWW.assetBundle.LoadAsset<TextAsset>(bundleDataName);
#else
            ta = initDataWWW.assetBundle.Load(bundleDataName) as TextAsset;
#endif
            versionInfo.bundles = JsonMapper.ToObject<List<BundleVersionInfo>>(ta.text);

            var bmConfigerName = "BMConfiger";
#if UNITY_5
            ta = initDataWWW.assetBundle.LoadAsset<TextAsset>(bmConfigerName);
#else
            ta = initDataWWW.assetBundle.Load(bmConfigerName) as TextAsset;
#endif
            versionInfo.bmConfiger = JsonMapper.ToObject<BMConfiger>(ta.text);

            initDataWWW.assetBundle.Unload(true);
            initDataWWW.Dispose();
        }

        if (versionInfo.bundles == null || versionInfo.bmConfiger == null)
        {
            if (versionInfo.error == null)
            {
                versionInfo.error = "{BM}versionInfo.bundles == null ||  versionInfo.bmConfiger == null";
            }
            yield break;
        }

        versionInfo.listVersion = serverBMDataVersion;    
    }

    private void mergeVersion(VersionInfo versionInfo)
    {
        if (!versionInfo.isValue) return;

        var newList = new List<BundleDownloadInfo>();
        foreach (var bundleVersionInfo in versionInfo.bundles)
        {
            var bundleName = bundleVersionInfo.name;
            BundleDownloadInfo downloadInfo = null;
            if (bundleDict.ContainsKey(bundleName))
            {
                downloadInfo = bundleDict[bundleName];
            }
            else
            {
                downloadInfo = new BundleDownloadInfo();
                downloadInfo.name = bundleName;
                downloadInfo.version = 0;
            }

            if (downloadInfo.versionInfo == null || downloadInfo.versionInfo.crc != bundleVersionInfo.crc ||
                (!downloadInfo.localBundle && !Caching.IsVersionCached(downloadInfo.url, downloadInfo.version)))
            {
                downloadInfo.versionInfo = bundleVersionInfo;
                downloadInfo.version++;
                downloadInfo.localBundle = versionInfo.rootUrl == localRootUrl;
                downloadInfo.url = formatUrl(versionInfo.rootUrl, bundleVersionInfo.requestString);
                downloadInfo.needDownload = !downloadInfo.localBundle;
            }

            newList.Add(downloadInfo);
        }

        bundles = newList;
        bmConfiger = versionInfo.bmConfiger ?? bmConfiger;

        BMUtility.SaveToPersistentData(bundles,DOWNLOAD_INFO_CACHE_NAME);
        BMUtility.SaveToPersistentData(bmConfiger,CONFIGER_CACHE_NAME);
    }


    string formatUrl(string rootUrl,string urlstr)
	{
		Uri url;
		if(!isAbsoluteUrl(urlstr))
		{
		    if (string.IsNullOrEmpty(rootUrl)) return string.Empty;
            url = new Uri(new Uri(rootUrl + '/'), urlstr);
		}
		else
			url = new Uri(urlstr);
		
		return url.AbsoluteUri;
	}

    private WWWRequest createWWWRequest(BundleDownloadInfo info)
    {
        if (info.localBundle) info.url = formatUrl(localRootUrl, info.versionInfo.requestString);
        return new WWWRequest(info);
    }

    bool isAbsoluteUrl(string url)
	{
	    Uri result;
	    return Uri.TryCreate(url, System.UriKind.Absolute, out result);
	}

    private void refreshBundleDict()
    {
        bundleDict.Clear();
        foreach (var bundle in bundles)
        {
            bundleDict.Add(bundle.name,bundle);
        }
    }

    bool isBundleUrl(string url)
	{
		return string.Compare(Path.GetExtension(url), "." + bmConfiger.bundleSuffix, System.StringComparison.OrdinalIgnoreCase)  == 0;
	}

	string stripBundleSuffix(string requestString)
	{
		return requestString.Substring(0, requestString.Length - bmConfiger.bundleSuffix.Length - 1);
	}
	
    public List<AssetBundle> dbgDownloadBundles = new List<AssetBundle>();
	
    // Members
	List<BundleDownloadInfo> bundles = null;
	BMConfiger bmConfiger = null;
	BMUrls bmUrl = null;

    private string remoteRootUrl = null;
	string localRootUrl = null;
	BuildPlatform curPlatform;
    List<string> sessionUrls = new List<string>();
    private bool initializationFinished = false;
    private bool needSaveDownloadInfoCache = false;
    private List<BundleDownloadInfo> updateList = null;
    
    Dictionary<string,BundleDownloadInfo> bundleDict = new Dictionary<string, BundleDownloadInfo>();
        
    // Request members
	Dictionary<string, WWWRequest> processingRequest = new Dictionary<string, WWWRequest>();
	Dictionary<string, WWWRequest> succeedRequest = new Dictionary<string, WWWRequest>();
	Dictionary<string, WWWRequest> failedRequest = new Dictionary<string, WWWRequest>();
	List<WWWRequest> waitingRequests = new List<WWWRequest>();
    List<string> requestedBeforeInit = new List<string>();

	static DownloadManager instance = null;
	static string manualUrl = "";


    ///<summary>
    /// ��ʼ����ϵͳ,������ʾ���ú������ܳ�ʼ��DownloadManager�ĵ���
    /// 
    /// ��ʼ��ʱ��ϵͳ���ָ����λ������BM.data��Ȼ�������������Bundle֮���������ϵ���������ÿ��bundle����Ϣ
    /// 
    /// �����������ϵͳ���ڱ༭ʱ�ṩ����URL��Ϊ����Bundle�Լ�BM.data��·��
    /// һ����RemoteDownloadUrl,һ����LocalDownloadUrl.
    /// 
    /// LocalDownloadUrl��λ��App�ڵ�����·��,�������һ����Դ���ڰ�װ��ʱ���Ѿ��������û����ֻ�
    /// RemoteDownloadUrl ��λ�ڻ������ϵķ�����������·����ͨ���Ѻ����ĸ��°��������·��
    /// 
    /// ��ʼ����ʱ����ͼ��RemoteDownload�ĸ�λ�û�ȡһ��BMDataVersion.txt
    /// ����ı��ļ��м�¼һ���������֣����������������BM.data
    /// </summary>>
    public static void Initialize()
    {
        if (instance == null)
        {
            instance = new GameObject("Download Manager").AddComponent<DownloadManager>();
            instance.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(instance.gameObject);
        }
    }

    /**
	 * Get instance of DownloadManager.
	 * This prop will create a GameObject named Downlaod Manager in scene when first time called.
	 */ 
	public static DownloadManager Instance
	{
		get
		{
			return instance;
		}
	}

	public static void SetManualUrl(string url)
	{
		if(instance != null)
		{
			Debug.LogError("Cannot use SetManualUrl after accessed DownloadManager.Instance. Make sure call SetManualUrl before access to DownloadManager.Instance.");
			return;
		}

		manualUrl = url;
	}


    public static void CleanCache()
    {
        Caching.CleanCache();
        BMUtility.DeletePersistentData(DOWNLOAD_INFO_CACHE_NAME);
        BMUtility.DeletePersistentData(CONFIGER_CACHE_NAME);
        PlayerPrefs.DeleteKey(BMDATA_VERSION_PREFSKEY);
    }

    class VersionInfo
    {
        public string rootUrl = null;

        public int listVersion = 0;
        public List<BundleVersionInfo> bundles;
        public BMConfiger bmConfiger;
        public string error;

        public bool isValue {get { return listVersion > 0; }}

        public VersionInfo(string rootUrl)
        {
            this.rootUrl = rootUrl;
        }
    }

    class WWWRequest
    {
        private static int PathSubstringindex = 0;

        public BundleDownloadInfo info = null;
        public AssetBundle assetBundle;

		public int triedTimes = 0;
        public int priority;

        private bool _isDone;
        private string _error;
        private float _progress;
        public string bundleName {get { return info.name; }}

        public string error     
        {
            get
            {
                if (www != null) _error = www.error;
                return _error;
            }
        }

        public bool isDone
        {
            get { if (www != null) _isDone = www.isDone;
                return _isDone;
            }
        }

        public float progress
        {
            get { if (www != null) _progress = www.progress;
                return _progress;
            }
        }

        public WWW www = null;
        private AssetBundleCreateRequest abr = null;

        public WWWRequest(BundleDownloadInfo info)
        {
            this.info = info;
            this.priority = info.versionInfo.priority;
            if (this.priority < 0) inheriority();
        }

        public void CreatWWW()
		{	
			triedTimes++;

            if (info == null)
            {
                _error = "BundleDownloadInfo is null";
                return;
            }
#if AVOID_USING_WWW
            if(info.localBundle)
            {
                byte[] bytes = null;
                try
                {
                    if(Application.platform == RuntimePlatform.Android)
                    {
                    }
                }
            }

#endif
            if(DownloadManager.instance.bmConfiger.useCache)
			{
#if !(UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
				if(DownloadManager.instance.bmConfiger.useCRC)
                    www = WWW.LoadFromCacheOrDownload(info.url, info.version, info.versionInfo.crc);
				else 
#endif
                    www = WWW.LoadFromCacheOrDownload(info.url, info.version);
			}
			else
				www = new WWW(info.url);
		}

        public void DisposeWWW()
        {
            if (www != null)
            {
                _error = www.error;
                _isDone = www.isDone;
                if (www.isDone && string.IsNullOrEmpty(www.error)) assetBundle = www.assetBundle;
                www.Dispose();
                www = null;

            }
        }

        private void inheriority()
        {
            var mgr = DownloadManager.instance;
            string bundleName = info.name;
            while (priority==BMUtility.InheritPriorityValue)
            {
                var bundle = mgr.bundleDict[bundleName];
                var parentBundleName = bundle.versionInfo.parent;
                if (string.IsNullOrEmpty(parentBundleName))
                {
                    Debug.LogWarning("{BM} Ingerited priority("+info.name+")-using default priority.");
                }
                else
                {
                    bundleName = parentBundleName;
                    bundle = mgr.bundleDict[bundleName];
                    priority = bundle.versionInfo.priority;
                    if(priority!=BMUtility.InheritPriorityValue)
                        Debug.LogWarning("{BM} Inherited priority("+ info.name +")-using priority of parent bundle."+bundleName);
                }
            }
        }
	}
}