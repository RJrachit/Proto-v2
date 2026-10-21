using UnityEngine;

// The glow of a thrown parcel: the parcel itself is a plain bright white (unlit), a dimmer white copy of it shows through anything that stands
// in front of it (see XRay.shader), and a soft white halo round it is drawn over everything, so the light can be seen through walls, roofs and trees.
// Added to every thrown paper by PaperThrower. The strength and the see-through switch are DEV menu settings (Look tab).
public class ParcelGlow : MonoBehaviour
{
    static Texture2D haloTex;
    static Material unlitWhite, xrayWhite, haloMat;
    static bool shadersTried;

    Paper paper;
    Transform halo;
    float seed;

    static Texture2D HaloTex()
    {
        if (haloTex != null) return haloTex;
        const int n = 64;
        haloTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        haloTex.wrapMode = TextureWrapMode.Clamp;
        haloTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(1f - d, 2.2f);   // bright in the middle, fading smoothly to nothing at the edge
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        haloTex.SetPixels(px);
        haloTex.Apply();
        return haloTex;
    }

    static void MakeMaterials()
    {
        if (shadersTried) return;
        shadersTried = true;
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");
        if (unlit != null)
        {
            unlitWhite = new Material(unlit);
            unlitWhite.color = Color.white;
            if (unlitWhite.HasProperty("_BaseColor")) unlitWhite.SetColor("_BaseColor", Color.white);
        }
        var xs = Shader.Find("PolyParcel/XRay");
        if (xs != null && xs.isSupported) xrayWhite = new Material(xs);
        var gs = Shader.Find("PolyParcel/GlowThrough");
        if (gs != null && gs.isSupported)
        {
            haloMat = new Material(gs);
            haloMat.mainTexture = HaloTex();
        }
        else Debug.LogWarning("ParcelGlow: the GlowThrough shader was not found, so the parcel has no halo.");
    }

    // The settings, applied to the shared materials (every parcel does it; it is only a few colour writes).
    static void Tick()
    {
        float k = Mathf.Clamp01(GameConfig.ParcelGlow);
        bool through = GameConfig.ParcelSeeThrough;
        if (xrayWhite != null) xrayWhite.SetColor("_BaseColor", new Color(1f, 1f, 1f, through ? 0.5f * k : 0f));
        if (haloMat != null)
        {
            haloMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, k));
            haloMat.SetFloat("_ZTest", through ? 8f : 4f);   // 8 = always drawn, 4 = hidden behind things
        }
    }

    void Start()
    {
        paper = GetComponent<Paper>();
        seed = Random.value * 6.28f;
        MakeMaterials();
        var r = GetComponent<Renderer>();
        if (r != null && unlitWhite != null)
            r.sharedMaterials = xrayWhite != null ? new Material[] { unlitWhite, xrayWhite } : new Material[] { unlitWhite };
        if (haloMat != null)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "ParcelHalo";
            Destroy(q.GetComponent<Collider>());
            var qr = q.GetComponent<Renderer>();
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            qr.receiveShadows = false;
            qr.sharedMaterial = haloMat;
            halo = q.transform;
        }
        Tick();
    }

    void LateUpdate()
    {
        if (halo == null) return;
        Tick();
        bool live = paper == null || paper.InFlight;
        halo.gameObject.SetActive(live && GameConfig.ParcelGlow > 0.01f);
        if (!live) return;
        var cam = Camera.main;
        halo.position = transform.position;
        if (cam != null) halo.rotation = cam.transform.rotation;   // always faces the camera
        float s = 1.7f + 0.2f * Mathf.Sin(Time.time * 9f + seed);
        halo.localScale = new Vector3(s, s, 1f);
    }

    void OnDestroy()
    {
        if (halo != null) Destroy(halo.gameObject);
    }
}
