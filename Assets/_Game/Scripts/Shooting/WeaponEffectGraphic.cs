using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Small code-native silhouettes; an optional sprite replaces the mesh without changing behaviour.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class WeaponEffectGraphic : MaskableGraphic
{
    [SerializeField] private WeaponShotVisualProfile.Shape shape;
    [SerializeField] private Sprite sprite;
    public override Texture mainTexture => sprite != null ? sprite.texture : Texture2D.whiteTexture;

    public void Configure(WeaponShotVisualProfile.Shape silhouette, Sprite art, Color tint)
    {
        shape = silhouette;
        sprite = art;
        color = tint;
        raycastTarget = false;
        SetAllDirty();
    }

    private static readonly System.Collections.Generic.Dictionary<WeaponShotVisualProfile.Shape, Vector2[]> outlines = new();

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (sprite != null)
        {
            Vector4 uv = DataUtility.GetOuterUV(sprite);
            mesh.AddVert(new Vector2(rect.xMin, rect.yMin), color, new Vector2(uv.x, uv.y));
            mesh.AddVert(new Vector2(rect.xMin, rect.yMax), color, new Vector2(uv.x, uv.w));
            mesh.AddVert(new Vector2(rect.xMax, rect.yMax), color, new Vector2(uv.z, uv.w));
            mesh.AddVert(new Vector2(rect.xMax, rect.yMin), color, new Vector2(uv.z, uv.y));
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
            return;
        }

        if (shape == WeaponShotVisualProfile.Shape.Gun)
        {
            Rectangle(mesh, rect, new Vector2(-0.5f, -0.05f), new Vector2(0.4f, 0.25f));
            Rectangle(mesh, rect, new Vector2(-0.2f, -0.5f), new Vector2(0f, 0f));
            Rectangle(mesh, rect, new Vector2(0.4f, 0f), new Vector2(0.5f, 0.2f));
            return;
        }
        // Convex and star-shaped polygons share a centre fan. Local +X is forward.
        const int segments = 24;
        mesh.AddVert(rect.center, color, Vector2.zero);
        if (!outlines.TryGetValue(shape, out var outline))
        {
            outline = BuildOutline(shape);
            outlines.Add(shape, outline);
        }
        for (int i = 0; i < segments; i++)
            mesh.AddVert(rect.center + Vector2.Scale(outline[i], rect.size * 0.5f), color, Vector2.zero);
        for (int i = 0; i < segments; i++) mesh.AddTriangle(0, i + 1, (i + 1) % segments + 1);
    }

    private static Vector2[] BuildOutline(WeaponShotVisualProfile.Shape shape)
    {
        const int segments = 24;
        var outline = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float radius = 1f;
            switch (shape)
            {
                case WeaponShotVisualProfile.Shape.Rock:
                    radius = 0.88f + 0.12f * Mathf.Sin(i * 2.3f); break;
                case WeaponShotVisualProfile.Shape.Durian:
                    radius = i % 2 == 0 ? 1f : 0.73f; break;
                case WeaponShotVisualProfile.Shape.Burst:
                    radius = i % 2 == 0 ? 1f : 0.4f; break;
                case WeaponShotVisualProfile.Shape.Spear:
                case WeaponShotVisualProfile.Shape.Spike:
                    radius = 1f / (Mathf.Abs(Mathf.Cos(angle)) + Mathf.Abs(Mathf.Sin(angle))); break;
                case WeaponShotVisualProfile.Shape.Streak:
                    radius = 1f / Mathf.Max(Mathf.Abs(Mathf.Cos(angle)), Mathf.Abs(Mathf.Sin(angle))); break;
            }
            Vector2 point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (shape == WeaponShotVisualProfile.Shape.Slipper) point.y *= 0.75f + point.x * 0.25f;
            if (shape == WeaponShotVisualProfile.Shape.Rocket || shape == WeaponShotVisualProfile.Shape.Dart)
                point.y *= point.x > 0f ? 1f - point.x * 0.7f : 0.7f;
            if (shape == WeaponShotVisualProfile.Shape.Cylinder)
                point.x = Mathf.Clamp(point.x * 1.5f, -1f, 1f);
            outline[i] = point;
        }
        return outline;
    }

    private void Rectangle(VertexHelper mesh, Rect rect, Vector2 min, Vector2 max)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(rect.center + Vector2.Scale(min, rect.size), color, Vector2.zero);
        mesh.AddVert(rect.center + Vector2.Scale(new Vector2(min.x, max.y), rect.size), color, Vector2.zero);
        mesh.AddVert(rect.center + Vector2.Scale(max, rect.size), color, Vector2.zero);
        mesh.AddVert(rect.center + Vector2.Scale(new Vector2(max.x, min.y), rect.size), color, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
