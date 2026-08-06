#region Header
/**
 * IJsonWrapper.cs
 * 表示能够处理各种JSON的类型的接口
 *   数据。这主要用于通过JsonMapper映射对象时，以及
 * 它是由JsonData实现的。
 *
 * 作者放弃对此源代码的版权。有关更多详细信息，请参阅
 * 此发行版中包含的 COPYING 文件。
 **/
#endregion


using System.Collections;
using System.Collections.Specialized;


namespace BestHTTP.JSON.LitJson
{
    public enum JsonType
    {
        None,

        Object,
        Array,
        String,
        Int,
        Long,
        Double,
        Boolean
    }

    public interface IOrderedDictionary : IDictionary
    {
        new IDictionaryEnumerator GetEnumerator();
        void Insert(int index, object key, object value);
        void RemoveAt(int index);

        object this[int index]
        {
            get;
            set;
        }
    }

    public interface IJsonWrapper : IList, IOrderedDictionary
    {
        bool IsArray   { get; }
        bool IsBoolean { get; }
        bool IsDouble  { get; }
        bool IsInt     { get; }
        bool IsLong    { get; }
        bool IsObject  { get; }
        bool IsString  { get; }

        bool     GetBoolean ();
        double   GetDouble ();
        int      GetInt ();
        JsonType GetJsonType ();
        long     GetLong ();
        string   GetString ();

        void SetBoolean  (bool val);
        void SetDouble   (double val);
        void SetInt      (int val);
        void SetJsonType (JsonType type);
        void SetLong     (long val);
        void SetString   (string val);

        string ToJson ();
        void   ToJson (JsonWriter writer);
    }
}
