using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

namespace XN
{
    public class ItemStarList : MonoBehaviour
    {
        public List<Transform> star5List = new();
        public TMP_Text textMore5;

        [Button("测试")]
        public void SetStarNum(int activeNum)
        {
            if (0 <= activeNum && activeNum <= 5)
            {
                for (int i = 0; i < star5List.Count; i++)
                {
                    var transform = star5List[i];
                    transform.parent.gameObject.SetActive(true);
                    transform.gameObject.SetActive(i<activeNum);
                }
                textMore5.gameObject.SetActive(false);
            }
            else
            {
                foreach (var obj in star5List)
                {
                    obj.parent.gameObject.SetActive(false);
                }
                textMore5.gameObject.SetActive(true);
                textMore5.SetText($"<size=36><sprite=3></size>x{activeNum}");
            }
        }

    }
}