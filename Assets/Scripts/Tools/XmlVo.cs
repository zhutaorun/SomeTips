using System;
using System.Reflection;
using System.Security;
using UnityEngine;
using System.Collections;

public class XmlVo
{
    private SecurityElement _xmlNode;

    public XmlVo()
    {
    }

    public XmlVo(SecurityElement xmlNode)
    {
        _xmlNode = xmlNode;
        fromXml(_xmlNode);
    }

    public virtual void fromXml(SecurityElement xmlNode)
    {
        _xmlNode = xmlNode;
        FieldInfo[] fields = this.GetType().GetFields();
        FieldInfo field = null;
        for (int i = 0; i < fields.Length; i++)
        {
            field = fields[i];
            if (field.FieldType == typeof (int))
            {
                field.SetValue(this,getInt(field.Name));
            }
            else if (field.FieldType == typeof(uint))
            {
                field.SetValue(this, getUint(field.Name));
            }
            else if (field.FieldType == typeof (float))
            {
                field.SetValue(this,getfloat(field.Name));
            }
            else if (field.FieldType == typeof(bool))
            {
                field.SetValue(this, getBool(field.Name));
            }
            else if (field.FieldType == typeof(string))
            {
                field.SetValue(this, getString(field.Name));
            }
        }
    }

    protected int getInt(String field)
    {
        string attr = _xmlNode.Attribute(field);
        if (!String.IsNullOrEmpty(attr))
            return int.Parse(attr);
        else
            return 0;
    }

    protected uint getUint(String field)
    {
        string attr = _xmlNode.Attribute(field);
        if (!String.IsNullOrEmpty(attr))
            return uint.Parse(attr);
        else
            return 0;
    }

    protected float getfloat(String field)
    {
        string attr = _xmlNode.Attribute(field);
        if (!String.IsNullOrEmpty(attr))
            return float.Parse(attr);
        else
            return 0;
    }
    protected bool getBool(String field)
    {
        string attr = _xmlNode.Attribute(field);
        if (!String.IsNullOrEmpty(attr))
            return bool.Parse(attr);
        else
            return false;
    }
    protected string getString(String field)
    {
        string attr = _xmlNode.Attribute(field);
        if (!String.IsNullOrEmpty(attr))
            return attr;
        else
            return "";
    }

    public void dispose()
    {
        _xmlNode = null;   
    }
}
