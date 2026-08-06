using UnityEngine;

namespace ByteDance.CloudSync.Mock
{
    public abstract class MockPopPanel : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}