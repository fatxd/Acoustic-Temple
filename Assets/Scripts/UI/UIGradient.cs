using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = new Color(0f, 0f, 0f, 0.75f);    // Negro semitransparente arriba
    public Color bottomColor = new Color(0f, 0f, 0f, 0f);     // Totalmente transparente abajo

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        UIVertex vertex = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.color = Color.Lerp(bottomColor, topColor, (vertex.position.y - GetMinY(vh)) / GetHeight(vh));
            vh.SetUIVertex(vertex, i);
        }
    }

    private float GetMinY(VertexHelper vh)
    {
        UIVertex vertex = new UIVertex();
        float minY = float.MaxValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.y < minY) minY = vertex.position.y;
        }
        return minY;
    }

    private float GetHeight(VertexHelper vh)
    {
        UIVertex vertex = new UIVertex();
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.y < minY) minY = vertex.position.y;
            if (vertex.position.y > maxY) maxY = vertex.position.y;
        }
        return maxY - minY;
    }
}