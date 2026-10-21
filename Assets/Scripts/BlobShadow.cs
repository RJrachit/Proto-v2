using System.Collections.Generic;
using UnityEngine;

// Cheap soft "contact shadows": a dark, soft-edged blob lying on the ground under houses, trees, bushes and the bike.
// They need no real-time shadow map (which showed white speckles before and costs fps on phones), so they are safe on any phone.
// Switch them off in the DEV menu (Look tab) or with GameConfig.BlobShadows.
public static class BlobShadow
{
    static Texture2D tex;
    static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    static bool checkedShader, available;

    // True when the unlit sprite shader (alpha-blended, takes a texture) is in the build. Otherwise blobs are simply not drawn.
    public static bool Available
    {
        get
        {
            if (!checkedShader)
            {
                checkedShader = true;
                var m = Mat.Particle();
                available = m != null && m.shader != null && m.shader.name.Contains("Sprites");
            }
            return available;
        }
    }

    static Texture2D Tex()
    {
        if (tex != null) return tex;
        const int n = 64;
        tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.SmoothStep(0.25f, 1f, d);   // solid in the middle, soft towards the edge
                px[y * n + x] = new Color(0f, 0f, 0f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D glowTex;

    // White with a soft round fade, for glows (the material colour tints it).
    public static Texture2D GlowTex()
    {
        if (glowTex != null) return glowTex;
        const int n = 64;
        glowTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        glowTex.wrapMode = TextureWrapMode.Clamp;
        glowTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.SmoothStep(0f, 1f, d);
                px[y * n + x] = new Color(1f, 1f, 1f, a * a);
            }
        glowTex.SetPixels(px);
        glowTex.Apply();
        return glowTex;
    }

    // Contact shadows read weakly once the sun's shadows fade and the ground goes dark, so they are made stronger at dusk and in the
    // lit pools under the street lamps at night. Look calls this every frame; the shared materials are re-tinted when the strength moves.
    static float strength = 1f;
    public static void SetStrength(float s)
    {
        s = Mathf.Clamp(s, 1f, 2.4f);
        if (Mathf.Abs(s - strength) < 0.01f) return;
        strength = s;
        foreach (var kv in mats)
            if (kv.Value != null) kv.Value.color = new Color(0.03f, 0.04f, 0.08f, Mathf.Min(1f, kv.Key / 20f * strength));
    }

    // A material of its own (for a shadow whose strength changes every frame).
    public static Material NewMat()
    {
        var m = Mat.Particle();
        m.mainTexture = Tex();
        m.color = new Color(0.03f, 0.04f, 0.08f, 0.5f);
        return m;
    }

    // One shared material per strength (in steps of 0.05), so blobs batch.
    public static Material MatFor(float alpha)
    {
        int key = Mathf.Clamp(Mathf.RoundToInt(alpha * 20f), 1, 20);
        Material m;
        if (!mats.TryGetValue(key, out m) || m == null)
        {
            m = Mat.Particle();
            m.mainTexture = Tex();
            m.color = new Color(0.03f, 0.04f, 0.08f, Mathf.Min(1f, key / 20f * strength));
            mats[key] = m;
        }
        return m;
    }

    // A flat quad facing up. Scale it (width along x, length along the road) and put it just above the ground.
    public static GameObject Create(Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BlobShadow";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }
}

// Keeps a long-lived blob (the bike's) in step with the GameConfig.BlobShadows switch.
public class BlobToggle : MonoBehaviour
{
    Renderer r;
    void Awake() { r = GetComponent<Renderer>(); }
    void Update() { if (r != null) r.enabled = GameConfig.BlobShadows; }
}
