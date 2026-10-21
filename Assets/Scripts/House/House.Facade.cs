using System.Collections.Generic;
using UnityEngine;

// House (partial) - Facade details: pilasters, cornice, window frames (frame, sill, lintel, pediment, shutters or tin awning, flower boxes, AC units),
// the door surround and canopy, an electricity meter, the parapet and rooftop props (water tank, solar panels, AC, dish, vent, washing line, stair-head),
// and the signs: shop and restaurant name boards, hotel roof boards, blade signs sticking out of the wall.
//
// It is all looks. Everything is built into ONE mesh per house (see FacadeMesh.cs) that has NO collider, so papers fly through it exactly as before,
// and nothing here touches a scoring zone, a window slot or a balcony. The signs are flat unlit pictures (see SignArt.cs) that stay bright at night.
// The look of a lot comes from its own dice (ShopKey), so it never changes while the lot stands and never uses the route's random generator.
// The main part of the class is in House.cs.
public partial class House
{
    // soft accents for shutters, awnings, rims and railings: no reds or oranges (those belong to the markers)
    static readonly Color[] FacadeAccents =
    {
        new Color(0.16f, 0.48f, 0.50f), new Color(0.17f, 0.25f, 0.48f), new Color(0.20f, 0.42f, 0.28f), new Color(0.38f, 0.27f, 0.50f),
        new Color(0.42f, 0.62f, 0.80f), new Color(0.40f, 0.31f, 0.24f), new Color(0.88f, 0.78f, 0.38f), new Color(0.45f, 0.72f, 0.58f),
    };
    static readonly Color[] ClothColors =
    {
        new Color(0.45f, 0.72f, 0.78f), new Color(0.95f, 0.70f, 0.82f), new Color(0.96f, 0.93f, 0.80f), new Color(0.60f, 0.78f, 0.95f),
        new Color(0.98f, 0.92f, 0.55f), new Color(0.65f, 0.55f, 0.85f),
    };
    static readonly Color SteelColor = new Color(0.32f, 0.35f, 0.40f);
    static readonly Color SignDark = new Color(0.09f, 0.11f, 0.18f);

    // the look chosen for this lot (PlanFacade)
    bool fOn, fMuted, fHotel, fBlade;
    int fRoof, fTrimMode, fSignStyle;
    float fL, fDoorZ, fSignLen, fSignH, fBladeZ, fBladeY;
    string fWord, fTag, fBladeWord;
    Color fTrim, fTrimDark, fAccent, fFrame;

    GameObject facadeGO;
    Mesh facadeMesh;
    static readonly FacadeBuilder fb = new FacadeBuilder();   // (shared: houses build one at a time)

    readonly List<Transform> signQuads = new List<Transform>();
    int signN;

    float FH(float salt) { return Hash01(ShopKey * 0.917f + salt + (side > 0f ? 3.3f : 47.1f)); }
    static int Idx(float h, int n) { return Mathf.Min(n - 1, (int)(h * n)); }

    // ---- planning ----

    // Decides the look of this lot. Runs before the shop is laid out (it needs the sign), the mesh is built later by LayoutFacade.
    void PlanFacade(float L, float doorZ, Color body)
    {
        signN = 0;
        fSignLen = 0f;
        fHotel = false;
        fBlade = false;
        fOn = GameConfig.FacadeDetail && !isPark && !isBeach && !bare && groundFloor;
        if (!fOn) return;

        fL = L;
        fDoorZ = doorZ;
        fRoof = Idx(FH(5.3f), 5);
        fTrimMode = Idx(FH(9.1f), 3);
        fAccent = FacadeAccents[Idx(FH(2.9f), FacadeAccents.Length)];
        fTrim = fTrimMode == 2 ? Color.Lerp(fAccent, Color.white, 0.4f) : Color.Lerp(body, Color.white, 0.6f);
        fTrimDark = new Color(body.r * 0.55f, body.g * 0.55f, body.b * 0.55f);
        fFrame = fTrimMode == 1 ? new Color(0.22f, 0.24f, 0.30f) : new Color(0.95f, 0.93f, 0.86f);
        fSignStyle = Idx(FH(11.3f), SignArt.Styles);

        bool flatRoof = !(roofGable != null && roofGable.gameObject.activeSelf);
        fHotel = shopKind == 0 && !fort && floors >= 2 && L >= 5.5f && flatRoof && !Tutorial.Active && FH(7.7f) < GameConfig.HotelChance;
        if (fHotel)
        {
            fWord = SignArt.HotelNames[Idx(FH(3.1f), SignArt.HotelNames.Length)];
            fTag = SignArt.HotelTags[Idx(FH(4.3f), SignArt.HotelTags.Length)];
            fBladeWord = SignArt.HotelBlades[Idx(FH(6.7f), SignArt.HotelBlades.Length)];
        }
        else if (shopKind == 1)
        {
            fWord = SignArt.ShopNames[Idx(FH(3.1f), SignArt.ShopNames.Length)];
            fTag = SignArt.ShopTags[Idx(FH(4.3f), SignArt.ShopTags.Length)];
            fBladeWord = SignArt.ShopBlades[Idx(FH(6.7f), SignArt.ShopBlades.Length)];
        }
        else if (shopKind == 2)
        {
            fWord = SignArt.FoodNames[Idx(FH(3.1f), SignArt.FoodNames.Length)];
            fTag = SignArt.FoodTags[Idx(FH(4.3f), SignArt.FoodTags.Length)];
            fBladeWord = SignArt.FoodBlades[Idx(FH(6.7f), SignArt.FoodBlades.Length)];
        }
        fBlade = (fHotel || shopKind != 0) && L >= 4.5f;
        if (fBlade)
        {
            // the blade sign hangs at the end of the frontage the bike comes from (the other end when the building's hoop is right there)
            float half = L * 0.5f;
            bool far = fHotel && floorHoopOn[1] && floorHoopAt[1] < -half + 1.9f;
            fBladeZ = far ? half - 0.38f : -half + 0.38f;
            fBladeY = fHotel ? FloorBase(1) + 0.9f : 3.55f;
        }
    }

    // ---- the shop's name board (called by LayoutShop) ----

