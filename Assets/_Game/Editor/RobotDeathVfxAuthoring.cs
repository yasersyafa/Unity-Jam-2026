using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the robot death VFX: placeholder debris atlas, particle materials, the prefab, and assigns it
/// to every enemy prefab. Safe to run again; replace the atlas PNG with final art using the same 4x2 grid.
/// </summary>
public static class RobotDeathVfxAuthoring
{
    private const string AtlasPath = "Assets/_Game/Arts/VFX/RobotDebrisAtlas.png";
    private const string AlphaMaterialPath = "Assets/_Game/Prefabs/VFX/RobotDeathVFX_Alpha.mat";
    private const string AdditiveMaterialPath = "Assets/_Game/Prefabs/VFX/RobotDeathVFX_Additive.mat";
    private const string PrefabPath = "Assets/_Game/Prefabs/VFX/RobotDeathVFX.prefab";
    private const string LegacyDeathVfxPath = "Assets/_Game/Prefabs/VFX/DeathVFX.prefab";
    private const string EnemyFolder = "Assets/_Game/Prefabs/Enemies";

    // Atlas layout (4x4): 0-4 debris, 5-6 lightning arcs, 7 soft spark, 8-10 comic fireball stages, 11 shockwave ring.
    private const int Columns = 4, Rows = 4, Cell = 128;

    [MenuItem("Tools/VFX/Build Robot Death VFX")]
    public static void Build()
    {
        if (!AtlasMatchesLayout())
        {
            if (File.Exists(AtlasPath)) Debug.LogWarning($"{AtlasPath} does not match the {Columns}x{Rows} layout; regenerating placeholder art.");
            WriteAtlas();
        }
        var atlas = ImportAtlas();
        var alpha = MakeMaterial(AlphaMaterialPath, atlas, additive: false);
        var additive = MakeMaterial(AdditiveMaterialPath, atlas, additive: true);
        var prefab = BuildPrefab(alpha, additive);
        int assigned = AssignToEnemies(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"Robot death VFX built at {PrefabPath}; assigned to {assigned} enemy prefabs.", prefab);
    }

    // ---------- Prefab ----------

