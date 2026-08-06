// Copyright@www.bytedance.com
// Author: DONEY Dong
// Date: 2024/07/16
// Description:

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using UnityEngine;
using UnityEngine.InputSystem;

#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace ByteDance.CloudSync
{
    /// <summary>
    /// PC键盘键码  对应PC键盘事件：`<see cref="OperateType.PC_KEYBOARD"/>` (=13)，对应事件数据结构：`<see cref="CloudGame.CloudPCKeyboardData"/>`
    /// </summary>
    /// <remarks>
    /// <para>注意区分：接入Android云游戏时，对应的键值标准为相关文档中的Android“按键事件”，而不是Windows“键盘操作事件”</para>
    /// <para>@see: 《GameService之DataChannel协议》#键盘操作事件 https://bytedance.larkoffice.com/wiki/HnzuwUfCFiRISjki9Goc6iUpnFh#3za5Iz </para>
    /// <para>@see: Windows键盘虚拟键码表 https://bytedance.larkoffice.com/docs/doccnfO8DfBdoaIRUIXD5ptCGEI</para>
    /// </remarks>
    public static class CloudPCKeycode
    {
        /*
         * Virtual Keys, Standard Set
         */
        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;
        public const int VK_CANCEL = 0x03;
        public const int VK_MBUTTON = 0x04; /* NOT contiguous with L & RBUTTON */

        // VK_XBUTTON1 =       0x05,    /* NOT contiguous with L & RBUTTON */
        // VK_XBUTTON2 =       0x06,    /* NOT contiguous with L & RBUTTON */

        /*
         * 0x07 : reserved
         */


        public const int VK_BACK = 0x08;
        public const int VK_TAB = 0x09;

        /*
         * 0x0A - 0x0B : reserved
         */

        public const int VK_CLEAR = 0x0C;
        public const int VK_RETURN = 0x0D;

        /*
         * 0x0E - 0x0F : unassigned
         */

        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const int VK_PAUSE = 0x13;
        public const int VK_CAPITAL = 0x14;

        public const int VK_KANA = 0x15;
        public const int VK_HANGEUL = 0x15; /* old name - should be here for compatibility */
        public const int VK_HANGUL = 0x15;
        public const int VK_IME_ON = 0x16;
        public const int VK_JUNJA = 0x17;
        public const int VK_ = 0x18;
        public const int VK_HANJA = 0x19;
        public const int VK_KANJI = 0x19;
        public const int VK_IME_OFF = 0x1A;

        public const int VK_ESCAPE = 0x1B;

        public const int VK_CONVERT = 0x1C;
        public const int VK_NONCONVERT = 0x1D;
        public const int VK_ACCEPT = 0x1E;
        public const int VK_MODECHANGE = 0x1F;

        public const int VK_SPACE = 0x20;
        public const int VK_PRIOR = 0x21;
        public const int VK_NEXT = 0x22;
        public const int VK_END = 0x23;
        public const int VK_HOME = 0x24;
        public const int VK_LEFT = 0x25;
        public const int VK_UP = 0x26;
        public const int VK_RIGHT = 0x27;
        public const int VK_DOWN = 0x28;
        public const int VK_SELECT = 0x29;
        public const int VK_PRINT = 0x2A;
        public const int VK_EXECUTE = 0x2B;
        public const int VK_SNAPSHOT = 0x2C;
        public const int VK_INSERT = 0x2D;
        public const int VK_DELETE = 0x2E;
        public const int VK_HELP = 0x2F;

        /*
         * VK_0 - VK_9 are the same as ASCII '0' - '9' (0x30 - 0x39)
         * 0x3A - 0x40 : unassigned
         * VK_A - VK_Z are the same as ASCII 'A' - 'Z' (0x41 - 0x5A)
         */
        public const int VK_0 = 0x30; // 48
        public const int VK_1 = 0x31; // 49
        public const int VK_9 = 0x39; // 57
        public const int VK_A = 0x41; // 65
        public const int VK_Z = 0x5A; // 90

        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_APPS = 0x5D;

        /*
         * 0x5E : reserved
         */

        public const int VK_SLEEP = 0x5F;

        public const int VK_NUMPAD0 = 0x60;
        public const int VK_NUMPAD1 = 0x61;
        public const int VK_NUMPAD2 = 0x62;
        public const int VK_NUMPAD3 = 0x63;
        public const int VK_NUMPAD4 = 0x64;
        public const int VK_NUMPAD5 = 0x65;
        public const int VK_NUMPAD6 = 0x66;
        public const int VK_NUMPAD7 = 0x67;
        public const int VK_NUMPAD8 = 0x68;
        public const int VK_NUMPAD9 = 0x69;
        public const int VK_MULTIPLY = 0x6A;
        public const int VK_ADD = 0x6B;
        public const int VK_SEPARATOR = 0x6C;
        public const int VK_SUBTRACT = 0x6D;
        public const int VK_DECIMAL = 0x6E;
        public const int VK_DIVIDE = 0x6F;
        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_F11 = 0x7A;
        public const int VK_F12 = 0x7B;

        public const int VK_NUMLOCK = 0x90;     // NUM LOCK 键
        public const int VK_SCROLL = 0x91;      // SCROLL LOCK 键

        public const int VK_LSHIFT = 0xA0;      // 左 SHIFT 键
        public const int VK_RSHIFT = 0xA1;      // 右 SHIFT 键
        public const int VK_LCONTROL = 0xA2;    // 左 Ctrl 键
        public const int VK_RCONTROL = 0xA3;    // 右 Ctrl 键
        public const int VK_LMENU = 0xA4;       // 左 ALT 键
        public const int VK_RMENU = 0xA5;       // 右 ALT 键

        public const int VK_OEM_1 =	0xBA;       // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键;:
        public const int VK_OEM_PLUS =	0xBB;   // 对于任何国家/地区，键+
        public const int VK_OEM_COMMA =	0xBC;   // 对于任何国家/地区，键,
        public const int VK_OEM_MINUS =	0xBD;   // 对于任何国家/地区，键-
        public const int VK_OEM_PERIOD = 0xBE;  // 对于任何国家/地区，键.
        public const int VK_OEM_2 =	0xBF;	    // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键/?
        public const int VK_OEM_3 =	0xC0;	    // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键`~
        // 0xC1-DA: reserved
        public const int VK_OEM_4 =	0xDB;	    // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键[{
        public const int VK_OEM_5 =	0xDC;	    // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键\\|
        public const int VK_OEM_6 =	0xDD;	    // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键]}
        public const int VK_OEM_7 =	0xDE;	    // 用于杂项字符；它可能因键盘而异。 对于美国标准键盘，键'"
        public const int VK_OEM_8 =	0xDF;	    // 用于杂项字符；它可能因键盘而异。

        public static Key ToInputSystemKey(int code)
        {
            // mapping to Unity InputSystem key code
            switch (code)
            {
                // 空格 回车 ESC
                case VK_SPACE:
                    return Key.Space;
                case VK_RETURN:
                    return Key.Enter;
                case VK_ESCAPE:
                    return Key.Escape;
                // ↑↓←→
                case VK_UP:
                    return Key.UpArrow;
                case VK_DOWN:
                    return Key.DownArrow;
                case VK_LEFT:
                    return Key.LeftArrow;
                case VK_RIGHT:
                    return Key.RightArrow;
                case VK_0:
                    return Key.Digit0;
                // 1234
                case >= VK_1 and <= VK_9:
                    return code - VK_1 + Key.Digit1;
                // WSAD
                case >= VK_A and <= VK_Z:
                    return code - VK_A + Key.A;
                default:
                    return Key.None;
            }
        }

        public static KeyCode ToUnityKeyCode(int code)
        {
            switch (code)
            {
                // Backspace 空格 回车 ESC
                case VK_BACK:
                    return KeyCode.Backspace;
                case VK_SPACE:
                    return KeyCode.Space;
                case VK_RETURN:
                    return KeyCode.Return;
                case VK_ESCAPE:
                    return KeyCode.Escape;
                // Ctrl Shift Alt Tab Caps
                case VK_LCONTROL:
                case VK_CONTROL:
                    return KeyCode.LeftControl;
                case VK_RCONTROL:
                    return KeyCode.RightControl;
                case VK_LSHIFT:
                case VK_SHIFT:
                    return KeyCode.LeftShift;
                case VK_RSHIFT:
                    return KeyCode.RightShift;
                case VK_LMENU:
                case VK_MENU:
                    return KeyCode.LeftAlt;
                case VK_RMENU:
                    return KeyCode.RightAlt;
                case VK_TAB:
                    return KeyCode.Tab;
                case VK_CAPITAL:
                    return KeyCode.CapsLock;
                // ; = , - . / ` [ \ ] '
                case VK_OEM_1:
                    return KeyCode.Semicolon;
                case VK_OEM_PLUS:
                    return KeyCode.Equals;
                case VK_OEM_COMMA:
                    return KeyCode.Comma;
                case VK_OEM_MINUS:
                    return KeyCode.Minus;
                case VK_OEM_PERIOD:
                    return KeyCode.Period;
                case VK_OEM_2:
                    return KeyCode.Slash;
                case VK_OEM_3:
                    return KeyCode.BackQuote;
                case VK_OEM_4:
                    return KeyCode.LeftBracket;
                case VK_OEM_5:
                    return KeyCode.Backslash;
                case VK_OEM_6:
                    return KeyCode.RightBracket;
                case VK_OEM_7:
                    return KeyCode.Quote;
                // insert delete home end page up page down
                case VK_INSERT:
                    return KeyCode.Insert;
                case VK_DELETE:
                    return KeyCode.Delete;
                case VK_HOME:
                    return KeyCode.Home;
                case VK_END:
                    return KeyCode.End;
                case VK_PRIOR:
                    return KeyCode.PageUp;
                case VK_NEXT:
                    return KeyCode.PageDown;
                // ↑↓←→
                case VK_UP:
                    return KeyCode.UpArrow;
                case VK_DOWN:
                    return KeyCode.DownArrow;
                case VK_LEFT:
                    return KeyCode.LeftArrow;
                case VK_RIGHT:
                    return KeyCode.RightArrow;
                // 0 - 9
                case >= VK_0 and <= VK_9:
                    return code - VK_0 + KeyCode.Alpha0;
                // Keypad
                case >= VK_NUMPAD0 and <= VK_NUMPAD9:
                    return code - VK_NUMPAD0 + KeyCode.Keypad0;
                case VK_NUMLOCK:
                    return KeyCode.Numlock;
                case VK_MULTIPLY:
                    return KeyCode.KeypadMultiply;
                case VK_ADD:
                    return KeyCode.KeypadPlus;
                case VK_SUBTRACT:
                    return KeyCode.KeypadMinus;
                case VK_DECIMAL:
                    return KeyCode.KeypadPeriod;
                case VK_DIVIDE:
                    return KeyCode.KeypadDivide;
                // A - Z
                case >= VK_A and <= VK_Z:
                    return code - VK_A + KeyCode.A;
                // F1 - F12
                case >= VK_F1 and <= VK_F12:
                    return code - VK_F1 + KeyCode.F1;
                default:
                    return KeyCode.None;
            }
        }

        public static readonly KeyCode[] SupportedKeys = {
            KeyCode.Backspace,
            KeyCode.Space,
            KeyCode.Return,
            KeyCode.Escape,

            KeyCode.LeftControl,
            KeyCode.LeftShift,
            KeyCode.Tab,
            KeyCode.CapsLock,

            KeyCode.Semicolon,
            KeyCode.Equals,
            KeyCode.Comma,
            KeyCode.Minus,
            KeyCode.Period,
            KeyCode.Slash,
            KeyCode.BackQuote,
            KeyCode.LeftBracket,
            KeyCode.Backslash,
            KeyCode.RightBracket,
            KeyCode.Quote,

            KeyCode.Insert,
            KeyCode.Delete,
            KeyCode.Home,
            KeyCode.End,
            KeyCode.PageUp,
            KeyCode.PageDown,

            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,

            KeyCode.Keypad0,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6,
            KeyCode.Keypad7,
            KeyCode.Keypad8,
            KeyCode.Keypad9,

            KeyCode.Numlock,
            KeyCode.KeypadMultiply,
            KeyCode.KeypadPlus,
            KeyCode.KeypadMinus,
            KeyCode.KeypadEnter,
            KeyCode.KeypadPeriod,
            KeyCode.KeypadDivide,

            KeyCode.Alpha0,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,

            KeyCode.A,
            KeyCode.B,
            KeyCode.C,
            KeyCode.D,
            KeyCode.E,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.I,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.M,
            KeyCode.N,
            KeyCode.O,
            KeyCode.P,
            KeyCode.Q,
            KeyCode.R,
            KeyCode.S,
            KeyCode.T,
            KeyCode.U,
            KeyCode.V,
            KeyCode.W,
            KeyCode.X,
            KeyCode.Y,
            KeyCode.Z,

            KeyCode.F1,
            KeyCode.F2,
            KeyCode.F3,
            KeyCode.F4,
            KeyCode.F5,
            KeyCode.F6,
            KeyCode.F7,
            KeyCode.F8,
            KeyCode.F9,
            KeyCode.F10,
        };
    }
}