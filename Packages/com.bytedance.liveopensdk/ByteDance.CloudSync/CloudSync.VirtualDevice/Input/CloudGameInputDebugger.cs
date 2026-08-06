using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ByteDance.CloudSync
{
    public class CloudGameInputDebugger : MonoBehaviour
    {
        public GameObject downEffect;
        public GameObject upEffect;
        public GameObject moveEffect;
        public Camera cam;
        public RawImage image;

        private float _x;
        private float _y;

        void Start()
        {
            var rt = new RenderTexture(Screen.width/4, Screen.height/4, 0);
            rt.name = "CloudGameInputDebuggerRT";
            cam.targetTexture = rt;
            image.texture = rt;
        }

        public void HandleOperate(PlayerOperate op)
        {
            if (op.op_type == OperateType.MOUSE)
            {
                var mouseData = op.event_data.ToObject<CloudMouseData>();
                var action = mouseData.action;

                if (MouseAction.MOVE == action)
                {

                    _x = (float)mouseData.x * cam.pixelWidth;
                    _y = (1f - (float)mouseData.y) * cam.pixelHeight;
                }

                var position = cam.ScreenToWorldPoint(new Vector3(_x, _y, 2));

                switch (action)
                {
                    case MouseAction.DOWN:
                        OnOpMouseDown(position);
                        break;
                    case MouseAction.UP:
                        OnOpMouseUp(position);
                        break;
                    case MouseAction.MOVE:
                        OnOpMouseMove(position);
                        break;

                }
            }
            else if(op.op_type == OperateType.TOUCH)
            {
                var touchDatas = op.event_data.ToObject<List<CloudTouchData>>();

                foreach (var touchData in touchDatas)
                {
                    var x = (float)touchData.x * cam.pixelWidth;
                    var y = (1f - (float)touchData.y) * cam.pixelHeight;
                    var action = touchData.action;

                    var position = cam.ScreenToWorldPoint(new Vector3(x, y, 2));

                    switch ((TouchAction)action)
                    {
                        case TouchAction.DOWN:
                            OnOpMouseDown(position);
                            break;
                        case TouchAction.UP:
                            OnOpMouseUp(position);
                            break;
                        case TouchAction.MOVE:
                            OnOpMouseMove(position);
                            break;

                    }
                }
            }

        }

        private void OnOpMouseDown(Vector3 pos)
        {
            var effect = Instantiate(downEffect, pos,Quaternion.identity);
            effect.transform.SetParent(transform,true);
            effect.transform.localScale = Vector3.one;
            effect.SetActive(true);
            Destroy(effect, 2f);
            moveEffect.transform.position = pos;
            moveEffect.SetActive(true);

            var text = effect.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = $"#{VirtualDeviceSystem.CurrentOperateFrame}";
            }
        }

        private void OnOpMouseUp(Vector3 pos)
        {
            var effect = Instantiate(upEffect, pos,Quaternion.identity);
            effect.transform.SetParent(transform,true);
            effect.transform.localScale = Vector3.one;
            effect.SetActive(true);
            Destroy(effect, 2f);
            moveEffect.transform.position = pos;
            moveEffect.SetActive(false);
        }

        private void OnOpMouseMove(Vector3 pos)
        {
            moveEffect.transform.position = pos;
        }

    }
}
