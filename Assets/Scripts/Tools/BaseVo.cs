using System.Security;
using UnityEngine;
using System.Collections;

public class BaseVo : XmlVo
{
    public int id;

    public BaseVo(SecurityElement xml)
        : base(xml)
    {

    }

    public BaseVo()
    {
    }

    public virtual void reset(SecurityElement xml)
    {

    }
}
