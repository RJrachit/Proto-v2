using UnityEngine;

// House (partial) - A glow behind each balcony, seen from the street: yellow and pulsing while the balcony still has to be served,
// steady green once it is done. The slab's own colour is only visible from above, so this is what tells them apart from the road.
// The main part of the class is in House.cs.
public partial class House
{
    static readonly Color AuraOpen = new Color(1.00f, 0.86f, 0.18f);   // still to do
    static readonly Color AuraDone = new Color(0.30f, 0.95f, 0.50f);   // served

    readonly Transform[] balcAura = new Transform[6];                  // one per balcony in the pool
    readonly Material[] balcAuraMat = new Material[6];
    static Texture2D auraTex;

    // A soft square glow: brightest in the middle (behind the balcony), fading out past its edges.
    static Texture2D AuraTex()
    {
        if (auraTex != null) return auraTex;
        const int n = 64;
        auraTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        auraTex.wrapMode = TextureWrapMode.Clamp;
        auraTex.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                float e = Mathf.Pow(Mathf.Pow(u, 3f) + Mathf.Pow(v, 3f), 1f / 3f);   // a rounded square
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - e), 1.1f));
            }
        auraTex.SetPixels(px);
        auraTex.Apply();
        return auraTex;
    }

    void BalconyAuras()
    {
        if (balconies == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + phase);
        float s = side;
        Vector3 intoHouse = transform.TransformDirection(new Vector3(s, 0f, 0f));   // the quad's back is to the house, its face to the road
        for (int i = 0; i < balcAura.Length && i < balconies.Length; i++)
        {
            var b = balconies[i];
            bool show = b.on && b.slab != null && b.slab.gameObject.activeInHierarchy;
            if (!show) { if (balcAura[i] != null) balcAura[i].gameObject.SetActive(false); continue; }
            if (balcAura[i] == null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "BalconyAura";
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(transform, false);
                var r = q.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                balcAuraMat[i] = new Material(Mat.Particle());
                balcAuraMat[i].mainTexture = AuraTex();
                r.sharedMaterial = balcAuraMat[i];
                balcAura[i] = q.transform;
            }
            bool done = b.hit || floorDone[b.floor];
            Color c = done ? AuraDone : AuraOpen;
            float a = done ? 0.60f : 0.65f + 0.30f * pulse;
            balcAuraMat[i].color = new Color(c.r, c.g, c.b, a);
            float baseY = FloorBase(b.floor);
            var t = balcAura[i];
            t.localPosition = new Vector3(s * 8.44f, baseY + 1.0f, b.z);   // on the wall, just in front of it: the slab and rail stand in front of the glow
            t.rotation = Quaternion.LookRotation(intoHouse, Vector3.up);
            float grow = done ? 1f : 1f + 0.06f * pulse;
            t.localScale = new Vector3((b.len + 1.8f) * grow, 3.0f * grow, 1f);
            t.gameObject.SetActive(true);
        }
        tfxOn = true;
    }

    void HideBalconyAuras()
    {
        for (int i = 0; i < balcAura.Length; i++) if (balcAura[i] != null) balcAura[i].gameObject.SetActive(false);
    }
}
