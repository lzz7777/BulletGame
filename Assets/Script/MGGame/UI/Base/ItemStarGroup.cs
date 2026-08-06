using UnityEngine;
using Sirenix.OdinInspector;

namespace XN
{
    /// <summary>
    /// 挂载在 ItemStarList 上，用于控制星星的总个数（BG层）与显示激活个数（FG层）。
    /// - totalCount 控制示例总数（背景占位星星）
    /// - activeCount 控制前景激活的星星数量（显示亮星）
    /// 支持在编辑器和运行时刷新；自动创建单一 Group 容器（缺失时）。
    /// 父节点下放一个示例条目（条目内包含子 BG、FG）；脚本会自动发现并隐藏该示例，
    /// 然后以示例条目为预制体克隆 totalCount 个条目，并仅切换每条目的 FG 显隐以控制 activeCount。
    /// </summary>
    [ExecuteAlways]
    public class ItemStarGroup : MonoBehaviour
    {
        [MinValue(0), OnValueChanged("Refresh"), Tooltip("总星星个数（BG 层占位）")]
        [SerializeField] private int totalCount = 5;

        [MinValue(0), OnValueChanged("Refresh"), Tooltip("激活星星个数（FG 层显示亮星），会被限制在 0..totalCount")]
        [SerializeField] private int activeCount = 3;

        [Title("容器与模板（单一 Group 模式）")]
        [Tooltip("克隆容器（单一分组）。若为空，将在当前对象下自动创建名为 Group 的子节点")]
        [SerializeField] private Transform groupRoot;

        [Tooltip("示例条目（条目下包含子 BG、FG）。若设置或自动发现，将以其为预制体克隆，并隐藏该示例")]
        [SerializeField] private Transform itemTemplate;

        // 旧版分离 BG/FG 容器与预制体的配置已移除，统一采用单一 Group + 条目模板模式。

        public void Refresh()
        {
            ClampCounts();
            EnsureGroupRoot();
            TryAutoSetupItemTemplate();
            BuildItems(totalCount);
            ApplyActiveToItems(activeCount);
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void SetStarNum(int activeNum, int totalNum)
        {
            totalCount = totalNum;
            activeCount = activeNum;
            Refresh();
        }
        // private void OnValidate()
        // {
        //     Refresh();
        // }

        private void ClampCounts()
        {
            if (totalCount < 0) totalCount = 0;
            activeCount = Mathf.Clamp(activeCount, 0, totalCount);
        }

        private void EnsureGroupRoot()
        {
            if (groupRoot == null)
            {
                var t = transform.Find("Group");
                groupRoot = t != null ? t : FindOrCreateChild("Group");
            }
        }

        private void BuildItems(int count)
        {
            if (groupRoot == null || itemTemplate == null) return;

            var clones = GetChildrenExcludingTemplate(groupRoot, itemTemplate);

            for (int i = 0; i < count; i++)
            {
                Transform child = i < clones.Count ? clones[i] : Instantiate(itemTemplate.gameObject, groupRoot).transform;
                child.SetSiblingIndex(i);
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                EnsureItemSubNodes(child);
            }

            for (int i = clones.Count - 1; i >= count; i--)
            {
                var extra = clones[i].gameObject;
                if (extra.activeSelf) extra.SetActive(false);
            }

            if (itemTemplate.gameObject.activeSelf) itemTemplate.gameObject.SetActive(false);
        }

        private void ApplyActiveToItems(int active)
        {
            if (groupRoot == null) return;
            var clones = GetChildrenExcludingTemplate(groupRoot, itemTemplate);

            for (int i = 0; i < clones.Count; i++)
            {
                var item = clones[i];
                if (!item.gameObject.activeSelf) continue;
                var bg = item.Find("BG");
                var fg = item.Find("FG");
                if (bg != null && !bg.gameObject.activeSelf) bg.gameObject.SetActive(true);
                if (fg != null)
                {
                    bool on = i < active;
                    if (fg.gameObject.activeSelf != on) fg.gameObject.SetActive(on);
                }
            }
        }

        private void EnsureItemSubNodes(Transform item)
        {
            var bg = item.Find("BG");
            if (bg == null)
            {
                // Debug.LogWarning($"示例条目缺少 BG 子节点：{item.name}", gameObject);
            }
            else if (!bg.gameObject.activeSelf)
            {
                bg.gameObject.SetActive(true);
            }
            // var fg = item.Find("FG");
            // if (fg == null)
            // {
            //     Debug.LogWarning($"示例条目缺少 FG 子节点：{item.name}", gameObject);
            // }
        }

        private System.Collections.Generic.List<Transform> GetChildrenExcludingTemplate(Transform root, Transform template)
        {
            var list = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (template != null && c == template) continue;
                list.Add(c);
            }
            return list;
        }

        private void TryAutoSetupItemTemplate()
        {
            if (itemTemplate == null)
            {
                itemTemplate = FindTemplateUnder(groupRoot != null ? groupRoot : transform);
            }
            if (itemTemplate != null && itemTemplate.gameObject.activeSelf)
                itemTemplate.gameObject.SetActive(false);
        }

        private Transform FindOrCreateChild(string childName)
        {
            var t = transform.Find(childName);
            if (t != null) return t;
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        /// <summary>
        /// 在指定根下寻找第一个“自身包含子 BG 或 FG”的子节点，作为示例条目。
        /// </summary>
        private Transform FindTemplateUnder(Transform root)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var hasBG = child.Find("BG") != null;
                var hasFG = child.Find("FG") != null;
                if (hasBG || hasFG)
                    return child;
            }
            return null;
        }
    }
}