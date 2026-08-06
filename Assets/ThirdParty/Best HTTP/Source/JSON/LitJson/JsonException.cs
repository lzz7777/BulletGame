#region Header
/**
 * JsonException.cs
 * 当发生解析错误时 LitJSON 抛出的基类。
 *
 * 作者放弃对此源代码的版权。有关更多详细信息，请参阅
 * 此发行版中包含的 COPYING 文件。
 **/
#endregion


using System;


namespace BestHTTP.JSON.LitJson
{
    public class JsonException : Exception
    {
        public JsonException () : base ()
        {
        }

        internal JsonException (ParserToken token) :
            base (String.Format (
                    "Invalid token '{0}' in input string", token))
        {
        }

        internal JsonException (ParserToken token,
                                Exception inner_exception) :
            base (String.Format (
                    "Invalid token '{0}' in input string", token),
                inner_exception)
        {
        }

        internal JsonException (int c) :
            base (String.Format (
                    "Invalid character '{0}' in input string", (char) c))
        {
        }

        internal JsonException (int c, Exception inner_exception) :
            base (String.Format (
                    "Invalid character '{0}' in input string", (char) c),
                inner_exception)
        {
        }


        public JsonException (string message) : base (message)
        {
        }

        public JsonException (string message, Exception inner_exception) :
            base (message, inner_exception)
        {
        }
    }
}
