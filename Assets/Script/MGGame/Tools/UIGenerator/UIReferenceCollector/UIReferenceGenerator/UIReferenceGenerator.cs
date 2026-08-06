#if UNITY_EDITOR
using System;

namespace XN
{
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