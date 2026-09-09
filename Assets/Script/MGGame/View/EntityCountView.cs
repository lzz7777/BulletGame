using UnityEngine;
using XN;

namespace XN
{
    public class EntityCountView : MonoBehaviour
    {
        public static EntityCountView Instance;
        
        /// <summary>
        /// 实体数量显示区域
        /// </summary>
        private Rect _entityCountRect;
        
        private GUIStyle _entityCountStyle = new GUIStyle();
        
        // 缓存数值避免每帧装箱/字符串插值
        private int _lastDisplayCount = -1;
        private int _lastTotalCount = -1;
        private string _entityCountStr = string.Empty;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // 置于 FPSView 下方
            _entityCountRect = new Rect(100, 60, 400, 50); 
            _entityCountStyle.fontSize = 40;
            _entityCountStyle.normal.textColor = Color.green;
        }

        private void OnGUI()
        {
            if (EntityManager.Instance == null) return;

            // 严格遵守 0 GC 约束，只有数据变化时才重新拼接字符串
            int currentCount = EntityManager.Instance.CurrentEntityCount;
            int totalCount = EntityManager.Instance.TotalCreatedEntityCount;

            if (currentCount != _lastDisplayCount || totalCount != _lastTotalCount)
            {
                _lastDisplayCount = currentCount;
                _lastTotalCount = totalCount;
                _entityCountStr = "Entities: " + _lastDisplayCount.ToString() + " (Total: " + _lastTotalCount.ToString() + ")";
            }
            
            GUI.Label(_entityCountRect, _entityCountStr, _entityCountStyle);
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}