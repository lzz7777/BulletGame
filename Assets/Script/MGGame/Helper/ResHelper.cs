//====================================================
//Author:lixin
//Time  :2025/11/25 16:57
//Desc  :
//====================================================

using UnityEngine;

namespace XN
{
    public static class ResHelper
    {
        // 没啥卵用，没必要随机
        public static string RandomStr(int len =7)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new System.Text.StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(chars[Random.Range(0, chars.Length)]);
            return sb.ToString();
        }


        public static string GetAvatarUrl(string path = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                return $"head_{Random.Range(1, 11)}";
            }
            return path;
        }

        public static string GetIconOrNone(string path = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("出现 空 图片路径 转换 none ");
                return "none";
            }
            return path;
        }
    }

}