    // A sign board over the awning: the picture of the name and an LED strip on the dark board, the board itself is solid like before.
    void PlaceShopSign(float s, float signLen, float sy)
    {
        fSignLen = signLen;
        fSignH = Mathf.Clamp(signLen / 3f + 0.14f, 0.6f, 1.2f);
        if (!fOn || fWord == null) return;
        float qw = Mathf.Min(signLen - 0.12f, (fSignH - 0.1f) * 3f);
        SignQuad(new Vector3(s * 8.34f, sy, 0f), new Vector2(qw, qw / 3f), SignArt.Board(fWord, fTag, fSignStyle), s * 90f);
        // the tube light over the board lights up at night
        ShopQuad(new Vector3(s * 8.22f, sy + fSignH * 0.5f + 0.16f, 0f), new Vector2(signLen * 0.66f, 0.05f), NightLights.Bulb);
    }

    // ---- the layout ----

    // Builds the facade mesh and the signs that are not part of a shop. Runs after the shop and the other layouts, before the pooled quads are hidden.
    void LayoutFacade(float L, float doorZ, bool porch)
    {
        if (!fOn)
        {
            if (facadeGO != null) facadeGO.SetActive(false);
            HideSigns();
            return;
        }
        fMuted = !(Subscriber && !resolved);
        float s = side;
        if (fHotel)
        {
            float hw, hh, cy;
            HotelBoard(out hw, out hh, out cy);
            float qw = hw - 0.1f;
            SignQuad(new Vector3(s * 8.70f, cy, 0f), new Vector2(qw, qw / 3f), SignArt.Board(fWord, fTag, fSignStyle), s * 90f);
            ShopQuad(new Vector3(s * 8.58f, cy + hh * 0.5f + 0.2f, 0f), new Vector2(hw * 0.7f, 0.05f), NightLights.Bulb);
        }
        if (fBlade)
        {
            var mat = SignArt.Blade(fBladeWord, fSignStyle + 3);
            float x = s * 7.95f;
            SignQuad(new Vector3(x, fBladeY, fBladeZ - 0.046f), new Vector2(0.84f, 0.46f), mat, 0f);       // seen from the road behind
            SignQuad(new Vector3(x, fBladeY, fBladeZ + 0.046f), new Vector2(0.84f, 0.46f), mat, 180f);     // and from the road ahead
        }
        // the customer marker floats higher over a hotel, clear of the roof board
        if (fHotel && beacon != null) beacon.transform.localPosition += new Vector3(0f, 1.2f, 0f);
        BuildFacadeMesh(porch);
        HideSigns();
    }

    // The customer's house went dull (served or missed): the details follow the walls.
    void RefreshFacadeTint()
    {
        if (!fOn) return;
        bool m = !(Subscriber && !resolved);
        if (m == fMuted) return;
        fMuted = m;
        BuildFacadeMesh(lastPorch);
    }

    bool lastPorch;

