using System;
using System.Collections.Generic;
using System.Linq;

namespace XN
{
    public static class CalculateHelper
    {
        private static Random _random = new Random();

        /// <summary>
        /// 使用洗牌算法随机选择（性能更优）
        /// </summary>
        public static List<T> GetRandomUsingShuffle<T>(T[] allValues, int count)
        {
            // 创建副本以避免修改原始数组
            List<T> shuffled = allValues.ToList();
        
            // Fisher-Yates洗牌算法
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
        
            return shuffled.Take(count).ToList();
        }
        
        /// <summary>
        /// 合并队列
        /// </summary>
        /// <param name="queue1"></param>
        /// <param name="queue2"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Queue<T> MergeQueues<T>(Queue<T> queue1, Queue<T> queue2)
        {
            return new Queue<T>(queue1.Concat(queue2));
        }
    }
}