using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BundleLoadManager : MonoBehaviour 
{
    static BundleLoadManager s_instance = null;
    public static BundleLoadManager Instance
    {
        get 
        {
            if (s_instance == null)
            {
                s_instance = E3D_Utils.GetOrAddComponent<BundleLoadManager>(DownloadManager.Instance.gameObject);
                s_instance.StartCoroutine(s_instance.submitPhoneInfo());
            }
            return s_instance;
        }
    }

    private Dictionary<string, Bundle> dict = new Dictionary<string, Bundle>();


    public BundleReference preloadBundle(string assetBundleName, MonoBehaviour whoIsReferencingMe)
    {
        if (!dict.ContainsKey(assetBundleName))
        {
            var bundle = new Bundle();
            StartCoroutine(bundle.startLoadBundleCo(assetBundleName));
            dict[assetBundleName] = bundle;
        }

        var b = dict[assetBundleName];
        var r = ScriptableObject.CreateInstance<BundleReference>();
        r._bundle = b;
        r._dbgAssetBundleName = b.assetBundleName;
        r._whoIsReferencingMe = whoIsReferencingMe;
        r.name = whoIsReferencingMe.name;
        b.references.Add(r);
        return r;
    }

    public void notifyRelease(BundleReference r, Bundle b)
    {
        b.references.Remove(r);
        if (b.references.Count <= 0)
        {
            dict.Remove(b.assetBundleName);
            b.destroy();
        }
    }

    public IEnumerable<Bundle> EDITOR_ForEachBundles()
    {
        foreach (var pair in dict)
        {
            yield return pair.Value;
        }
    }

    IEnumerator submitPhoneInfo()
    {
        var b = PlayerPrefs.GetInt("_SUBMIT_PHONE_INFO", 0);
        if (b != 0) yield break;
        //do { yield return null; } while (!(Loader.CurrentDomain is LoadingPve));
        var url = "http://phone7.herokuapp.com";
        PlayerPrefs.SetInt("_SUBMIT_PHONE_INFO",1);
    }

    public class Bundle
    {
        public string assetBundleName { get;private set; }

        public string error { get; private set; }
        public bool isDone { get; private set; }
        public bool isSuccess { get; private set; }
        public bool isUnloaded { get; private set; }
        public float progress { get { return DownloadManager.Instance.ProgressOfBundle(assetBundleName); } }

        public AssetBundle assetBundle;

        internal List<Object> objects = new List<Object>();
        internal List<BundleReference> references = new List<BundleReference>();

        public System.Collections.IEnumerator startLoadBundleCo(string assetBundleName)
        {
            this.assetBundleName = assetBundleName;

            var mgr = DownloadManager.Instance;
            mgr.StartDownload(assetBundleName);
            assetBundle = null;
            do
            {
                if(this==null)
                    yield break;
                assetBundle =mgr.GetAssetBundle(assetBundleName);
                //下载已完成，退出等待循环
                if(assetBundle!=null) break;
                var error = mgr.GetError(assetBundleName);
                if(error!=null)
                {
                    //下载Bundle的过程中产生了WWW错误，返回失败.
                    this.error = error;
                #if UNITY_EDITOR 
                    Debug.LogError("加载AsseyBundle("+assetBundleName +")失败："+error);
                #else   
                    GWDebug.LogError("Load AssetBundle("+assetBundleName+")Failed:"+error);
                #endif
                    mgr.DisposeBundle(assetBundleName);
                    this.isDone = true;
                    yield break;
                }
                else if (mgr.GetBundleInfo(assetBundleName) == null)
                {
                    //这种情况是Bundle列表里没有要请求的Bundle，返回失败
                    this.error = "Requested bundle is not in bundle list.";
                    this.isDone = true;
                    yield break;
                }
                yield return null;
            }while(true);
            this.isSuccess = true;
            isDone = true;
        }

        public bool contains(string resourceName)
        {
            if (!isDone)
            { 
        #if UNITY_EDITOR
                Debug.LogError("无法加载资源, Bundle(" + assetBundleName + ")还没有加载完成.");    
        #else
                Debug.LogError("Cannot load resource from BundleReference(" + assetBundleName + ") because it has not been completely loaded.");
        #endif
                return false;
            }
            if (!isSuccess)
            { 
#if UNITY_EDITOR
                Debug.LogError("无法加载资源, 因为Bundle(" + assetBundleName + ")有错误: " + error);
#else
                Debug.LogError("Cannot load resource from BundleReference("+assetBundleName+") because it has an error:"+error);
#endif
                return false;
            }
            if (isUnloaded)
            {
                //从缓存中搜寻资源
                for (int i = 0; i < objects.Count; i++)
                {
                    var _obj = objects[i];
                    var objType = _obj.GetType();
                    if (_obj.name == resourceName /* && (objType==type || objType.IsSubclassOf(type))*/) return true;
                }
                return false;
            }
            else
            {
                return assetBundle.Contains(resourceName);
            }
        }

        public Object load(string resourceName, System.Type type)
        {
            var obj = loadChecking(resourceName, type);
            if (obj != BundleLoadManager.Instance) return obj;
            
            //从AssetBundle中加载资源并且缓存起来
            obj= assetBundle.LoadAsset(resourceName, type);
            if (obj == null)
            {
#if UNITY_EDITOR
                Debug.LogError("加载资源失败，AssetBundle("+assetBundle+")中并不包含资源："+resourceName+"("+type.Name+")");
#else
                Debug.LogError("Load resource:"+resourceName +"("+type.Name+") from AssetBundle("+assetBundleName+") got a null value");
#endif
                return null;
            }
            objects.Add(obj);
            return obj;
        }

        public IEnumerator preloadAsync(string resourceName, System.Type type)
        {
            var obj = loadChecking(resourceName, type);
            if(obj!=BundleLoadManager.Instance) yield break;

            //从AssetBundle中加载资源并且缓存起来
            var _asyncOp = assetBundle.LoadAssetAsync(resourceName, type);
            yield return _asyncOp;
            if(assetBundle==null) yield break;
            obj = _asyncOp.asset;
            if (obj == null)
            {
#if UNITY_EDITOR
                Debug.LogError("加载资源失败，AssetBundle("+assetBundle+")中并不包含资源："+resourceName+"("+type.Name+")");
#else
                Debug.LogError("Load resource:"+resourceName+"("+type.Name+") from AssetBundle("+assetBundle+")got a null value");
#endif
                yield break;
            }
            objects.Add(obj);
        }

        //加载BundleResource的检查.
        // - 返回BundleLoadManager.Instance则为没有错误
        // - 返回空为有错误
        // - 返回其他对象是从缓存中得到了已经加载好的资源
        private Object loadChecking(string resourceName, System.Type type)
        {
            if (!isDone)
            {
#if UNITY_EDITOR
                Debug.LogError("无法加载资源，Bundle("+assetBundleName+")还没有加载完成.");
#else
                Debug.LogError("Cannot load resource from BundleReference("+assetBundleName+") because it has not been completely loaded.");
#endif
                return null;
            }
            if (!isSuccess)
            {
#if UNITY_EDITOR
                Debug.LogError("无法加载资源，因为Bundle("+assetBundleName+")有错误："+error);
#else
                Debug.LogError("Cannnot load resource from BundleReference("+assetBundleName+")because it has an error:"+error);
#endif
                return null;
            }

            //从缓存中搜寻资源
            for (int i = 0; i < objects.Count; i++)
            {
                var _obj = objects[i];
                var objType = _obj.GetType();
                if(_obj.name==resourceName && (objType==type || objType.IsSubclassOf(type)))return _obj;
            }
            //如果缓存中不存在，并且AssetBundle已经被卸载，则不能加载新资源
            if (isUnloaded)
            {
#if UNITY_EDITOR
                Debug.LogError("错误的操作！你无法在卸载一个BundleReference之后还试图从它里面加载资源！Bundle("+assetBundle+")");
#else
                Debug.LogError("Invalid opertation! You cannot load a resource from a BundleReference("+ assetBundle+") after it was unloaded.");
#endif
                return null;
            }
            return BundleLoadManager.Instance;
        }

        public void unloadBundle(bool guaranteeFutureLoading)
        {
            if (assetBundle)
            {
                isUnloaded = true;
                //如果参数为true则预加载所有资源
                if (guaranteeFutureLoading)
                {
                    objects = new List<Object>(assetBundle.LoadAllAssets());
                }
                //卸载AssetBundle(仅Bundle部分)
                DownloadManager.Instance.DisposeBundle(assetBundleName);
            }
        }

        public void destroy()
        {
            if (!isDone)
            {
                DownloadManager.Instance.StopDownload(assetBundleName);
            }
            //显示卸载所有已缓存的资源
            foreach (var obj in objects)
            {
                if(obj is GameObject || obj is Component || obj is ScriptableObject) continue;
                Resources.UnloadAsset(obj);
            }
            objects.Clear();
            //如果AssetBundle未被释放则完全释放AssetBundle
            if(assetBundle)
                assetBundle.Unload(true);
            DownloadManager.Instance.DisposeBundle(assetBundleName);
        }
    }
}
