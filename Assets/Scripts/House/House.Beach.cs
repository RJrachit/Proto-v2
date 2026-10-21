using System.Collections.Generic;
using UnityEngine;

// House (partial) - Beach lots: sand, sea, dunes, palms, sunbathers, picnic, lifeguard hut and cabins.
// The main part of the class is in House.cs.
public partial class House
{
    // (Palm shots are no longer scored: no palm zone is switched on, so this is not reached.) A paper that stays on a palm crown counted as a delivery or a small bonus.
    public void HandlePalm(HouseTarget zone, Vector3 pos)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (Subscriber) { HandleHit(HouseTarget.Kind.Palm, pos, false); return; }
        int idx = System.Array.IndexOf(palmZones, zone);
        if (idx < 0) return;
        if (palmUsed[idx]) { gm.CountHit(); return; }   // (already scored: no points, but the hit counts)
        palmUsed[idx] = true;
        gm.Bonus(GameConfig.PointsHoopBonus, "Palm shot! +", pos);
    }

    // ---- a beach lot: sand and sea in place of the house ----
    static readonly Color[] BeachColors =
    {
        new Color(0.20f, 0.65f, 0.70f), new Color(0.25f, 0.45f, 0.85f), new Color(0.60f, 0.42f, 0.80f),
        new Color(0.98f, 0.86f, 0.30f), new Color(0.40f, 0.75f, 0.45f), new Color(0.93f, 0.93f, 0.90f),
    };

    void LayoutBeach(float L, float doorZ, bool flip)
    {
        if (beachScene == 5) { LayoutDune(L); return; }
        float s = side;
        float half = L * 0.5f;
        float h = Hash01(transform.position.z * 3.171f + (side > 0 ? 6.9f : 29.3f));
        float cover = L + GameConfig.LotGap + 0.1f;    // also covers the half-gaps either side, so the sand has no seams
        const float seaX = 11.6f;
        Color sand = GameConfig.SandColor;

        // sand from the footpath to the sea, then the sea and a line of foam
        lawnT.localPosition = new Vector3(s * (4.4f + seaX) * 0.5f, 0.04f, 0f);
        lawnT.localScale = new Vector3(seaX - 4.4f, 0.08f, cover);
        lawnT.gameObject.SetActive(true);
        if (padRenderer != null) padRenderer.material.color = sand;
        // the shoreline is not a straight line: sand tongues run out into the sea in slices, each with a wet band, a pale shallows band and a lip of foam
        int shN = Mathf.Max(2, Mathf.RoundToInt(cover / 2.5f));
        float shW = cover / shN;
        for (int shI = 0; shI < shN; shI++)
        {
            float shZ = -cover * 0.5f + shW * (shI + 0.5f);
            float shX = ShoreX(transform.position.z + shZ);
            ShoreSlice(shZ, shW, seaX, shX, shX);
        }

        // the scene: a sunbather behind the doormat, a picnic blanket, a lifeguard hut, or a row of changing cabins
        int ci = (int)(h * 997f) % BeachColors.Length;
        Color towel = BeachColors[ci], suit = BeachColors[(ci + 2) % BeachColors.Length];
        Color skin = new Color(0.88f, 0.68f, 0.52f), hair = new Color(0.25f, 0.16f, 0.10f);
        float bx = s * 9.3f;
        yardSpotValid = false;
        if (beachScene == 1) LayoutPicnic(L, doorZ, ci, skin, hair);
        else if (beachScene == 2) LayoutLifeguard(L, doorZ, ci, skin);
        else if (beachScene == 3) LayoutCabins(L, doorZ, ci);
        else if (beachScene == 4) LayoutRecliner(L, doorZ, ci, skin, hair);
        else
        {
            if (Subscriber)
            {
                Part(new Vector3(bx, 0.1f, doorZ), new Vector3(1.1f, 0.04f, 2.1f), towel, false);
                Part(new Vector3(bx, 0.2f, doorZ + 0.05f), new Vector3(0.5f, 0.16f, 0.7f), suit, false);
                Part(new Vector3(bx, 0.18f, doorZ + 0.75f), new Vector3(0.34f, 0.12f, 0.7f), skin, false);
                Part(new Vector3(bx, 0.22f, doorZ - 0.55f), new Vector3(0.26f, 0.24f, 0.26f), skin, false);
                Part(new Vector3(bx, 0.32f, doorZ - 0.58f), new Vector3(0.28f, 0.08f, 0.28f), hair, false);
                var um = NewProp(new Vector3(s * 10.2f, 0f, doorZ), new Vector3(2.2f, 0.3f, 2.2f), BeachColors[(ci + 1) % BeachColors.Length], false, new Vector3(0f, 2.3f, 0f));
                Kid(um, new Vector3(0f, 1.15f, 0f), new Vector3(0.07f, 2.3f, 0.07f), new Color(0.85f, 0.85f, 0.87f));
                Kid(um, new Vector3(0f, 2.3f, 0f), new Vector3(2.2f, 0.1f, 2.2f), BeachColors[(ci + 1) % BeachColors.Length]);
                yardSpot = new Vector3(s * 9.3f, 2.0f, doorZ);
                yardSpotValid = true;
            }
            else
            {
                // an empty towel and a lounger on a quiet stretch
                Part(new Vector3(bx, 0.1f, doorZ), new Vector3(1.1f, 0.04f, 2.1f), towel, false);
                yardSpotValid = false;
            }
        }

        // a sandcastle on the side of the lot away from the door (smashable, no points)
        if (h < 0.7f && beachScene != 1)
        {
            float cz = Mathf.Clamp(doorZ >= 0f ? -half * 0.55f : half * 0.55f, -half + 0.9f, half - 0.9f);
            Color wall = new Color(0.86f, 0.72f, 0.45f), tower = new Color(0.80f, 0.66f, 0.40f);
            var cs = NewProp(new Vector3(s * 5.7f, 0.3f, cz), new Vector3(0.95f, 0.6f, 0.95f), wall);
            Kid(cs, new Vector3(0f, -0.1f, 0f), new Vector3(0.95f, 0.4f, 0.95f), wall);
            Kid(cs, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.35f, 0.45f), tower);
            for (int i = 0; i < 4; i++)
                Kid(cs, new Vector3((i & 1) == 0 ? -0.38f : 0.38f, 0.12f, (i & 2) == 0 ? -0.38f : 0.38f), new Vector3(0.22f, 0.45f, 0.22f), tower);
        }

        // an umbrella with a lounger on the other side, now and then
        if (h > 0.35f && beachScene == 0)
        {
            float uz = Mathf.Clamp(doorZ >= 0f ? -half * 0.45f : half * 0.45f, -half + 1.3f, half - 1.3f);
            Color canopy = BeachColors[(ci + 3) % BeachColors.Length];
            var u2 = NewProp(new Vector3(s * 6.7f, 0f, uz), new Vector3(2.0f, 0.3f, 2.0f), canopy, false, new Vector3(0f, 2.2f, 0f));
            Kid(u2, new Vector3(0f, 1.1f, 0f), new Vector3(0.07f, 2.2f, 0.07f), new Color(0.85f, 0.85f, 0.87f));
            Kid(u2, new Vector3(0f, 2.2f, 0f), new Vector3(2.0f, 0.1f, 2.0f), canopy);
            // the lounger under it now has someone lying on it
            Lounger(s * 7.9f, uz, BeachColors[(ci + 1) % BeachColors.Length], BeachColors[(ci + 5) % BeachColors.Length], skin, hair, true);
        }

        // a small lifeguard tower on one plain lot in six
        if (h > 0.84f && beachScene == 0)
        {
            float lz = Mathf.Clamp(doorZ >= 0f ? -half * 0.4f : half * 0.4f, -half + 1.0f, half - 1.0f);
            Color post = new Color(0.55f, 0.40f, 0.28f);
            for (int i = 0; i < 4; i++)
                Part(new Vector3(s * (10.6f + ((i & 1) == 0 ? -0.5f : 0.5f)), 0.8f, lz + ((i & 2) == 0 ? -0.5f : 0.5f)), new Vector3(0.12f, 1.6f, 0.12f), post, false);
            Part(new Vector3(s * 10.6f, 1.75f, lz), new Vector3(1.4f, 0.1f, 1.4f), post, false);
            Part(new Vector3(s * 10.6f, 2.35f, lz), new Vector3(1.2f, 1.1f, 1.2f), new Color(0.93f, 0.93f, 0.90f), false);
            Part(new Vector3(s * 10.6f, 3.0f, lz), new Vector3(1.6f, 0.12f, 1.6f), new Color(0.20f, 0.55f, 0.70f), false);
        }

        // palm trees: one near the road side of the sand on most lots, one near the sea on some scenery lots
        float ph = Hash01(transform.position.z * 7.331f + (side > 0 ? 5.1f : 44.4f));
        float pz = doorZ >= 0f ? -1f : 1f;
        if (ph < 0.70f)
            Palm(s * 5.1f, Mathf.Clamp(pz * half * 0.8f, -half + 1.0f, half - 1.0f), ph * 11f);
        if (!Subscriber && beachScene != 2 && ph > 0.55f && ph < 0.90f && !(h > 0.84f && beachScene == 0))
            Palm(s * 11.0f, Mathf.Clamp(pz * half * 0.7f, -half + 1.0f, half - 1.0f), ph * 7f);
        if (BeachGreen > 0.05f) BeachGreenery(half, BeachGreen, 999f, 0f, false);
    }
    

    // Which scene a beach lot gets: 0 = sunbather, 1 = picnic blanket, 2 = lifeguard hut, 3 = changing cabins (scenery only), 4 = recliner with a sunbather (scenery only), 5 = dune lot at an edge of a beach (set by the spawner).
    int PickBeachScene(bool customer)
    {
        float sh = Hash01(transform.position.z * 5.913f + (side > 0 ? 21.7f : 77.1f));
        int scene;
        if (customer) scene = sh < 0.50f ? 1 : (sh < 0.80f ? 2 : 0);                          // picnic 50%, lifeguard hut 30%, sunbather 20%
        else scene = sh < 0.20f ? 1 : (sh < 0.26f ? 2 : (sh < 0.34f ? 3 : (sh < 0.48f ? 4 : 0)));   // scenery: picnic 20%, hut 6%, cabins 8%, recliner 14%, the rest plain
        // the big scenes are spaced out along each side of the road
        if (scene == 2 || scene == 3)
        {
            int i = side > 0 ? 0 : 1;
            float z = transform.position.z;
            float[] last = scene == 2 ? lastHutZ : lastCabinZ;
            float gap = scene == 2 ? HutSpacing : CabinSpacing;
            if (z >= last[i] && z - last[i] < gap) scene = customer ? 1 : 0;
            else last[i] = z;
        }
        return scene;
    }
    static readonly float[] lastCabinZ = { -9999f, -9999f };
    static readonly float[] lastHutZ = { -9999f, -9999f };
    const float CabinSpacing = 90f;
    const float HutSpacing = 60f;

    // ---- a picnic: a big checked blanket (the target, doorstep points), a basket and cooler, and friends under an umbrella behind ----
    void LayoutPicnic(float L, float doorZ, int ci, Color skin, Color hair)
    {
        float s = side;
        float half = L * 0.5f;
        float bz = Mathf.Clamp(doorZ, -half + 1.5f, half - 1.5f);
        float bx = s * 7.0f;
        const float bw = 2.6f;
        Color[] cols = { new Color(0.40f, 0.68f, 0.88f), new Color(0.98f, 0.86f, 0.30f), new Color(0.55f, 0.80f, 0.60f), new Color(0.62f, 0.50f, 0.85f) };
        Color blanket = cols[ci % cols.Length];
        Color stripe = new Color(0.95f, 0.95f, 0.92f);

        if (Subscriber)
        {
            // the doormat becomes the blanket: it takes the status colour (green, then gold), the stripes sit on top
            SetZone(doorstepZone, new Vector3(bx, 0.45f, bz), new Vector3(bw, 0.9f, bw));
            if (matRenderer != null) matRenderer.transform.localScale = new Vector3(bw, 0.08f, bw);
            doorstepZone.gameObject.SetActive(true);
        }
        else Part(new Vector3(bx, 0.12f, bz), new Vector3(bw, 0.08f, bw), blanket, false);
        for (int i = -1; i <= 1; i++)
        {
            Part(new Vector3(bx + s * i * 0.8f, 0.17f, bz), new Vector3(0.16f, 0.02f, bw), stripe, false);
            Part(new Vector3(bx, 0.172f, bz + i * 0.8f), new Vector3(bw, 0.02f, 0.16f), stripe, false);
        }

        // on the blanket: a basket with a handle, a cooler, two cups
        Color wicker = new Color(0.72f, 0.55f, 0.32f);
        Part(new Vector3(bx - s * 0.55f, 0.34f, bz - 0.65f), new Vector3(0.5f, 0.28f, 0.38f), wicker, false);
        Part(new Vector3(bx - s * 0.55f, 0.56f, bz - 0.65f), new Vector3(0.06f, 0.16f, 0.4f), wicker, false);
        Part(new Vector3(bx + s * 0.6f, 0.34f, bz + 0.7f), new Vector3(0.55f, 0.32f, 0.38f), new Color(0.25f, 0.62f, 0.68f), false);
        Part(new Vector3(bx + s * 0.6f, 0.52f, bz + 0.7f), new Vector3(0.58f, 0.06f, 0.4f), new Color(0.93f, 0.93f, 0.90f), false);
        Part(new Vector3(bx + s * 0.1f, 0.25f, bz + 0.15f), new Vector3(0.11f, 0.13f, 0.11f), new Color(0.98f, 0.86f, 0.30f), false);
        Part(new Vector3(bx - s * 0.2f, 0.25f, bz - 0.25f), new Vector3(0.11f, 0.13f, 0.11f), new Color(0.93f, 0.93f, 0.90f), false);

        // two people sitting just behind the blanket, in the shade of a smashable umbrella (not part of the target)
        for (int k = 0; k < 2; k++)
        {
            float pz = bz + (k == 0 ? -0.9f : 0.9f);
            Color shirt = BeachColors[(ci + 1 + k * 2) % BeachColors.Length];
            Part(new Vector3(s * 9.15f, 0.12f, pz), new Vector3(1.0f, 0.03f, 0.8f), BeachColors[(ci + k * 3) % BeachColors.Length], false);
            Part(new Vector3(s * 9.35f, 0.45f, pz), new Vector3(0.36f, 0.5f, 0.34f), shirt, false);
            Part(new Vector3(s * 8.85f, 0.2f, pz), new Vector3(0.7f, 0.14f, 0.28f), new Color(0.35f, 0.40f, 0.55f), false);
            Part(new Vector3(s * 9.35f, 0.82f, pz), new Vector3(0.27f, 0.27f, 0.27f), skin, false);
            Part(new Vector3(s * 9.37f, 0.97f, pz), new Vector3(0.29f, 0.08f, 0.29f), hair, false);
        }
        Color canopy = BeachColors[(ci + 4) % BeachColors.Length];
        var um = NewProp(new Vector3(s * 9.9f, 0f, bz), new Vector3(2.2f, 0.3f, 2.2f), canopy, false, new Vector3(0f, 2.3f, 0f));
        Kid(um, new Vector3(0f, 1.15f, 0f), new Vector3(0.07f, 2.3f, 0.07f), new Color(0.85f, 0.85f, 0.87f));
        Kid(um, new Vector3(0f, 2.3f, 0f), new Vector3(2.2f, 0.1f, 2.2f), canopy);

        if (Subscriber) { tGlowPos = new Vector3(bx, 0.2f, bz); tGlowSize = new Vector2(bw, bw); }
        yardSpot = new Vector3(bx, 1.8f, bz);
        yardSpotValid = true;
    }

    // ---- lifeguard hut: a white hut with a canopy and a yellow sign on a raised wooden platform, with a railing and stairs.
    // The platform's open front part is the target (a paper landing on it is caught). The lifeguard stands there and
    // jumps and waves when you deliver. ----
    Transform lgFx, lgPad, lgArm, lgBody;
    Renderer lgPadR;
    float lgAnimT = -1f;
    float lgSide = 1f;

    void LayoutLifeguard(float L, float doorZ, int ci, Color skin)
    {
        float s = side;
        float half = L * 0.5f;
        float cz = Mathf.Clamp(doorZ, -half + 1.5f, half - 1.5f);
        float cx = s * 8.7f;
        float st = cz < 0f ? 1f : -1f;                       // the stairs go up on the side with more room
        const float deck = 1.925f;                            // top of the platform
        Color wood = new Color(0.62f, 0.45f, 0.30f), woodDark = new Color(0.45f, 0.32f, 0.22f);
        Color white = new Color(0.93f, 0.93f, 0.90f), rail = new Color(0.70f, 0.72f, 0.76f);
        Color yellow = new Color(0.98f, 0.82f, 0.12f);

        // platform and legs
        Part(new Vector3(cx, 1.85f, cz), new Vector3(2.8f, 0.15f, 2.6f), wood, true);
        for (int i = 0; i < 4; i++)
            Part(new Vector3(cx + s * ((i & 1) == 0 ? -1.25f : 1.25f), 0.925f, cz + ((i & 2) == 0 ? -1.15f : 1.15f)), new Vector3(0.16f, 1.85f, 0.16f), woodDark, true);
        Part(new Vector3(cx - s * 1.25f, 0.9f, cz), new Vector3(0.1f, 0.1f, 2.3f), woodDark, false);
        Part(new Vector3(cx - s * 1.25f, 0.95f, cz), new Vector3(0.07f, 0.07f, 2.94f), woodDark, false, new Vector3(-35.3f, 0f, 0f));
        Part(new Vector3(cx - s * 1.25f, 0.95f, cz), new Vector3(0.07f, 0.07f, 2.94f), woodDark, false, new Vector3(35.3f, 0f, 0f));
        Part(new Vector3(cx, 0.9f, cz - 1.15f), new Vector3(2.5f, 0.1f, 0.1f), woodDark, false);
        Part(new Vector3(cx, 0.9f, cz + 1.15f), new Vector3(2.5f, 0.1f, 0.1f), woodDark, false);

        // the hut: white walls, a window and a yellow sign on the road side, a canopy that overhangs the front
        float hx = cx + s * 0.75f;
        Part(new Vector3(hx, deck + 0.95f, cz), new Vector3(1.2f, 1.9f, 2.3f), white, true);
        Part(new Vector3(cx + s * 0.14f, deck + 1.0f, cz), new Vector3(0.02f, 0.8f, 1.5f), new Color(0.35f, 0.50f, 0.60f), false);
        Part(new Vector3(cx + s * 0.1f, deck + 1.65f, cz), new Vector3(0.06f, 0.38f, 2.0f), yellow, false);
        Part(new Vector3(cx + s * 0.07f, deck + 1.65f, cz), new Vector3(0.07f, 0.12f, 1.3f), new Color(0.15f, 0.20f, 0.35f), false);
        Part(new Vector3(cx + s * 0.6f, deck + 2.05f, cz), new Vector3(1.9f, 0.16f, 2.9f), white, false);   // (not solid: a high arc must not bounce off it)

        // railing round the open part (stairs side left open), a rescue buoy on the front rail
        float rx = cx - s * 1.38f;
        Part(new Vector3(rx, deck + 0.95f, cz), new Vector3(0.06f, 0.06f, 2.6f), rail, false);
        Part(new Vector3(rx, deck + 0.5f, cz), new Vector3(0.05f, 0.05f, 2.6f), rail, false);
        for (int k = -2; k <= 2; k++) Part(new Vector3(rx, deck + 0.5f, cz + k * 0.62f), new Vector3(0.06f, 1.0f, 0.06f), rail, false);
        Part(new Vector3(cx - s * 0.65f, deck + 0.95f, cz - st * 1.28f), new Vector3(1.5f, 0.06f, 0.06f), rail, false);   // the side without stairs
        Part(new Vector3(cx - s * 0.65f, deck + 0.5f, cz - st * 1.28f), new Vector3(1.5f, 0.05f, 0.05f), rail, false);
        Part(new Vector3(rx - s * 0.04f, deck + 0.62f, cz - st * 0.55f), new Vector3(0.1f, 0.45f, 0.4f), yellow, false);

        // stairs up the stairs side: seven blocky steps, with a handrail
        for (int i = 0; i < 7; i++)
        {
            float top = 0.27f * (i + 1);
            Part(new Vector3(cx + s * (-1.5f + 0.45f * i), top * 0.5f, cz + st * 1.65f), new Vector3(0.45f, top, 0.7f), wood, true);
        }
        Part(new Vector3(cx - s * 0.15f, 1.98f, cz + st * 2.0f), new Vector3(3.15f, 0.05f, 0.05f), rail, false, new Vector3(0f, 0f, 31f * s));

        // a flag on the sea side
        Part(new Vector3(cx + s * 1.3f, deck + 2.7f, cz + 0.9f), new Vector3(0.06f, 1.8f, 0.06f), white, false);
        Part(new Vector3(cx + s * 1.3f, deck + 3.4f, cz + 1.2f), new Vector3(0.04f, 0.35f, 0.6f), new Color(0.20f, 0.45f, 0.85f), false);
        Blob(new Vector3(cx, 0f, cz), 3.6f, 3.6f, 0.3f);

        // the lifeguard and the pad (built once): the pad shows the target
        if (lgFx == null)
        {
            lgFx = new GameObject("LifeguardFx").transform;
            lgFx.SetParent(transform, false);
            lgPad = Prim.Make(PrimitiveType.Cube, "Pad", lgFx, Vector3.zero, new Vector3(1.5f, 0.04f, 2.5f), Color.white).transform;
            lgPadR = lgPad.GetComponent<Renderer>();
            lgBody = new GameObject("Guard").transform;
            lgBody.SetParent(lgFx, false);
            Prim.Make(PrimitiveType.Cube, "LegL", lgBody, new Vector3(0f, 0.3f, -0.09f), new Vector3(0.14f, 0.6f, 0.15f), new Color(0.15f, 0.25f, 0.50f));
            Prim.Make(PrimitiveType.Cube, "LegR", lgBody, new Vector3(0f, 0.3f, 0.09f), new Vector3(0.14f, 0.6f, 0.15f), new Color(0.15f, 0.25f, 0.50f));
            Prim.Make(PrimitiveType.Cube, "Torso", lgBody, new Vector3(0f, 0.88f, 0f), new Vector3(0.24f, 0.56f, 0.42f), new Color(0.98f, 0.82f, 0.12f));
            Prim.Make(PrimitiveType.Cube, "ArmL", lgBody, new Vector3(0f, 0.85f, -0.3f), new Vector3(0.1f, 0.5f, 0.1f), new Color(0.88f, 0.68f, 0.52f));
            lgArm = new GameObject("ArmPivot").transform;
            lgArm.SetParent(lgBody, false);
            lgArm.localPosition = new Vector3(0f, 1.1f, 0.3f);
            Prim.Make(PrimitiveType.Cube, "ArmR", lgArm, new Vector3(0f, -0.25f, 0f), new Vector3(0.1f, 0.5f, 0.1f), new Color(0.88f, 0.68f, 0.52f));
            Prim.Make(PrimitiveType.Sphere, "Head", lgBody, new Vector3(0f, 1.33f, 0f), new Vector3(0.27f, 0.27f, 0.27f), new Color(0.88f, 0.68f, 0.52f));
            Prim.Make(PrimitiveType.Cube, "Hair", lgBody, new Vector3(0f, 1.46f, 0f), new Vector3(0.29f, 0.08f, 0.29f), new Color(0.25f, 0.16f, 0.10f));
        }
        lgSide = s;
        lgFx.localPosition = new Vector3(cx, deck, cz);
        lgFx.gameObject.SetActive(true);
        lgPad.localPosition = new Vector3(-s * 0.62f, 0.03f, 0f);
        lgPad.gameObject.SetActive(Subscriber);
        lgBody.localPosition = new Vector3(-s * 0.2f, 0f, 1.0f * st);
        lgBody.localEulerAngles = new Vector3(0f, s > 0f ? 180f : 0f, 0f);   // faces the road
        SetGuardPose(0f, 0f);

        if (Subscriber)
        {
            if (doorstepZone != null) doorstepZone.gameObject.SetActive(false);   // no doormat on a platform
            hoopZone.kind = HouseTarget.Kind.Lifeguard;
            // a generous catch area: the whole open deck, out over the front rail, and down past the deck's front edge,
            // so a paper that would clip the edge or land on the deck is still caught while it is coming down
            // x: from 0.5 m beyond the front edge to the sea-side edge (the deck spans -1.4 to +1.4 round cx); z: the whole deck width
            hoopZone.openingHalf = 1.65f;
            hoopZone.openingHalfZ = 1.45f;
            SetZone(hoopZone, new Vector3(cx - s * 0.25f, deck + 0.5f, cz), new Vector3(3.3f, 1.8f, 2.9f));
            hoopSnapT.localPosition = new Vector3(cx - s * 0.25f, deck + 0.12f, cz);
            hoopZone.gameObject.SetActive(true);
            tGlowPos = new Vector3(cx - s * 0.62f, deck + 0.08f, cz); tGlowSize = new Vector2(1.5f, 2.5f);
        }
        yardSpot = new Vector3(cx, deck + 3.0f, cz);
        yardSpotValid = true;
    }

    // armLift: 0 = arm down, 1 = arm up and waving. hop: height of the jump.
    void SetGuardPose(float armLift, float hop)
    {
        if (lgFx == null) return;
        lgBody.localPosition = new Vector3(lgBody.localPosition.x, hop, lgBody.localPosition.z);
        lgArm.localEulerAngles = new Vector3(armLift * 160f, 0f, 0f);
    }

    // After a delivery: the lifeguard hops and waves for a moment.
    void UpdateLifeguard()
    {
        lgAnimT += Time.deltaTime;
        float t = lgAnimT;
        float lift = Mathf.Clamp01(t / 0.2f) * (t < 1.7f ? 1f : Mathf.Clamp01(1f - (t - 1.7f) / 0.2f));
        float wave = lift > 0.99f ? Mathf.Sin(t * 14f) * 0.12f : 0f;
        float hop = t < 1.2f ? Mathf.Abs(Mathf.Sin(t * 8f)) * 0.12f : 0f;
        SetGuardPose(lift + wave, hop);
        if (t > 2f) { lgAnimT = -1f; SetGuardPose(0f, 0f); }
    }

    // ---- the shoreline: the sea's edge wanders in and out as a smooth wave measured along the world z, so neighbouring lots line up ----
    static float ShoreX(float wz)
    {
        float w = 0.65f * Mathf.Sin(wz * 0.42f) + 0.35f * Mathf.Sin(wz * 1.07f + 1.3f);   // -1..1
        return 11.6f + 1.5f * (0.5f + 0.5f * w);                                             // 11.6 .. 13.1 m from the road
    }

    // One slice (width along the road) of shore: sand from fromX out to sandX, the sea starting at seaX with a wet band on the sand,
    // a pale shallows band and a lip of foam at the edge.
    void ShoreSlice(float zc, float width, float fromX, float sandX, float seaX)
    {
        float s = side;
        Color sand = GameConfig.SandColor;
        Color wet = Color.Lerp(sand, new Color(0.45f, 0.38f, 0.28f), 0.35f);
        Color shallow = new Color(0.42f, 0.80f, 0.88f), deep = new Color(0.22f, 0.58f, 0.82f), foam = new Color(0.92f, 0.96f, 0.98f);
        if (sandX > fromX + 0.05f)
            Part(new Vector3(s * (fromX + sandX) * 0.5f, 0.045f, zc), new Vector3(sandX - fromX, 0.07f, width), sand, false);
        if (sandX >= seaX - 0.1f)
            Part(new Vector3(s * (seaX - 0.35f), 0.05f, zc), new Vector3(0.7f, 0.065f, width), wet, false);
        Part(new Vector3(s * (seaX + 235f), 0.03f, zc), new Vector3(470f, 0.06f, width), deep, false);          // the sea runs on to the horizon
        Part(new Vector3(s * (seaX + 1.4f), 0.036f, zc), new Vector3(2.8f, 0.06f, width), shallow, false);
        Part(new Vector3(s * (seaX + 0.15f), 0.07f, zc), new Vector3(0.45f, 0.05f, width), foam, false);
    }

    // On a dune lot, with zz measured along the lot (b = which end the houses are at): where the sea starts and where the sand stops.
    float DuneSeaX(float zz, float cover, float b)
    {
        float t = Mathf.Clamp01((zz * b + cover * 0.5f) / cover);
        return Mathf.Lerp(ShoreX(transform.position.z + zz), 17.5f, Mathf.SmoothStep(0f, 1f, t));
    }

    float DuneSandX(float zz, float cover, float b)
    {
        float t = Mathf.Clamp01((zz * b + cover * 0.5f) / cover);
        float coast = ShoreX(transform.position.z + zz);
        return Mathf.Min(DuneSeaX(zz, cover, b), Mathf.Lerp(coast, 8.2f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.15f))));
    }

    // ---- beach vegetation: low shrubs and grass clumps, thicker towards the ends of a beach ----
    void Shrub(float bx, float bz, float seed, float size)
    {
        Color g1 = new Color(0.30f, 0.56f, 0.30f), g2 = new Color(0.40f, 0.66f, 0.34f), g3 = new Color(0.48f, 0.70f, 0.38f);
        float r1 = Hash01(seed * 1.3f);
        Part(new Vector3(bx, size * 0.3f, bz), new Vector3(size, size * 0.6f, size * 0.9f), g1, false, new Vector3(0f, 40f * r1, 0f));
        Part(new Vector3(bx + size * 0.15f, size * 0.58f, bz - size * 0.1f), new Vector3(size * 0.7f, size * 0.5f, size * 0.7f), g2, false, new Vector3(0f, 15f + 30f * r1, 0f));
        Part(new Vector3(bx - size * 0.2f, size * 0.42f, bz + size * 0.2f), new Vector3(size * 0.5f, size * 0.4f, size * 0.55f), g3, false, new Vector3(0f, 70f * r1, 0f));
    }

    void GrassClump(float bx, float bz, float seed)
    {
        Color g = Color.Lerp(new Color(0.40f, 0.62f, 0.30f), new Color(0.58f, 0.72f, 0.34f), Hash01(seed));
        for (int bl = 0; bl < 4; bl++)
            Part(new Vector3(bx + (bl - 1.5f) * 0.09f, 0.22f, bz + ((bl & 1) == 0 ? -0.06f : 0.06f)), new Vector3(0.06f, 0.44f, 0.06f), g, false, new Vector3((bl - 1.5f) * 12f, bl * 40f, (bl - 1.5f) * 10f));
    }

    // e = 0..1, how close this lot is to an end of the beach. Shrubs and clumps go on the sand next to the road and by the shore, with an extra palm near the ends.
    void BeachGreenery(float half, float e, float avoidZ = 999f, float avoidHalf = 0f, bool withShrubs = true)
    {
        float s = side;
        float hs = transform.position.z * 2.73f + (side > 0 ? 3.3f : 17.9f);
        int shrubs = withShrubs ? Mathf.RoundToInt(e * 4f) : 0;   // the green boxes belong on the grassy dune lots at the ends, not scattered over the open beach
        int clumps = 2 + Mathf.RoundToInt(e * 8f);
        for (int i = 0; i < shrubs; i++)
        {
            float x = (i & 1) == 0 ? 5.3f + Hash01(hs + i * 1.9f) * 1.1f : 10.1f + Hash01(hs + i * 2.3f) * 0.9f;
            float z = (Hash01(hs + i * 3.7f) - 0.5f) * (half * 2f - 1.6f);
            if (x > 9.3f && Mathf.Abs(z - avoidZ) < avoidHalf) continue;
            Shrub(s * x, z, hs + i, 0.8f + Hash01(hs + i * 5.1f) * 0.6f);
        }
        for (int i = 0; i < clumps; i++)
        {
            float x = (i % 3 == 0) ? 10.0f + Hash01(hs + i * 4.1f) * 1.2f : 5.2f + Hash01(hs + i * 4.1f) * 1.6f;
            float z = (Hash01(hs + i * 6.3f) - 0.5f) * (half * 2f - 1.0f);
            if (x > 9.3f && Mathf.Abs(z - avoidZ) < avoidHalf) continue;
            GrassClump(s * x, z, hs + i * 2f);
        }
        if (e > 0.3f && Hash01(hs * 1.7f) < e)
            Palm(s * 6.7f, Mathf.Clamp((Hash01(hs * 2.1f) - 0.5f) * half * 1.6f, -half + 1.0f, half - 1.0f), Hash01(hs * 3.9f) * 9f);
    }

    // ---- a dune lot: the first and last lot of a beach. The sand narrows towards the neighbouring houses, the sea steps away from the road
    // (grass and dune tufts between), and a rocky headland closes the water at the houses' end. Scenery only. ----
    void LayoutDune(float L)
    {
        float s = side;
        float half = L * 0.5f;
        float b = BeachEdge == 1 ? -1f : 1f;                 // the houses are at -z (start of a beach) or +z (end)
        float cover = L + GameConfig.LotGap + 0.1f;
        float third = cover / 3f;
        Color sand = GameConfig.SandColor;
        Color sandDark = Color.Lerp(sand, new Color(0.70f, 0.60f, 0.40f), 0.3f);
        Color water = new Color(0.22f, 0.58f, 0.82f), foam = new Color(0.92f, 0.96f, 0.98f);
        Color grassTuft = new Color(0.40f, 0.62f, 0.30f), rock = new Color(0.55f, 0.55f, 0.58f), rockDark = new Color(0.46f, 0.46f, 0.50f);

        // sand next to the road over the whole lot, then three slices from the beach side to the houses' side: the sand and sea step back
        lawnT.localPosition = new Vector3(s * (4.4f + 8.2f) * 0.5f, 0.04f, 0f);
        lawnT.localScale = new Vector3(8.2f - 4.4f, 0.08f, cover);
        lawnT.gameObject.SetActive(true);
        if (padRenderer != null) padRenderer.material.color = sand;
        // from the beach's end of the lot to the houses' end the shore wanders out from the sand's edge, the sand narrows and grass fills the gap, all in small slices
        int shN = Mathf.Max(3, Mathf.RoundToInt(cover / 2.2f));
        float shW = cover / shN;
        for (int shI = 0; shI < shN; shI++)
        {
            float shZ = -cover * 0.5f + shW * (shI + 0.5f);
            ShoreSlice(shZ, shW, 8.2f, DuneSandX(shZ, cover, b), DuneSeaX(shZ, cover, b));
        }

        // two sandy dunes (stacked, turned slabs)
        float dz0 = -b * third * 0.9f;
        Part(new Vector3(s * 9.8f, 0.2f, dz0), new Vector3(3.2f, 0.4f, 2.4f), sandDark, false, new Vector3(0f, 20f, 0f));
        Part(new Vector3(s * 9.8f, 0.5f, dz0), new Vector3(2.0f, 0.35f, 1.5f), sandDark, false, new Vector3(0f, 35f, 0f));
        Part(new Vector3(s * 12.8f, 0.22f, -b * third * 0.25f), new Vector3(3.6f, 0.44f, 2.6f), sandDark, false, new Vector3(0f, -15f, 0f));
        Part(new Vector3(s * 12.8f, 0.55f, -b * third * 0.25f), new Vector3(2.2f, 0.4f, 1.6f), sandDark, false, new Vector3(0f, 10f, 0f));

        // the beach house sits on the houses' side of the lot (below), so the tufts keep out of its footprint
        float depth = Mathf.Min(3.0f, L - 1.2f);
        float hzc = b * (half - depth * 0.5f - 0.15f);
        const float hx = 11.4f;

        // dune grass tufts in the grass between the sand and the sea
        float hs = transform.position.z * 3.31f + (side > 0 ? 1.7f : 8.9f);
        for (int t = 0; t < 7; t++)
        {
            float tx = 8.8f + Hash01(hs + t * 1.37f) * 4.8f;
            float tz = (Hash01(hs + t * 2.91f) - 0.5f) * (L - 1.2f);
            if (Mathf.Abs(tx - hx) < 2.6f && Mathf.Abs(tz - hzc) < depth * 0.5f + 0.9f) continue;
            for (int bl = 0; bl < 3; bl++)
                Part(new Vector3(s * (tx + (bl - 1) * 0.1f), 0.25f, tz + (bl - 1) * 0.08f), new Vector3(0.06f, 0.5f, 0.06f), grassTuft, false, new Vector3((bl - 1) * 14f, 0f, (bl - 1) * 12f));
        }

        // a beach house on stilts at the houses' end: pastel walls, a sloping roof, a deck with steps and a surfboard, on a patch of sand
        {
            Color[] pastel =
            {
                new Color(0.55f, 0.85f, 0.82f), new Color(0.98f, 0.93f, 0.64f), new Color(0.96f, 0.95f, 0.92f),
                new Color(0.70f, 0.90f, 0.78f), new Color(0.62f, 0.82f, 0.95f),
            };
            int pi = (int)(Hash01(hs * 4.1f) * 997f) % pastel.Length;
            Color wall = pastel[pi];
            Color roofC = pi == 0 || pi == 4 ? new Color(0.94f, 0.94f, 0.92f) : new Color(0.18f, 0.42f, 0.48f);
            Color wood = new Color(0.72f, 0.58f, 0.40f), woodDark = new Color(0.45f, 0.32f, 0.22f), glass = new Color(0.70f, 0.88f, 0.96f);
            float w = 3.0f, dk = 1.0f;       // wall width (x) and the deck height

            Part(new Vector3(s * hx, 0.05f, hzc), new Vector3(5.4f, 0.06f, depth + 1.6f), sand, false);                      // sand under it
            for (int px = -1; px <= 1; px += 2)
                for (int pz = -1; pz <= 1; pz += 2)
                    Part(new Vector3(s * (hx + px * (w * 0.5f + 0.2f)), dk * 0.5f, hzc + pz * (depth * 0.5f - 0.1f)), new Vector3(0.18f, dk, 0.18f), woodDark, true);   // stilts
            Part(new Vector3(s * hx, dk + 0.06f, hzc), new Vector3(w + 0.9f, 0.12f, depth), wood, true);                      // deck
            Part(new Vector3(s * hx, dk + 0.12f + 0.85f, hzc), new Vector3(w, 1.7f, depth - 0.6f), wall, true);               // walls
            Part(new Vector3(s * hx, dk + 0.12f + 1.8f, hzc), new Vector3(w + 0.8f, 0.16f, depth + 0.3f), roofC, false, new Vector3(0f, 0f, s * 7f));   // roof
            Part(new Vector3(s * (hx - w * 0.5f - 0.02f), dk + 0.12f + 0.6f, hzc - b * 0.3f), new Vector3(0.06f, 1.1f, 0.75f), woodDark, false);   // door (road side)
            Part(new Vector3(s * (hx - w * 0.5f - 0.02f), dk + 0.12f + 1.1f, hzc + b * 0.7f), new Vector3(0.06f, 0.55f, 0.6f), glass, false);     // window
            for (int st = 0; st < 3; st++)                                                                                       // steps down to the sand
                Part(new Vector3(s * (hx - w * 0.5f - 0.75f - 0.32f * st), dk - 0.28f * (st + 1) + 0.12f, hzc - b * 0.3f), new Vector3(0.34f, 0.1f, 0.9f), wood, false);
            Color board = pi == 1 ? new Color(0.20f, 0.75f, 0.78f) : new Color(0.98f, 0.88f, 0.30f);
            Part(new Vector3(s * (hx - w * 0.5f - 0.3f), dk + 0.12f + 0.85f, hzc + b * 1.0f), new Vector3(0.1f, 1.7f, 0.4f), board, false, new Vector3(0f, 0f, s * 6f));   // a surfboard against the wall
        }

        // rocks along the shore and one standing in the sea, then a rocky headland at the houses' end that closes the water
        Part(new Vector3(s * 14.2f, 0.2f, b * third * 0.05f), new Vector3(0.8f, 0.4f, 0.7f), rock, true, new Vector3(0f, 30f, 5f));
        Part(new Vector3(s * 14.6f, 0.25f, -b * third * 0.45f), new Vector3(0.6f, 0.5f, 0.6f), rockDark, true, new Vector3(0f, -10f, 8f));
        Part(new Vector3(s * 16.0f, 0.3f, -b * third * 0.9f), new Vector3(1.1f, 0.6f, 0.9f), rock, true, new Vector3(0f, 55f, 6f));
        Part(new Vector3(s * 19.5f, 0.35f, -b * half * 0.3f), new Vector3(1.4f, 0.7f, 1.2f), rockDark, true, new Vector3(0f, -35f, 4f));

        // a few rocks on the sand edge, and a rocky headland at the houses' end that closes the water
        Part(new Vector3(s * 8.5f, 0.25f, b * (half - 1.6f)), new Vector3(0.7f, 0.5f, 0.6f), rock, true, new Vector3(0f, 25f, 6f));
        Part(new Vector3(s * 9.2f, 0.2f, -b * (half - 2.2f)), new Vector3(0.5f, 0.4f, 0.5f), rockDark, true, new Vector3(0f, -20f, 4f));
        float hz = b * (half - 0.9f);
        Part(new Vector3(s * 16.2f, 0.7f, hz), new Vector3(2.2f, 1.4f, 2.0f), rock, false, new Vector3(0f, 18f, 5f));
        Part(new Vector3(s * 17.6f, 1.0f, hz - b * 0.6f), new Vector3(2.0f, 2.0f, 1.8f), rockDark, false, new Vector3(0f, -25f, -6f));
        Part(new Vector3(s * 15.2f, 0.45f, hz - b * 1.1f), new Vector3(1.3f, 0.9f, 1.2f), rockDark, false, new Vector3(0f, 40f, 8f));
        Part(new Vector3(s * 18.4f, 1.6f, hz + b * 0.2f), new Vector3(1.2f, 1.2f, 1.1f), rock, false, new Vector3(0f, 10f, 4f));

        // vegetation thickens towards the houses: shrubs and clumps in the grass between the sand and the sea, and more beside the road
        BeachGreenery(half, 1f, hzc, depth * 0.5f + 0.9f);
        for (int gi = 0; gi < 6; gi++)
        {
            float gz = (Hash01(hs * 1.1f + gi * 3.3f) - 0.5f) * (L - 1.0f);
            float lo = DuneSandX(gz, cover, b) + 0.5f, hi = DuneSeaX(gz, cover, b) - 1.1f;
            if (hi - lo < 0.8f) continue;
            float gx = Mathf.Lerp(lo, hi, Hash01(hs * 1.7f + gi * 5.9f));
            if (Mathf.Abs(gx - hx) < 2.7f && Mathf.Abs(gz - hzc) < depth * 0.5f + 1.0f) continue;
            if (gi % 2 == 0) Shrub(s * gx, gz, hs + gi, 0.9f + Hash01(hs + gi * 7.7f) * 0.7f);
            else GrassClump(s * gx, gz, hs + gi * 3f);
        }
        Palm(s * 8.9f, Mathf.Clamp(hzc - b * (depth * 0.5f + 0.9f), -half + 1.0f, half - 1.0f), Hash01(hs * 5.3f) * 9f);

        // palms: one at the road edge near the houses, one further in
        Palm(s * 5.1f, Mathf.Clamp(b * (half - 1.3f), -half + 1.0f, half - 1.0f), Hash01(hs * 1.9f) * 9f);
        Palm(s * 10.6f, Mathf.Clamp(-b * (half * 0.5f), -half + 1.0f, half - 1.0f), Hash01(hs * 2.9f) * 9f);
        yardSpotValid = false;
    }

    // ---- a palm tree: a slightly leaning trunk of tapering rings, a crown of drooping fronds, a few coconuts. The trunk and fronds are solid (a paper bounces off them). ----
    void Palm(float bx, float bz, float seed)
    {
        float s = side;
        float k = Hash01(seed * 3.7f + bz);
        float lean = (0.6f + 0.5f * k) * s;                    // total sideways lean at the top, towards the sea
        float lz = (Hash01(seed * 5.3f) - 0.5f) * 0.5f;        // and a little along the road
        float height = 4.0f + 0.8f * Hash01(seed * 1.9f);
        const int rings = 5;
        Color trunkA = new Color(0.58f, 0.45f, 0.32f), trunkB = new Color(0.50f, 0.38f, 0.27f);
        Vector3 top = new Vector3(bx, 0f, bz);
        for (int i = 0; i < rings; i++)
        {
            float t0 = (float)i / rings, t1 = (float)(i + 1) / rings;
            // a gentle curve: the lean grows faster towards the top
            Vector3 p0 = new Vector3(bx + lean * t0 * t0, t0 * height, bz + lz * t0 * t0);
            Vector3 p1 = new Vector3(bx + lean * t1 * t1, t1 * height, bz + lz * t1 * t1);
            Vector3 dir = p1 - p0;
            float tiltZ = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;     // leans towards +x
            float tiltX = Mathf.Atan2(dir.z, dir.y) * Mathf.Rad2Deg;     // leans towards +z
            float w = Mathf.Lerp(0.36f, 0.22f, t0);
            Part((p0 + p1) * 0.5f, new Vector3(w, dir.magnitude * 1.04f, w), (i & 1) == 0 ? trunkA : trunkB, true, new Vector3(tiltX, 0f, -tiltZ));
            top = p1;
        }
        // fronds: eight round the crown, each a rising piece and a drooping piece
        Color leafA = new Color(0.25f, 0.60f, 0.28f), leafB = new Color(0.31f, 0.69f, 0.31f);
        float yaw0 = Hash01(seed * 2.3f) * 45f;
        for (int f = 0; f < 8; f++)
        {
            float phi = (yaw0 + f * 45f) * Mathf.Deg2Rad;
            Vector3 dirH = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
            float len1 = 1.0f, len2 = 1.0f, e1 = 18f * Mathf.Deg2Rad, e2 = -38f * Mathf.Deg2Rad;
            Vector3 c1 = top + dirH * (len1 * 0.5f * Mathf.Cos(e1)) + Vector3.up * (len1 * 0.5f * Mathf.Sin(e1));
            Vector3 end1 = top + dirH * (len1 * Mathf.Cos(e1)) + Vector3.up * (len1 * Mathf.Sin(e1));
            Vector3 c2 = end1 + dirH * (len2 * 0.5f * Mathf.Cos(e2)) + Vector3.up * (len2 * 0.5f * Mathf.Sin(e2));
            float yaw = -phi * Mathf.Rad2Deg;
            Color leaf = (f & 1) == 0 ? leafA : leafB;
            Part(c1, new Vector3(len1, 0.05f, 0.5f), leaf, true, new Vector3(0f, yaw, e1 * Mathf.Rad2Deg));
            Part(c2, new Vector3(len2, 0.05f, 0.34f), leaf, true, new Vector3(0f, yaw, e2 * Mathf.Rad2Deg));
        }
        // a flat hub where the fronds meet (palms are scenery only: the old palm-shot scoring zone is switched off)
        Part(top + Vector3.up * 0.04f, new Vector3(0.9f, 0.14f, 0.9f), leafA, true);
        // a few coconuts under the crown
        Color nut = new Color(0.40f, 0.28f, 0.18f);
        for (int c = 0; c < 3; c++)
        {
            float a = (c * 120f + 20f) * Mathf.Deg2Rad;
            Part(top + new Vector3(Mathf.Cos(a) * 0.2f, -0.15f, Mathf.Sin(a) * 0.2f), new Vector3(0.2f, 0.2f, 0.2f), nut, false, new Vector3(20f, c * 40f, 15f));
        }
        Blob(new Vector3(bx + lean * 0.3f, 0f, bz), 2.6f, 2.6f, 0.22f);
    }

    // ---- a recliner (scenery only): two sunbathers lying on loungers that face the sea, an umbrella behind their heads, a towel at their feet ----
    void LayoutRecliner(float L, float doorZ, int ci, Color skin, Color hair)
    {
        float s = side;
        float half = L * 0.5f;
        float rz = Mathf.Clamp(doorZ, -half + 1.4f, half - 1.4f);
        float x = s * 7.9f;
        Color suit = BeachColors[(ci + 3) % BeachColors.Length];

        // a second lounger beside the first when there is room (towards the middle of the lot)
        float dz = rz > 0f ? -1.35f : 1.35f;
        bool two = Mathf.Abs(rz + dz) <= half - 0.9f;
        float z1 = two ? rz - dz * 0.5f : rz;
        float mz = two ? z1 + dz * 0.5f : z1;
        Lounger(x, z1, BeachColors[ci], suit, skin, hair, true);
        if (two) Lounger(x, z1 + dz, BeachColors[(ci + 1) % BeachColors.Length], BeachColors[(ci + 5) % BeachColors.Length], new Color(0.70f, 0.50f, 0.36f), new Color(0.12f, 0.09f, 0.07f), true);

        // flip-flops at the foot end and a towel on the sand beyond them
        Color flop = new Color(0.25f, 0.62f, 0.68f);
        Part(new Vector3(x + s * 1.1f, 0.07f, z1 - 0.12f), new Vector3(0.3f, 0.04f, 0.14f), flop, false);
        Part(new Vector3(x + s * 1.1f, 0.07f, z1 + 0.12f), new Vector3(0.3f, 0.04f, 0.14f), flop, false);
        Color towel = BeachColors[(ci + 2) % BeachColors.Length];
        float tw = two ? 2.4f : 1.3f;
        Part(new Vector3(x + s * 1.75f, 0.1f, mz), new Vector3(0.9f, 0.03f, tw), towel, false);
        Part(new Vector3(x + s * 1.75f, 0.115f, mz), new Vector3(0.12f, 0.02f, tw), new Color(0.95f, 0.95f, 0.92f), false);

        // a little table with a drink at the head end, between the loungers
        Part(new Vector3(x - s * 0.3f, 0.2f, two ? mz : z1 + 0.65f), new Vector3(0.36f, 0.4f, 0.36f), new Color(0.62f, 0.45f, 0.30f), false);
        Part(new Vector3(x - s * 0.3f, 0.47f, two ? mz : z1 + 0.65f), new Vector3(0.11f, 0.14f, 0.11f), new Color(0.98f, 0.86f, 0.30f), false);

        // a smashable umbrella behind their heads
        Color canopy = BeachColors[(ci + 4) % BeachColors.Length];
        var um = NewProp(new Vector3(x - s * 1.2f, 0f, mz), new Vector3(2.4f, 0.3f, 2.4f), canopy, false, new Vector3(0f, 2.2f, 0f));
        Kid(um, new Vector3(0f, 1.1f, 0f), new Vector3(0.07f, 2.2f, 0.07f), new Color(0.85f, 0.85f, 0.87f));
        Kid(um, new Vector3(0f, 2.2f, 0f), new Vector3(2.4f, 0.1f, 2.4f), canopy);

        Blob(new Vector3(x, 0f, mz), 3.2f, 2.6f, 0.25f);
        yardSpot = new Vector3(x, 1.8f, mz);
        yardSpotValid = true;
    }

    // One lounger facing the sea (the sea is the +side direction): the back is raised at the road end, the feet point to the sea.
    // A person lies on it, built from joined pieces: hips and legs on the seat, torso and head on the back.
    void Lounger(float x, float z, Color cushion, Color suit, Color skin, Color hair, bool person)
    {
        float s = side;
        Color frame = new Color(0.93f, 0.93f, 0.90f);
        Vector3 tilt = new Vector3(0f, 0f, -30f * s);     // raises the road end
        for (int i = 0; i < 4; i++)
            Part(new Vector3(x + s * ((i & 1) == 0 ? 0.8f : -0.1f), 0.15f, z + ((i & 2) == 0 ? -0.3f : 0.3f)), new Vector3(0.05f, 0.3f, 0.05f), frame, false);
        Part(new Vector3(x + s * 0.35f, 0.32f, z), new Vector3(1.0f, 0.08f, 0.7f), cushion, false);
        Part(new Vector3(x - s * 0.538f, 0.538f, z), new Vector3(0.85f, 0.08f, 0.7f), cushion, false, tilt);
        if (!person) return;

        // The body, built from the lounger's own geometry so every piece touches the next. w runs from the hinge (the seat/back joint) towards
        // the sea (+), y is up. "OnBack(along, off)" is a point on the raised back: along it from the hinge, and off it (out of the cushion).
        // The cushion's top surface at the hinge is at y = 0.36; the back rises at 30 degrees towards the road.
        const float hingeW = -0.15f, topY = 0.36f, c30 = 0.866f, s30 = 0.5f;
        System.Func<float, float, Vector2> onBack = (along, off) => new Vector2(hingeW - c30 * along + s30 * off, topY + s30 * along + c30 * off);
        System.Func<float, float, float, Vector3> at = (w, y, zz) => new Vector3(x + s * w, y, zz);

        // hips and legs lie flat on the seat
        Part(at(-0.02f, topY + 0.08f, z), new Vector3(0.32f, 0.16f, 0.38f), suit, false);                              // swimsuit bottoms
        Part(at(0.45f, topY + 0.06f, z - 0.1f), new Vector3(0.62f, 0.12f, 0.15f), skin, false);                       // legs
        Part(at(0.45f, topY + 0.06f, z + 0.1f), new Vector3(0.62f, 0.12f, 0.15f), skin, false);
        Part(at(0.27f, topY + 0.07f, z - 0.1f), new Vector3(0.24f, 0.14f, 0.16f), suit, false);                       // shorts over the thighs
        Part(at(0.27f, topY + 0.07f, z + 0.1f), new Vector3(0.24f, 0.14f, 0.16f), suit, false);
        Part(at(0.8f, topY + 0.035f, z - 0.1f), new Vector3(0.14f, 0.07f, 0.15f), skin, false);                       // feet
        Part(at(0.8f, topY + 0.035f, z + 0.1f), new Vector3(0.14f, 0.07f, 0.15f), skin, false);

        // torso, shoulders and head follow the back's slope
        Vector2 p;
        p = onBack(0.28f, 0.09f); Part(at(p.x, p.y, z), new Vector3(0.56f, 0.18f, 0.34f), skin, false, tilt);          // torso
        p = onBack(0.40f, 0.11f); Part(at(p.x, p.y, z), new Vector3(0.20f, 0.20f, 0.36f), suit, false, tilt);          // swimsuit top
        p = onBack(0.55f, 0.08f); Part(at(p.x, p.y, z), new Vector3(0.14f, 0.16f, 0.50f), skin, false, tilt);          // shoulders
        p = onBack(0.33f, 0.07f);
        Part(at(p.x, p.y, z - 0.25f), new Vector3(0.52f, 0.10f, 0.10f), skin, false, tilt);                            // arms resting at the sides
        Part(at(p.x, p.y, z + 0.25f), new Vector3(0.52f, 0.10f, 0.10f), skin, false, tilt);
        p = onBack(0.72f, 0.14f); Part(at(p.x, p.y, z), new Vector3(0.26f, 0.26f, 0.26f), skin, false, tilt);          // head
        p = onBack(0.85f, 0.15f); Part(at(p.x, p.y, z), new Vector3(0.08f, 0.24f, 0.28f), hair, false, tilt);          // hair behind the head
        p = onBack(0.72f, 0.28f); Part(at(p.x, p.y, z), new Vector3(0.07f, 0.03f, 0.24f), new Color(0.12f, 0.12f, 0.15f), false, tilt);   // sunglasses
    }

    // ---- changing cabins: a short row of open-roof cabins at the back of the beach. Scenery only. ----
    const float CabinW = 1.8f, CabinH = 2.4f, CabinWall = 0.12f;

    void LayoutCabins(float L, float doorZ, int ci)
    {
        float s = side;
        float half = L * 0.5f;
        float cz = Mathf.Clamp(doorZ, -half + 1.1f, half - 1.1f);
        float cx = s * 7.3f;
        Color[] cols = { new Color(0.55f, 0.80f, 0.88f), new Color(0.65f, 0.88f, 0.75f), new Color(0.98f, 0.90f, 0.55f), new Color(0.80f, 0.72f, 0.92f) };
        CabinShell(cx, cz, cols[ci % cols.Length], BeachColors[(ci + 1) % BeachColors.Length]);
        Blob(new Vector3(cx, 0f, cz), 2.5f, 2.5f, 0.28f);
        // at most one neighbour, on whichever side has room
        int first = Hash01(transform.position.z * 1.913f) < 0.5f ? -1 : 1;
        for (int j = 0; j < 2; j++)
        {
            int k = j == 0 ? first : -first;
            float nz = cz + k * 2.1f;
            if (Mathf.Abs(nz) <= half - 1.0f)
            {
                CabinShell(cx, nz, cols[(ci + 2 + (k > 0 ? 1 : 0)) % cols.Length], BeachColors[(ci + 3 + (k > 0 ? 1 : 0)) % BeachColors.Length]);
                Blob(new Vector3(cx, 0f, nz), 2.5f, 2.5f, 0.28f);
                break;
            }
        }
        yardSpotValid = false;
    }

    // One cabin: four solid walls, a floor, trim round the open top, a doorway with a curtain on the road side.
    void CabinShell(float cx, float cz, Color body, Color curtainCol)
    {
        float s = side;
        float hw = CabinW * 0.5f;
        Color trim = new Color(0.95f, 0.95f, 0.92f);
        Part(new Vector3(cx + s * (hw - CabinWall * 0.5f), CabinH * 0.5f, cz), new Vector3(CabinWall, CabinH, CabinW), body, true);
        Part(new Vector3(cx - s * (hw - CabinWall * 0.5f), CabinH * 0.5f, cz), new Vector3(CabinWall, CabinH, CabinW), body, true);
        Part(new Vector3(cx, CabinH * 0.5f, cz + hw - CabinWall * 0.5f), new Vector3(CabinW, CabinH, CabinWall), body, true);
        Part(new Vector3(cx, CabinH * 0.5f, cz - hw + CabinWall * 0.5f), new Vector3(CabinW, CabinH, CabinWall), body, true);
        Part(new Vector3(cx, 0.1f, cz), new Vector3(CabinW - 0.2f, 0.04f, CabinW - 0.2f), new Color(0.72f, 0.58f, 0.40f), false);
        float ty = CabinH + 0.04f;
        Part(new Vector3(cx + s * hw, ty, cz), new Vector3(0.2f, 0.08f, CabinW + 0.1f), trim, false);
        Part(new Vector3(cx - s * hw, ty, cz), new Vector3(0.2f, 0.08f, CabinW + 0.1f), trim, false);
        Part(new Vector3(cx, ty, cz + hw), new Vector3(CabinW + 0.1f, 0.08f, 0.2f), trim, false);
        Part(new Vector3(cx, ty, cz - hw), new Vector3(CabinW + 0.1f, 0.08f, 0.2f), trim, false);
        Part(new Vector3(cx - s * (hw + 0.005f), 1.05f, cz), new Vector3(0.02f, 1.9f, 1.0f), new Color(0.12f, 0.12f, 0.15f), false);
        Part(new Vector3(cx - s * (hw + 0.02f), 2.12f, cz), new Vector3(0.05f, 0.14f, 1.2f), trim, false);
        Part(new Vector3(cx - s * (hw + 0.02f), 1.05f, cz + 0.53f), new Vector3(0.05f, 1.95f, 0.07f), trim, false);
        Part(new Vector3(cx - s * (hw + 0.02f), 1.05f, cz - 0.53f), new Vector3(0.05f, 1.95f, 0.07f), trim, false);
        Part(new Vector3(cx - s * (hw + 0.03f), 1.05f, cz), new Vector3(0.03f, 1.9f, 1.0f), curtainCol, false);
    }

    // ---- the shimmer on a beach customer's target: the blanket (or the hut's pad) pulses between the pad colour and white,
    // and an unlit ripple spreads out from the target. Stops once the customer is served. ----
    Transform tGlow;
    Material tGlowMat;
    Vector3 tGlowPos;
    Vector2 tGlowSize;

    void AnimateBeachTarget()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f + phase);
        Color c = Color.Lerp(GameConfig.PadColor, Color.white, 0.10f + 0.35f * pulse);
        if (beachScene == 1 && matRenderer != null) matRenderer.material.color = c;
        if (beachScene == 2 && lgPadR != null) lgPadR.material.color = c;

        if (tGlowSize.x <= 0f || !NightLights.Available) return;
        if (tGlow == null)
        {
            var q = BlobShadow.Create(transform);
            q.name = "TargetGlow";
            tGlowMat = new Material(NightLights.Bulb);
            q.GetComponent<Renderer>().sharedMaterial = tGlowMat;
            tGlow = q.transform;
        }
        float p = Mathf.Repeat(Time.time / 1.4f + phase * 0.159f, 1f);   // a ripple every 1.4 s
        float grow = 1f + 0.3f * p;
        tGlow.localPosition = tGlowPos;
        tGlow.localScale = new Vector3(tGlowSize.x * grow, tGlowSize.y * grow, 1f);
        Color g = Color.Lerp(GameConfig.PadColor, Color.white, 0.5f);
        tGlowMat.color = new Color(g.r, g.g, g.b, (1f - p) * 0.6f);
        tGlow.gameObject.SetActive(true);
    }

    void ResetBeachFx()
    {
        tGlowSize = Vector2.zero;
        lgAnimT = -1f;
        if (lgFx != null) lgFx.gameObject.SetActive(false);
    }
}
