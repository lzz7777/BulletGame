using System.Linq;
using ByteDance.CloudSync.TextureProvider;
using UnityEngine;

namespace ByteDance.CloudSync.UGUI
{
    /// <summary>
    /// 主播视图界面
    /// </summary>
    public class UCloudView : MonoBehaviour, ICloudView
    {
        public SeatIndex Index { get; set; }
        public int IntIndex => Index.ToInt();

        /// <summary>
        /// 根 Canvas
        /// </summary>
        public Canvas canvas;

        public ScreenTextureProviderCollection providerCollection;

        public GameObject GameObject => gameObject;

        public Transform UIRoot => canvas.transform;

        /// <summary>
        /// 对应用户座位
        /// </summary>
        /// <remarks>
        /// 在云同步初始化成功后，座位Seat总是保持有对象、非空。注意，Seat非空并不表示座位上就有用户。
        /// 若要判断Seat上用户是否加入、离开，请处理事件回调 <see cref="OnPlayerJoined"/>、<see cref="OnPlayerLeaving"/>、或判断状态枚举 <see cref="ICloudSeat.State"/>
        /// </remarks>
        public virtual ICloudSeat Seat => _seat ??= ICloudSync.Instance.SeatManager?.GetSeat(Index);

        private ICloudSeat _seat;
        private static readonly SdkDebugLogger Debug = new(nameof(UCloudView));

        protected virtual void Awake()
        {
            Debug.Assert(Index != SeatIndex.Invalid, "Assert Index");
            CloudSyncSdk.NotifyUsingCloudSync(true);
        }

        /// <summary>
        /// 事件回调：当 ICloudSeat 初始化完成，并且有玩家加入了此座位
        /// </summary>
        /// <param name="seat">对应的座位</param>
        public virtual void OnPlayerJoined(ICloudSeat seat)
        {
            CGLogger.Log($"CloudView OnPlayerJoined index: {seat.Index}");
        }

        /// 兼容：本地启动单人模式，不使用云同步、不使用云启动
        public virtual void InitLocalSingle()
        {
        }

        /// <summary>
        /// 事件回调：当玩家正在离开此座位
        /// </summary>
        /// <param name="seat">对应的座位</param>
        public virtual void OnPlayerLeaving(ICloudSeat seat)
        {
            CGLogger.Log($"CloudView OnPlayerLeaving index: {seat.Index}");
        }

        /// <summary>
        /// 事件回调：当Host主机关闭玩法、云游戏实例即将销毁，可以将非 Host 主播踢回去
        /// </summary>
        public virtual void OnWillDestroy(ICloudSeat seat, DestroyInfo destroyInfo)
        {
            CGLogger.Log($"CloudView OnWillDestroy index: {seat.Index}, time: {destroyInfo.Time}, reason: {destroyInfo.Reason}");
        }

        // Editor-only event
        protected virtual void OnValidate()
        {
            // CGLogger.Log($"云同步-UCloudView OnValidate {gameObject.name}"); // local debug only
            if (!CloudSyncUtil.IsInActiveScene(gameObject))
                return;

            var sceneName = gameObject.scene.name;
            CGLogger.Log($"云同步-UCloudView OnValidate, {CloudSyncUtil.GetTransformPath(transform)}, scene: {sceneName}"); // local debug only
            CloudSyncSdk.NotifyUsingCloudSync(true);
            ValidateInputSystemSettings();
        }

        internal interface IEditorValidator
        {
            bool IsEditorBusy();
            void ValidateInputSystemSettings();
            bool IsInputSystemSettingsChecked();
        }

        internal static IEditorValidator EditorValidator; // static state, 编译时重置状态
        private bool _inputSystemSettingsValidated; // local state, 组件重新载入时重置状态

        private void ValidateInputSystemSettings()
        {
            if (!Application.isEditor)
                return;
            var validator = EditorValidator;
            if (validator == null)
            {
                Debug.LogError("Assert InputSystemSettingsValidator failed!");
                return;
            }

            if (validator.IsInputSystemSettingsChecked() && _inputSystemSettingsValidated)
                return;

            if (!gameObject.scene.IsValid())
                return;

            var views = FindObjectsOfType<UCloudView>(true);
            if (views.Length > 1 && views.Skip(1).Contains(this))
                return;

            validator.ValidateInputSystemSettings();
            _inputSystemSettingsValidated = validator.IsInputSystemSettingsChecked();
        }
    }
}