using System;

namespace XN
{
    public static class FlagsEnumExtensions
    {
        /// <summary>
        /// 添加一个或多个枚举值
        /// </summary>
        public static T Add<T>(this T value, T flag) where T : Enum
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            var valueLong = Convert.ToInt64(value);
            var flagLong = Convert.ToInt64(flag);

            return (T)Enum.ToObject(typeof(T), valueLong | flagLong);
        }

        /// <summary>
        /// 移除一个或多个枚举值
        /// </summary>
        public static T Remove<T>(this T value, T flag) where T : Enum
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            var valueLong = Convert.ToInt64(value);
            var flagLong = Convert.ToInt64(flag);

            return (T)Enum.ToObject(typeof(T), valueLong & ~flagLong);
        }

        /// <summary>
        /// 检查是否包含指定的枚举值
        /// </summary>
        public static bool Contains<T>(this T value, T flag) where T : Enum
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            var valueLong = Convert.ToInt64(value);
            var flagLong = Convert.ToInt64(flag);

            return (valueLong & flagLong) == flagLong;
        }

        /// <summary>
        /// 切换枚举值（如果存在则移除，不存在则添加）
        /// </summary>
        public static T Toggle<T>(this T value, T flag) where T : Enum
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            var valueLong = Convert.ToInt64(value);
            var flagLong = Convert.ToInt64(flag);

            return (T)Enum.ToObject(typeof(T), valueLong ^ flagLong);
        }
    }
}