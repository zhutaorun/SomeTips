using System;
using UnityEngine;
using System.Collections;

public class StringTools
{
    public static bool CalaculateChineseWord(string str,SimplifiedChinese charset)
    {
        for (int i = 0; i < str.Length; i++)
        {
            if (!isCh_sm(str[i], charset))
            {
                return false;
            }
        }
        return true;
    }

    public static bool isCh_sm(char ch, SimplifiedChinese charset)
    {
        uint c = (uint) ch;
        if (Array.IndexOf(charset.UnicodeSc, c) != -1)
            return true;
        return false;
    }
}
