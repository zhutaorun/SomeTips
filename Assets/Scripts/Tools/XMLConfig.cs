using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class XMLConfig
{
    private static List<ConfigDataVo> _list;

    private static void init()
    {
        _list = new List<ConfigDataVo>();
        _list.Add(new ConfigDataVo(onGlobalConfigLoaded, "config/global.xml"));
    }


    public static IEnumerator loadAll()
    {
        init();
        for (int i = 0; i < _list.Count; i++)
        {
            ConfigDataVo cfgVo = _list[i];
            XMLLoader loader = new XMLLoader(cfgVo.loadHandler,cfgVo.url);
            yield return Client.ins.StartCoroutine(loader.doLoadAsync(Client.ins));
        }
        do
        {
            yield return null;
        } while (GameConfig.ins.PetMaxLevel != 0);
        //_list.clear();

    }

    private static void onGlobalConfigLoaded(ArrayList xmlList)
    {
        GameConfig.ins.onXmlConifgLoaded(xmlList);
    }
}
