using System.Collections.Generic;
using UnityEngine;

// House (partial) - Shops and restaurants, and their breakable props.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- shops and restaurants ----
    int shopKind;                       // 0 = a house, 1 = a shop, 2 = a restaurant
    readonly List<Transform> shopQuads = new List<Transform>();
    readonly List<Destructible> shopQuadLink = new List<Destructible>();
    int shopQuadN;
    static readonly Color[] ShopBody =
    {
        new Color(0.30f, 0.55f, 0.58f), new Color(0.38f, 0.45f, 0.62f), new Color(0.50f, 0.62f, 0.50f),
        new Color(0.52f, 0.40f, 0.56f), new Color(0.90f, 0.86f, 0.74f), new Color(0.40f, 0.43f, 0.48f),
        new Color(0.88f, 0.70f, 0.28f), new Color(0.18f, 0.55f, 0.52f),
    };
    // awning stripe colours: no reds or oranges (those are for the delivery markers)
    static readonly Color[][] AwningPairs =
    {
        new[] { new Color(0.95f, 0.95f, 0.92f), new Color(0.20f, 0.50f, 0.55f) },
        new[] { new Color(0.95f, 0.95f, 0.92f), new Color(0.18f, 0.28f, 0.55f) },
        new[] { new Color(0.95f, 0.95f, 0.92f), new Color(0.25f, 0.55f, 0.32f) },
        new[] { new Color(0.92f, 0.90f, 0.80f), new Color(0.45f, 0.30f, 0.52f) },
    };

    // ---- breakable shop props: tables, chairs, parasols, planters, the chalkboard, the awning and the display windows ----
    // Each prop is one invisible box collider (a Destructible, one hit) with plain cubes as children, so the whole thing flies off
    // together. No points, no penalty: smashing them is just for fun. Pooled like the other lot parts.
    class ShopProp
    {
        public Destructible d;
        public BoxCollider box;
        public readonly List<Transform> kids = new List<Transform>();
        public readonly List<Renderer> rends = new List<Renderer>();
        public int n;
    }
    readonly List<ShopProp> shopProps = new List<ShopProp>();
    int shopPropN;

    ShopProp NewProp(Vector3 center, Vector3 colSize, Color tint, bool glass = false, Vector3 colCenter = default(Vector3))
    {
        if (shopPropN >= shopProps.Count)
        {
            var go = new GameObject("ShopProp");
            go.transform.SetParent(transform, false);
            var box = go.AddComponent<BoxCollider>();
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var d = go.AddComponent<Destructible>();
            shopProps.Add(new ShopProp { d = d, box = box });
        }
        var p = shopProps[shopPropN++];
        p.n = 0;
        p.d.owner = this;
        p.d.group = -2;
        p.d.crate = false;
        p.d.fixedPos = true;
        p.d.glass = glass;
        p.box.size = colSize;
        p.box.center = colCenter;
        p.d.Place(0, 1, center, Vector3.one, tint);
        return p;
    }

    void Kid(ShopProp p, Vector3 localPos, Vector3 scale, Color col, Vector3 euler = default(Vector3))
    {
        if (p.n >= p.kids.Count)
        {
            var go = Prim.Make(PrimitiveType.Cube, "Kid", p.d.transform, Vector3.zero, Vector3.one, Color.white);
            p.kids.Add(go.transform);
            p.rends.Add(go.GetComponent<Renderer>());
        }
        var t = p.kids[p.n];
        t.localPosition = localPos;
        t.localEulerAngles = euler;
        t.localScale = scale;
        p.rends[p.n].material.color = col;
        t.gameObject.SetActive(true);
        p.n++;
    }

    void HideShopProps()
    {
        for (int i = 0; i < shopPropN; i++)
            for (int k = shopProps[i].n; k < shopProps[i].kids.Count; k++) shopProps[i].kids[k].gameObject.SetActive(false);
        for (int i = shopPropN; i < shopProps.Count; i++) shopProps[i].d.gameObject.SetActive(false);
    }

    // A flat quad on the wall that glows at night (unlit), pooled. Shown only while the windows are lit.
    void ShopQuad(Vector3 pos, Vector2 size, Material mat, Destructible link = null)
    {
        if (!NightLights.Available) return;
        while (shopQuadN >= shopQuads.Count)
        {
            shopQuadLink.Add(null);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "ShopGlow";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            var r = q.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            shopQuads.Add(q.transform);
        }
        shopQuadLink[shopQuadN] = link;   // a display window's glow goes out when the window is broken
        var t = shopQuads[shopQuadN++];
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
        t.localScale = new Vector3(size.x, size.y, 1f);
        t.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void HideShopQuads()
    {
        for (int i = shopQuadN; i < shopQuads.Count; i++) shopQuads[i].gameObject.SetActive(false);
    }

    void LayoutShop(float L, float doorZ)
    {
        float s = side;
        float half = L * 0.5f;
        float h = Hash01(transform.position.z * 1.913f + (side > 0 ? 8.3f : 44.1f));
        bool restaurant = shopKind == 2;
        var pair = AwningPairs[(int)(h * 997f) % AwningPairs.Length];
        int neon = (int)(h * 313f) % NightLights.NeonCount;
        Material winMat = (h * 7f) % 1f < 0.3f ? NightLights.WindowCool : NightLights.WindowWarm;
        Color glass = new Color(0.62f, 0.78f, 0.88f);

        // striped awning over the door and the window, sloping down away from the wall
        float awnLen = L - 0.6f;
        int n = Mathf.Max(3, Mathf.RoundToInt(awnLen / 0.65f));
        float sw = awnLen / n;
        var awn = NewProp(new Vector3(s * 7.75f, 2.78f, 0f), new Vector3(1.6f, 0.7f, awnLen), pair[1]);
        for (int i = 0; i < n; i++)
        {
            float z = -awnLen * 0.5f + sw * (i + 0.5f);
            Kid(awn, new Vector3(0f, 0f, z), new Vector3(1.55f, 0.07f, sw * 0.98f), (i & 1) == 0 ? pair[0] : pair[1], new Vector3(0f, 0f, s * 18f));
        }

        // sign board above the awning, with a neon sign on it
        float signLen = Mathf.Min(L - 1.0f, 3.8f);
        if (GameConfig.FacadeDetail && fOn)
        {
            // the name of the shop and an LED strip, drawn on the board (House.Facade.cs)
            PlaceShopSign(s, signLen, 4.3f);
            Part(new Vector3(s * 8.43f, 4.3f, 0f), new Vector3(0.14f, fSignH, signLen), new Color(0.09f, 0.11f, 0.18f), true);
        }
        else
        {
            Part(new Vector3(s * 8.43f, 4.3f, 0f), new Vector3(0.14f, 0.9f, signLen), new Color(0.09f, 0.11f, 0.18f), true);
            // three bars of neon, like lettering
            float[] frac = { 0.30f, 0.20f, 0.34f };
            float used = (frac[0] + frac[1] + frac[2]) * signLen + 2f * 0.18f;
            float zz = -used * 0.5f;
            for (int b = 0; b < 3; b++)
            {
                float bl = frac[b] * signLen;
                ShopQuad(new Vector3(s * 8.34f, 4.3f, zz + bl * 0.5f), new Vector2(bl, 0.32f), NightLights.Neon(neon));
                zz += bl + 0.18f;
            }
        }

        // display windows either side of the door (they replace the ordinary ground-floor windows)
        for (int side2 = -1; side2 <= 1; side2 += 2)
        {
            float z0 = side2 < 0 ? -half + 0.35f : doorZ + 0.95f;
            float z1 = side2 < 0 ? doorZ - 0.95f : half - 0.35f;
            float w = z1 - z0;
            if (w < 1.0f) continue;
            float zc = (z0 + z1) * 0.5f;
            var wp = NewProp(new Vector3(s * 8.43f, 1.45f, zc), new Vector3(0.3f, 1.6f, w), glass, true);
            Kid(wp, Vector3.zero, new Vector3(0.14f, 1.6f, w), glass);
            ShopQuad(new Vector3(s * 8.33f, 1.45f, zc), new Vector2(w - 0.12f, 1.45f), winMat, wp.d);
        }

        if (!restaurant)
        {
            // planters either side of the door and a chalkboard
            for (int side2 = -1; side2 <= 1; side2 += 2)
            {
                float pz = doorZ + side2 * 1.2f;
                if (Mathf.Abs(pz) > half - 0.5f) continue;
                var pl = NewProp(new Vector3(s * 7.9f, 0.45f, pz), new Vector3(0.7f, 0.9f, 0.7f), new Color(0.22f, 0.50f, 0.26f));
                Kid(pl, new Vector3(0f, -0.15f, 0f), new Vector3(0.7f, 0.6f, 0.7f), new Color(0.62f, 0.60f, 0.58f));
                Kid(pl, new Vector3(0f, 0.27f, 0f), new Vector3(0.58f, 0.34f, 0.58f), new Color(0.22f, 0.50f, 0.26f));
            }
            float cz = Mathf.Clamp(doorZ + (h < 0.5f ? 1.9f : -1.9f), -half + 0.5f, half - 0.5f);
            var cb = NewProp(new Vector3(s * 6.7f, 0.45f, cz), new Vector3(0.3f, 0.9f, 0.6f), new Color(0.12f, 0.20f, 0.16f));
            Kid(cb, Vector3.zero, new Vector3(0.08f, 0.9f, 0.6f), new Color(0.12f, 0.20f, 0.16f), new Vector3(0f, 0f, s * -8f));
        }
        else
        {
            // outdoor tables with chairs and a parasol each, clear of the door
            Color umb = pair[1];
            int tables = L >= 8f ? 3 : (L >= 5.8f ? 2 : 1);
            float step = L / tables;
            for (int k = 0; k < tables; k++)
            {
                float tz = -half + step * (k + 0.5f);
                if (Mathf.Abs(tz - doorZ) < 1.4f) tz = tz + (tz >= doorZ ? 1f : -1f) * (1.4f - Mathf.Abs(tz - doorZ));
                tz = Mathf.Clamp(tz, -half + 0.9f, half - 0.9f);
                float tx = s * 6.2f;
                Color topC = new Color(0.93f, 0.92f, 0.86f), legC = new Color(0.25f, 0.27f, 0.3f);
                var tb = NewProp(new Vector3(tx, 0.42f, tz), new Vector3(0.9f, 0.85f, 0.9f), topC);
                Kid(tb, new Vector3(0f, -0.02f, 0f), new Vector3(0.14f, 0.8f, 0.14f), legC);
                Kid(tb, new Vector3(0f, 0.4f, 0f), new Vector3(0.85f, 0.07f, 0.85f), topC);
                for (int c = -1; c <= 1; c += 2)
                {
                    Color frame = new Color(0.30f, 0.32f, 0.36f), seat = new Color(0.80f, 0.78f, 0.70f);
                    var ch = NewProp(new Vector3(tx + c * 0.72f, 0.5f, tz), new Vector3(0.6f, 1.0f, 0.45f), seat);
                    Kid(ch, new Vector3(0f, -0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), frame);
                    Kid(ch, new Vector3(0f, 0.02f, 0f), new Vector3(0.42f, 0.06f, 0.42f), seat);
                    Kid(ch, new Vector3(c * 0.19f, 0.25f, 0f), new Vector3(0.05f, 0.45f, 0.42f), seat);
                }
                var pa = NewProp(new Vector3(tx, 0f, tz), new Vector3(2.0f, 0.3f, 2.0f), umb, false, new Vector3(0f, 2.4f, 0f));
                Kid(pa, new Vector3(0f, 1.2f, 0f), new Vector3(0.07f, 2.4f, 0.07f), new Color(0.80f, 0.80f, 0.82f));
                Kid(pa, new Vector3(0f, 2.4f, 0f), new Vector3(2.0f, 0.1f, 2.0f), umb);
            }
        }

        // the sign's light: a warm or cool glow on the pavement and a real light at night
        Color lc = restaurant ? new Color(1.0f, 0.82f, 0.55f) : new Color(0.75f, 0.88f, 1.0f);
        Glow(new Vector3(s * 7.3f, 2.4f, 0f), new Vector3(s * 6.4f, 0.235f, 0f), Mathf.Min(L, 7.0f), NightLights.DoorPool, 0.25f, 8f, 2.6f, lc);
    }
}
