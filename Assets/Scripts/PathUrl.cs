using UnityEngine;
using System.Collections;

/// <summary>
/// 路径
/// </summary>
public class PathUrl
{
    /// <summary>
    /// UI路径
    /// </summary>
    public static string GetUIUrl(string fileName)
    {
        return string.Format("UI/{0}", fileName);
    }

    /// <summary>
    /// UI节点树Prefab
    /// </summary>
    public static string GetUIHullUrl(string viewGroupName)
    {
        return string.Format("UI/UIHull_{0}", viewGroupName);
    }

    /// <summary>
    /// UI特效
    /// </summary>
    public static string GetUIEffectUrl(string effectName)
    {
        return string.Format("UIEffect/{0}", effectName);
    }

    /// <summary>
    /// UITexture
    /// </summary>
    public static string GetUITextureUrl(string fileName)
    {
        return string.Format("UITexture/{0}", fileName);
    }

    public static string GetAtlasUrl(string atlasName)
    {
        return string.Format("Atlas/{0}", atlasName);
    }

    /// <summary>
    /// 场景路径地址; {0}替换成场景名
    /// </summary>
    public static string GetSceneLevelUrl(string fileName)
    {
        return string.Format("Level/{1}", Application.dataPath, fileName);
    }
    /// <summary>
    /// 人物、NPC、怪物模型路径
    /// </summary>
    public static string GetAvatarUrl(string fileName)
    {
        return string.Format("Character/{1}", Application.dataPath, fileName);
    }
    /// <summary>
    /// 特效路径
    /// </summary>
    public static string GetEffectUrl(string fileName)
    {
        return string.Format("Effect/{1}", Application.dataPath, fileName);
    }

    /// <summary>
    /// 模型资源路径
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetModelUrl(string name)
    {
        return string.Format("Model/{1}", Application.dataPath, name);
    }

    /// <summary>
    /// 声音路径
    /// </summary>
    public static string GetSoundUrl(string name)
    {
        var ext = System.IO.Path.GetExtension(name);
        var str = name.Replace(ext, "");
        return string.Format("{1}", Application.dataPath, str);
    }
    /// <summary>
    /// 获得材质球路径
    /// </summary>
    public static string GetMaterialsUrl(string name)
    {
        return string.Format("Materials/{1}", Application.dataPath, name);
    }
    /// <summary>
    /// 获得动作路径
    /// </summary>
    public static string GetAnimationUrl(string name)
    {
        return string.Format("Animation/{1}", Application.dataPath, name);
    }
}