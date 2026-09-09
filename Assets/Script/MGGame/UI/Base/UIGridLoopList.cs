using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace XN
{
    // 强制要求挂载该脚本的物体上必须有 ScrollRect 组件
    [RequireComponent(typeof(ScrollRect))]
    public class UIGridLoopList : MonoBehaviour
    {
        private ScrollRect _scrollRect;
        private RectTransform _content;

        [Header("Grid Settings")]
        [SerializeField] private int _columnCount = 4;
        [SerializeField] private Vector2 _spacing = Vector2.zero;

        // 预制体名字，用于从ObjectPoolManager获取
        [SerializeField] private GameObject _prefab;

        private struct ItemInfo
        {
            public GameObject Go;
            public UIItemBase UIItem;
            public RectTransform Rect;
            public int DataIndex;
        }

        private struct RefreshTask
        {
            public ItemInfo ItemInfo;
            public int DataIndex;
        }

        private List<ItemInfo> _itemList = new();
        private int _totalCount;
        
        // 分帧加载队列
        private Queue<RefreshTask> _refreshQueue = new();
        // 开启分帧加载机制（默认开启）
        [SerializeField] private bool _enableTimeSlicing = true;
        // 每帧最大耗时预算(秒)，默认2ms
        [SerializeField] private float _timeBudgetPerFrame = 0.002f;

        // 缓存数据
        private int _instantiateCount; // 实际实例化的数量
        private bool _isInit;
        private float _viewHeight;
        private float _itemHeight;
        private float _itemWidth;

        private List<UIItemDataBase> _itemDataList = new();

        private void Awake()
        {
            // 获取同物体上的 ScrollRect 组件
            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;

            _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        public void OnClose()
        {
            ClearData();
            ReturnAllItems();
        }

        /// <summary>
        /// 初始化列表（异步版本，解决未预热直接打开卡顿问题）
        /// </summary>
        private async UniTask InitAsync()
        {
            if (_isInit) return;

            var prefabRect = _prefab.transform as RectTransform;
            _itemWidth = prefabRect.rect.width;
            _itemHeight = prefabRect.rect.height;

            _viewHeight = _scrollRect.viewport != null
                ? _scrollRect.viewport.rect.height
                : GetComponent<RectTransform>().rect.height;

            // 计算需要实例化的行数：视口高度 / (Item高度 + Y间距) + 2个缓冲行
            int viewRowCount = Mathf.CeilToInt(_viewHeight / (_itemHeight + _spacing.y)) + 1;
            
            // 实例化的总数 = 行数 * 列数
            _instantiateCount = (viewRowCount + 2) * _columnCount;

            // 异步预先加载Item，避免主线程峰值卡顿
            for (int i = 0; i < _instantiateCount; i++)
            {
                var item = await ObjectPoolManager.Instance.GetFromPoolAsync(_prefab.name, _content);
                if (item == null) continue;
                
                item.SetActive(false);

                // 对于Grid格子，固定锚点为左上角，取消拉伸
                var rect = item.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0f, 1f);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(_itemWidth, _itemHeight);

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
            _refreshQueue.Clear();
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
        /// 尾部新增数据（O(1) 增量更新）
        /// </summary>
        public void AppendData(UIItemDataBase item)
        {
            _itemDataList.Add(item);
            _totalCount = _itemDataList.Count;

            // 更新 Content 高度
            int totalRowCount = Mathf.CeilToInt((float)_totalCount / _columnCount);
            float totalHeight = totalRowCount * (_itemHeight + _spacing.y) - _spacing.y;
            if (totalHeight < 0) totalHeight = 0;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            // 补充加载：如果当前数据量还不足以填满视口（或刚够），触发滚动更新以显示新条目
            if (_totalCount <= _instantiateCount)
            {
                RefreshDisplay();
            }
        }

        /// <summary>
        /// 局部数据更新（O(1) 穿透刷新，高度不变）
        /// </summary>
        public void UpdateDataAt(int index)
        {
            if (index < 0 || index >= _totalCount) return;
            if (!_isInit || _itemList.Count == 0) return;

            int itemIndex = index % _instantiateCount;
            var itemInfo = _itemList[itemIndex];

            // 仅当该数据当前正在视口中显示时，才定向触发刷新
            if (itemInfo.DataIndex == index && itemInfo.Go.activeSelf)
            {
                if (_enableTimeSlicing)
                {
                    _refreshQueue.Enqueue(new RefreshTask 
                    { 
                        ItemInfo = itemInfo, 
                        DataIndex = index 
                    });
                    itemInfo.UIItem.ShowLoadingState();
                }
                else
                {
                    itemInfo.UIItem.Refresh(_itemDataList[index]);
                }
            }
        }

        /// <summary>
        /// 中间插入数据 (O(N-k))
        /// </summary>
        public void InsertDataAt(int index, UIItemDataBase item)
        {
            if (index < 0 || index > _totalCount) return;

            _itemDataList.Insert(index, item);
            _totalCount = _itemDataList.Count;

            // 更新 Content 高度
            int totalRowCount = Mathf.CeilToInt((float)_totalCount / _columnCount);
            float totalHeight = totalRowCount * (_itemHeight + _spacing.y) - _spacing.y;
            if (totalHeight < 0) totalHeight = 0;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            ResetVisibleItemsIndexAndRefresh();
        }

        /// <summary>
        /// 移除数据 (O(N-k))
        /// </summary>
        public void RemoveDataAt(int index)
        {
            if (index < 0 || index >= _totalCount) return;

            _itemDataList.RemoveAt(index);
            _totalCount = _itemDataList.Count;

            // 更新 Content 高度
            int totalRowCount = Mathf.CeilToInt((float)_totalCount / _columnCount);
            float totalHeight = totalRowCount * (_itemHeight + _spacing.y) - _spacing.y;
            if (totalHeight < 0) totalHeight = 0;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            ResetVisibleItemsIndexAndRefresh();
        }

        private void ResetVisibleItemsIndexAndRefresh()
        {
            if (!_isInit) return;
            
            // 强制所有现有缓存槽位的 DataIndex 失效，让 OnScroll 重新排版
            for (int i = 0; i < _itemList.Count; i++)
            {
                var info = _itemList[i];
                info.DataIndex = -1;
                
                // 强制隐藏所有节点，让 OnScroll 重新分配显示
                if (info.Go.activeSelf)
                {
                    info.Go.SetActive(false);
                }
                
                _itemList[i] = info;
            }
            
            RefreshDisplay();
        }

        /// <summary>
        /// 刷新ui
        /// </summary>
        /// <param name="resetPos"></param>
        public async UniTask RefreshItem(bool resetPos = true)
        {
            await InitAsync();

            if (!_isInit) return;

            _totalCount = _itemDataList.Count;

            // 计算总行数并设置Content高度
            int totalRowCount = Mathf.CeilToInt((float)_totalCount / _columnCount);
            float totalHeight = totalRowCount * (_itemHeight + _spacing.y) - _spacing.y;
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

            // 1. 计算当前可见区域的起始行索引
            float contentY = _content.anchoredPosition.y;
            int startRowIndex = Mathf.FloorToInt(contentY / (_itemHeight + _spacing.y));

            // 限制索引范围，防止越界
            if (startRowIndex < 0) startRowIndex = 0;
            
            int totalRowCount = Mathf.CeilToInt((float)_totalCount / _columnCount);
            if (startRowIndex > totalRowCount - 1) startRowIndex = totalRowCount - 1;

            // 2. 遍历缓冲区内的所有Item槽位
            for (int i = 0; i < _instantiateCount; i++)
            {
                // 计算当前槽位对应的数据索引：起始行索引 * 列数 + 偏移
                int dataIndex = startRowIndex * _columnCount + i;

                // 使用模运算找到对应的 Item 实例，实现完美的一维数组复用到二维网格
                int itemIndex = dataIndex % _instantiateCount;

                var itemInfo = _itemList[itemIndex];

                if (dataIndex < _totalCount)
                {
                    // 如果该Item当前绑定的数据不是我们要显示的，则更新
                    if (itemInfo.DataIndex != dataIndex)
                    {
                        itemInfo.Go.SetActive(true);

                        // 计算当前数据所在的行与列
                        int row = dataIndex / _columnCount;
                        int col = dataIndex % _columnCount;

                        // 更新位置 (根据左上角锚点计算)
                        float posX = col * (_itemWidth + _spacing.x);
                        float posY = -row * (_itemHeight + _spacing.y);
                        
                        itemInfo.Rect.anchoredPosition = new Vector2(posX, posY);
                        // 更新记录（struct直接覆盖，0 GC）
                        itemInfo.DataIndex = dataIndex;
                        
                        if (_enableTimeSlicing)
                        {
                            // 加入分帧刷新队列
                            _refreshQueue.Enqueue(new RefreshTask 
                            { 
                                ItemInfo = itemInfo, 
                                DataIndex = dataIndex 
                            });
                            // 显示加载状态防串位
                            itemInfo.UIItem.ShowLoadingState();
                        }
                        else
                        {
                            // 传统同步刷新UI
                            itemInfo.UIItem.Refresh(_itemDataList[dataIndex]);
                        }

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

            _refreshQueue.Clear();
            _isInit = false;
        }

        private void Update()
        {
            if (!_enableTimeSlicing || _refreshQueue.Count == 0) return;

            float startTime = Time.realtimeSinceStartup;

            while (_refreshQueue.Count > 0)
            {
                var task = _refreshQueue.Dequeue();
                
                // 防御性校验：快速滑动时，该UI可能已经被重新分配给别的数据索引
                // 只有队列里的索引和UI实际当前绑定的索引一致时，才执行耗时的刷新逻辑
                if (task.ItemInfo.DataIndex == task.DataIndex && task.DataIndex < _itemDataList.Count)
                {
                    task.ItemInfo.UIItem.Refresh(_itemDataList[task.DataIndex]);
                }

                // 耗时超过预算，中断循环，剩余任务留到下一帧
                if (Time.realtimeSinceStartup - startTime > _timeBudgetPerFrame)
                {
                    break;
                }
            }
        }
    }
}