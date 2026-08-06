// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using UnityEngine;

namespace ByteDance.CloudSync
{
    public static class CloudUnityKeyCode
    {
        private static readonly Dictionary<KeyCode, CharConfig> KeycodeToCharacterMap;
        private const char Char0 = '\0';

        static CloudUnityKeyCode()
        {
            KeycodeToCharacterMap = new Dictionary<KeyCode, CharConfig>
            {
                { KeyCode.Tab, new CharConfig('\t', '\t') },
                { KeyCode.Return, new CharConfig('\n', '\n') }, // 13, // 0x0000000D
                { KeyCode.Escape, new CharConfig((char)0) }, // 27, // 0x0000001B
                { KeyCode.Space, new CharConfig(' ') }, // 32, // 0x00000020

                { KeyCode.Exclaim, new CharConfig('!') }, // 33, // 0x00000021
                { KeyCode.DoubleQuote, new CharConfig('\"') },
                { KeyCode.Hash, new CharConfig('#') },
                { KeyCode.Dollar, new CharConfig('$') },
                { KeyCode.Percent, new CharConfig('%') },
                { KeyCode.Ampersand, new CharConfig('&') },
                { KeyCode.Quote, new CharConfig('\'') },
                { KeyCode.LeftParen, new CharConfig('(') },
                { KeyCode.RightParen, new CharConfig(')') },
                { KeyCode.Asterisk, new CharConfig('*') },
                { KeyCode.Plus, new CharConfig('+') },
                { KeyCode.Comma, new CharConfig(',', '<') },
                { KeyCode.Minus, new CharConfig('-', '_') },
                { KeyCode.Period, new CharConfig('.', '>') },
                { KeyCode.Slash, new CharConfig('/', '?') }, // 47, // 0x0000002F

                { KeyCode.Alpha0, new CharConfig('0', ')') }, // 48, // 0x00000030
                { KeyCode.Alpha1, new CharConfig('1', '!') },
                { KeyCode.Alpha2, new CharConfig('2', '@') },
                { KeyCode.Alpha3, new CharConfig('3', '#') },
                { KeyCode.Alpha4, new CharConfig('4', '$') },
                { KeyCode.Alpha5, new CharConfig('5', '%') },
                { KeyCode.Alpha6, new CharConfig('6', '^') },
                { KeyCode.Alpha7, new CharConfig('7', '&') },
                { KeyCode.Alpha8, new CharConfig('8', '*') },
                { KeyCode.Alpha9, new CharConfig('9', '(') }, // 57, // 0x00000039

                { KeyCode.Colon, new CharConfig(':') },
                { KeyCode.Semicolon, new CharConfig(';', ':') },
                { KeyCode.Less, new CharConfig('<') },
                { KeyCode.Equals, new CharConfig('=', '+') },
                { KeyCode.Greater, new CharConfig('>') },
                { KeyCode.Question, new CharConfig('?') },
                { KeyCode.At, new CharConfig('@') }, // 64, // 0x00000040

                { KeyCode.LeftBracket, new CharConfig('[', '{') }, // 91, // 0x0000005B
                { KeyCode.Backslash, new CharConfig('\\', '|') },
                { KeyCode.RightBracket, new CharConfig(']', '}') },
                { KeyCode.Caret, new CharConfig('^') },
                { KeyCode.Underscore, new CharConfig('_') },
                { KeyCode.BackQuote, new CharConfig('`', '~') }, // 96, // 0x00000060

                { KeyCode.A, new CharConfig('a', 'A') }, // 97, // 0x00000061
                { KeyCode.Z, new CharConfig('z', 'Z') }, // Z = 122, // 0x0000007A

                { KeyCode.LeftCurlyBracket, new CharConfig('{') }, // 123, // 0x0000007B
                { KeyCode.Pipe, new CharConfig('|') },
                { KeyCode.RightCurlyBracket, new CharConfig('}') },
                { KeyCode.Tilde, new CharConfig('~') },
                { KeyCode.Delete, new CharConfig(Char0) }, // 127, // 0x0000007F

                { KeyCode.Keypad0, new CharConfig('0', Char0) }, // 256, // 0x00000100
                { KeyCode.Keypad1, new CharConfig('1', Char0) },
                { KeyCode.Keypad2, new CharConfig('2', Char0) },
                { KeyCode.Keypad3, new CharConfig('3', Char0) },
                { KeyCode.Keypad4, new CharConfig('4', Char0) },
                { KeyCode.Keypad5, new CharConfig('5', Char0) },
                { KeyCode.Keypad6, new CharConfig('6', Char0) },
                { KeyCode.Keypad7, new CharConfig('7', Char0) },
                { KeyCode.Keypad8, new CharConfig('8', Char0) },
                { KeyCode.Keypad9, new CharConfig('9', Char0) }, // 265, // 0x00000109

                { KeyCode.KeypadPeriod, new CharConfig('.', Char0) },
                { KeyCode.KeypadDivide, new CharConfig('/') },
                { KeyCode.KeypadMultiply, new CharConfig('*') },
                { KeyCode.KeypadMinus, new CharConfig('-') },
                { KeyCode.KeypadPlus, new CharConfig('+') },
                { KeyCode.KeypadEnter, new CharConfig('\n', '\n') },
                { KeyCode.KeypadEquals, new CharConfig('=', Char0) },
            };
        }

        public struct CharConfig
        {
            public char ch;

            /// shifted
            public char sch;

            public CharConfig(char c)
            {
                ch = c;
                sch = c;
            }

            public CharConfig(char c, char sc)
            {
                ch = c;
                sch = sc;
            }
        }

        public static char ToCharacter(KeyCode keyCode, EventModifiers modifiers)
        {
            var isShiftOn = modifiers.HasFlag(EventModifiers.Shift);
            if (!HasCharacter(keyCode))
                return Char0;
            var config = GetCharacterConfig(keyCode);
            return isShiftOn ? config.sch : config.ch;
        }

        public static bool HasCharacter(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case >= KeyCode.Alpha0 and <= KeyCode.Alpha9:
                case >= KeyCode.A and <= KeyCode.Z:
                case >= KeyCode.Keypad0 and <= KeyCode.Keypad9:
                    return true;
            }

            return KeycodeToCharacterMap.ContainsKey(keyCode);
        }

        public static CharConfig GetCharacterConfig(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case >= KeyCode.Alpha0 and <= KeyCode.Alpha9:
                    return KeycodeToCharacterMap[keyCode];
                case >= KeyCode.A and <= KeyCode.Z:
                    var offset = (char)(keyCode - KeyCode.A);
                    return new CharConfig((char)('a' + offset), (char)('A' + offset));
                case >= KeyCode.Keypad0 and <= KeyCode.Keypad9:
                    return KeycodeToCharacterMap[keyCode];
            }

            return KeycodeToCharacterMap.GetValueOrDefault(keyCode);
        }
    }
}