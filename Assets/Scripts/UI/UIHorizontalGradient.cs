using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Horizontal Gradient")]
public class UIHorizontalGradient : BaseMeshEffect
{
    public Color leftColor = new Color(0f, 0f, 0f, 0.75f);   // Oscuro a la izquierda
    public Color rightColor = new Color(0f, 0f, 0f, 0f);     // Transparente a la derecha

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        UIVertex vertex = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            // Aplica el color según la posición horizontal (X)
            vertex.color = Color.Lerp(leftColor, rightColor, (vertex.position.x - GetMinX(vh)) / GetWidth(vh));
            vh.SetUIVertex(vertex, i);
        }
    }

    private float GetMinX(VertexHelper vh)
    {
        UIVertex vertex = new UIVertex();
        float minX = float.MaxValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.x < minX) minX = vertex.position.x;
        }
        return minX;
    }

    private float GetWidth(VertexHelper vh)
    {
        UIVertex vertex = new UIVertex();
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.x < minX) minX = vertex.position.x;
            if (vertex.position.x > maxX) maxX = vertex.position.x;
        }
        return maxX - minX;
    }
}