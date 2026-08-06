#if UNITY_EDITOR
using Spine.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XN
{
    public class UIGeneratorBase : Editor
    {
        public static string GetPrimaryComponentType(Transform trans)
        {
            if (trans.GetComponent<TMP_InputField>() != null) return "TMP_InputField";
            if (trans.GetComponent<TextMeshProUGUI>() != null) return "TextMeshProUGUI";
            if (trans.GetComponent<ToggleGroup>() != null) return "ToggleGroup";
            if (trans.GetComponent<Toggle>() != null) return "Toggle";
            if (trans.GetComponent<Grid>() != null) return "Grid";
            if (trans.GetComponent<GridLayoutGroup>() != null) return "GridLayoutGroup";
            if (trans.GetComponent<HorizontalLayoutGroup>() != null) return "HorizontalLayoutGroup";
            if (trans.GetComponent<VerticalLayoutGroup>() != null) return "VerticalLayoutGroup";
            if (trans.GetComponent<ScrollRect>() != null) return "ScrollRect";
            if (trans.GetComponent<Slider>() != null) return "Slider";
            if (trans.GetComponent<Button>() != null) return "Button";
            if (trans.GetComponent<RawImage>() != null) return "RawImage";
            if (trans.GetComponent<Image>() != null) return "Image";
            if (trans.GetComponent<Text>() != null) return "Text";
            if (trans.GetComponent<SkeletonGraphic>() != null) return "SkeletonGraphic";
            if (trans.GetComponent<TextMeshPro>() != null) return "TextMeshPro";
            if (trans.GetComponent<SpriteRenderer>() != null) return "SpriteRenderer";
            return "RectTransform";
        }

    }
}
#endif