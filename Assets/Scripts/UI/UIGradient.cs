using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color topColor = new Color(0f, 0f, 0f, 0.75f);    // Negro semitransparente arriba
    public Color bottomColor = new Color(0f, 0f, 0f, 0f);     // Totalmente transparente abajo

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex vertex = new UIVertex();
        float minimum = float.MaxValue;
        float maximum = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            minimum = Mathf.Min(minimum, vertex.position.y);
            maximum = Mathf.Max(maximum, vertex.position.y);
        }

        float span = maximum - minimum;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = span > Mathf.Epsilon ? (vertex.position.y - minimum) / span : 0f;
            vertex.color = Color.Lerp(bottomColor, topColor, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}