    void SignQuad(Vector3 pos, Vector2 size, Material mat, float yaw)
    {
        while (signN >= signQuads.Count)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "SignQuad";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            var r = q.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            signQuads.Add(q.transform);
        }
        var t = signQuads[signN++];
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        t.localScale = new Vector3(size.x, size.y, 1f);
        t.GetComponent<Renderer>().sharedMaterial = mat;
        t.gameObject.SetActive(true);
    }

    void HideSigns()
    {
        for (int i = signN; i < signQuads.Count; i++) signQuads[i].gameObject.SetActive(false);
    }

    // ---- the mesh ----

    // A box on the wall: o0..o1 is how far it reaches out from the wall plane towards the road (negative = sunk into the wall).
    void WallBox(FacadeBuilder b, float z, float y, float w, float h, float o0, float o1, Color c)
    {
        b.Box(new Vector3(side * (8.5f - (o0 + o1) * 0.5f), y, z), new Vector3(o1 - o0, h, w), c);
    }

    void WallRot(FacadeBuilder b, float z, float y, float w, float h, float o0, float o1, Color c, Quaternion q)
    {
        b.Box(new Vector3(side * (8.5f - (o0 + o1) * 0.5f), y, z), new Vector3(o1 - o0, h, w), c, q);
    }

    void BuildFacadeMesh(bool porch)
    {
        lastPorch = porch;
        var b = fb;
        b.Clear();
        b.muted = fMuted;
        float L = fL, half = L * 0.5f, topY = BodyH;
        bool gable = roofGable != null && roofGable.gameObject.activeSelf;

        // corner pilasters and the cornice under the roof edge
        WallBox(b, -half + 0.14f, topY * 0.5f, 0.28f, topY, -0.02f, 0.13f, fTrim);
        WallBox(b, half - 0.14f, topY * 0.5f, 0.28f, topY, -0.02f, 0.13f, fTrim);
        WallBox(b, 0f, topY - 0.1f, L + 0.1f, 0.2f, 0f, 0.3f, fTrim);
        WallBox(b, 0f, topY - 0.27f, L + 0.1f, 0.14f, 0f, 0.17f, fTrimDark);
        if (fRoof == 3)
        {
            int n = Mathf.Max(2, (int)(L / 0.45f));
            for (int i = 0; i < n; i++)
            {
                float z = -half + 0.2f + (L - 0.4f) * i / (n - 1);
                WallBox(b, z, topY - 0.39f, 0.16f, 0.14f, 0f, 0.22f, fTrim);
            }
        }

        // windows
        FrameWindows(b, groundWindows, 0f);
        FrameWindows(b, upperWindows, 100f);

        // the door: a surround, and a small canopy when there is no porch
        if (shopKind == 0 && !fort)
        {
            float dz = fDoorZ;
            WallBox(b, dz - 0.6f, 0.97f, 0.12f, 1.94f, 0f, 0.14f, fFrame);
            WallBox(b, dz + 0.6f, 0.97f, 0.12f, 1.94f, 0f, 0.14f, fFrame);
            WallBox(b, dz, 1.98f, 1.5f, 0.16f, 0f, 0.22f, fFrame);
            if (!porch)
            {
                WallBox(b, dz, 2.26f, 1.8f, 0.08f, 0f, 0.8f, fTrim);
                WallBox(b, dz - 0.78f, 2.1f, 0.1f, 0.24f, 0f, 0.4f, fFrame);
                WallBox(b, dz + 0.78f, 2.1f, 0.1f, 0.24f, 0f, 0.4f, fFrame);
                // an electricity meter board above it, with its pipe
                if (!wallHoop && !reverseHoop && !wallPadOn && FH(14.1f) < 0.7f)
                {
                    WallBox(b, dz, 3.3f, 0.5f, 0.66f, 0f, 0.2f, new Color(0.60f, 0.64f, 0.62f));
                    WallBox(b, dz, 3.38f, 0.3f, 0.22f, 0.2f, 0.23f, new Color(0.88f, 0.92f, 0.95f));
                    WallBox(b, dz, 2.85f, 0.07f, 0.34f, 0f, 0.1f, SteelColor);
                }
            }
        }

        // wall lamps either side of the door (not when a window is close beside it)
        bool lampsFree = true;
        if (groundWindows != null)
            foreach (var gw in groundWindows)
                if (gw != null && gw.visual != null && gw.visual.gameObject.activeSelf && Mathf.Abs(gw.visual.localPosition.z - fDoorZ) < 2.1f) lampsFree = false;
        if (shopKind == 0 && !fort && lampsFree)
        {
            for (int k = -1; k <= 1; k += 2)
            {
                WallBox(b, fDoorZ + k * 1.0f, 2.15f, 0.16f, 0.3f, 0f, 0.16f, SteelColor);
                WallBox(b, fDoorZ + k * 1.0f, 2.15f, 0.1f, 0.18f, 0.16f, 0.2f, new Color(1.0f, 0.92f, 0.65f));
                WallBox(b, fDoorZ + k * 1.0f, 2.33f, 0.2f, 0.05f, 0f, 0.2f, SteelColor);
            }
        }

        // a drainpipe down one end (the end away from the blade sign)
        if (FH(31.7f) < 0.7f)
        {
            bool nearEnd = fBlade && fBladeZ < 0f;
            float pz = (nearEnd ? 1f : (FH(32.1f) < 0.5f ? -1f : 1f)) * (half - 0.14f);
            float ph = topY - 0.5f;
            Color pipe = new Color(0.25f, 0.27f, 0.30f);
            WallBox(b, pz, 0.2f + ph * 0.5f, 0.1f, ph, 0.13f, 0.23f, pipe);
            for (float by = 1.2f; by < topY - 0.4f; by += 1.6f) WallBox(b, pz, by, 0.2f, 0.07f, 0.13f, 0.27f, pipe);
            WallBox(b, pz, 0.2f, 0.1f, 0.12f, 0.13f, 0.4f, pipe);   // the foot
        }

        // balcony awnings and street furniture
        BalconyAwnings(b);
        StreetProps(b, L);

        // the roof
        if (!gable) FlatRoof(b, L, topY);
        else HippedExtras(b, L, topY);

        // sign frames
        if (fSignLen > 0f)
        {
            float sy = 4.3f, bh = fSignH;
            WallBox(b, 0f, sy + bh * 0.5f + 0.04f, fSignLen + 0.2f, 0.09f, 0f, 0.2f, fAccent);
            WallBox(b, 0f, sy - bh * 0.5f - 0.04f, fSignLen + 0.2f, 0.09f, 0f, 0.2f, fAccent);
            WallBox(b, -(fSignLen * 0.5f + 0.05f), sy, 0.09f, bh + 0.18f, 0f, 0.2f, fAccent);
            WallBox(b, (fSignLen * 0.5f + 0.05f), sy, 0.09f, bh + 0.18f, 0f, 0.2f, fAccent);
            WallBox(b, 0f, sy + bh * 0.5f + 0.16f, fSignLen * 0.7f, 0.07f, 0.14f, 0.34f, SteelColor);
        }
        if (fBlade)
        {
            WallBox(b, fBladeZ, fBladeY + 0.36f, 0.06f, 0.05f, 0f, 1.0f, SteelColor);
            WallBox(b, fBladeZ, fBladeY, 0.08f, 0.52f, 0.1f, 1.0f, SignDark);
            WallBox(b, fBladeZ, fBladeY + 0.31f, 0.03f, 0.1f, 0.14f, 0.17f, SteelColor);
            WallBox(b, fBladeZ, fBladeY + 0.31f, 0.03f, 0.1f, 0.92f, 0.95f, SteelColor);
        }

        if (facadeGO == null)
        {
            facadeGO = new GameObject("Facade");
            facadeGO.transform.SetParent(transform, false);
            facadeGO.AddComponent<MeshFilter>();
            var mr = facadeGO.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        facadeGO.GetComponent<MeshRenderer>().sharedMaterial = FacadePalette.Mat;
        facadeMesh = b.Upload(facadeMesh);
        facadeGO.GetComponent<MeshFilter>().sharedMesh = facadeMesh;
        facadeGO.SetActive(true);
    }

    // ---- windows ----

    void FrameWindows(FacadeBuilder b, WindowSlot[] arr, float salt)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            var w = arr[i];
            if (w == null || w.visual == null || !w.visual.gameObject.activeSelf) continue;
            Vector3 p = w.visual.localPosition;
            WindowFrame(b, p.z, p.y, w.wd, w.ht, w.type, salt + i * 3.7f);
        }
    }

    // A frame round four bars: the glass is wd wide and ht tall, t thick, depth out from the wall.
    void Surround(FacadeBuilder b, float z, float y, float wd, float ht, float t, float depth, Color c)
    {
        WallBox(b, z - (wd * 0.5f + t * 0.5f), y, t, ht + 2f * t, 0f, depth, c);
        WallBox(b, z + (wd * 0.5f + t * 0.5f), y, t, ht + 2f * t, 0f, depth, c);
        WallBox(b, z, y + (ht * 0.5f + t * 0.5f), wd, t, 0f, depth, c);
        WallBox(b, z, y - (ht * 0.5f + t * 0.5f), wd, t, 0f, depth, c);
    }

    // A bar over the glass (it stands a little in front of it, so the glass looks divided into panes).
    void PaneBar(FacadeBuilder b, float z, float y, float wd, float ht, Color c)
    {
        WallBox(b, z, y, wd, ht, 0.06f, 0.1f, c);
    }

    // One window of the given style (see House.Windows.cs) in the house's way of decorating it.
    void WindowFrame(FacadeBuilder b, float z, float y, float W, float H, int type, float key)
    {
        float s = side;
        float rz = -s;   // seen from the road, "to the right" along the wall is towards -z on the right side of the road and +z on the left
        float h = Hash01(key * 7.31f + ShopKey * 0.37f);
        bool single = W <= 1.3f;

        switch (type)
        {
            case 0:   // tall twin: grey frame, a bar down the middle
                {
                    Color fr = new Color(0.66f, 0.67f, 0.70f);
                    Surround(b, z, y, W, H, 0.1f, 0.12f, fr);
                    PaneBar(b, z, y, 0.07f, H, fr);
                    WallBox(b, z, y - H * 0.5f - 0.15f, W + 0.3f, 0.08f, 0f, 0.2f, Color.Lerp(fr, Color.black, 0.15f));
                }
                break;
            case 1:   // wide grid: four columns, two rows, white frame and a heavy sill
                {
                    Color wh = new Color(0.96f, 0.96f, 0.93f);
                    Surround(b, z, y, W, H, 0.12f, 0.14f, wh);
                    for (int i = 1; i <= 3; i++) PaneBar(b, z - W * 0.5f + W * i / 4f, y, 0.06f, H, wh);
                    PaneBar(b, z, y, W, 0.07f, wh);
                    WallBox(b, z, y - H * 0.5f - 0.2f, W + 0.5f, 0.1f, 0f, 0.3f, wh);
                    WallBox(b, z, y - H * 0.5f - 0.31f, W + 0.3f, 0.1f, 0f, 0.2f, Color.Lerp(wh, Color.black, 0.2f));
                }
                break;
            case 2:   // wide mixed: two tall panes and a column split in two, dark brown frame
                {
                    Color br = new Color(0.30f, 0.17f, 0.15f);
                    Surround(b, z, y, W, H, 0.1f, 0.12f, br);
                    float c1 = -W * 0.5f + 0.36f * W, c2 = c1 + 0.36f * W;
                    PaneBar(b, z + rz * c1, y, 0.06f, H, br);
                    PaneBar(b, z + rz * c2, y, 0.06f, H, br);
                    PaneBar(b, z + rz * (c2 + W * 0.5f) * 0.5f, y, W * 0.5f - c2, 0.06f, br);
                    WallBox(b, z, y - H * 0.5f - 0.13f, W + 0.25f, 0.07f, 0f, 0.18f, br);
                }
                break;
            case 3:   // square mixed: a tall pane, and a small pane over a tall one
                {
                    Color gr = new Color(0.36f, 0.33f, 0.42f);
                    Surround(b, z, y, W, H, 0.1f, 0.12f, gr);
                    float vx = -W * 0.5f + 0.56f * W;
                    PaneBar(b, z + rz * vx, y, 0.06f, H, gr);
                    PaneBar(b, z + rz * (vx + W * 0.5f) * 0.5f, y + H * 0.2f, W * 0.5f - vx, 0.06f, gr);
                    WallBox(b, z, y - H * 0.5f - 0.13f, W + 0.25f, 0.07f, 0f, 0.18f, gr);
                }
                break;
            case 4:   // stone classic: cream surround, lintel with a keystone, sill on corbels, two columns and a transom
                {
                    Color st = new Color(0.90f, 0.85f, 0.72f);
                    Surround(b, z, y, W, H, 0.14f, 0.16f, st);
                    WallBox(b, z, y + H * 0.5f + 0.25f, W + 0.5f, 0.14f, 0f, 0.26f, st);
                    WallBox(b, z, y + H * 0.5f + 0.22f, 0.2f, 0.3f, 0f, 0.32f, Color.Lerp(st, Color.white, 0.3f));
                    WallBox(b, z, y - H * 0.5f - 0.2f, W + 0.5f, 0.12f, 0f, 0.32f, st);
                    for (int k = -1; k <= 1; k += 2) WallBox(b, z + k * (W * 0.5f + 0.12f), y - H * 0.5f - 0.34f, 0.12f, 0.18f, 0f, 0.2f, st);
                    PaneBar(b, z, y, 0.07f, H, st);
                    PaneBar(b, z, y + H * 0.22f, W, 0.06f, st);
                }
                break;
            default:  // plain: the old single pane in a simple frame
                {
                    Color fc = fFrame;
                    Surround(b, z, y, W, H, 0.12f, 0.13f, fc);
                    WallBox(b, z, y + H * 0.5f + 0.2f, W + 0.4f, 0.12f, 0f, 0.2f, fc);
                    WallBox(b, z, y - H * 0.5f - 0.2f, W + 0.36f, 0.1f, 0f, 0.26f, fc);
                    PaneBar(b, z, y, 0.05f, H, fc);
                    PaneBar(b, z, y + 0.08f, W, 0.05f, fc);
                }
                break;
        }

        // the house's extra on its windows: open slatted shutters, flower boxes or a tin awning
        if (fWin == 1 && single && type != 4)
        {
            Color panel = new Color(0.30f, 0.34f, 0.38f), slat = new Color(0.62f, 0.65f, 0.68f);
            float hinge = W * 0.5f + 0.12f;
            for (int k = -1; k <= 1; k += 2)
            {
                Quaternion q = Quaternion.Euler(0f, -s * k * 55f, 0f);   // the outer end swings out towards the road
                float zc = z + k * (hinge + 0.143f);
                WallRot(b, zc, y, 0.5f, H + 0.1f, 0.285f, 0.325f, panel, q);
                for (int i = -1; i <= 1; i++)
                    WallRot(b, zc, y + i * H * 0.33f, 0.46f, 0.07f, 0.27f, 0.345f, slat, q);
            }
        }
        else if (fWin == 2 && single && (type == 0 || type == 3) && h < 0.6f)
        {
            float by = y - H * 0.5f - 0.42f;
            WallBox(b, z, by, W + 0.15f, 0.2f, 0f, 0.3f, new Color(0.40f, 0.30f, 0.22f));
            WallBox(b, z, by + 0.14f, W + 0.05f, 0.12f, 0.02f, 0.26f, new Color(0.25f, 0.55f, 0.28f));
            WallBox(b, z - 0.3f, by + 0.2f, 0.12f, 0.1f, 0.06f, 0.2f, new Color(0.95f, 0.70f, 0.82f));
            WallBox(b, z, by + 0.2f, 0.12f, 0.1f, 0.06f, 0.2f, new Color(0.98f, 0.92f, 0.55f));
            WallBox(b, z + 0.3f, by + 0.2f, 0.12f, 0.1f, 0.06f, 0.2f, new Color(0.96f, 0.96f, 0.96f));
        }
        else if (fWin == 3)
        {
            Quaternion q = Quaternion.Euler(0f, 0f, s * 18f);
            float aw = W + 0.4f;
            int n = Mathf.Max(4, Mathf.RoundToInt(aw / 0.25f));
            float sw = aw / n;
            float ay = y + H * 0.5f + (type == 4 ? 0.8f : 0.62f);
            for (int i = 0; i < n; i++)
                WallRot(b, z - aw * 0.5f + sw * (i + 0.5f), ay, sw, 0.05f, 0.02f, 0.74f, (i & 1) == 0 ? fAccent : new Color(0.93f, 0.92f, 0.86f), q);
        }

        // an AC unit under some windows
        if (fWin != 2 && (type == 0 || type == 3 || type == 4) && h > 0.8f)
        {
            float ay2 = y - H * 0.5f - (type == 4 ? 0.75f : 0.6f);
            WallBox(b, z + 0.15f, ay2, 0.8f, 0.5f, 0.12f, 0.55f, new Color(0.84f, 0.86f, 0.88f));
            WallBox(b, z + 0.15f, ay2, 0.5f, 0.3f, 0.55f, 0.58f, new Color(0.22f, 0.24f, 0.28f));
        }
    }

    // ---- the roof ----

    // The height of the parapet above the roof slab (the slab's top is at BodyH + 0.6).
    float ParapetH()
    {
        switch (fRoof)
        {
            case 1: return 0.72f;   // with its merlons
            case 2: return 0.88f;   // with its railing
            case 3: return 0.5f;
            case 4: return 0.9f;   // the low wall and its picket fence
            default: return 0.55f;
        }
    }

    // The sign stands on the roof edge: width, height and the height of its middle.
    void HotelBoard(out float hw, out float hh, out float cy)
    {
        hw = Mathf.Min(fL - 0.8f, 4.5f);
        hh = hw / 3f;
        cy = BodyH + 0.6f + ParapetH() + 0.3f + hh * 0.5f;
    }

    void FlatRoof(FacadeBuilder b, float L, float topY)
    {
        float s = side;
        float rt = topY + 0.6f;              // top of the roof slab
        float rl = L + 0.8f;                 // the slab's length along the road (x from 8.1 to 14.9)
        float half = L * 0.5f;
        Color wallC = fRoof == 3 ? fAccent : fTrim;
        Color coping = Color.Lerp(fTrim, Color.white, 0.5f);
        float hp = fRoof == 1 ? 0.32f : (fRoof == 2 ? 0.28f : (fRoof == 3 ? 0.5f : (fRoof == 4 ? 0.3f : 0.55f)));

        // the parapet wall round the roof, with a coping on top
        b.Box(new Vector3(s * 8.2f, rt + hp * 0.5f, 0f), new Vector3(0.2f, hp, rl), wallC);
        b.Box(new Vector3(s * 14.8f, rt + hp * 0.5f, 0f), new Vector3(0.2f, hp, rl), wallC);
        for (int k = -1; k <= 1; k += 2)
            b.Box(new Vector3(s * 11.5f, rt + hp * 0.5f, k * (rl * 0.5f - 0.1f)), new Vector3(6.8f, hp, 0.2f), wallC);
        if (fRoof != 1)
        {
            b.Box(new Vector3(s * 8.2f, rt + hp + 0.04f, 0f), new Vector3(0.34f, 0.08f, rl + 0.08f), coping);
            b.Box(new Vector3(s * 14.8f, rt + hp + 0.04f, 0f), new Vector3(0.34f, 0.08f, rl + 0.08f), coping);
            for (int k = -1; k <= 1; k += 2)
                b.Box(new Vector3(s * 11.5f, rt + hp + 0.04f, k * (rl * 0.5f - 0.1f)), new Vector3(6.88f, 0.08f, 0.34f), coping);
        }

        if (fRoof == 0)
        {
            b.Box(new Vector3(s * 8.09f, rt + hp * 0.5f, 0f), new Vector3(0.04f, 0.12f, rl - 0.2f), fAccent);   // a coloured stripe along the front
        }
        else if (fRoof == 1)
        {
            // merlons along the front and the two sides
            int n = Mathf.Max(2, Mathf.RoundToInt((rl - 0.9f) / 0.9f) + 1);
            for (int i = 0; i < n; i++)
            {
                float z = -rl * 0.5f + 0.45f + (rl - 0.9f) * i / (n - 1);
                b.Box(new Vector3(s * 8.2f, rt + hp + 0.2f, z), new Vector3(0.2f, 0.4f, 0.5f), wallC);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = 9.2f + i * 1.2f;
                for (int k = -1; k <= 1; k += 2)
                    b.Box(new Vector3(s * x, rt + hp + 0.2f, k * (rl * 0.5f - 0.1f)), new Vector3(0.5f, 0.4f, 0.2f), wallC);
            }
            b.Box(new Vector3(s * 8.09f, rt + hp * 0.5f, 0f), new Vector3(0.04f, 0.1f, rl - 0.2f), fAccent);
        }
        else if (fRoof == 2)
        {
            // a metal railing on the low wall: posts, a top rail and a middle rail, along the front and the sides
            int n = Mathf.Max(2, Mathf.RoundToInt(rl / 0.9f) + 1);
            for (int i = 0; i < n; i++)
            {
                float z = -rl * 0.5f + 0.15f + (rl - 0.3f) * i / (n - 1);
                b.Box(new Vector3(s * 8.2f, rt + hp + 0.3f, z), new Vector3(0.06f, 0.6f, 0.06f), fAccent);
            }
            b.Box(new Vector3(s * 8.2f, rt + hp + 0.58f, 0f), new Vector3(0.07f, 0.06f, rl - 0.1f), fAccent);
            b.Box(new Vector3(s * 8.2f, rt + hp + 0.3f, 0f), new Vector3(0.05f, 0.05f, rl - 0.1f), fAccent);
            for (int i = 0; i < 5; i++)
            {
                float x = 8.9f + i * 1.5f;
                for (int k = -1; k <= 1; k += 2)
                    b.Box(new Vector3(s * x, rt + hp + 0.3f, k * (rl * 0.5f - 0.1f)), new Vector3(0.06f, 0.6f, 0.06f), fAccent);
            }
            for (int k = -1; k <= 1; k += 2)
            {
                b.Box(new Vector3(s * 11.5f, rt + hp + 0.58f, k * (rl * 0.5f - 0.1f)), new Vector3(6.6f, 0.06f, 0.07f), fAccent);
                b.Box(new Vector3(s * 11.5f, rt + hp + 0.3f, k * (rl * 0.5f - 0.1f)), new Vector3(6.6f, 0.05f, 0.05f), fAccent);
            }
        }
        else if (fRoof == 3)
        {
            // a deep fascia band under the rim
            b.Box(new Vector3(s * 7.95f, topY + 0.45f, 0f), new Vector3(0.3f, 0.3f, rl + 0.4f), fTrimDark);
        }
        else
        {
            // a roof terrace: a white picket fence with two rails round the low wall
            Color white = new Color(0.95f, 0.95f, 0.92f);
            int nf = Mathf.Max(3, Mathf.RoundToInt(rl / 0.3f));
            for (int i = 0; i < nf; i++)
            {
                float z = -rl * 0.5f + 0.15f + (rl - 0.3f) * i / (nf - 1);
                b.Box(new Vector3(s * 8.2f, rt + hp + 0.3f, z), new Vector3(0.05f, 0.6f, 0.07f), white);
            }
            b.Box(new Vector3(s * 8.2f, rt + hp + 0.52f, 0f), new Vector3(0.05f, 0.05f, rl - 0.1f), white);
            b.Box(new Vector3(s * 8.2f, rt + hp + 0.14f, 0f), new Vector3(0.05f, 0.05f, rl - 0.1f), white);
            int ns = Mathf.RoundToInt(6.2f / 0.3f);
            for (int i = 0; i < ns; i++)
            {
                float x = 8.5f + 6.0f * i / (ns - 1);
                for (int k = -1; k <= 1; k += 2)
                    b.Box(new Vector3(s * x, rt + hp + 0.3f, k * (rl * 0.5f - 0.1f)), new Vector3(0.07f, 0.6f, 0.05f), white);
            }
            for (int k = -1; k <= 1; k += 2)
            {
                b.Box(new Vector3(s * 11.5f, rt + hp + 0.52f, k * (rl * 0.5f - 0.1f)), new Vector3(6.4f, 0.05f, 0.05f), white);
                b.Box(new Vector3(s * 11.5f, rt + hp + 0.14f, k * (rl * 0.5f - 0.1f)), new Vector3(6.4f, 0.05f, 0.05f), white);
            }
        }

        // what stands on the roof, in slots along it: a tank, AC units, solar panels, a dish, a vent, a washing line, or the stair-head
        float top = rt;
        int slots = Mathf.Max(1, Mathf.FloorToInt((L - 0.8f) / 2.3f));
        float step = (L - 0.8f) / slots;
        bool stairDone = false;
        for (int k = 0; k < slots; k++)
        {
            float z = -half + 0.4f + step * (k + 0.5f);
            bool end = k == 0 || k == slots - 1;
            if (!stairDone && end && L >= 6f && floors >= 2 && FH(77f + k) < 0.5f)
            {
                StairHead(b, z, top);
                stairDone = true;
                continue;
            }
            float r = FH(60f + k * 4.3f);
            if (fRoof == 4)
            {
                if (k == slots / 2) { TerraceAwning(b, z, top); continue; }
                if (r < 0.3f) { Crates(b, z, top); continue; }
                if (r < 0.55f) { Plants(b, z, top); continue; }
            }
            if (r < 0.12f) continue;
            if (r < 0.37f) WaterTank(b, z, top, r);
            else if (r < 0.52f) RoofAc(b, z, top);
            else if (r < 0.67f) SolarPanels(b, z, top);
            else if (r < 0.75f) Dish(b, z, top);
            else if (r < 0.85f) VentStack(b, z, top);
            else WashingLine(b, z, top, k);
        }

        // the hotel's roof board on posts
        if (fHotel)
        {
            float hw, hh, cy;
            HotelBoard(out hw, out hh, out cy);
            float boardBottom = cy - hh * 0.5f;
            float postH = boardBottom - rt;
            for (int k = -1; k <= 1; k += 2)
                b.Box(new Vector3(s * 8.8f, rt + postH * 0.5f, k * (hw * 0.5f - 0.45f)), new Vector3(0.12f, postH, 0.12f), SteelColor);
            b.Box(new Vector3(s * 8.8f, cy, 0f), new Vector3(0.16f, hh, hw), SignDark);
            b.Box(new Vector3(s * 8.72f, cy + hh * 0.5f + 0.03f, 0f), new Vector3(0.2f, 0.07f, hw + 0.14f), fAccent);
            b.Box(new Vector3(s * 8.72f, cy - hh * 0.5f - 0.03f, 0f), new Vector3(0.2f, 0.07f, hw + 0.14f), fAccent);
            for (int k = -1; k <= 1; k += 2)
                b.Box(new Vector3(s * 8.72f, cy, k * (hw * 0.5f + 0.035f)), new Vector3(0.2f, hh + 0.14f, 0.07f), fAccent);
            b.Box(new Vector3(s * 8.55f, cy + hh * 0.5f + 0.13f, 0f), new Vector3(0.3f, 0.07f, hw * 0.7f), SteelColor);   // the tube light's housing
        }
    }

    void WaterTank(FacadeBuilder b, float z, float rt, float r)
    {
        float s = side;
        float x = 12.0f;
        Color[] tanks = { new Color(0.15f, 0.16f, 0.18f), new Color(0.35f, 0.60f, 0.80f), new Color(0.90f, 0.91f, 0.92f), new Color(0.25f, 0.40f, 0.30f) };
        Color tc = tanks[Idx(Hash01(r * 91.7f + ShopKey), tanks.Length)];
        Color conc = new Color(0.62f, 0.62f, 0.60f);
        b.Box(new Vector3(s * x, rt + 0.04f, z), new Vector3(1.3f, 0.08f, 1.3f), conc);
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                b.Box(new Vector3(s * (x + i * 0.5f), rt + 0.33f, z + j * 0.5f), new Vector3(0.1f, 0.5f, 0.1f), conc);
        b.Box(new Vector3(s * x, rt + 0.62f, z), new Vector3(1.3f, 0.08f, 1.3f), conc);
        b.Cyl(new Vector3(s * x, rt + 0.66f + 0.45f, z), 0.62f, 0.62f, 0.9f, 8, tc, Quaternion.identity);
        b.Cyl(new Vector3(s * x, rt + 0.66f + 0.95f, z), 0.4f, 0.34f, 0.1f, 8, Color.Lerp(tc, Color.black, 0.3f), Quaternion.identity);
    }

    void RoofAc(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        for (int k = -1; k <= 1; k += 2)
        {
            float zz = z + k * 0.45f;
            b.Box(new Vector3(s * 9.6f, rt + 0.1f, zz), new Vector3(0.5f, 0.2f, 0.6f), SteelColor);
            b.Box(new Vector3(s * 9.6f, rt + 0.2f + 0.3f, zz), new Vector3(0.5f, 0.6f, 0.7f), new Color(0.84f, 0.86f, 0.88f));
            b.Cyl(new Vector3(s * (9.6f - 0.26f), rt + 0.5f, zz), 0.24f, 0.24f, 0.04f, 8, new Color(0.22f, 0.24f, 0.28f), Quaternion.Euler(0f, 0f, 90f));
        }
    }

    void SolarPanels(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        Quaternion q = Quaternion.Euler(0f, 0f, s * 22f);   // low edge towards the road
        for (int k = -1; k <= 1; k += 2)
        {
            float zz = z + k * 0.55f;
            b.Box(new Vector3(s * 11.0f, rt + 0.62f, zz), new Vector3(1.5f, 0.05f, 1.0f), new Color(0.55f, 0.58f, 0.62f), q);
            b.Box(new Vector3(s * 11.0f, rt + 0.64f, zz), new Vector3(1.4f, 0.04f, 0.9f), new Color(0.10f, 0.17f, 0.34f), q);
            b.Box(new Vector3(s * 11.6f, rt + 0.45f, zz), new Vector3(0.07f, 0.9f, 0.07f), SteelColor);
            b.Box(new Vector3(s * 10.4f, rt + 0.2f, zz), new Vector3(0.07f, 0.4f, 0.07f), SteelColor);
        }
    }

    void Dish(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        b.Box(new Vector3(s * 12.4f, rt + 0.6f, z), new Vector3(0.07f, 1.2f, 0.07f), SteelColor);
        b.Cyl(new Vector3(s * 12.4f, rt + 1.35f, z), 0.4f, 0.4f, 0.06f, 8, new Color(0.90f, 0.91f, 0.92f), Quaternion.Euler(0f, 0f, s * 55f));
        b.Box(new Vector3(s * (12.4f - 0.28f), rt + 1.55f, z), new Vector3(0.3f, 0.04f, 0.04f), SteelColor);
    }

    void VentStack(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        b.Box(new Vector3(s * 13.0f, rt + 0.55f, z), new Vector3(0.5f, 1.1f, 0.5f), new Color(0.78f, 0.77f, 0.74f));
        b.Box(new Vector3(s * 13.0f, rt + 1.14f, z), new Vector3(0.7f, 0.08f, 0.7f), new Color(0.30f, 0.30f, 0.34f));
    }

    void WashingLine(FacadeBuilder b, float z, float rt, int k)
    {
        float s = side;
        float x = 10.8f;
        b.Box(new Vector3(s * x, rt + 0.65f, z - 0.95f), new Vector3(0.05f, 1.3f, 0.05f), SteelColor);
        b.Box(new Vector3(s * x, rt + 0.65f, z + 0.95f), new Vector3(0.05f, 1.3f, 0.05f), SteelColor);
        b.Box(new Vector3(s * x, rt + 1.2f, z), new Vector3(0.02f, 0.02f, 1.9f), new Color(0.80f, 0.80f, 0.78f));
        for (int i = 0; i < 4; i++)
        {
            Color c = ClothColors[Idx(Hash01(ShopKey * 0.61f + k * 5.3f + i * 2.9f), ClothColors.Length)];
            b.Box(new Vector3(s * x, rt + 0.93f, z - 0.6f + i * 0.4f), new Vector3(0.02f, 0.5f, 0.3f), c);
        }
    }

    // The little room over the stairs, with a door on the side facing the road.
    void StairHead(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        Color wall = Color.Lerp(fTrim, fTrimDark, 0.2f);
        b.Box(new Vector3(s * 12.4f, rt + 1.1f, z), new Vector3(2.2f, 2.2f, 2.0f), wall);
        b.Box(new Vector3(s * 12.4f, rt + 2.27f, z), new Vector3(2.5f, 0.14f, 2.3f), fTrim);
        b.Box(new Vector3(s * (12.4f - 1.1f - 0.02f), rt + 0.75f, z), new Vector3(0.06f, 1.5f, 0.8f), Color.Lerp(fAccent, Color.black, 0.35f));
        b.Box(new Vector3(s * (12.4f - 1.1f - 0.02f), rt + 1.75f, z), new Vector3(0.06f, 0.35f, 0.5f), new Color(0.65f, 0.85f, 1.0f));
    }

    // ---- hipped roof extras: dormers and a cluster of chimney pipes ----

    void HippedExtras(FacadeBuilder b, float L, float topY)
    {
        float s = side;
        float ridgeY = topY + 1.5f;
        float run = (L + 0.2f) * 0.28f;   // half the length of the ridge
        Color pipeC = new Color(0.45f, 0.46f, 0.50f), capC = new Color(0.28f, 0.29f, 0.33f), roofDark = new Color(0.20f, 0.27f, 0.33f);

        int np = 2 + Idx(FH(51.3f), 3);
        for (int i = 0; i < np; i++)
        {
            float z = Mathf.Lerp(-run * 0.8f, run * 0.8f, (i + 0.5f) / np) + (FH(52f + i) - 0.5f) * 0.3f;
            float hgt = 0.5f + 0.8f * FH(53f + i * 2.1f);
            float x = 11.5f + (FH(54f + i) - 0.5f) * 0.5f;
            b.Box(new Vector3(s * x, ridgeY + hgt * 0.5f - 0.05f, z), new Vector3(0.18f, hgt, 0.18f), pipeC);
            b.Box(new Vector3(s * x, ridgeY + hgt + 0.01f, z), new Vector3(0.3f, 0.06f, 0.3f), capC);
        }
        if (FH(55f) < 0.6f)
        {
            float z = (FH(58f) - 0.5f) * run * 1.4f;
            b.Box(new Vector3(s * 10.9f, ridgeY + 0.25f, z), new Vector3(0.45f, 0.6f, 0.45f), pipeC);
            b.Cyl(new Vector3(s * 10.9f, ridgeY + 0.68f, z), 0.42f, 0.1f, 0.2f, 4, capC, Quaternion.Euler(0f, 45f, 0f));
        }

        // box dormers on the road-side slope
        int nd = L >= 9f ? 2 : 1;
        for (int i = 0; i < nd; i++)
        {
            if (nd == 1 && FH(56f) > 0.8f) break;
            float z = nd == 1 ? (FH(57f) - 0.5f) * run * 0.6f : (i == 0 ? -1f : 1f) * run * 0.5f;
            float fx = 9.3f;                                    // x of the dormer's front face
            float rise = 1.5f * (fx - 8.0f) / 3.5f;             // the height of the slope there
            float baseY = topY + rise - 0.08f;
            float hgt = 1.0f;
            b.Box(new Vector3(s * (fx + 0.6f), baseY + hgt * 0.5f, z), new Vector3(1.2f, hgt, 1.3f), fAccent);
            b.Box(new Vector3(s * (fx - 0.03f), baseY + hgt * 0.5f + 0.02f, z), new Vector3(0.07f, 0.7f, 0.85f), fFrame);
            b.Box(new Vector3(s * (fx - 0.055f), baseY + hgt * 0.5f + 0.02f, z), new Vector3(0.04f, 0.56f, 0.7f), new Color(0.65f, 0.85f, 1.0f));
            b.Box(new Vector3(s * (fx + 0.6f), baseY + hgt + 0.05f, z), new Vector3(1.45f, 0.1f, 1.55f), roofDark);
        }
    }

    // ---- balcony awnings (only where no customer is waiting: they would hide the target) and street furniture ----

    void BalconyAwnings(FacadeBuilder b)
    {
        if (Subscriber || balconies == null) return;
        float s = side;
        for (int i = 0; i < balconies.Length; i++)
        {
            var bs = balconies[i];
            if (!bs.on || Hash01(ShopKey * 0.77f + bs.floor * 3.1f + bs.z) > 0.55f) continue;
            float by = FloorBase(bs.floor);
            for (int k = -1; k <= 1; k += 2)
                b.Box(new Vector3(s * 7.0f, by + 1.55f, bs.z + k * (bs.len * 0.5f - 0.1f)), new Vector3(0.06f, 2.0f, 0.06f), SteelColor);
            int n = Mathf.Max(4, Mathf.RoundToInt(bs.len / 0.3f));
            float sw = bs.len / n;
            Quaternion q = Quaternion.Euler(0f, 0f, s * 12f);   // low edge towards the road
            for (int j = 0; j < n; j++)
                b.Box(new Vector3(s * 7.6f, by + 2.7f, bs.z - bs.len * 0.5f + sw * (j + 0.5f)), new Vector3(1.9f, 0.05f, sw),
                      (j & 1) == 0 ? new Color(0.95f, 0.95f, 0.92f) : new Color(0.80f, 0.85f, 0.88f), q);
        }
    }

    // A bench and a fire hydrant on the pavement (they stand in the open: papers pass through them, they are only for looks).
    void StreetProps(FacadeBuilder b, float L)
    {
        if (FH(41.3f) > 0.3f) return;
        float s = side, half = L * 0.5f;
        bool first = FH(42.7f) < 0.5f;
        float bz = (first ? -1f : 1f) * (half - 1.3f);
        float hz = -bz * 0.9f;
        Color wood = new Color(0.86f, 0.70f, 0.30f), leg = new Color(0.30f, 0.32f, 0.36f);
        float bx = 3.45f - setback;
        b.Box(new Vector3(s * bx, 0.45f, bz), new Vector3(0.5f, 0.06f, 1.2f), wood);
        b.Box(new Vector3(s * (bx + 0.27f), 0.75f, bz), new Vector3(0.06f, 0.5f, 1.2f), wood);
        for (int k = -1; k <= 1; k += 2) b.Box(new Vector3(s * bx, 0.22f, bz + k * 0.5f), new Vector3(0.4f, 0.44f, 0.08f), leg);
        Color hyd = new Color(0.90f, 0.80f, 0.25f);
        float hx = 3.25f - setback;
        b.Cyl(new Vector3(s * hx, 0.3f, hz), 0.13f, 0.12f, 0.6f, 8, hyd, Quaternion.identity);
        b.Cyl(new Vector3(s * hx, 0.64f, hz), 0.15f, 0.1f, 0.08f, 8, hyd, Quaternion.identity);
        b.Box(new Vector3(s * hx, 0.4f, hz), new Vector3(0.1f, 0.1f, 0.4f), hyd);
    }

    // ---- roof terrace furniture ----

    void TerraceAwning(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        float x0 = 10.4f;
        Color white = new Color(0.95f, 0.95f, 0.92f);
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                b.Box(new Vector3(s * (x0 + i * 0.95f), rt + 1.0f, z + j * 0.95f), new Vector3(0.07f, 2.0f, 0.07f), white);
        Quaternion q = Quaternion.Euler(0f, 0f, s * 8f);   // low edge towards the road
        for (int k = 0; k < 8; k++)
            b.Box(new Vector3(s * x0, rt + 2.05f, z - 1.0f + 0.25f * (k + 0.5f)), new Vector3(2.1f, 0.05f, 0.25f),
                  (k & 1) == 0 ? white : new Color(0.80f, 0.86f, 0.88f), q);
        // a table and two chairs under it
        Color top = new Color(0.86f, 0.70f, 0.30f), leg = SteelColor;
        b.Box(new Vector3(s * x0, rt + 0.7f, z), new Vector3(0.8f, 0.06f, 0.8f), top);
        b.Box(new Vector3(s * x0, rt + 0.35f, z), new Vector3(0.1f, 0.7f, 0.1f), leg);
        for (int k = -1; k <= 1; k += 2)
        {
            b.Box(new Vector3(s * x0, rt + 0.4f, z + k * 0.65f), new Vector3(0.4f, 0.06f, 0.4f), top);
            b.Box(new Vector3(s * x0, rt + 0.2f, z + k * 0.65f), new Vector3(0.36f, 0.4f, 0.36f), leg);
        }
    }

    void Crates(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        Color c = new Color(0.62f, 0.48f, 0.30f), c2 = new Color(0.55f, 0.42f, 0.27f);
        b.Box(new Vector3(s * 13.0f, rt + 0.25f, z - 0.3f), new Vector3(0.5f, 0.5f, 0.5f), c);
        b.Box(new Vector3(s * 13.0f, rt + 0.25f, z + 0.3f), new Vector3(0.5f, 0.5f, 0.5f), c2);
        b.Box(new Vector3(s * 13.0f, rt + 0.75f, z), new Vector3(0.5f, 0.5f, 0.5f), c, Quaternion.Euler(0f, 18f, 0f));
    }

    void Plants(FacadeBuilder b, float z, float rt)
    {
        float s = side;
        for (int k = -1; k <= 1; k += 2)
        {
            b.Box(new Vector3(s * 12.0f, rt + 0.18f, z + k * 0.6f), new Vector3(0.4f, 0.36f, 0.4f), new Color(0.55f, 0.56f, 0.60f));
            b.Box(new Vector3(s * 12.0f, rt + 0.62f, z + k * 0.6f), new Vector3(0.55f, 0.5f, 0.55f), new Color(0.28f, 0.58f, 0.30f), Quaternion.Euler(0f, 20f, 0f));
        }
    }
}
