using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Deforms each graphic in panel space so the entire hierarchy shares one funnel.
[DisallowMultipleComponent]
public sealed class SlotGenieMeshEffect : BaseMeshEffect
{
    private RectTransform panel;
    private Vector2 target;
    private float amount;
    private float dockScale;
    private TMP_Text text;
    private Vector3[][] originalText;
    private readonly System.Collections.Generic.List<UIVertex> vertices = new(24);
    private Matrix4x4 toPanel, fromPanel;
    private Vector2 center, direction;
    private float extent;
    private static readonly Unity.Profiling.ProfilerMarker deformMarker = new("Game.Genie.Deform");

    private void PrepareWarp()
    {
        if (panel == null) return;
        toPanel = panel.worldToLocalMatrix * transform.localToWorldMatrix;
        fromPanel = transform.worldToLocalMatrix * panel.localToWorldMatrix;
        center = panel.rect.center;
        direction = target - center;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;
        direction.Normalize();
        extent = Mathf.Abs(direction.x) * panel.rect.width * 0.5f + Mathf.Abs(direction.y) * panel.rect.height * 0.5f;
    }

    protected override void Awake()
    {
        base.Awake();
        text = GetComponent<TMP_Text>();
        if (text != null) text.OnPreRenderText += DeformText;
    }

    public void Configure(RectTransform root, Vector2 destination, float progress, float minimumScale)
    {
        panel = root;
        target = destination;
        amount = Mathf.Clamp01(progress);
        dockScale = minimumScale;
        if (text != null)
        {
            if (text.havePropertiesChanged || originalText == null) text.ForceMeshUpdate();
            else
            {
                ApplyText(text.textInfo);
                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
        }
        else if (graphic != null) graphic.SetVerticesDirty();
    }

    private Vector3 Warp(Vector3 vertex)
    {
        if (panel == null || amount <= 0f) return vertex;
        Vector3 point = toPanel.MultiplyPoint3x4(vertex);
        float along = Vector2.Dot((Vector2)point - center, direction);
        float proximity = Mathf.InverseLerp(-extent, extent, along);
        // Edge nearest the dock leads; the far edge follows, creating the Genie neck.
        float pull = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((amount - (1f - proximity) * 0.38f) / 0.62f));
        Vector2 collapsed = target + ((Vector2)point - center) * dockScale;
        point = Vector3.Lerp(point, new Vector3(collapsed.x, collapsed.y, point.z), pull);
        return fromPanel.MultiplyPoint3x4(point);
    }

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || amount <= 0f || text != null) return;
        using var measurement = deformMarker.Auto();
        PrepareWarp();
        vertices.Clear();
        helper.GetUIVertexStream(vertices);
        helper.Clear();
        for (int i = 0; i + 2 < vertices.Count; i += 3)
            AddTriangle(helper, vertices[i], vertices[i + 1], vertices[i + 2], 3);
    }

    private void AddTriangle(VertexHelper helper, UIVertex a, UIVertex b, UIVertex c, int depth)
    {
        if (depth > 0)
        {
            UIVertex ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
            AddTriangle(helper, a, ab, ca, depth - 1);
            AddTriangle(helper, ab, b, bc, depth - 1);
            AddTriangle(helper, ca, bc, c, depth - 1);
            AddTriangle(helper, ab, bc, ca, depth - 1);
            return;
        }
        int index = helper.currentVertCount;
        a.position = Warp(a.position); b.position = Warp(b.position); c.position = Warp(c.position);
        helper.AddVert(a); helper.AddVert(b); helper.AddVert(c);
        helper.AddTriangle(index, index + 1, index + 2);
    }

    private static UIVertex Midpoint(UIVertex a, UIVertex b)
    {
        a.position = (a.position + b.position) * 0.5f;
        a.color = Color32.Lerp(a.color, b.color, 0.5f);
        a.uv0 = (a.uv0 + b.uv0) * 0.5f;
        a.uv1 = (a.uv1 + b.uv1) * 0.5f;
        return a;
    }

    private void DeformText(TMP_TextInfo info)
    {
        // TMP supplies fresh, undeformed geometry on every regeneration.
        if (originalText == null || originalText.Length != info.meshInfo.Length)
            originalText = new Vector3[info.meshInfo.Length][];
        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            var mesh = info.meshInfo[m];
            if (originalText[m] == null || originalText[m].Length < mesh.vertexCount)
                originalText[m] = new Vector3[mesh.vertices.Length];
            System.Array.Copy(mesh.vertices, originalText[m], mesh.vertexCount);
        }
        ApplyText(info);
    }

    private void ApplyText(TMP_TextInfo info)
    {
        using var measurement = deformMarker.Auto();
        PrepareWarp();
        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            var mesh = info.meshInfo[m];
            for (int i = 0; i < mesh.vertexCount; i++) mesh.vertices[i] = Warp(originalText[m][i]);
        }
    }

    protected override void OnDisable()
    {
        amount = 0f;
        if (text != null && originalText != null)
        {
            ApplyText(text.textInfo);
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        if (text != null) text.OnPreRenderText -= DeformText;
        base.OnDestroy();
    }
}
