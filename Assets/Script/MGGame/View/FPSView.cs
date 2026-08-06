//====================================================
//Author:Tzw
//Time  :2023-12-12  11:54
//Desc  :
//====================================================

using System;
using UnityEngine;

namespace XN
{
    public class FPSView : UnityEngine.MonoBehaviour
    {
        public static FPSView Instance;
        
        /// <summary>
        /// 上一次更新帧率的时间
        /// </summary>
        private float m_lastUpdateShowTime = 0f;
        /// <summary>
        /// 更新显示帧率的时间间隔
        /// </summary>
        private readonly float m_updateTime = 0.05f;
        /// <summary>
        /// 帧数
        /// </summary>
        private int m_frames = 0;
        /// <summary>
        /// 帧间间隔
        /// </summary>
        private float m_frameDeltaTime = 0;
        private float    m_FPS = 0;
        private Rect     m_fps, m_dtime;
        private GUIStyle m_style = new GUIStyle();

        void Awake()
        {
            // Application.targetFrameRate = -1;    // TODO  lixin 注释查看优化  -1（默认无限制）
            // QualitySettings.vSyncCount = 0;     // TODO  lixin   0 = VSync关闭
            // Application.targetFrameRate = 60;      // TODO lixin  渲染线程出现间断为0，此处切换到 -1（空等主线程 WaitForGfxCommandsFromMainThread）
            Instance = this;
        }

        void Start()
        {
            m_lastUpdateShowTime = Time.realtimeSinceStartup;
            m_fps = new Rect(100, 0, 200, 50);
            m_dtime = new Rect(100, 60, 100, 50);
            m_style.fontSize = 40;
            m_style.normal.textColor = Color.red;
        }

        void Update()
        {
            m_frames++;
            if (Time.realtimeSinceStartup - m_lastUpdateShowTime >= m_updateTime)
            {
                m_FPS = m_frames / (Time.realtimeSinceStartup - m_lastUpdateShowTime);
                m_frameDeltaTime = (Time.realtimeSinceStartup - m_lastUpdateShowTime) / m_frames;
                m_frames = 0;
                m_lastUpdateShowTime = Time.realtimeSinceStartup;
                //Debug.Log("FPS: " + m_FPS + "，间隔: " + m_FrameDeltaTime);
            }
        }

        void OnGUI()
        {
            int m_frameDeltaTimeMs = (int)(m_frameDeltaTime * 1000);
            string str = $"FPS: {m_FPS:F2} ({m_frameDeltaTimeMs}ms)";
            GUI.Label(m_fps, str, m_style);
            
            // GUI.Label(m_dtime, "间隔: " + m_frameDeltaTimeMs, m_style);
        }

        void OnDestroy()
        {
            Instance = null;
        }
    }
}