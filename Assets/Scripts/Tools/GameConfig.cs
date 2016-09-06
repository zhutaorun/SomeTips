using UnityEngine;
using System.Collections;

/// <summary>
/// 游戏杂项配置
/// </summary>
public class GameConfig
{
    private static GameConfig s_instance;

    public static GameConfig ins
    {
        get
        {
            if (s_instance == null)
                s_instance = new GameConfig();
            return s_instance;
        }
    }

    /// <summary>
    /// 宠物最高等级
    /// </summary>
    public int PetMaxLevel { get; private set; }

   

    public void onXmlConifgLoaded(ArrayList xmlList)
    {
        PetMaxLevel = XMLTools.getInt(xmlList, "PetMaxLevel");
    }
}
