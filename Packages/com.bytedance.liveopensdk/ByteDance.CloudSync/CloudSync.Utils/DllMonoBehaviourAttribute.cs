// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;

namespace ByteDance.CloudSync
{
    /// <summary>
    /// 标记一个 MonoBehaviour 是会打包在 .dll 文件中的
    /// SDK 正常开发时无影响，在正式打包时生成一份 .cs 副本代码保存在 sdk 发布文件夹中，以防止 .prefab 引用丢失
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DllMonoBehaviourAttribute : Attribute
    {
    }
}