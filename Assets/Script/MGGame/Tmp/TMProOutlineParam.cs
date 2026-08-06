//====================================================
//Author:HDS
//Time  :2025/12/08 13:12:10
//Desc  :
//====================================================

using UnityEngine;

namespace XN
{
    public struct TMProOutlineParam
    {
        // 暂时都是默认0
        [Range(0, 1)] public float FaceSoftness;
        [Range(0, 1)] public float FaceDilate;
        [Range(0, 1)] public float OutlineThickness;

        public static TMProOutlineParam Get(TmproOutlineType type)
        {
            return type switch
            {
                TmproOutlineType.AlimamaShuHeiTi => new TMProOutlineParam { FaceDilate = 0.28f, OutlineThickness = 0.28f },
                TmproOutlineType.SourceHanScensSC => new TMProOutlineParam { FaceDilate = 0.28f, OutlineThickness = 0.28f },
                _ => new TMProOutlineParam(),
            };
        }
    }
}