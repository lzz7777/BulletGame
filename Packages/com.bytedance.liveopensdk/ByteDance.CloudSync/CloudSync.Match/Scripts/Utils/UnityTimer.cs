using UnityEngine;

namespace ByteDance.CloudSync.Match
{
    public class UnityTimer : MonoBehaviour
    {
        public float Interval { get; set; }
        public bool IsRunning { get; private set; }

        public event System.Action Elapsed;
        
        private float _elapsedTime;
        
        public static UnityTimer CreateTimer()
        {
            GameObject timerObject = new GameObject("UnityTimer");
            var timer = timerObject.AddComponent<UnityTimer>();
            DontDestroyOnLoad(timerObject); // 设置为不在场景转换时被销毁
            return timer;
        }

        public static void DestroyTimer(UnityTimer timer)
        {
            if (timer)
            {
                Destroy(timer.gameObject);
            }
        }

        void Update()
        {
            if (!IsRunning)
            {
                return;
            }
            
            _elapsedTime += Time.deltaTime * 1000;

            if (_elapsedTime >= Interval)
            {
                _elapsedTime -= Interval;
                Elapsed?.Invoke();
            }
        }

        public void Begin()
        {
            _elapsedTime = 0f;
            IsRunning = true;
        }
        
        public void End()
        {
            IsRunning = false;
            _elapsedTime = 0f;
        }

        public void Resume()
        {
            IsRunning = false;
        }

        
        public void Pause()
        {
            IsRunning = false;
        }

    }
}
