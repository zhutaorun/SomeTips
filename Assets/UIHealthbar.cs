using UnityEngine;

public class UIHealthbar : MonoBehaviour
{
    private UISprite _bkSprite;
    private UISlider _normal;
    private UISlider _normalDmg;
    private UISprite _barSprite;
    private const float AnimDuration = 0.6f;
    private const iTween.EaseType EaseType = iTween.EaseType.linear;
    private bool _isFading;
    private float _timer;

    public bool autoHide = true;

    public int _barCount = 1;

    private float _totalPercent = 1.0f;

    private float _tmpValue = 0.0f;

    public Color[] _barColor = new Color[] { Color.cyan, Color.green, Color.gray, Color.yellow, Color.red, Color.blue };

    private int _barIndex = 0;

    private bool IsVisible
    {
        get { return _barSprite.color.a >= 1; }
        set
        {
            _timer = 0;
            if (value != IsVisible && !_isFading)
            {
                _isFading = true; 
                if (value)
                {
                    iTween.ValueTo(gameObject,
                        iTween.Hash("from", 0, "to", 1, "time", AnimDuration, "easetype",
                            EaseType, "onupdate", "OnFadeIn",
                            "onupdatetarget", gameObject, "oncomplete", "OnFadeInComplete", "oncompletetarget", gameObject));
                }
                else
                {
                    iTween.ValueTo(gameObject,
                        iTween.Hash("from", 1, "to", 0, "time", AnimDuration, "easetype",
                            EaseType, "onupdate", "OnFadeOut",
                            "onupdatetarget", gameObject, "oncomplete", "OnFadeOutComplete", "oncompletetarget", gameObject));
                }
            }
        }
    }

    private void Awake()
    {
        _bkSprite = transform.FindChild("background").GetComponent<UISprite>();
        _normal = transform.FindChild("Normal").GetComponent<UISlider>();
        _normalDmg = transform.FindChild("NormalDmg").GetComponent<UISlider>();
        _barSprite = transform.GetComponent<UISprite>();

        if (autoHide)
        {
            _barSprite.color = new Color(1, 1, 1, 0);
        }

        _totalPercent *= _barCount;

        if (_bkSprite)
        {
            _bkSprite.color = _barColor[_barIndex];

            if (_barCount == 1)
            {
                _bkSprite.gameObject.SetActive(false);
            }
        }
    }

    void OnEnable()
    {
        IsNormalDamaging = false;
    }
    private void Update()
    {
        if (autoHide && !_isFading && IsVisible)
        {
            _timer += Time.deltaTime;
            if (_timer > 5f)
            {
                IsVisible = false;
            }
        }
    }

    private bool IsNormalDamaging
    {
        get { return _normalDmg.gameObject.activeSelf; }
        set { _normalDmg.gameObject.SetActive(value); }
    }

    public bool IsDamaging
    {
        get { return IsNormalDamaging; }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(310, 10, 200, 100), "Hit - 30%"))
        {
            AddDamage(0.6f);
        }
    }

    public float AddDamage(float percent)
    {
        if (!IsVisible)
        {
            IsVisible = true;
        }
        if (percent > 1f)
        {
            return _normal.value;
        }
        if (_normal.value <= 0f)
        {
            return 0;
        }

        _normalDmg.value = _normal.value;
        float tmp = _normal.value;
        tmp -= percent;
        IsNormalDamaging = false;

        bool nextBar = false;
        if (tmp <= 0)
        {
            _barCount--;
            if (_barCount == 0)
            {
                _normal.value = 0;
            }
            else
            {
                nextBar = true;
                _normal.value = tmp + 1;
            }
        }
        else
        {
            _normal.value = tmp;
        }

        if (!nextBar)
        {
            iTween tween = gameObject.GetComponent<iTween>();
            if (tween)
            {
                Destroy(tween);
            }


            iTween.ValueTo(gameObject, iTween.Hash("from", _normalDmg.value, "to", _normal.value, "time", AnimDuration, "easetype", EaseType,
           "onupdate", "OnNormalDamage",
           "onupdatetarget", gameObject, "oncomplete", "NormalDamageDone", "oncompletetarget", gameObject));

            return _normal.value;
        }
        else
        {
            iTween tween = gameObject.GetComponent<iTween>();
            if (tween)
            {
                Destroy(tween);
            }
            iTween.ValueTo(gameObject, iTween.Hash("from", _normalDmg.value, "to", 0, "time", AnimDuration, "easetype", EaseType,
           "onupdate", "OnNormalDamage",
           "onupdatetarget", gameObject, "oncomplete", "PreNormalDamageDone", "oncompletetarget", gameObject));

            _tmpValue = _normal.value;

            _normal.value = 0;
        }

        return _normal.value;
    }

    private void PreNormalDamageDone()
    {
        ((UISprite)_normal.foregroundWidget).color = _barColor[_barIndex % _barColor.Length];
        _barIndex++;
        _bkSprite.color = _barColor[_barIndex % _barColor.Length];

        if (_barCount == 1)
        {
            _bkSprite.gameObject.SetActive(false);
        }

        _normalDmg.value = 1;
        _normal.value = _tmpValue;

        iTween tween = gameObject.GetComponent<iTween>();
        if (tween)
        {
            Destroy(tween);
        }


        iTween.ValueTo(gameObject, iTween.Hash("from", 1, "to", _normal.value, "time", AnimDuration, "easetype", EaseType,
           "onupdate", "OnNormalDamage",
           "onupdatetarget", gameObject, "oncomplete", "NormalDamageDone", "oncompletetarget", gameObject));

    }



    private void OnNormalDamage(float value)
    {
        _normalDmg.value = value;
    }

    private void NormalDamageDone()
    {
        IsNormalDamaging = false;
    }

    private void OnFadeIn(float value)
    {
        _barSprite.color = new Color(1, 1, 1, value);
    }

    private void OnFadeInComplete()
    {
        _isFading = false;
        _timer = 0;
    }

    private void OnFadeOut(float value)
    {
        _barSprite.color = new Color(1, 1, 1, value);
    }

    private void OnFadeOutComplete()
    {
        _isFading = false;
        _timer = 0;
    }
}