using UnityEngine.InputSystem;

namespace XN
{
    public class MGInputSystemManager : MonoSingleton<MGInputSystemManager>
    {
        protected override void OnInit()
        {
            // 键盘事件监听
            Keyboard.current.onTextInput += OnTextInput;
        }

        protected override void OnRemove()
        {
        }

        void OnTextInput(char character)
        {
#if UNITY_EDITOR

            // if (character is >= '1' and <= '9')
            // {
            //     int num = (int)char.GetNumericValue(character);
            //
            //     EventsManager.BroadCast(GameEnum.GMAddSpeed, num - 1);
            // }

#endif
        }
    }
}