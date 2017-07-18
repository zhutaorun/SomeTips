using UnityEngine;
using System.Collections.Generic;

public class UIPanelMaterialCache : MonoBehaviour 
{
    private Dictionary<string,Material> _Dict = new Dictionary<string,Material>();

    public Material Get(Material material)
    {
        if (material == null)
            return null;
        if (material.name.Contains("(Cache)")) return material;
        var key = material.GetHashCode().ToString();
        if (!_Dict.ContainsKey(key))
        {
            var m = Object.Instantiate(material) as Material;
            m.name += "(Cache)";
            _Dict[key] = m;
        }
        return _Dict[key];
    }

    public Material GetClipping(Material material, int clipCount)
    {
        if (material.name.Contains("(ClippingCache)")) return material;
        var key = string.Format("{0}Clipping{1}",material.GetHashCode(),clipCount);
        if (!_Dict.ContainsKey(key))
        {
            var mat = Object.Instantiate(material) as Material;
            var shaderName = mat.shader.name + " " + clipCount;
            mat.shader = Shader.Find(shaderName);
            mat.name += "(ClippingCache)";
            _Dict[key] = mat;
        }
        return _Dict[key];
    }

}