    private static GameObject BuildPrefab(Material alpha, Material additive)
    {
        var root = new GameObject("RobotDeathVFX");
        try
        {
            Flash(Child(root.transform, "Flash"), additive);
            var debris = Child(root.transform, "Debris");
            Debris(debris, alpha);
            var trail = Child(debris.transform, "DebrisSparks");
            DebrisSparks(trail, additive);
            var subEmitters = debris.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(trail, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
            Sparks(Child(root.transform, "Sparks"), additive);
            Arcs(Child(root.transform, "Arcs"), additive);
            Smoke(Child(root.transform, "Smoke"), alpha);
            Fireball(Child(root.transform, "Fireball"), alpha);
            Shockwave(Child(root.transform, "Shockwave"), additive);
            CopyComicAccentLines(root.transform);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static ParticleSystem Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false; // VfxPool plays it
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        return system;
    }

    private static void Flash(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 0.1f;
        main.startLifetime = 0.09f;
        main.startSpeed = 0f;
        main.startSize = 2.6f;
        main.startColor = new Color(0.85f, 1f, 1f, 1f);
        main.maxParticles = 1;
        Burst(system, 1, 1);
        Shape(system, false);
        SizeOverLifetime(system, 0.6f, 1.25f);
        Fade(system, 0.4f);
        Frames(system, 7, 7);
        Render(system, material, ParticleSystemRenderMode.Billboard);
    }

    private static void Debris(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 1.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 1.5f;
        main.maxParticles = 16;
        Burst(system, 10, 14);
        // Cone along local +Z (away from the camera), tilted up so pieces arc before falling.
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.35f;
        shape.rotation = new Vector3(-20f, 0f, 0f);
        var rotation = system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-540f * Mathf.Deg2Rad, 540f * Mathf.Deg2Rad);
        SizeOverLifetime(system, 1f, 0.55f);
        Fade(system, 0.8f);
        Frames(system, 0, 4);
        Render(system, material, ParticleSystemRenderMode.Billboard);
    }

    private static void DebrisSparks(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 1.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.35f), new Color(0.5f, 0.95f, 1f));
        main.gravityModifier = 0.5f;
        main.maxParticles = 200;
        var emission = system.emission;
        emission.rateOverTime = 25f;
        Shape(system, true, 0.02f);
        Fade(system, 0.3f);
        Frames(system, 7, 7);
        Render(system, material, ParticleSystemRenderMode.Stretch);
    }

    private static void Sparks(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(0.55f, 1f, 1f));
        main.gravityModifier = 2f;
        main.maxParticles = 32;
        Burst(system, 20, 28);
        Shape(system, true, 0.2f);
        Fade(system, 0.5f);
        Frames(system, 7, 7);
        Render(system, material, ParticleSystemRenderMode.Stretch);
    }

    private static void Arcs(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 0.45f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.9f, 1f), Color.white);
        main.maxParticles = 12;
        // Three short bursts read as a flickering short circuit.
        var emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2, 3, 3, 0.12f) });
        Shape(system, true, 0.4f);
        Frames(system, 5, 6);
        Render(system, material, ParticleSystemRenderMode.Billboard);
    }

    private static void Smoke(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 0.9f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.16f, 0.15f, 0.18f, 0.65f);
        main.gravityModifier = -0.05f;
        main.maxParticles = 8;
        Burst(system, 4, 6);
        Shape(system, true, 0.3f);
        SizeOverLifetime(system, 0.7f, 1.4f);
        Fade(system, 0.6f);
        Frames(system, 7, 7);
        var renderer = Render(system, material, ParticleSystemRenderMode.Billboard);
        renderer.sortingFudge = 10f; // draw behind debris and sparks
    }

    private static void Fireball(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 1.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 6;
        // One big core plus a ring of smaller puffs a frame later, so the blast bubbles outward.
        system.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1, 1), new ParticleSystem.Burst(0.03f, 3, 4) });
        Shape(system, true, 0.45f);
        var size = system.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.35f, 0f, 6f), new Keyframe(0.35f, 1.05f), new Keyframe(1f, 1.2f));
        size.size = new ParticleSystem.MinMaxCurve(1.3f, curve);
        Fade(system, 0.7f);
        AnimatedFrames(system, 8, 10);
        var renderer = Render(system, material, ParticleSystemRenderMode.Billboard);
        renderer.sortingFudge = 5f; // behind debris, in front of smoke
    }

    private static void Shockwave(ParticleSystem system, Material material)
    {
        var main = system.main;
        main.duration = 0.3f;
        main.startLifetime = 0.22f;
        main.startSpeed = 0f;
        main.startSize = 4.2f;
        main.startColor = new Color(1f, 0.95f, 0.8f, 0.9f);
        main.maxParticles = 1;
        Burst(system, 1, 1);
        Shape(system, false);
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.12f, 0f, 3f), new Keyframe(1f, 1f)));
        Fade(system, 0.2f);
        Frames(system, 11, 11);
        Render(system, material, ParticleSystemRenderMode.Billboard);
    }

    private static void CopyComicAccentLines(Transform root)
    {
        var legacy = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyDeathVfxPath);
        var lines = legacy != null ? legacy.transform.Find("ComicAccentLines") : null;
        if (lines == null) return;
        var copy = Object.Instantiate(lines.gameObject, root);
        copy.name = lines.name;
        foreach (var system in copy.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = system.main;
            main.playOnAwake = false;
        }
    }

    // ---------- Module helpers ----------

    private static void Burst(ParticleSystem system, short min, short max)
    {
        system.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, min, max) });
    }

    private static void Shape(ParticleSystem system, bool sphere, float radius = 0.01f)
    {
        var shape = system.shape;
        shape.enabled = sphere;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
    }

    private static void SizeOverLifetime(ParticleSystem system, float start, float end)
    {
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
    }

    /// <summary>Holds full alpha until <paramref name="holdUntil"/>, then fades out.</summary>
    private static void Fade(ParticleSystem system, float holdUntil)
    {
        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }

    /// <summary>Picks a random atlas frame in [first, last] per particle, without animating it.</summary>
    private static void Frames(ParticleSystem system, int first, int last)
    {
        var sheet = system.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = Columns;
        sheet.numTilesY = Rows;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        // Frame values are normalised over the whole sheet (0..1), like the serialized frameOverTime.
        const float frames = Columns * Rows;
        sheet.startFrame = first == last
            ? new ParticleSystem.MinMaxCurve((first + 0.5f) / frames)
            : new ParticleSystem.MinMaxCurve(first / frames, (last + 0.999f) / frames);
        sheet.cycleCount = 1;
    }

    /// <summary>Plays atlas frames [first, last] once over each particle's lifetime.</summary>
    private static void AnimatedFrames(ParticleSystem system, int first, int last)
    {
        Frames(system, 0, 0);
        var sheet = system.textureSheetAnimation;
        const float frames = Columns * Rows;
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f);
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, first / frames, 1f, (last + 0.999f) / frames));
    }

    private static ParticleSystemRenderer Render(ParticleSystem system, Material material, ParticleSystemRenderMode mode)
    {
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = mode;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (mode == ParticleSystemRenderMode.Stretch)
        {
            renderer.lengthScale = 2f;
            renderer.velocityScale = 0.04f;
        }
        return renderer;
    }

    // ---------- Materials ----------

    private static Material MakeMaterial(string path, Texture2D atlas, bool additive)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetTexture("_BaseMap", atlas);
        material.SetColor("_BaseColor", Color.white);
        // Mirrors what the URP particle inspector sets for a transparent surface.
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        material.SetFloat("_SrcBlendAlpha", (float)(additive ? BlendMode.SrcAlpha : BlendMode.One));
        material.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- Enemy assignment ----------

    private static int AssignToEnemies(GameObject prefab)
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnemyFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var feedback = root.GetComponentInChildren<EnemyFeedback>(true);
                if (feedback == null) continue;
                var serialized = new SerializedObject(feedback);
                serialized.FindProperty("deathVfxPrefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return count;
    }

    // ---------- Placeholder atlas ----------

    private static Texture2D ImportAtlas()
    {
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
    }

    private static readonly Color Ink = new(0.05f, 0.04f, 0.07f, 1f);
    private static readonly Color Metal = new(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color MetalDark = new(0.36f, 0.39f, 0.45f, 1f);
    private static readonly Color Copper = new(0.95f, 0.55f, 0.2f, 1f);
    private static readonly Color Accent = new(0.95f, 0.78f, 0.15f, 1f);

    private static void WriteAtlas()
    {
        var pixels = new Color[Columns * Cell * Rows * Cell];
        // Debris frames: region function returns a fill color, or clear when outside the piece.
        DrawDebris(pixels, 0, Gear);
        DrawDebris(pixels, 1, Bolt);
        DrawDebris(pixels, 2, Plate);
        DrawDebris(pixels, 3, Panel);
        DrawDebris(pixels, 4, Cable);
        Lightning(pixels, 5, 11);
        Lightning(pixels, 6, 29);
        Glow(pixels, 7);
        // Comic fireball: tight bright core -> billowing -> breaking into embers (shares the ink outline).
        DrawDebris(pixels, 8, p => Fire(p, 0));
        DrawDebris(pixels, 9, p => Fire(p, 1));
        DrawDebris(pixels, 10, p => Fire(p, 2));
        Ring(pixels, 11);

        var texture = new Texture2D(Columns * Cell, Rows * Cell, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath));
        File.WriteAllBytes(AtlasPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private delegate Color Region(Vector2 p); // p in -1..1 cell space; alpha 0 = empty

    /// <summary>Frames are numbered left-to-right from the top row, matching the particle texture sheet order.</summary>
    private static void CellOrigin(int frame, out int ox, out int oy)
    {
        ox = frame % Columns * Cell;
        oy = (Rows - 1 - frame / Columns) * Cell; // texture rows start at the bottom
    }

    private static void DrawDebris(Color[] pixels, int frame, Region region)
    {
        CellOrigin(frame, out int ox, out int oy);
        var fill = new Color[Cell * Cell];
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
            fill[y * Cell + x] = region(new Vector2((x + 0.5f) / Cell * 2f - 1f, (y + 0.5f) / Cell * 2f - 1f));
        const int outline = 5; // thick comic ink outline
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            Color color = fill[y * Cell + x];
            if (color.a <= 0f)
            {
                bool near = false;
                for (int dy = -outline; dy <= outline && !near; dy++)
                for (int dx = -outline; dx <= outline && !near; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    near = dx * dx + dy * dy <= outline * outline && nx >= 0 && ny >= 0 && nx < Cell && ny < Cell
                        && fill[ny * Cell + nx].a > 0f;
                }
                color = near ? Ink : Color.clear;
            }
            pixels[(oy + y) * Columns * Cell + ox + x] = color;
        }
    }

    private static Color Gear(Vector2 p)
    {
        float r = p.magnitude, angle = Mathf.Atan2(p.y, p.x);
        bool tooth = Mathf.Repeat(angle / (Mathf.PI * 2f) * 8f, 1f) < 0.45f;
        if (r > (tooth ? 0.78f : 0.6f) || r < 0.2f) return Color.clear;
        return r < 0.32f ? MetalDark : Shade(Metal, p);
    }

    private static Color Bolt(Vector2 p)
    {
        Vector2 q = Rotate(p, 35f);
        bool head = Mathf.Abs(q.x) < 0.42f && q.y > 0.25f && q.y < 0.62f;
        bool shaft = Mathf.Abs(q.x) < 0.18f && q.y > -0.75f && q.y <= 0.25f;
        if (!head && !shaft) return Color.clear;
        if (shaft && Mathf.Repeat(q.y * 8f, 1f) < 0.3f) return MetalDark; // thread
        return Shade(head ? Metal : MetalDark * 1.3f, q);
    }

    private static Color Plate(Vector2 p)
    {
        Vector2 q = Rotate(p, -18f);
        float bend = q.x > 0.1f ? (q.x - 0.1f) * 0.6f : 0f;
        if (Mathf.Abs(q.x) > 0.72f || q.y < -0.45f + bend || q.y > 0.4f + bend) return Color.clear;
        if ((new Vector2(q.x + 0.5f, q.y - 0.2f)).magnitude < 0.08f || (new Vector2(q.x - 0.5f, q.y - 0.25f - bend)).magnitude < 0.08f) return MetalDark;
        return q.x > 0.1f ? Shade(MetalDark * 1.25f, q) : Shade(Metal, q);
    }

    private static Color Panel(Vector2 p)
    {
        Vector2 q = Rotate(p, 12f);
        if (Mathf.Abs(q.x) > 0.6f || Mathf.Abs(q.y) > 0.5f) return Color.clear;
        if (q.x > 0.25f && q.y > 0.15f) return Color.clear; // torn corner
        if (q.y < -0.15f && q.y > -0.35f && Mathf.Repeat(q.x * 4f, 1f) < 0.5f) return Accent; // hazard stripes
        return Shade(Metal, q);
    }

    private static Color Cable(Vector2 p)
    {
        float wave = Mathf.Sin(p.x * 3.2f) * 0.35f;
        float distance = Mathf.Abs(p.y - wave);
        if (Mathf.Abs(p.x) > 0.8f || distance > 0.13f) return Color.clear;
        if (Mathf.Abs(p.x) > 0.62f) return distance < 0.06f ? Copper : Color.clear; // exposed wire ends
        return distance < 0.05f ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.65f, 0.15f, 0.12f);
    }

    private static Color Shade(Color baseColor, Vector2 p)
    {
        float light = Mathf.Clamp01(0.85f + (p.y - p.x) * 0.2f);
        return new Color(baseColor.r * light, baseColor.g * light, baseColor.b * light, 1f);
    }

    private static Vector2 Rotate(Vector2 p, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
    }

    private static void Lightning(Color[] pixels, int frame, int seed)
    {
        CellOrigin(frame, out int ox, out int oy);
        var random = new System.Random(seed);
        // Zig-zag bolt from left to right edge with one fork.
        var points = new Vector2[9];
        for (int i = 0; i < points.Length; i++)
            points[i] = new Vector2(8 + i * (Cell - 16) / (points.Length - 1f), Cell / 2f + (float)(random.NextDouble() - 0.5) * 60f);
        Vector2 forkStart = points[4];
        Vector2 forkEnd = new(forkStart.x + 30f, forkStart.y + (random.NextDouble() < 0.5 ? -40f : 40f));
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float d = float.MaxValue;
            for (int i = 0; i + 1 < points.Length; i++) d = Mathf.Min(d, SegmentDistance(p, points[i], points[i + 1]));
            d = Mathf.Min(d, SegmentDistance(p, forkStart, forkEnd) + 1f);
            float core = 1f - Mathf.SmoothStep(1.5f, 3f, d);
            float glow = Mathf.Clamp01(1f - d / 12f) * 0.45f;
            float alpha = Mathf.Max(core, glow * glow);
            pixels[(oy + y) * Columns * Cell + ox + x] = new Color(1f, 1f, 1f, alpha);
        }
    }

    private static bool AtlasMatchesLayout()
    {
        if (!File.Exists(AtlasPath)) return false;
        var probe = new Texture2D(2, 2);
        try { return probe.LoadImage(File.ReadAllBytes(AtlasPath)) && probe.width == Columns * Cell && probe.height == Rows * Cell; }
        finally { Object.DestroyImmediate(probe); }
    }

    private static readonly Color FireCore = new(1f, 0.98f, 0.8f, 1f);
    private static readonly Color FireYellow = new(1f, 0.84f, 0.18f, 1f);
    private static readonly Color FireOrange = new(1f, 0.48f, 0.08f, 1f);
    private static readonly Color FireRed = new(0.82f, 0.16f, 0.1f, 1f);
    private static readonly Color Soot = new(0.28f, 0.24f, 0.26f, 1f);

    /// <summary>Cel-shaded explosion blob with hard colour bands; later stages grow lumpier, redder and break apart.</summary>
    private static Color Fire(Vector2 p, int stage)
    {
        float angle = Mathf.Atan2(p.y, p.x);
        float lumps = stage == 0 ? 0.06f : stage == 1 ? 0.11f : 0.12f;
        // Max radius stays under ~0.9 so the 5 px ink outline never clips at the cell border.
        float edge = (stage == 0 ? 0.6f : 0.68f)
            + lumps * Mathf.Sin(angle * 5f + stage) + lumps * 0.6f * Mathf.Sin(angle * 9f - stage * 2f);
        float r = p.magnitude;
        if (r > edge) return Color.clear;
        if (stage == 2)
        {
            // Holes punched through the cloud as it dissipates.
            if ((p - new Vector2(0.2f, 0.15f)).magnitude < 0.22f || (p - new Vector2(-0.3f, -0.25f)).magnitude < 0.16f
                || (p - new Vector2(-0.15f, 0.42f)).magnitude < 0.12f) return Color.clear;
        }
        // Offset the core up-left so the bands read as lit volume rather than flat rings.
        float d = (p - new Vector2(-0.12f, 0.12f) * (1 - stage * 0.3f)).magnitude / edge;
        return stage switch
        {
            0 => d < 0.45f ? FireCore : d < 0.75f ? FireYellow : FireOrange,
            1 => d < 0.28f ? FireCore : d < 0.55f ? FireYellow : d < 0.8f ? FireOrange : FireRed,
            _ => d < 0.3f ? FireYellow : d < 0.6f ? FireOrange : d < 0.85f ? FireRed : Soot,
        };
    }

    private static void Ring(Color[] pixels, int frame)
    {
        CellOrigin(frame, out int ox, out int oy);
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            float r = new Vector2((x + 0.5f) / Cell * 2f - 1f, (y + 0.5f) / Cell * 2f - 1f).magnitude;
            float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.07f);
            pixels[(oy + y) * Columns * Cell + ox + x] = new Color(1f, 1f, 1f, band * band);
        }
    }

    private static void Glow(Color[] pixels, int frame)
    {
        CellOrigin(frame, out int ox, out int oy);
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            float r = new Vector2((x + 0.5f) / Cell * 2f - 1f, (y + 0.5f) / Cell * 2f - 1f).magnitude;
            float alpha = Mathf.Clamp01(1f - r);
            pixels[(oy + y) * Columns * Cell + ox + x] = new Color(1f, 1f, 1f, alpha * alpha);
        }
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}
