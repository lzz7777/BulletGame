using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 关闭多余射线检测
/// </summary>
public class UIChecker : EditorWindow
{
    // 执行逻辑
    [MenuItem("Tools/UI/关闭选中对象的多余 RaycastTarget", false, 1)]
    public static void DisableSelectedUselessRaycastTargets()
    {
        // 获取当前选中的所有 GameObject（支持 Hierarchy 节点 和 Project 预制体）
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("请先选中需要处理的 UI 节点或预制体！");
            return;
        }

        int changedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            // 获取选中节点及其所有子节点的 Graphic 组件（包含未激活的节点）
            Graphic[] graphics = obj.GetComponentsInChildren<Graphic>(true);

            foreach (var g in graphics)
            {
                if (!g.raycastTarget) continue;

                // 检查自身是否挂载了交互组件
                if (g.GetComponent<Selectable>() != null)
                    continue;
                
                if (g.GetComponent<EventTrigger>() != null)
                    continue;
                
                // 检查是否挂载了实现 UGUI 事件接口的自定义脚本 (如 IPointerClickHandler)
                if (g.GetComponent<IEventSystemHandler>() != null)
                    continue;

                if (g.GetComponent<Mask>() != null || g.GetComponent<RectMask2D>() != null) 
                    continue;
                
                if (g.name == "Mask")
                    continue;
                
                // 如果没有任何交互组件，则关闭 raycastTarget
                // 记录 Undo，允许你在编辑器里 Ctrl+Z 撤销
                Undo.RecordObject(g, "Disable Raycast Target");
                    
                g.raycastTarget = false;
                    
                // 标记为脏数据，确保预制体或场景的修改能被保存
                EditorUtility.SetDirty(g);
                changedCount++;
            }
        }

        // 如果你是在 Project 视图直接选中的 Prefab 资产，需要强制保存一下写入磁盘
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>UI 优化完成！在选中的对象中，共关闭了 {changedCount} 个不必要的 RaycastTarget。</color>");
    }

    // 验证逻辑：只有在选中了 GameObject 时，菜单才可用
    [MenuItem("Tools/UI/关闭选中对象的多余 RaycastTarget", true)]
    public static bool ValidateDisableSelected()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }
}