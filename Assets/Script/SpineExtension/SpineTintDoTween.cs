//====================================================
//Author:HDS
//Time  :2025/12/25 16:12:28
//Desc  :
//====================================================

using DG.Tweening;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class SpineTintDoTween : MonoBehaviour
{
    private readonly int ColorId = Shader.PropertyToID("_Color");
    private readonly string ShaderName = "Spine/Skeleton Tint";

#if UNITY_EDITOR
    [Sirenix.OdinInspector.LabelText("延迟时间")]
#endif
    public float DelayTime = 0.1f;
#if UNITY_EDITOR
    [Sirenix.OdinInspector.LabelText("恢复时间")]
#endif
    public float RecoverTime = 0.1f;
#if UNITY_EDITOR
    [Sirenix.OdinInspector.LabelText("恢复曲线")]
#endif
    public AnimationCurve RecoverCurve;

#if UNITY_EDITOR
    [Sirenix.OdinInspector.LabelText("闪烁颜色"), ColorUsage(true, true)]
#endif
    public Color BlinkColor;

    private Material mat;
    private Color originalColor;
    private Tweener tweener;

    private void Awake() => UpdateData();

    private void OnDestroy()
    {
        tweener?.Kill();
        this.mat = null;
    }

#if UNITY_EDITOR
    [Sirenix.OdinInspector.Button("闪烁测试")]
    public void TestBlink()
    {
        if (!EditorApplication.isPlaying)
        {
            if (EditorUtility.DisplayDialog("提示", "需要运行状态下测试，是否运行？", "是", "否"))
            {
                EditorApplication.isPlaying = true;
            }
            else return;
        }

        Blink();
    }
#endif
    public void Blink()
    {
        this.tweener.Restart();
    }

#if UNITY_EDITOR
    [Sirenix.OdinInspector.Button("刷新数据")]
#endif
    public void UpdateData()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        var mats = GetComponentInChildren<MeshRenderer>().materials;
        foreach (var item in mats)
        {
            if (item.shader.name.Equals(this.ShaderName))
            {
                this.mat = item;
                this.originalColor = this.mat.GetColor(ColorId);
                this.tweener = DOTween.To(() => mat.GetColor(ColorId), p => this.mat.SetColor(ColorId, p), BlinkColor, RecoverTime).SetDelay(DelayTime).SetEase(RecoverCurve).From(this.originalColor).SetAutoKill(false).Pause();
                this.mat.SetColor(ColorId, this.originalColor);
                break;
            }
        }
    }
}