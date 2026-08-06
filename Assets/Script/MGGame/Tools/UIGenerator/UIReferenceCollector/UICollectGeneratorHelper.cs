#if UNITY_EDITOR
public static class UICollectGeneratorHelper
{
    /// <summary>
    /// 字符裁剪 
    /// </summary>
    public static string Between(string src, string findfrom, string findto, string defaultString)
    {
        int start = src.IndexOf(findfrom);
        int to = src.IndexOf(findto, start + findfrom.Length);
        if (start < 0 || to < 0) return defaultString;
        string s = src.Substring(start + findfrom.Length, to - start - findfrom.Length);
        return s;
    }
}
#endif