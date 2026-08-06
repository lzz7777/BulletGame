using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions; // For Regex.IsMatch and Regex.Replace

namespace XN.Tools.SensitiveWord
{
    
public class AhoCorasick
{
    private TrieNode _root;

    public AhoCorasick()
    {
        _root = new TrieNode();
    }

    /// <summary>
    /// 添加敏感词到Trie树。
    /// </summary>
    /// <param name="word">要添加的敏感词。</param>
    public void AddWord(string word)
    {
        // 规范化敏感词：移除所有非字母数字字符
        string normalizedWord = Regex.Replace(word, @"[^\p{L}\p{N}]", ""); // \p{L} 匹配任何Unicode字母，\p{N} 匹配任何Unicode数字
        if (string.IsNullOrEmpty(normalizedWord))
        {
            return; // 如果规范化后为空，则不添加
        }

        TrieNode current = _root;
        foreach (char c in normalizedWord)
        {
            current = current.GetOrCreateChild(c);
        }
        current.MatchedWords.Add(normalizedWord); // 标记为敏感词的结尾，存储规范化后的词
    }

    /// <summary>
    /// 从文件加载敏感词列表。每行一个敏感词。
    /// </summary>
    /// <param name="filePath">敏感词文件路径。</param>
    public void LoadSensitiveWordsFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            var words = File.ReadAllLines(filePath)
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .Select(line => line.Trim());
            foreach (var word in words)
            {
                AddWord(word); // 使用 AddWord 方法将词添加到 Trie 树
            }
            Console.WriteLine($"已从 '{filePath}' 加载 {words.Count()} 个敏感词。");
        }
        else
        {
            Console.WriteLine($"敏感词文件 '{filePath}' 不存在。");
        }
    }
    
    public void LoadSensitiveWorlds(IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            AddWord(word); // 使用 AddWord 方法将词添加到 Trie 树
        }
        Console.WriteLine($"已加载 {words.Count()} 个敏感词。");
    }

    /// <summary>
    /// 构建所有节点的失败指针。
    /// </summary>
    public void BuildFailureLinks()
    {
        Queue<TrieNode> queue = new Queue<TrieNode>();

        // 根节点的子节点的失败指针指向根节点
        foreach (var child in _root.Children.Values)
        {
            child.FailureLink = _root;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            TrieNode current = queue.Dequeue();

            foreach (var entry in current.Children)
            {
                char c = entry.Key;
                TrieNode child = entry.Value;

                TrieNode failureNode = current.FailureLink;
                while (failureNode != null && !failureNode.Children.ContainsKey(c))
                {
                    failureNode = failureNode.FailureLink;
                }

                if (failureNode == null) // 回溯到根节点仍未找到匹配
                {
                    child.FailureLink = _root;
                }
                else
                {
                    child.FailureLink = failureNode.Children[c];
                    // 将失败指针指向的节点的匹配词也添加到当前节点的匹配词中
                    // 这是为了处理 "abc" 和 "bc" 都是敏感词，当匹配到 "abc" 时，"bc" 也应该被识别
                    child.MatchedWords.AddRange(child.FailureLink.MatchedWords);
                }
                queue.Enqueue(child);
            }
        }
    }

    /// <summary>
    /// 在文本中查找所有敏感词的匹配。
    /// 返回一个列表，每个元素包含匹配到的词和其在文本中的起始索引。
    /// </summary>
    /// <param name="text">要搜索的文本。</param>
    /// <returns>匹配结果列表。</returns>
    /// <summary>
    /// 辅助方法：规范化文本，移除所有非字母数字字符，并返回原始索引到规范化索引的映射。
    /// </summary>
    /// <param name="originalText">原始文本。</param>
    /// <param name="normalizedToOriginalMap">输出参数：规范化文本中每个字符对应的原始文本索引。</param>
    /// <returns>规范化后的文本。</returns>
    private string NormalizeText(string originalText, out int[] normalizedToOriginalMap)
    {
        StringBuilder normalizedBuilder = new StringBuilder();
        List<int> mapList = new List<int>();

        for (int i = 0; i < originalText.Length; i++)
        {
            char c = originalText[i];
            // 检查字符是否是字母或数字
            if (char.IsLetterOrDigit(c))
            {
                normalizedBuilder.Append(c);
                mapList.Add(i);
            }
        }
        normalizedToOriginalMap = mapList.ToArray();
        return normalizedBuilder.ToString();
    }

    /// <summary>
    /// 在文本中查找所有敏感词的匹配。
    /// 返回一个列表，每个元素包含匹配到的词和其在原始文本中的起始索引。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>匹配结果列表。</returns>
    public List<Tuple<string, int>> FindAll(string text)
    {
        List<Tuple<string, int>> matches = new List<Tuple<string, int>>();
        
        // 规范化输入文本并获取索引映射
        int[] normalizedToOriginalMap;
        string normalizedText = NormalizeText(text, out normalizedToOriginalMap);

        TrieNode current = _root;

        for (int i = 0; i < normalizedText.Length; i++)
        {
            char c = normalizedText[i];

            // 沿着失败指针回溯，直到找到匹配的字符或回到根节点
            while (current != _root && !current.Children.ContainsKey(c))
            {
                current = current.FailureLink;
            }

            // 如果找到匹配的字符，则前进
            if (current.Children.ContainsKey(c))
            {
                current = current.Children[c];
            }
            else // 如果在根节点也未找到匹配，则停留在根节点
            {
                current = _root;
            }

            // 如果当前节点是敏感词的结尾，则记录匹配
            if (current.MatchedWords.Any())
            {
                foreach (string word in current.MatchedWords)
                {
                    // 计算匹配词在规范化文本中的起始索引
                    int normalizedStartIndex = i - word.Length + 1;
                    if (normalizedStartIndex >= 0 && normalizedStartIndex < normalizedToOriginalMap.Length)
                    {
                        // 将规范化文本中的起始索引映射回原始文本中的起始索引
                        int originalStartIndex = normalizedToOriginalMap[normalizedStartIndex];
                        matches.Add(Tuple.Create(word, originalStartIndex));
                    }
                }
            }
        }
        return matches;
    }

    /// <summary>
    /// 屏蔽聊天文本中的敏感词，将其替换为指定字符。
    /// </summary>
    /// <param name="text">原始聊天文本。</param>
    /// <param name="replacementChar">用于替换敏感词的字符，默认为 '*'。</param>
    /// <returns>屏蔽后的文本。</returns>
    public string Filter(string text, char replacementChar = '*')
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        char[] filteredChars = text.ToCharArray();
        List<Tuple<string, int>> matches = FindAll(text);

        // 对匹配到的敏感词进行替换
        // 对匹配到的敏感词进行替换
        foreach (var match in matches)
        {
            string matchedNormalizedWord = match.Item1; // 匹配到的规范化后的敏感词
            int originalStartIndex = match.Item2;       // 匹配在原始文本中的起始索引

            // 遍历原始文本，从 originalStartIndex 开始，替换掉匹配到的敏感词的原始字符
            // 这里需要找到原始文本中对应于规范化敏感词的字符范围
            int charsReplaced = 0;
            for (int i = originalStartIndex; i < text.Length && charsReplaced < matchedNormalizedWord.Length; i++)
            {
                if (char.IsLetterOrDigit(text[i]))
                {
                    filteredChars[i] = replacementChar;
                    charsReplaced++;
                }
                else
                {
                    // 如果是非字母数字字符，也替换掉，或者保持不变，取决于需求
                    // 这里选择替换掉，以确保整个敏感词区域被屏蔽
                    filteredChars[i] = replacementChar;
                }
            }
        }
        return new string(filteredChars);
    }

    /// <summary>
    /// 检查文本是否包含敏感词。
    /// </summary>
    /// <param name="text">要检查的文本。</param>
    /// <returns>如果包含敏感词则返回 true，否则返回 false。</returns>
    public bool ContainsSensitiveWord(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        return FindAll(text).Any();
    }
}

}
