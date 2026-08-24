using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace XN
{
    // 强制要求挂载该脚本的物体上必须有 ScrollRect 组件
    [RequireComponent(typeof(ScrollRect))]
    public class UILoopList : MonoBehaviour
    {
        private ScrollRect _scrollRect;
        private RectTransform _content;

        [SerializeField] private float _spacing = 0;

        // 预制体名字，用于从ObjectPoolManager获取
        [SerializeField] private GameObject _prefab;

        private struct ItemInfo
        {
            public GameObject Go;
            public UIItemBase UIItem;
            public RectTransform Rect;
            public int DataIndex;
        }

        private List<ItemInfo> _itemList = new();
        private int _totalCount;

        // 缓存数据
        private int _instantiateCount; // 实际实例化的数量
        private bool _isInit;
        private float _viewHeight;
        private float _itemHeight;

        private List<UIItemDataBase> _itemDataList = new();

        private void Awake()
        {
            // 获取同物体上的 ScrollRect 组件
            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;

            _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void OnDestroy()
        {
            ReturnAllItems();
        }

        /// <summary>
        /// 初始化列表
        /// </summary>
        private async UniTask Init()
        {
            if (_isInit) return;

            _itemHeight = (_prefab.transform as RectTransform).rect.height;

            _viewHeight = _scrollRect.viewport != null
                ? _scrollRect.viewport.rect.height
                : GetComponent<RectTransform>().rect.height;

            // 计算需要实例化的数量：视口高度 / (Item高度 + 间距) + 2个缓冲
            // 增加缓冲数量以确保滚动流畅
            int viewCount = Mathf.CeilToInt(_viewHeight / (_itemHeight + _spacing)) + 1;
            _instantiateCount = viewCount + 2;

            // 预先加载Item
            for (int i = 0; i < _instantiateCount; i++)
            {
                var item = await ObjectPoolManager.Instance.GetFromPool(_prefab.name, _content);
                item.SetActive(false);

                // 设置锚点为左上角
                var rect = item.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, _itemHeight);

                _itemList.Add(new ItemInfo
                {
                    Go = item,
                    UIItem = item.GetComponent<UIItemBase>(),
                    Rect = rect,
                    DataIndex = -1
                });
            }

            _isInit = true;
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void ClearData()
        {
            _itemDataList.Clear();
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="item"></param>
        public void AddData<T>(out T item) where T : UIItemDataBase, new()
        {
            item = new();
            _itemDataList.Add(item);
        }

        /// <summary>
        /// 刷新ui
        /// </summary>
        /// <param name="resetPos"></param>
        public async UniTask RefreshItem(bool resetPos = true)
        {
            await Init();

            if (!_isInit) return;

            _totalCount = _itemDataList.Count;

            // 设置Content高度
            float totalHeight = _totalCount * (_itemHeight + _spacing) - _spacing;
            if (totalHeight < 0) totalHeight = 0;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            if (resetPos)
            {
                _content.anchoredPosition = Vector2.zero;
            }

            // 无论是否重置位置，都必须重置记录的索引，以确保 OnScroll 能够触发强制刷新
            for (int i = 0; i < _itemList.Count; i++)
            {
                var info = _itemList[i];
                info.DataIndex = -1;
                _itemList[i] = info;
            }

            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            OnScroll(Vector2.zero);
        }

        private void OnScroll(Vector2 pos)
        {
            if (!_isInit || _itemList.Count == 0 || _totalCount == 0) return;

            // 1. 计算当前可见区域的起始数据索引
            float contentY = _content.anchoredPosition.y;
            int startIndex = Mathf.FloorToInt(contentY / (_itemHeight + _spacing));

            // 限制索引范围，防止越界（虽然ScrollRect会限制Content位置，但计算值可能略有偏差）
            if (startIndex < 0) startIndex = 0;
            // 如果总数很少，startIndex也限制住
            if (startIndex > _totalCount - 1) startIndex = _totalCount - 1;

            // 2. 遍历缓冲区内的所有Item槽位
            // 我们使用 _instantiateCount 个 Item 来循环显示
            // 当前显示的范围是 [startIndex, startIndex + _instantiateCount - 1]

            for (int i = 0; i < _instantiateCount; i++)
            {
                int dataIndex = startIndex + i;

                // 使用模运算找到对应的 Item 实例
                // 这样 Item 0 总是负责 Data 0, Data N, Data 2N...
                // 当 DataIndex 变化时（比如从 0 变到 N），Item 0 就会移动并刷新
                int itemIndex = dataIndex % _instantiateCount;

                var itemInfo = _itemList[itemIndex];

                if (dataIndex < _totalCount)
                {
                    // 如果该Item当前绑定的数据不是我们要显示的，则更新
                    if (itemInfo.DataIndex != dataIndex)
                    {
                        itemInfo.Go.SetActive(true);

                        // 更新位置
                        float posY = -dataIndex * (_itemHeight + _spacing);
                        itemInfo.Rect.anchoredPosition = new Vector2(itemInfo.Rect.anchoredPosition.x, posY);

                        // 刷新UI
                        itemInfo.UIItem.Refresh(_itemDataList[dataIndex]);

                        // 更新记录
                        itemInfo.DataIndex = dataIndex;
                        _itemList[itemIndex] = itemInfo;
                    }
                    else if (!itemInfo.Go.activeSelf)
                    {
                        itemInfo.Go.SetActive(true);
                    }
                }
                else
                {
                    // 超出数据范围，隐藏
                    if (itemInfo.Go.activeSelf)
                    {
                        itemInfo.Go.SetActive(false);
                        itemInfo.DataIndex = -1;
                        _itemList[itemIndex] = itemInfo;
                    }
                }
            }
        }

        /// <summary>
        /// 回收所有Item到对象池
        /// </summary>
        public void ReturnAllItems()
        {
            if (_itemList.Count > 0)
            {
                List<GameObject> gos = new List<GameObject>();
                foreach (var item in _itemList)
                {
                    gos.Add(item.Go);
                }

                ObjectPoolManager.Instance.ReturnToPool(gos);
                _itemList.Clear();
            }

            _isInit = false;
        }
    }
}