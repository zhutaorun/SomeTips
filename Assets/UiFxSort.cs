using System;
using UnityEngine;
using System.Collections;

public class UiFxSort : MonoBehaviour
{

    internal UIPanel UiPanel;
    public UIWidget UiWidgetInfrontOfMe;
    public bool AddQueue = true;

    private Renderer[] _renderers;

    private void Awake()
    {
        _renderers = this.GetComponentsInChildren<Renderer>(true);
    }

    public void InitPanel(UIPanel p)
    {
        UiPanel = p;
    }

    private void Start()
    {
        UiPanel = NGUITools.FindInParents<UIPanel>(gameObject);
        UiPanel.AddUiSort(this);
    }

    public void UpdateSortUi()
    {
        if (UiWidgetInfrontOfMe != null && UiWidgetInfrontOfMe.drawCall != null)
        {
            int rq = UiWidgetInfrontOfMe.drawCall.renderQueue + 1;
            if (!AddQueue) rq -= 2;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer _renderer = _renderers[i];
                if(_renderer==null) return;
                if(_renderer.materials ==null) return;
                if(_renderer.materials.Length==0) return;

                foreach (Material mat in _renderer.materials)
                {
                    if (mat.renderQueue != rq)
                    {
                        mat.renderQueue = rq;
                    }
                }
            }
        }
    }

    private void OnDestory()
    {
        if (UiPanel != null) UiPanel.RemoveUiSort(this);
    }
}
