using UnityEngine;
using System.Collections;

public class E3D_Utils
{
    public delegate bool TransformOprFunc(Transform t, int level);


    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T r = go.GetComponent<T>();
        if (r == null)
            r = go.AddComponent<T>();
        return r;
    }
}
