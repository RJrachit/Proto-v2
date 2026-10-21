using System.Collections.Generic;
using UnityEngine;

// House (partial) - Roof, trim, plinth and the house shell.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- roofs, trim and soft shadows ----

    Transform roofGable;                // the pitched roof (built the first time a house needs it)
    Renderer roofGableR;
    static Mesh gableMesh;
    static readonly Color[] RoofColors =
    {
        new Color(0.32f, 0.36f, 0.46f), new Color(0.28f, 0.29f, 0.33f), new Color(0.25f, 0.42f, 0.45f),
        new Color(0.46f, 0.40f, 0.38f), new Color(0.40f, 0.33f, 0.45f), new Color(0.25f, 0.36f, 0.30f),
        new Color(0.20f, 0.27f, 0.33f), new Color(0.20f, 0.27f, 0.33f), new Color(0.16f, 0.30f, 0.32f),   // dark slate, like the reference buildings
    };

    // A unit hipped roof: x from -0.5 to 0.5 (ridge in the middle), y from 0 to 1 (ridge height), z from -0.5 to 0.5 (along the road). The ridge stops
    // short of both ends (HipInset of the length) and the ends slope down too. Convex, so it works as a solid collider.
    // Every face has its own vertices, so it is shaded flat (the low-poly look).
    const float HipInset = 0.22f;
    static Mesh GableMesh()
    {
        if (gableMesh != null) return gableMesh;
        var v = new List<Vector3>();
        var tri = new List<int>();
        Vector3 a = new Vector3(-0.5f, 0f, -0.5f), b = new Vector3(0.5f, 0f, -0.5f);
        Vector3 c = new Vector3(0.5f, 0f, 0.5f), d = new Vector3(-0.5f, 0f, 0.5f);
        Vector3 r0 = new Vector3(0f, 1f, -0.5f + HipInset), r1 = new Vector3(0f, 1f, 0.5f - HipInset);
        AddTri(v, tri, a, d, r1); AddTri(v, tri, a, r1, r0);     // slope on the -x side
        AddTri(v, tri, c, b, r0); AddTri(v, tri, c, r0, r1);     // slope on the +x side
        AddTri(v, tri, d, c, r1);                                // hip at +z
        AddTri(v, tri, b, a, r0);                                // hip at -z
        gableMesh = new Mesh();
        gableMesh.name = "HippedRoof";
        gableMesh.SetVertices(v);
        gableMesh.SetTriangles(tri, 0);
        gableMesh.RecalculateNormals();
        gableMesh.RecalculateBounds();
        return gableMesh;
    }

    static void AddTri(List<Vector3> v, List<int> tri, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        int i = v.Count;
        v.Add(p0); v.Add(p1); v.Add(p2);
        tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
    }

    // Gable roof on top of the walls instead of the flat slab. Two or three colours per street, no reds or oranges.
    void LayoutRoof(float L)
    {
        // shops keep a flat roof, and so do most houses (a parapet, rooftop props and signs: see House.Facade.cs); only a share of houses get a gable
        bool on = GameConfig.PitchedRoofs && !isPark && !isBeach && !bare && shopKind == 0
                  && Hash01(ShopKey * 0.317f + (side > 0 ? 2.9f : 61.3f)) < GameConfig.PitchedRoofShare;
        if (!on)
        {
            if (roofGable != null) roofGable.gameObject.SetActive(false);
            return;
        }
        if (roofGable == null)
        {
            var go = new GameObject("RoofGable");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = GableMesh();
            var mc = go.AddComponent<MeshCollider>();   // solid: papers bounce off the pitched roof and slide down it
            mc.sharedMesh = GableMesh();
            mc.convex = true;
            go.AddComponent<BankSurface>();
            var r = go.AddComponent<MeshRenderer>();
            Prim.FixMaterial(r);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            roofGable = go.transform;
            roofGableR = r;
        }
        float h = Mathf.Repeat(Mathf.Sin(transform.position.z * 23.17f + (side > 0 ? 5.9f : 71.3f)) * 43758.5453f, 1f);
        roofGableR.material.color = RoofColors[(int)(h * 997f) % RoofColors.Length];
        roofGable.localPosition = new Vector3(side * 11.5f, BodyH, 0f);   // (see FloorBase)
        roofGable.localScale = new Vector3(7.0f, 1.5f, L + 0.2f);
        roofGable.gameObject.SetActive(true);
        SetActiveSafe(roofT, false);   // the flat slab is not needed under it
    }

    // A cornice under the roof, a darker plinth at the foot of the wall (split around the door), a soft shadow along the wall,
    // and a chimney on some pitched roofs.
    void LayoutHouseTrim(float L, float doorZ, Color body, bool park)
    {
        if (park) return;
        float s = side;
        float half = L * 0.5f;

        Blob(new Vector3(s * 8.05f, 0f, 0f), 1.0f, L + 0.6f, 0.24f);

        float topY = BodyH;
        if (!GameConfig.FacadeDetail) Part(new Vector3(s * 8.42f, topY - 0.12f, 0f), new Vector3(0.18f, 0.22f, L + 0.1f), Color.Lerp(body, Color.white, 0.6f), false);   // (the facade has its own, deeper cornice)
        if (floors >= 3) Part(new Vector3(s * 11.5f, FloorBase(2) + 0.05f, 0f), new Vector3(6.2f, 0.2f, L + 0.2f), new Color(0.55f, 0.50f, 0.50f), false);   // the band between the first and second floor

        Color plinth = new Color(body.r * 0.62f, body.g * 0.62f, body.b * 0.62f);
        PlinthRun(s, -half + 0.05f, doorZ - 0.75f, plinth);
        PlinthRun(s, doorZ + 0.75f, half - 0.05f, plinth);

        if (GameConfig.PitchedRoofs && roofGable != null && roofGable.gameObject.activeSelf && GameConfig.ChimneyChance > 0f)
        {
            float wz = transform.position.z;
            float h = Mathf.Repeat(Mathf.Sin(wz * 61.31f + (side > 0 ? 2.1f : 33.7f)) * 43758.5453f, 1f);
            if (h < GameConfig.ChimneyChance)
            {
                float h2 = Mathf.Repeat(Mathf.Sin(wz * 12.77f + 9.4f) * 43758.5453f, 1f);
                float ridgeRun = Mathf.Max(0.3f, 0.28f * (L + 0.2f) - 0.5f);   // the ridge of the hipped roof is shorter than the house
                float cz = Mathf.Lerp(-ridgeRun, ridgeRun, h2);
                Part(new Vector3(s * 10.1f, topY + 1.55f, cz), new Vector3(0.7f, 1.6f, 0.7f), new Color(0.62f, 0.60f, 0.58f), true);
                Part(new Vector3(s * 10.1f, topY + 2.4f, cz), new Vector3(0.9f, 0.12f, 0.9f), new Color(0.30f, 0.30f, 0.34f), true);
            }
        }
    }

    void PlinthRun(float s, float z0, float z1, Color col)
    {
        float len = z1 - z0;
        if (len < 0.3f) return;
        Part(new Vector3(s * 8.45f, 0.25f, (z0 + z1) * 0.5f), new Vector3(0.12f, 0.5f, len), col, false);
    }

    // soft contact shadows on the ground (see BlobShadow)
    readonly List<Transform> blobs = new List<Transform>();
    int blobN;
}
