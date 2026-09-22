#if UNITY_EDITOR
using System;

namespace XN
{
    // 面试讲述点：UIReferenceGenerator (生成器入口)
    // 这是代码生成的控制中枢，根据收集到的特殊组件标记，将生成任务路由到不同的具体生成器。
    // 面试时可以说：“工具会根据节点上是否挂载了 SubView 或 LoopListItem，走不同的生成分支，生成对应的逻辑壳子”
    public static class UIReferenceGenerator
    {
        enum GenerateType
        {
            View,
            SubView,
            Item
        }

        public static void Generate(UIReferenceCollector collector)
        {
            GenerateType generateType = GenerateType.View;

            // 1. 遍历收集到的数据，进行类型嗅探
            foreach (var data in collector.objectDatas)
            {
                bool isFound = false;
                foreach (var componentData in data.componentDatas)
                {
                    switch (componentData.ComponentEnum)
                    {
                        case UICollectorComponentEnum.LoopListItem:
                            generateType = GenerateType.Item;
                            isFound = true;
                            break;
                        case UICollectorComponentEnum.SubView:
                            generateType = GenerateType.SubView;
                            isFound = true;
                            break;
                    }
                }

                if (isFound)
                    break;
            }

            // 2. 根据嗅探结果，派发到对应的生成器去生成 C# 脚本
            switch (generateType)
            {
                case GenerateType.View:
                    UIReferenceGeneratorView.Generate(collector);
                    break;
                case GenerateType.SubView:
                    UIReferenceGeneratorSubView.Generate(collector);
                    break;
                case GenerateType.Item:
                    UIReferenceGeneratorItem.Generate(collector);
                    break;
            }
        }
    }
}
#endif