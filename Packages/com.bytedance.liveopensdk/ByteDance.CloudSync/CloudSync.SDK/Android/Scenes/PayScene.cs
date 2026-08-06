using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    public sealed class PayOrder
    {

        //游戏订单ID, request and important
        public string OrderId
        {
            get
            {
                return AndroidJavaObject.Call<string>("getOrderId");
            }
            private set
            {
                AndroidJavaObject.Call("setOrderId", value);
            }
        }
        //订单金额, 单位:分
        public long OrderAmount
        {
            get
            {
                return AndroidJavaObject.Call<long>("getOrderAmount");
            }
            set
            {
                AndroidJavaObject.Call("setOrderAmount", value);
            }
        }
        //商品ID
        public string GoodsId
        {
            get
            {
                return AndroidJavaObject.Call<string>("getGoodsId");
            }
            set
            {
                AndroidJavaObject.Call("setGoodsId", value);
            }
        }
        //商品名称
        public string GoodsName
        {
            get
            {
                return AndroidJavaObject.Call<string>("getGoodsName");
            }
            set
            {
                AndroidJavaObject.Call("setGoodsName", value);
            }
        }
        //
        public string Extra
        {
            get
            {
                return AndroidJavaObject.Call<string>("getExtra");
            }
            set
            {
                AndroidJavaObject.Call("setExtra", value);
            }
        }
        internal AndroidJavaObject AndroidJavaObject { get; }
        public PayOrder(string orderId, long orderAmount, string goodsId, string goodsName, string extra = null)
        {
            AndroidJavaObject = new AndroidJavaObject("com.bytedance.cloudplay.gamesdk.api.model.PayOrder");
            OrderId = orderId;
            OrderAmount = orderAmount;
            GoodsId = goodsId;
            GoodsName = goodsName;
            Extra = extra;

        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PayOrder: ");
            sb.AppendLine($"OrderId: {OrderId}");
            sb.AppendLine($"OrderAmount: {OrderAmount}");
            sb.AppendLine($"GoodsId: {GoodsId}");
            sb.AppendLine($"GoodsName: {GoodsName}");
            sb.AppendLine($"Extra: {Extra}");
            return sb.ToString();
        }
        internal PayOrder(AndroidJavaObject ajo)
        {
            AndroidJavaObject = ajo;
        }
        ~PayOrder()
        {
            AndroidJavaObject.Dispose();
        }

    }
    public class PayScene : CloudGameScene
    {
        private const string TAG = "PayScene";
        internal static AndroidJavaClass PaySceneJavaClass { get; set; } = new AndroidJavaClass("com.bytedance.cloudplay.gamesdk.api.scene.PayScene");
        internal PayScene(AndroidJavaObject ajo) : base(ajo)
        {

        }
        /// <summary>
        /// 注册回调函数
        /// </summary>
        /// <param name="sendPayOrderCallBack">void(bool isSuccess,string orderId)</param>
        /// <param name="clientPayCallBack">void(bool isSuccess,string orderId)</param>
        public void SetSceneListener(SendPayOrderCallBack sendPayOrderCallBack, ClientPayCallBack clientPayCallBack)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("setSceneListener", new PaySceneListener(sendPayOrderCallBack, clientPayCallBack)), TAG);
        }
        public void SendPayOrder(PayOrder payOrder)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("sendPayOrder", payOrder.AndroidJavaObject), TAG);
        }
        public void SendRefundOrder(string orderId)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("sendRefundOrder", orderId), TAG);
        }
        public void SetSecret(string secret)
        {
            LogUtils.WrapExceptionLog(() => SceneJavaObject.Call("setSecret", secret), TAG);
        }
    }
}
