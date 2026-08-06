//====================================================
//Author:HDS
//Time  :2024/07/03 14:07:10
//Desc  :
//====================================================

using cfg;
using Unity.Mathematics;

namespace cfg
{
    public static class LubanMappingTool
    {
        public static float2 NewFloat2(Vec2 vec2) => new(vec2.X, vec2.Y);
        public static float3 NewFloat3(Vec3 vec3) => new(vec3.X, vec3.Y, vec3.Z);
        public static float4 NewFloat4(Vec4 vec4) => new(vec4.X, vec4.Y, vec4.Z, vec4.W);
    }
}