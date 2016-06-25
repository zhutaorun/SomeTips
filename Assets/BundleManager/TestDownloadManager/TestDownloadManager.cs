/*
 * This is a test case of Download Manager. 
 * In this test case used all the method of DownloadManager. 
 * It's a good example for you to start work with DownloadManager
 * 
 * Before run the test scene. You should use the BundleManager interface to build all the bundle it needs.
 * If every thing is alright, the test scene will finally print out "TEST finished". 
 * There' will be 1 error log. Don't worry about it, it's also a test.
 * 
 * After you down with the test case. You can delete all the bundles created in the BundleManager for this test.
 * But you can alway find the orign bundle data for this test from TestDownloadManager/TestBundleDatas.
 * Replace the same files in the BundleManager to recover test case bundles.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestDownloadManager : MonoBehaviour 
{
	IEnumerator Start()
	{
		Debug.Log(Caching.spaceOccupied);

		// Prevent Test instance destroied when level loading
		DontDestroyOnLoad(gameObject);

		
		// Test start download
		string[] sceneList = new string[]{"LA", "LB", "LC", "LD"};
		
		foreach(string sceneBundleName in sceneList)
			DownloadManager.Instance.StartDownload(sceneBundleName + ".assetBundle");
		
		if(!DownloadManager.Instance.IsUrlRequested("LA.assetBundle"))
			Debug.LogError("TEST:IsUrlRequested() ERORR");
		else
			Debug.Log("TEST:IsUrlRequested() test finished.");
		
		bool sceneBundleSucceed = false;
		do
		{
			sceneBundleSucceed = true;
			foreach(string sceneBundleName in sceneList)
			{
				if(DownloadManager.Instance.GetWWW(sceneBundleName + ".assetBundle") == null)
					sceneBundleSucceed = false;
			}
			
			List<string> sceneBundles = new List<string>();
			foreach(string sceneBundleName in sceneList)
				sceneBundles.Add(sceneBundleName + ".assetBundle");
			float progressOfScenes = DownloadManager.Instance.ProgressOfBundles(sceneBundles.ToArray());
			Debug.Log("scenes' Progress " + progressOfScenes);
			
			yield return null;
			
		}while(!sceneBundleSucceed);
		
		Debug.Log("TEST:StartDownload() test finished.");
		
		// Test WaitDownload
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("LE.assetBundle") );
		Debug.Log("TEST:WaitDownload() test finished.");
		
		string errMsg = DownloadManager.Instance.GetError("LE.assetBundle");
		if(errMsg == null)
			Debug.LogError("TEST:GetError() ERORR");
		else
			Debug.Log("TEST:GetError() test finished.");
		
		// Test DisposeWWW()
		foreach(string sceneBundleName in sceneList)
			DownloadManager.Instance.DisposeWWW(sceneBundleName + ".assetBundle");
		
		if(DownloadManager.Instance.GetWWW("LA.assetBundle") != null)
			Debug.LogError("TEST:DisposeWWW() ERORR");
		else
			Debug.Log("TEST:DisposeWWW() test finished.");
		
		// Test StopAll()
		foreach(string sceneBundleName in sceneList)
			DownloadManager.Instance.StartDownload(sceneBundleName + ".assetBundle");
		DownloadManager.Instance.StopAll();
		
		yield return new WaitForSeconds(2f);
		if(DownloadManager.Instance.GetWWW("LA.assetBundle") != null)
			Debug.LogError("TEST:StopAll() ERORR");
		else
			Debug.Log("TEST:StopAll() test finished.");
		
		// Test scene bundles based on scene bundles
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("sub/LA-A.assetBundle") );
		#pragma warning disable 0168
		var sceneBundle = DownloadManager.Instance.GetWWW("sub/LA-A.assetBundle").assetBundle;
		#pragma warning restore 0168
		Application.LoadLevel("LA-A");
		yield return null;
		if(GameObject.Find("LA-AWorker") == null)
			Debug.LogError("TEST:Load scene bundle based on scene bundle ERORR ");
		else
			Debug.Log("TEST Load scene bundle based on scene bundle finished.");
		
		// Test asset bundles based on scene bundles
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("AB-A.assetBundle") );
#if UNITY_5
		var workerObj = DownloadManager.Instance.GetWWW("AB-A.assetBundle").assetBundle.LoadAsset<GameObject>("worker");
#else
		var workerObj = DownloadManager.Instance.GetWWW("AB-A.assetBundle").assetBundle.Load("worker");
#endif
		if(workerObj == null)
			Debug.LogError("TEST:Load asset bundle based on scene bundle ERORR");
		else
			Debug.Log("TEST Load asset bundle based on scene bundle finished.");
		
		// Test scene bundles based on asset bundles
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("LB-A.assetBundle") );
		#pragma warning disable 0168
		sceneBundle = DownloadManager.Instance.GetWWW("LB-A.assetBundle").assetBundle;
		#pragma warning restore 0168
		Application.LoadLevel("LB-A");
		yield return null;
		if(GameObject.Find("LB-AWorker") == null)
			Debug.LogError("TEST:Load scene bundle based on asset bundle ERORR ");
		else
			Debug.Log("TEST Load scene bundle based on asset bundle finished.");
		
		// Test asset bundles based on asset bundles
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("AA-A.assetBundle") );
#if UNITY_5
		workerObj = DownloadManager.Instance.GetWWW("AA-A.assetBundle").assetBundle.LoadAsset<GameObject>("worker");
#else
		workerObj = DownloadManager.Instance.GetWWW("AA-A.assetBundle").assetBundle.Load("worker");
#endif		
		if(workerObj == null)
			Debug.LogError("TEST:Load asset bundle based on asset bundle ERORR");
		else
			Debug.Log("TEST Load asset bundle based on asset bundle finished.");
		
		// Test Load deep dependend tree
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("LAAAAA.assetBundle") );
		#pragma warning disable 0168
		sceneBundle = DownloadManager.Instance.GetWWW("LAAAAA.assetBundle").assetBundle;
		#pragma warning restore 0168
		Application.LoadLevel("LAAAAA");
		yield return null;
		if(GameObject.Find("LAAAAAWorker") == null)
			Debug.LogError("TEST:Load deep dependend tree ERORR ");
		else
			Debug.Log("TEST Load deep dependend tree finished.");
		
		
		// Test two bundle based on one bundle
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("ACA.assetBundle") );
		yield return StartCoroutine( DownloadManager.Instance.WaitDownload("ACB.assetBundle") );
#if UNITY_5
		var cube = DownloadManager.Instance.GetWWW("ACA.assetBundle").assetBundle.LoadAsset<UnityEngine.Object>("Cube");
		var sphere = DownloadManager.Instance.GetWWW("ACB.assetBundle").assetBundle.LoadAsset<UnityEngine.Object>("Sphere");
#else
		var cube = DownloadManager.Instance.GetWWW("ACA.assetBundle").assetBundle.Load("Cube");
		var sphere = DownloadManager.Instance.GetWWW("ACB.assetBundle").assetBundle.Load("Sphere");
#endif
		if(cube == null || sphere == null)
			Debug.LogError("TEST: two bundle based on one bundle ERORR");
		else
			Debug.Log("TEST two bundle based on one bundle finished");


		// Test Built Bundles
		if(DownloadManager.Instance.ConfigLoaded)
		{
			var bundles = DownloadManager.Instance.BuiltBundles;
			if(bundles != null && bundles.Length > 0)
				Debug.Log("TEST BuiltBundles finished");				
			else
				Debug.LogError("TEST: BuiltBundles ERORR");
		}

		Debug.Log("TEST finished");
	}
}
