using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UILocalUV : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        // 获取当前 UI 节点的真实宽高和坐标边界
        Rect bounds = graphic.rectTransform.rect;
        UIVertex vert = new UIVertex();

        // 遍历所有顶点，计算相对 (0~1) 的本地 UV
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);

            // 无论图集怎么变，基于 RectTransform 算出来的 UV 永远是 0~1 的正方形
            float u = (vert.position.x - bounds.xMin) / bounds.width;
            float v = (vert.position.y - bounds.yMin) / bounds.height;

            // 将计算好的本地 UV 塞进 uv1 通道 (Shader 里的 TEXCOORD1)
            vert.uv1 = new Vector2(u, v);
            
            vh.SetUIVertex(vert, i);
        }
    }
}