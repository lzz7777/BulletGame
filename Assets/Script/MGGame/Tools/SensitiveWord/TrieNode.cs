using System.Collections.Generic;

namespace XN.Tools.SensitiveWord
{
    public class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; private set; }
        public TrieNode FailureLink { get; set; } // 失败指针
        public List<string> MatchedWords { get; private set; } // 匹配到的敏感词列表（如果此节点是某个词的结尾）

        public TrieNode()
        {
            Children = new Dictionary<char, TrieNode>();
            MatchedWords = new List<string>();
        }

        public TrieNode GetOrCreateChild(char c)
        {
            if (!Children.TryGetValue(c, out TrieNode child))
            {
                child = new TrieNode();
                Children[c] = child;
            }

            return child;
        }
    }
}