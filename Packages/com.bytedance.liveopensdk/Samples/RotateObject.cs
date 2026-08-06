using UnityEngine;


namespace Douyin.LiveOpenSDK.Samples
{
    public class RotateObject : MonoBehaviour
    {
        public float rotationSpeed = 50f; // 设置旋转速度

        void Update()
        {
            // 让Cube绕着Y轴旋转
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
