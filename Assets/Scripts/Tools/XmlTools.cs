using System;
using System.Security;
using UnityEngine;
using System.Collections;

public class XMLTools
{

    public static SecurityElement getFirstXmlNode(ArrayList nodeList, String firstNode)
    {
        if (nodeList != null)
        {
            foreach (SecurityElement subNode in nodeList)
            {
                //只读一个
                if (subNode.Tag == firstNode)
                {
                    return subNode;
                }
            }
        }
        return null;
    }

    public static int getInt(ArrayList xmlList,string nodeName)
    {
        SecurityElement xml = getFirstXmlNode(xmlList, nodeName);
        if (String.IsNullOrEmpty(xml.Text))
            return 0;
        else
            return int.Parse(xml.Text);
    }
    public static uint getUint(ArrayList xmlList, string nodeName)
    {
        SecurityElement xml = getFirstXmlNode(xmlList, nodeName);
        if (String.IsNullOrEmpty(xml.Text))
            return 0;
        else
            return uint.Parse(xml.Text);
    }

    public static float getFloat(ArrayList xmlList, string nodeName)
    {
        SecurityElement xml = getFirstXmlNode(xmlList, nodeName);
        if (String.IsNullOrEmpty(xml.Text))
            return 0;
        else
            return float.Parse(xml.Text);
    }

    public static bool getBool(ArrayList xmlList, string nodeName)
    {
        SecurityElement xml = getFirstXmlNode(xmlList, nodeName);
        if (String.IsNullOrEmpty(xml.Text))
            return false;
        else
            return bool.Parse(xml.Text);
    }

    public static string getString(ArrayList xmlList, string nodeName)
    {
        SecurityElement xml = getFirstXmlNode(xmlList, nodeName);
        if (String.IsNullOrEmpty(xml.Text))
            return "";
        else
            return xml.Text;
    }
}
