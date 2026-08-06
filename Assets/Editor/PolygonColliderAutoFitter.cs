#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自动适配PolygonCollider2D，用来image精准点击像素
/// </summary>
public static class PolygonColliderAutoFitter
{
    // 在 Image 组件的右键菜单中注入此功能
    [MenuItem("CONTEXT/Image/自动适配 PolygonCollider2D (无GC精确点击)")]
    public static void FitCollider(MenuCommand command)
    {
        Image image = command.context as Image;
        if (image == null || image.sprite == null)
        {
            Debug.LogWarning("自动适配失败：未找到有效的 Image 或 Sprite。");
            return;
        }

        // 获取或添加 PolygonCollider2D
        PolygonCollider2D collider = image.gameObject.GetComponent<PolygonCollider2D>();
        if (collider == null)
        {
            collider = image.gameObject.AddComponent<PolygonCollider2D>();
        }

        Sprite sprite = image.sprite;
        int pathCount = sprite.GetPhysicsShapeCount();
        collider.pathCount = pathCount;

        // 计算缩放比例：RectTransform 尺寸 / 物理世界的 bounds 尺寸
        // 这样可以完美兼容拉伸过大小的 Image
        Rect rect = image.rectTransform.rect;
        Vector2 scale = new Vector2(
            rect.width / sprite.bounds.size.x, 
            rect.height / sprite.bounds.size.y
        );

        List<Vector2> path = new List<Vector2>();

        // 遍历提取 Sprite 的物理轮廓并按比例放大到 UGUI 坐标系
        for (int i = 0; i < pathCount; i++)
        {
            path.Clear();
            sprite.GetPhysicsShape(i, path);
            
            for (int j = 0; j < path.Count; j++)
            {
                // 将物理坐标映射到 UGUI 的局部坐标
                path[j] = new Vector2(path[j].x * scale.x, path[j].y * scale.y);
            }
            collider.SetPath(i, path.ToArray());
        }

        // 设为 Trigger，避免参与实际物理碰撞
        collider.isTrigger = true;
        
        EditorUtility.SetDirty(collider);
        Debug.Log($"<color=green><b>[完美适配]</b></color> 已为 {image.gameObject.name} 生成贴合 UI 尺寸的多边形碰撞体！");
    }
}
#endif