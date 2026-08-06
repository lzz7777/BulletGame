using UnityEngine;

namespace XN
{
    public class WheelRotation : MonoBehaviour
    {
        public bool Rotating = false;

        public float Speed = 2;

        private Transform SelfTrans;

        private void Awake()
        {
            SelfTrans = transform;
        }

        void Update()
        {
            if (!Rotating) return;

            SelfTrans.rotation = Quaternion.Euler(0, 0, SelfTrans.rotation.eulerAngles.z - Time.deltaTime * 1000 * Speed);
        }
    }
}