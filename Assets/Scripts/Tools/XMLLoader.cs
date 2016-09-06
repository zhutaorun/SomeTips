using System.Security;
using UnityEngine;
using System;
using System.Collections;
using global.loader;
using Mono.Xml;
public class XMLLoader : BaseLoader
{
    public delegate void XmlLoaded(ArrayList xmlList);


    public XmlLoaded onXmlLoaded;

    public float progress { get { return _loader != null ? _loader.progress : 0; } }

    private WWW _loader;

    private string _xml;

    public XMLLoader(XmlLoaded onConfigLoaded, String url) : base(url)
    {
        this.onXmlLoaded = onConfigLoaded;
    }

    public override void loadSync()
    {

    }

    public override void loadASync(MonoBehaviour host)
    {
        String path = PathUtils.GetStreamFilePath(url);
        Debug.LogError("异步加载xml开始："+path);
        host.StartCoroutine(starAsyncLoad(path));
    }

    public IEnumerator doLoadAsync(MonoBehaviour host)
    {
        String path = PathUtils.GetStreamFilePath(url);
        Debug.LogError("异步加载xml开始:"+path);
        yield return host.StartCoroutine(starAsyncLoad(path));
    }

    public override IEnumerator starAsyncLoad(String path)
    {
        _loader = new WWW(path);
        yield return _loader;

        if (String.IsNullOrEmpty(_loader.error))
        {
            SecurityParser sp = new SecurityParser();
            sp.LoadXml(_loader.text);
            SecurityElement se = sp.ToXml();
            if (onXmlLoaded != null)
                onXmlLoaded(se.Children);
            if (onLoader != null)
                onLoader();
            _loader.Dispose();
        }
        else
        {
            if(_loader!=null)
                Debug.LogError("异步加载xml失败"+_loader.error+":"+path);
        }
    }
}
