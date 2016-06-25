using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;

public class UIEffectSpawner : MonoBehaviour
{
    public GameObject Prefab;

    void Awake()
    {
        if (Prefab != null)
        {
            var go = Instantiate(Prefab) as GameObject;
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
        }
    }
}
