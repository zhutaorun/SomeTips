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
	/**
	 * Get the error string of WWW request.
	 * @return The error string of WWW. Return null if WWW request succeed or still in processing.
	 */ 
	public string GetError(string url)
	{
		if(!ConfigLoaded)
			return null;
		
		url = formatUrl(url);
		if(failedRequest.ContainsKey(url))
			return failedRequest[url].www.error;
		else
			return null;
	}
	
	/**
	 * Test if the url is already requested.
	 */
	public bool IsUrlRequested(string url)
	{
		if(!ConfigLoaded)
		{
			return isInBeforeInitList(url);
		}
		else
		{
			url = formatUrl(url);
			bool isRequested = isInWaitingList(url) || processingRequest.ContainsKey(url) || succeedRequest.ContainsKey(url) || failedRequest.ContainsKey(url);
			return isRequested;
		}
	}
	
	/**
	 * Get WWW instance of the url.
	 * @return Return null if the WWW request haven't succeed.
	 */ 
	public WWW GetWWW(string url)
	{
		if(!ConfigLoaded)
			return null;
		
		url = formatUrl(url);
		
		if(succeedRequest.ContainsKey(url))
		{
			WWWRequest request = succeedRequest[url];
			prepareDependBundles( stripBundleSuffix(request.requestString) );
			return request.www;
		}
		else
			return null;
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
	public IEnumerator WaitDownload(string url, int priority)
	{
		while(!ConfigLoaded)
			yield return null;
		
		WWWRequest request = new WWWRequest();
		request.requestString = url;
		request.url = formatUrl(url);
		request.priority = priority;
		download(request);
		
		while(isDownloadingWWW(request.url))
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
	public void StartDownload(string url, int priority)
	{
		WWWRequest request = new WWWRequest();
		request.requestString = url;
		request.url = url;
		request.priority = priority;

		if(!ConfigLoaded)
		{
			if(!isInBeforeInitList(url))
				requestedBeforeInit.Add(request);
		}
		else
			download(request);
	}
	
	/**
	 * Stop a request.
	 */ 
	public void StopDownload(string url)
	{
		if(!ConfigLoaded)
		{
			requestedBeforeInit.RemoveAll(x => x.url == url);
		}
		else
		{
			url = formatUrl(url);
			
			waitingRequests.RemoveAll(x => x.url == url);
			
			if(processingRequest.ContainsKey(url))
			{
				processingRequest[url].www.Dispose();
				processingRequest.Remove(url);
			}
		}
	}
	
	/**
	 * Dispose a finished WWW request.
	 */ 
	public void DisposeWWW(string url)
	{
		url = formatUrl(url);
		StopDownload(url);
		
		if(succeedRequest.ContainsKey(url))
		{
			succeedRequest[url].www.Dispose();
			succeedRequest.Remove(url);
		}
		
		if(failedRequest.ContainsKey(url))
		{
			failedRequest[url].www.Dispose();
			failedRequest.Remove(url);
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
			request.www.Dispose();
		
		processingRequest.Clear();
	}
	
	/**
	 * Get download progress of bundles.
	 * All bundle dependencies will be counted too.
	 * This method can only used on self built bundles.
	 */ 
	public float ProgressOfBundles(string[] bundlefiles)
	{
		if(!ConfigLoaded)
			return 0f;
		
		List<string> bundles = new List<string>();
		foreach(string bundlefile in bundlefiles)
		{
			if(!bundlefile.EndsWith( "." + bmConfiger.bundleSuffix, System.StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning("ProgressOfBundles only accept bundle files. " + bundlefile + " is not a bundle file.");
				continue;
			}
			
			bundles.Add(Path.GetFileNameWithoutExtension(bundlefile));
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
			if(!buildStatesDict.ContainsKey(bundleName))
			{
				Debug.LogError("Cannot get progress of [" + bundleName + "]. It's not such bundle in bundle build states list.");		
				continue;
			}
				
			long bundleSize = buildStatesDict[bundleName].size;
			totalSize += bundleSize;
			
			string url = formatUrl( bundleName + "." + bmConfiger.bundleSuffix );
			if(processingRequest.ContainsKey(url))
				currentSize += (long)(processingRequest[url].www.progress * bundleSize);
			
			if(succeedRequest.ContainsKey(url))
				currentSize += bundleSize;
		}
		
		if(totalSize == 0)
			return 0;
		else
			return ((float)currentSize)/totalSize;
	}

	/**
	 * Check if the config files downloading finished.
	 */
	public bool ConfigLoaded
	{
		get
		{
			return bundles != null && buildStates != null && bmConfiger != null;
		}
	}

	/**
	 * Get list of the built bundles. 
	 * Before use this, please make sure ConfigLoaded is true.
	 */ 
	public BundleData[] BuiltBundles
	{
		get
		{
			if(bundles == null)
				return null;
			else
				return bundles.ToArray();
		}
	}

	/**
	 * Get list of the BuildStates. 
	 * Before use this, please make sure ConfigLoaded is true.
	 */ 
	public BundleBuildState[] BuildStates
	{
		get
		{
			if(buildStates == null)
				return null;
			else
				return buildStates.ToArray();
		}
	}

	// Privats
	IEnumerator Start() 
	{
		// Initial download urls
		initRootUrl();

		// Try to get Url redirect file
		WWW redirectWWW = new WWW(formatUrl("BMRedirect.txt"));
		yield return redirectWWW;
		
		if(redirectWWW.error == null)
		{
			// Redirect download
			string downloadPathStr = BMUtility.InterpretPath(redirectWWW.text, curPlatform);
			Uri downloadUri = new Uri(downloadPathStr);
			downloadRootUrl = downloadUri.AbsoluteUri;
		}

		redirectWWW.Dispose();


		// Download the initial data bundle
		const string verNumKey = "BMDataVersion";
		string bmDataUrl = formatUrl("BM.data");
		int lastBMDataVersion = 0;
		if(PlayerPrefs.HasKey(verNumKey))
			lastBMDataVersion = PlayerPrefs.GetInt(verNumKey);
		
		// Download and cache new data version
		WWW initDataWWW;

		if(bmUrl.offlineCache)
		{
			initDataWWW = WWW.LoadFromCacheOrDownload(bmDataUrl, lastBMDataVersion + 1);

			yield return initDataWWW;
			if(initDataWWW.error != null)
			{
				initDataWWW.Dispose();
				Debug.Log("Cannot load BMData from target url. Try load it from cache.");

				initDataWWW = WWW.LoadFromCacheOrDownload(bmDataUrl, lastBMDataVersion);
				yield return initDataWWW;

				if(initDataWWW.error != null)
				{
					Debug.LogError("Download BMData failed.\nError: " + initDataWWW.error);
					yield break;
				}
			}
			else
			{
				// Update cache version number
				PlayerPrefs.SetInt(verNumKey, lastBMDataVersion + 1);
			}
		}
		else
		{
			initDataWWW = new WWW(bmDataUrl);

			yield return initDataWWW;

			if(initDataWWW.error != null)
			{
				Debug.LogError("Download BMData failed.\nError: " + initDataWWW.error);
				yield break;
			}
		}

		// Init datas
		//
		// Bundle Data
#if UNITY_5
		TextAsset ta = initDataWWW.assetBundle.LoadAsset<TextAsset>("BundleData");
#else
		TextAsset ta = initDataWWW.assetBundle.Load("BundleData") as TextAsset;
#endif
		bundles = JsonMapper.ToObject< List< BundleData > >(ta.text);
		foreach(var bundle in bundles)
			bundleDict.Add(bundle.name, bundle);

		// Build States
#if UNITY_5
		ta = initDataWWW.assetBundle.LoadAsset<TextAsset>("BuildStates");
#else
		ta = initDataWWW.assetBundle.Load("BuildStates") as TextAsset;
#endif
		buildStates = JsonMapper.ToObject< List< BundleBuildState > >(ta.text);
		foreach(var buildState in buildStates)
			buildStatesDict.Add(buildState.bundleName, buildState);

		// BMConfiger
#if UNITY_5
		ta = initDataWWW.assetBundle.LoadAsset<TextAsset>("BMConfiger");
#else
		ta = initDataWWW.assetBundle.Load("BMConfiger") as TextAsset;
#endif
		bmConfiger = JsonMapper.ToObject< BMConfiger >(ta.text);

		initDataWWW.assetBundle.Unload(true);
		initDataWWW.Dispose();

		
		// Start download for requests before init
		foreach(WWWRequest request in requestedBeforeInit)
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
			if(request.www.error != null)
			{
				if(request.triedTimes - 1 < bmConfiger.downloadRetryTime)
				{
					// Retry download
					request.CreatWWW();
				}
				else
				{
					newFaileds.Add( request.url );
					Debug.LogError("Download " + request.url + " failed for " + request.triedTimes + " times.\nError: " + request.www.error);
				}
			}
			else if(request.www.isDone)
			{
				newFinisheds.Add( request.url );
			}
		}
		
		// Move complete bundles out of downloading list
		foreach(string finishedUrl in newFinisheds)
		{
			succeedRequest.Add(finishedUrl, processingRequest[finishedUrl]);
			//var bundle = processingRequest[finishedUrl].www.assetBundle;
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
			
			bool canStartDownload = curRequest.bundleData == null || isBundleDependenciesReady( curRequest.bundleData.name );
			if(canStartDownload)
			{
				waitingRequests.Remove(curRequest);
				curRequest.CreatWWW();
				processingRequest.Add(curRequest.url, curRequest);
			}
		}
	}
	
	bool isBundleDependenciesReady(string bundleName)
	{
		List<string> dependencies = getDependList(bundleName);
		foreach(string dependBundle in dependencies)
		{
			string url = formatUrl(dependBundle + "." + bmConfiger.bundleSuffix);
			if(!succeedRequest.ContainsKey(url))
				return false;
		}
		
		return true;
	}

	void prepareDependBundles(string bundleName)
	{
		List<string> dependencies = getDependList(bundleName);
		foreach(string dependBundle in dependencies)
		{
			string dependUrl = formatUrl(dependBundle + "." + bmConfiger.bundleSuffix);
			if(succeedRequest.ContainsKey(dependUrl))
			{
				#pragma warning disable 0168
				var assetBundle = succeedRequest[dependUrl].www.assetBundle;
				#pragma warning restore 0168
			}
		}
	}
	
	// This private method should be called after init
	void download(WWWRequest request)
	{
		request.url = formatUrl(request.url);
		
		if(isDownloadingWWW(request.url) || succeedRequest.ContainsKey(request.url))
			return;
		
		if(isBundleUrl(request.url))
		{
			string bundleName = stripBundleSuffix(request.requestString);
			if(!bundleDict.ContainsKey(bundleName))
			{
				Debug.LogError("Cannot download bundle [" + bundleName + "]. It's not in the bundle config.");
				return;
			}
			
			List<string> dependlist = getDependList(bundleName);
			foreach(string bundle in dependlist)
			{
				string bundleRequestStr = bundle + "." + bmConfiger.bundleSuffix;
				string bundleUrl = formatUrl(bundleRequestStr);
				if(!processingRequest.ContainsKey(bundleUrl) && !succeedRequest.ContainsKey(bundleUrl))
				{
					WWWRequest dependRequest = new WWWRequest();
					dependRequest.bundleData = bundleDict[bundle];
					dependRequest.bundleBuildState = buildStatesDict[bundle];
					dependRequest.requestString = bundleRequestStr;
					dependRequest.url = bundleUrl;
					dependRequest.priority = dependRequest.bundleData.priority;
					addRequestToWaitingList(dependRequest);
				}
			}
			
			request.bundleData = bundleDict[bundleName];
			request.bundleBuildState = buildStatesDict[bundleName];
			if(request.priority == -1)
				request.priority = request.bundleData.priority;  // User didn't change the default priority
			addRequestToWaitingList(request);
		}
		else
		{
			if(request.priority == -1)
				request.priority = 0; // User didn't give the priority
			addRequestToWaitingList(request);
		}
	}
	
	bool isInWaitingList(string url)
	{
		foreach(WWWRequest request in waitingRequests)
			if(request.url == url)
				return true;
		
		return false;
	}
	
	void addRequestToWaitingList(WWWRequest request)
	{
		if(succeedRequest.ContainsKey(request.url) || isInWaitingList(request.url))
			return;
		
		int insertPos = waitingRequests.FindIndex(x => x.priority < request.priority);
		insertPos = insertPos == -1 ? waitingRequests.Count : insertPos;
		waitingRequests.Insert(insertPos, request);
	}
	
	bool isDownloadingWWW(string url)
	{
		foreach(WWWRequest request in waitingRequests)
			if(request.url == url)
				return true;
		
		return processingRequest.ContainsKey(url);
	}
	
	bool isInBeforeInitList(string url)
	{
		foreach(WWWRequest request in requestedBeforeInit)
		{
			if(request.url == url)
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
			
		while(bundleDict[bundle].parent != "")
		{
			bundle = bundleDict[bundle].parent;
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
			curPlatform = bmUrl.bundleTarget;
		}
		else
		{
			curPlatform = getRuntimePlatform();
		}

		if(manualUrl == "")
		{
			string downloadPathStr;
			if(bmUrl.downloadFromOutput)
				downloadPathStr = bmUrl.GetInterpretedOutputPath(curPlatform);
			else
				downloadPathStr = bmUrl.GetInterpretedDownloadUrl(curPlatform);
				
			Uri downloadUri = new Uri(downloadPathStr);
			downloadRootUrl = downloadUri.AbsoluteUri;
		}
		else
		{
			string downloadPathStr = BMUtility.InterpretPath(manualUrl, curPlatform);
			Uri downloadUri = new Uri(downloadPathStr);
			downloadRootUrl = downloadUri.AbsoluteUri;
		}
	}
	
	string formatUrl(string urlstr)
	{
		Uri url;
		if(!isAbsoluteUrl(urlstr))
		{
			url = new Uri(new Uri(downloadRootUrl + '/'), urlstr);
		}
		else
			url = new Uri(urlstr);
		
		return url.AbsoluteUri;
	}
	
	bool isAbsoluteUrl(string url)
	{
	    Uri result;
	    return Uri.TryCreate(url, System.UriKind.Absolute, out result);
	}
	
	bool isBundleUrl(string url)
	{
		return string.Compare(Path.GetExtension(url), "." + bmConfiger.bundleSuffix, System.StringComparison.OrdinalIgnoreCase)  == 0;
	}

	string stripBundleSuffix(string requestString)
	{
		return requestString.Substring(0, requestString.Length - bmConfiger.bundleSuffix.Length - 1);
	}
	
	// Members
	List<BundleData> bundles = null;
	List<BundleBuildState> buildStates = null;
	BMConfiger bmConfiger = null;
	BMUrls bmUrl = null;
	
	string downloadRootUrl = null;
	BuildPlatform curPlatform;
	
	Dictionary<string, BundleData> bundleDict = new Dictionary<string, BundleData>();
	Dictionary<string, BundleBuildState> buildStatesDict = new Dictionary<string, BundleBuildState>();
	
	// Request members
	Dictionary<string, WWWRequest> processingRequest = new Dictionary<string, WWWRequest>();
	Dictionary<string, WWWRequest> succeedRequest = new Dictionary<string, WWWRequest>();
	Dictionary<string, WWWRequest> failedRequest = new Dictionary<string, WWWRequest>();
	List<WWWRequest> waitingRequests = new List<WWWRequest>();
	List<WWWRequest> requestedBeforeInit = new List<WWWRequest>();

	static DownloadManager instance = null;
	static string manualUrl = "";
	
	/**
	 * Get instance of DownloadManager.
	 * This prop will create a GameObject named Downlaod Manager in scene when first time called.
	 */ 
	public static DownloadManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new GameObject("Download Manager").AddComponent<DownloadManager> ();
				DontDestroyOnLoad(instance.gameObject);
			}
 
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
	
	class WWWRequest
	{
		public string requestString = "";
		public string url = "";
		public int triedTimes = 0;
		public int priority = 0;
		public BundleData bundleData = null;
		public BundleBuildState bundleBuildState = null;
		public WWW www = null;
		
		public void CreatWWW()
		{	
			triedTimes++;
			
			if(DownloadManager.instance.bmConfiger.useCache && bundleBuildState != null)
			{
#if !(UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
				if(DownloadManager.instance.bmConfiger.useCRC)
					www = WWW.LoadFromCacheOrDownload(url, bundleBuildState.version, bundleBuildState.crc);
				else 
#endif
					www = WWW.LoadFromCacheOrDownload(url, bundleBuildState.version);
			}
			else
				www = new WWW(url);
		}
	}
}