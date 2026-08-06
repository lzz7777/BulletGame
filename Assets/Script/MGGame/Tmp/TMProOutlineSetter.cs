using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace XN
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMProOutlineSetter : MonoBehaviour
    {
        [LabelText("描边类型")] public TmproOutlineType mOutlineType;
        [LabelText("描边颜色")] public Color mOutlineColor = Color.black;
        private TextMeshProUGUI mTextMeshProUGUI;
        private Material mInstanceMaterial;

        void Start()
        {
            ApplyOutline();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            ApplyOutline();
        }
#endif

        public void Set(TmproOutlineType type)
        {
            mOutlineType = type;
            ApplyOutline();
        }

        public void Set(TmproOutlineType type, Color color)
        {
            mOutlineType = type;
            mOutlineColor = color;
            ApplyOutline();
        }

        private void ApplyOutline()
        {
            if (mTextMeshProUGUI == null) mTextMeshProUGUI = GetComponent<TextMeshProUGUI>();

            // 获取TextMeshProUGUI的材质
            Material mat = mTextMeshProUGUI.materialForRendering;
            if (mat == null)
            {
                Debug.LogError("TMP材质是空的，无法设置描边效果");
                return;
            }

            var param = TMProOutlineParam.Get(mOutlineType);

            if (mInstanceMaterial == null) mInstanceMaterial = new Material(mat);
            mInstanceMaterial.SetFloat("_FaceDilate", param.FaceDilate);
            mInstanceMaterial.SetFloat("_OutlineSoftness", param.FaceSoftness);
            mInstanceMaterial.SetFloat("_OutlineWidth", param.OutlineThickness);
            mInstanceMaterial.SetColor("_OutlineColor", mOutlineColor);
            mTextMeshProUGUI.material = mInstanceMaterial;
            mTextMeshProUGUI.fontMaterial = mInstanceMaterial;
            mTextMeshProUGUI.fontSharedMaterial = mInstanceMaterial;

            // 强制重新构建文字几何体
            mTextMeshProUGUI.SetVerticesDirty();
            mTextMeshProUGUI.SetMaterialDirty();
        }
    }
}