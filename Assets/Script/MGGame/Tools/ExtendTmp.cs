using TMPro;
using UnityEngine;

public class ExtendTmp : MonoBehaviour
{
    public Color color = Color.clear;
    public float outlineWidth = 0.28f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextMeshProUGUI tmp = gameObject.GetComponent<TextMeshProUGUI>();
        tmp.outlineColor = color;
        tmp.outlineWidth = outlineWidth;
    }
}