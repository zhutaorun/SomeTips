using System;
using UnityEngine;
using System.Collections.Generic;

public class UiFxSort : MonoBehaviour
{

    internal UIPanel UiPanel;
    public UIWidget UiWidgetInfrontOfMe;
    public bool AddQueue = true;

    private UIPanelMaterialCache _panelMaterialCache;

    private Renderer[] _renderers;
    private List<Material> _cachedMaterials = new List<Material>();


    public void InitPanel(UIPanel p)
    {
        UiPanel = p;
    }

    private void Start()
    {
        ResortRenderers();
    }

    public void UpdateSortUi()
    {
        _cachedMaterials.Clear();
        if (UiWidgetInfrontOfMe != null && UiWidgetInfrontOfMe.drawCall != null)
        {
            int rq = UiWidgetInfrontOfMe.drawCall.renderQueue + 1;
            if (!AddQueue) rq -= 2;

            var clipCount = UiPanel.clipCount;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer _renderer = _renderers[i];
                if(_renderer==null) return;
                var materials = _renderer.sharedMaterials;
                if(materials ==null||  materials.Length==0) continue;

                for (int j = 0; j < materials.Length; j++)
                {
                    var mat = materials[j];
                    if(mat==null)
                        continue;
                    if (clipCount > 0)
                    {
                        mat = _panelMaterialCache.GetClipping(mat, clipCount);
                        _cachedMaterials.Add(mat);
                    }
                    else
                    {
                        mat = _panelMaterialCache.Get(mat);
                    }
                    materials[j] = mat;
                    mat.renderQueue = rq;
                }
                _renderer.materials = materials;
            }
        }
    }

    private void LateUpdate()
    {
        UpdateClipping();
    }


    public void ResortRenderers()
    {
        _renderers = this.GetComponentsInChildren<Renderer>(true);
        UiPanel = NGUITools.FindInParents<UIPanel>(gameObject);
        UiPanel.AddUiSort(this);
        _panelMaterialCache = E3D_Utils.GetOrAddComponent<UIPanelMaterialCache>(UiPanel.gameObject);
        _panelMaterialCache.hideFlags = HideFlags.HideAndDontSave;
    }

    public void Clear()
    {
        _renderers = new Renderer[0];
        _cachedMaterials.Clear();
    }
    private void OnDestory()
    {
        if (UiPanel != null) UiPanel.RemoveUiSort(this);
    }

#region Clipping

    static int[] ClipRange = new int[0];
    static int[] ClipArgs = new int[0];

    private void UpdateClipping()
    {
        if (ClipArgs.Length == 0)
        {
            ClipRange = new[]
            {
                Shader.PropertyToID("_ClipRange0"),
                Shader.PropertyToID("_ClipRange1"),
                Shader.PropertyToID("_ClipRange2"),
                Shader.PropertyToID("_ClipRange4"),

            };
            ClipArgs = new[]
            {
                Shader.PropertyToID("_ClipArgs0"),
                Shader.PropertyToID("_ClipArgs1"),
                Shader.PropertyToID("_ClipArgs2"),
                Shader.PropertyToID("_ClipArgs3"),
            };
        }
        var panel = UiPanel;
        UIPanel currentPanel = panel;

        for(int clipIndex = 0;currentPanel!=null;)
        {
            if (currentPanel.hasClipping)
            {
                float angle = 0f;
                Vector4 cr = currentPanel.drawCallClipRange;
                var worldCorners = currentPanel.worldCorners;
                var worldCenter = new Vector3(worldCorners[0].x + worldCorners[2].x, worldCorners[0].y + worldCorners[1].y)*0.5f;

                worldCenter = currentPanel.root.transform.InverseTransformPoint(worldCenter);
                var h = currentPanel.root.activeHeight;
                var w = (int) (h*Screen.width/Screen.height);

                Vector4 clipSize;
                clipSize.x = worldCenter.x/w;
                clipSize.y = worldCenter.y/h;
                clipSize.z = cr.z/w;
                clipSize.w = cr.w/h;
                clipSize *= 2;

                //Clipping regions past the first one need additional math
                if (currentPanel != panel)
                {
                    Vector3 pos = currentPanel.cachedTransform.InverseTransformPoint(panel.cachedTransform.position);
                    cr.x -= pos.x;
                    cr.y -= pos.y;

                    Vector3 v0 = panel.cachedTransform.rotation.eulerAngles;
                    Vector3 v1 = currentPanel.cachedTransform.rotation.eulerAngles;
                    Vector3 diff = v1 - v0;

                    diff.x = NGUIMath.WrapAngle(diff.x);
                    diff.y = NGUIMath.WrapAngle(diff.y);
                    diff.z = NGUIMath.WrapAngle(diff.z);

                    if(Mathf.Abs(diff.x)>0.001f || Math.Abs(diff.y)>0.001f)
                        Debug.LogWarning("Panel can only be clipped properly if X and Y rotation is left at 0",panel);
                    angle = diff.z;
                }

                //Pass the Clipping parameters to the shader
                for (int i = 0; i < _cachedMaterials.Count; i++)
                {
                    _cachedMaterials[i].SetVector("_ClipSize",clipSize);
                    SetClipping(_cachedMaterials[i], clipIndex, cr, currentPanel.clipSoftness, angle);
                }
                ++clipIndex;
            }
            currentPanel = currentPanel.parentPanel;
        }
        }

    private void SetClipping(Material m, int index, Vector4 cr, Vector2 soft, float angle)
    {
        angle *= Mathf.Deg2Rad;

        Vector2 sharpness = new Vector2(1000.0f,1000.0f);
        if (soft.x > 0f) sharpness.x = cr.z/soft.x;
        if (soft.y > 0f) sharpness.y = cr.w/soft.y;

        if (index < ClipRange.Length)
        {
            var v0 = new Vector4(-cr.x/cr.z, -cr.y/cr.w, 1f/cr.z, 1f/cr.w);
            m.SetVector(ClipRange[index],v0);
            var v1 = new Vector4(sharpness.x, sharpness.y, Mathf.Sin(angle), Mathf.Cos(angle));
            m.SetVector(ClipArgs[index],v1);
        }
    }

    #endregion
}
