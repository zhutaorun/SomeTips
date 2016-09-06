using System.IO;
using UnityEngine;
using System.Collections;

public class ConfigDataVo
{
    public XMLLoader.XmlLoaded loadHandler { get; private set; }

    public string url { get; private set; }

    public string assetName { get; private set; }

    public ConfigDataVo(XMLLoader.XmlLoaded loadHandler, string url)
    {
        this.loadHandler = loadHandler;
        this.url = url;

        assetName = Path.GetFileName(url);//根据路径取得文件名
        assetName = Path.GetFileNameWithoutExtension(assetName);//去掉文件后缀
    }

}
