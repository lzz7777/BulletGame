using UnityEngine;

namespace XN
{
    public class BackgroundCtrlStartingLine : BackgroundCtrlBase
    {
        public float speed = 5;
        public float outposX;
        public bool isOut;
        
        public override void Init()
        {
            isOut = false;
            transform.localPosition = new Vector3(-0.5f, -2.15f, 0);
        }

        public override void UpdateScene()
        {
        }

        private void Update()
        {
            if (!GameStateCtrl.IsGaming || isOut)
            {
                return;
            }

            if (transform.position.x < outposX)
            {
                isOut = true;
                return;
            }

            transform.position += Vector3.left * (speed * Time.deltaTime);
        }
    }
}