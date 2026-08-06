// Copyright@www.bytedance.com
// Author: zhouxu.ken
// Date: 2024/06/08
// Description:

using UnityEngine;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 无它，仅用于存取 Shader
    /// </summary>
    [DllMonoBehaviour]
    public class RenderTextureHelper : MonoBehaviour
    {
        /// <summary>
        /// 混合并进行gamma矫正 Shader
        /// </summary>
        public ComputeShader mixShader;

        /// <summary>
        /// 将画面进行上下翻转的 Shader
        /// </summary>
        public ComputeShader flipShader;

        public Shader composureShader;

        public Shader flipXYShader;
    }
}