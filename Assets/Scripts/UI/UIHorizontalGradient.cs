using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Horizontal Gradient")]
public class UIHorizontalGradient : BaseMeshEffect
{
    public Color leftColor = new Color(0f, 0f, 0f, 0.75f);   // Oscuro a la izquierda
    public Color rightColor = new Color(0f, 0f, 0f, 0f);     // Transparente a la derecha

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex vertex = new UIVertex();
        float minimum = float.MaxValue;
        float maximum = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            minimum = Mathf.Min(minimum, vertex.position.x);
            maximum = Mathf.Max(maximum, vertex.position.x);
        }

        float span = maximum - minimum;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = span > Mathf.Epsilon ? (vertex.position.x - minimum) / span : 0f;
            vertex.color = Color.Lerp(leftColor, rightColor, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}
