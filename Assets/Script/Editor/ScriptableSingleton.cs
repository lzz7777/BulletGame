//====================================================
//Author:Makka Pakka
//Time  :2022-12-01 19:37:01
//Desc  :
//====================================================
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace XN.Tools
{

    public interface IOnLoad
    {
        void OnLoad();
    }

    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        public string FilePath
        {
            get => GetFilePath();
        }

        private static T mInstance;

        public static T Inst
        {
            get
            {
                mInstance ??= LoadOrCreate();

                return mInstance;
            }
        }

        public static T LoadOrCreate()
        {
            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
#if UNITY_EDITOR
                T instance = AssetDatabase.LoadAssetAtPath<T>(filePath);
#else
            T instance = Resources.Load<T>(filePath);
#endif
                if (!instance)
                {
#if UNITY_EDITOR
                    instance = CreateInstance<T>();

                    AssetDatabase.CreateAsset(instance, filePath);

                    AssetDatabase.Refresh();
#endif
                }

                if (instance is IOnLoad load) load.OnLoad();
                return instance;
            }
            else throw new ArgumentNullException($"{nameof(ScriptableSingleton<T>)}: 请指定单例存档路径");
        }

        protected static string GetFilePath() => typeof(T).GetCustomAttributes(true).Cast<FilePathAttribute>().FirstOrDefault(p => p != null)?.FilePath;
    }

}