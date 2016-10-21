using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UiFxSort))]
public class UIEffectLoader : MonoBehaviour 
{
    BundleReference _effectBundle;
    GameObject _effectGO;
    string _pending;
    UiFxSort _fxSort;

    void Awake()
    {
        _fxSort = GetComponent<UiFxSort>();
    }

    public void loadEffect(string effectName)
    {
        _pending = effectName;
        loadPending();
    }

    public void cleanup()
    {
        if (_fxSort != null) _fxSort.Clear();
        Destroy(_effectBundle);
        Destroy(_effectGO);
    }

    void loadPending()
    {
        cleanup();
        if (_pending != null && isActiveAndEnabled)
        {
            StopAllCoroutines();
            Client.Ins.StartCoroutione(loadCo(_pending));
            _pending = null;
        }
    }

    IEnumerator loadCo(string effectName)
    {
        yield return null;
        var url = PathUrl.GetUIEffectUrl(effectName);
        _effectBundle = BundleReference.Get(url, this);
        if (!_effectBundle.contains(effectName))
        {
            _fxSort.enabled = false;
            yield break;
        }
        var prefab = _effectBundle.load<GameObject>(effectName);
        if (prefab == null) yield break;
        _effectGO = Instantiate(prefab) as GameObject;
        var t = _effectGO.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
        _fxSort.enabled = true;
        _fxSort.ResortRenderers();
    }

    void OnEnable()
    {
        if (!string.IsNullOrEmpty(_pending))
            loadPending();
    }

    void OnDestroy()
    {
        cleanup();
    }
}
