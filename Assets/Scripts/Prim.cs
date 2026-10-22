using UnityEngine;
using UnityEngine.Rendering;

// Tiny helper for building the prototype out of Unity's built-in primitives.
public static class Prim
{
    // Referencing these types keeps them in a phone build. Without it Unity's code stripping can remove a collider
    // class and CreatePrimitive then fails with "Can't add component because class '...Collider' doesn't exist".
    static readonly System.Type[] KeepTypes =
    {
        typeof(BoxCollider), typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider),
    };

    // In a URP project, runtime-created primitives get the Built-in "Standard" material, which URP can't draw
    // (magenta in a phone build). Use the pipeline's own default material instead. Does nothing in Built-in projects.
    static Material urpLit;

    public static void FixMaterial(Renderer r)
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null) return;   // Built-in pipeline: the primitive's own material is fine

        if (urpLit == null)
        {
            // Prefer the URP Lit shader by name (added to the build by Editor/AddShadersToBuild).
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) urpLit = new Material(shader);
        }
        // (the whole list is replaced, not just the first entry: a part that was a glowing ring also carries the x-ray pass as a second material,
        // and a reused pole, wall or balcony must not keep it)
        if (urpLit != null) r.sharedMaterials = new Material[] { urpLit };
        else if (pipeline.defaultMaterial != null) r.sharedMaterials = new Material[] { pipeline.defaultMaterial };
    }

    // The hoop rings glow orange: bright whatever the light (an unlit material), so they stand out clearly, day or night.
    public static readonly Color RimOrange = new Color(1.00f, 0.45f, 0.05f);
    static Material rimMat;

    public static void GlowOrange(Renderer r, bool xray = true)
    {
        if (r == null) return;
        if (rimMat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) { r.material.color = RimOrange; return; }   // (no unlit shader in this build: plain orange)
            rimMat = new Material(shader);
            rimMat.color = RimOrange;
            if (rimMat.HasProperty("_BaseColor")) rimMat.SetColor("_BaseColor", RimOrange);
        }
        var xm = xray ? XRay() : null;
        if (xm != null) r.sharedMaterials = new Material[] { rimMat, xm };   // the ring again, dimmer, where something stands in front of it (see XRay.shader)
        else r.sharedMaterials = new Material[] { rimMat };
    }

    // The backboards glow a soft white: bright whatever the light (unlit), like the rings, so every board stands out day or night.
    static Material boardMat;
    public static void GlowWhite(Renderer r)
    {
        if (r == null) return;
        if (boardMat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return;   // (no unlit shader in this build: the board stays plain white)
            boardMat = new Material(shader);
            var white = new Color(0.97f, 0.97f, 1.00f);
            boardMat.color = white;
            if (boardMat.HasProperty("_BaseColor")) boardMat.SetColor("_BaseColor", white);
        }
        r.sharedMaterials = new Material[] { boardMat };
    }

    // Any colour as a bright, unlit part (it ignores the light, so it never looks dull in shade, at dusk or at night). One material per colour, shared.
    static readonly System.Collections.Generic.Dictionary<Color, Material> glowByColor = new System.Collections.Generic.Dictionary<Color, Material>();
    public static void GlowColor(Renderer r, Color c)
    {
        if (r == null) return;
        Material m;
        if (!glowByColor.TryGetValue(c, out m) || m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) { r.material.color = c; return; }   // (no unlit shader in this build: the plain colour)
            m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            glowByColor[c] = m;
        }
        r.sharedMaterials = new Material[] { m };
    }

    // The x-ray look of the rings: the same orange, only drawn through things (the shader's depth test is reversed).
    static Material xrayMat;
    static Material XRay()
    {
        if (xrayMat != null) return xrayMat;
        var shader = Shader.Find("PolyParcel/XRay");
        if (shader == null || !shader.isSupported) return null;
        xrayMat = new Material(shader);
        TickXRay();
        return xrayMat;
    }

    // Called every frame from Look: applies the DEV menu toggle and brightness.
    public static void TickXRay()
    {
        if (xrayMat == null) return;
        float a = GameConfig.HoopXRay ? Mathf.Clamp01(GameConfig.HoopXRayAlpha) : 0f;
        xrayMat.SetColor("_BaseColor", new Color(RimOrange.r, RimOrange.g, RimOrange.b, a));
    }

    // A cylinder primitive comes with a capsule collider, which is the wrong shape for a flat disc: give it a collider that matches the disc.
    public static void RoundCollider(GameObject go)
    {
        var old = go.GetComponent<Collider>();
        if (old != null) Object.Destroy(old);
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        mc.convex = true;
    }

    public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 localPos,
                                  Vector3 scale, Color color, bool keepCollider = false,
                                  Vector3 euler = default(Vector3))
    {
        if (KeepTypes.Length == 0) return null;   // never true; just keeps KeepTypes referenced

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        FixMaterial(r);
        r.material.color = color;
        if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }
}
