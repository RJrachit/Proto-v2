using System.Collections.Generic;
using UnityEngine;

// Low-poly stylised road vehicles, built at runtime from tapered blocks and eight-sided wheels (no art files):
//   Sedan  a long bonnet and boot with a tinted glass cabin and a body-coloured roof
//   Suv    taller and boxier, black cladding and bumpers, big wheels, a white roof
//   Police a two-tone sedan (black ends, white middle and roof) with a light bar and a badge
//   Truck  a box truck: a cab with a big dark windscreen and a long ribbed cargo box
// Each (type, colour) is ONE mesh with the shared facade palette material, shared by every car of that kind. The vehicle is solid: a few box
// colliders with BankSurface, so papers bounce off it like they did off the old box cars. Local axes: +z is the front, +x the right, y up.
public class LowPolyCar
{
    public const int Sedan = 0, Suv = 1, Police = 2, Truck = 3;

    // No reds or oranges (those are the delivery markers' colours).
    public static readonly Color[] Colors =
    {
        new Color(0.35f, 0.58f, 0.82f), new Color(0.15f, 0.55f, 0.55f), new Color(0.15f, 0.30f, 0.60f), new Color(0.90f, 0.90f, 0.93f),
        new Color(0.28f, 0.30f, 0.35f), new Color(0.92f, 0.78f, 0.22f), new Color(0.68f, 0.50f, 0.42f), new Color(0.55f, 0.76f, 0.92f),
        new Color(0.26f, 0.56f, 0.36f),
    };

    static readonly float[] Lengths = { 3.8f, 4.0f, 3.8f, 6.2f };
    static readonly float[] Tops = { 1.4f, 1.9f, 1.75f, 2.7f };
    static readonly float[] Widths = { 1.7f, 1.85f, 1.7f, 2.1f };
    public static float Length(int type) { return Lengths[type]; }
    public static float Width(int type) { return Widths[type]; }
    public static float Top(int type) { return Tops[type]; }

    // A random vehicle: a truck only where the yard is long enough for one.
    public static int RandomType(bool allowTruck)
    {
        if (allowTruck && Random.value < 0.3f) return Truck;
        float r = Random.value;
        return r < 0.5f ? Sedan : (r < 0.88f ? Suv : Police);
    }

    static readonly Color Glass = new Color(0.10f, 0.13f, 0.18f);
    static readonly Color Tyre = new Color(0.10f, 0.10f, 0.12f);
    static readonly Color Rim = new Color(0.62f, 0.63f, 0.68f);
    static readonly Color Dark = new Color(0.16f, 0.17f, 0.20f);
    static readonly Color Bumper = new Color(0.30f, 0.31f, 0.35f);
    static readonly Color Lamp = new Color(1.00f, 0.96f, 0.72f);
    static readonly Color TailLamp = new Color(0.90f, 0.20f, 0.38f);

    // ---- meshes ----

    static readonly Dictionary<int, Mesh> meshes = new Dictionary<int, Mesh>();
    static readonly FacadeBuilder cb = new FacadeBuilder();   // (shared: one mesh is built at a time)

    // (Unity-aware null check: a mesh destroyed when Play mode ended is rebuilt.)
    static Mesh MeshFor(int type, int colour)
    {
        int key = type == Police ? type * 100 : (type == Truck ? type * 100 + colour % 2 : type * 100 + colour);
        Mesh m;
        if (meshes.TryGetValue(key, out m) && m != null) return m;
        cb.Clear();
        cb.muted = false;
        Color body = Colors[colour % Colors.Length];
        switch (type)
        {
            case Suv: BuildSuv(cb, body); break;
            case Police: BuildPolice(cb); break;
            case Truck: BuildTruck(cb, colour); break;
            default: BuildSedan(cb, body); break;
        }
        m = cb.Upload(null);
        m.name = "LowPolyCar" + key;
        meshes[key] = m;
        return m;
    }

    static void Wheel(FacadeBuilder b, float x, float y, float z, float r, float w, int side)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, side > 0 ? -90f : 90f);   // the flat face looks outwards
        b.Cyl(new Vector3(x, y, z), r, r, w, 8, Tyre, rot);
        b.Cyl(new Vector3(x + side * (w * 0.5f + 0.006f), y, z), r * 0.6f, r * 0.6f, 0.02f, 8, Rim, rot);
    }

    static void Lights(FacadeBuilder b, float w, float zf, float zb, float y, float lw)
    {
        b.Box(new Vector3(-w * 0.32f, y, zf), new Vector3(lw, 0.15f, 0.07f), Lamp);
        b.Box(new Vector3(w * 0.32f, y, zf), new Vector3(lw, 0.15f, 0.07f), Lamp);
        b.Box(new Vector3(-w * 0.34f, y, zb), new Vector3(lw, 0.15f, 0.07f), TailLamp);
        b.Box(new Vector3(w * 0.34f, y, zb), new Vector3(lw, 0.15f, 0.07f), TailLamp);
    }

    static void Mirrors(FacadeBuilder b, float x, float y, float z, Color c)
    {
        b.Box(new Vector3(-x, y, z), new Vector3(0.13f, 0.11f, 0.2f), c);
        b.Box(new Vector3(x, y, z), new Vector3(0.13f, 0.11f, 0.2f), c);
    }

    // The sedan body, in three lengthwise pieces so the police car can paint them differently.
    static void SedanBody(FacadeBuilder b, Color rear, Color mid, Color front)
    {
        b.Frustum(0.28f, 0.80f, -1.90f, -0.80f, -1.82f, -0.80f, 1.70f, 1.62f, 0f, rear);
        b.Frustum(0.28f, 0.80f, -0.80f, 0.55f, -0.80f, 0.55f, 1.70f, 1.62f, 0f, mid);
        b.Frustum(0.28f, 0.80f, 0.55f, 1.90f, 0.55f, 1.78f, 1.70f, 1.62f, 0f, front);
        b.Frustum(0.80f, 0.90f, 0.55f, 1.78f, 0.62f, 1.50f, 1.62f, 1.50f, 0f, front);     // the bonnet slopes down to the nose
        b.Frustum(0.80f, 0.90f, -1.82f, -0.80f, -1.66f, -0.80f, 1.62f, 1.50f, 0f, rear);  // the boot lid
    }

    static void SedanTop(FacadeBuilder b, Color roof)
    {
        b.Frustum(0.80f, 1.30f, -0.95f, 0.60f, -0.60f, 0.15f, 1.60f, 1.38f, 0f, Glass);   // the glass cabin, the windscreen and rear window slanted
        b.Frustum(1.30f, 1.38f, -0.62f, 0.17f, -0.60f, 0.15f, 1.40f, 1.34f, 0f, roof);
    }

    static void SedanFittings(FacadeBuilder b, Color body)
    {
        b.Box(new Vector3(0f, 0.38f, 1.93f), new Vector3(1.70f, 0.22f, 0.12f), Bumper);
        b.Box(new Vector3(0f, 0.38f, -1.93f), new Vector3(1.70f, 0.22f, 0.12f), Bumper);
        b.Box(new Vector3(0f, 0.60f, 1.90f), new Vector3(0.44f, 0.16f, 0.06f), Dark);       // the grille
        Lights(b, 1.70f, 1.91f, -1.91f, 0.62f, 0.34f);
        Mirrors(b, 0.90f, 0.98f, 0.38f, body);
        for (int i = -1; i <= 1; i += 2)
        {
            Wheel(b, i * 0.78f, 0.34f, 1.15f, 0.34f, 0.24f, i);
            Wheel(b, i * 0.78f, 0.34f, -1.15f, 0.34f, 0.24f, i);
            b.Box(new Vector3(i * 0.87f, 0.58f, 0.10f), new Vector3(0.02f, 0.03f, 0.3f), Dark);   // door handles
        }
    }

    static void BuildSedan(FacadeBuilder b, Color body)
    {
        SedanBody(b, body, body, body);
        SedanTop(b, body);
        SedanFittings(b, body);
    }

    static void BuildPolice(FacadeBuilder b)
    {
        Color black = new Color(0.12f, 0.12f, 0.15f), white = new Color(0.93f, 0.93f, 0.95f);
        SedanBody(b, black, white, black);
        SedanTop(b, white);
        SedanFittings(b, white);
        // the light bar on the roof: blue, white, blue (no reds: those are the markers' colour)
        b.Box(new Vector3(0f, 1.43f, -0.2f), new Vector3(0.9f, 0.04f, 0.2f), Bumper);
        b.Box(new Vector3(-0.30f, 1.50f, -0.2f), new Vector3(0.30f, 0.12f, 0.2f), new Color(0.20f, 0.35f, 0.95f));
        b.Box(new Vector3(0f, 1.50f, -0.2f), new Vector3(0.30f, 0.12f, 0.2f), white);
        b.Box(new Vector3(0.30f, 1.50f, -0.2f), new Vector3(0.30f, 0.12f, 0.2f), new Color(0.20f, 0.35f, 0.95f));
        for (int i = -1; i <= 1; i += 2)   // a gold badge on each door
            b.Box(new Vector3(i * 0.86f, 0.58f, 0.0f), new Vector3(0.02f, 0.2f, 0.2f), new Color(0.95f, 0.80f, 0.20f));
    }

    static void BuildSuv(FacadeBuilder b, Color body)
    {
        Color white = new Color(0.92f, 0.93f, 0.95f);
        b.Frustum(0.38f, 1.05f, -2.00f, 2.00f, -1.95f, 1.90f, 1.85f, 1.80f, 0f, body);                // the lower body
        b.Frustum(1.05f, 1.15f, 0.75f, 1.90f, 0.85f, 1.62f, 1.80f, 1.66f, 0f, body);                  // the bonnet
        b.Frustum(1.05f, 1.78f, -1.78f, 0.80f, -1.66f, 0.20f, 1.80f, 1.60f, 0f, Glass);               // the glass cabin
        b.Frustum(1.78f, 1.88f, -1.72f, 0.24f, -1.68f, 0.22f, 1.66f, 1.62f, 0f, white);               // the roof
        b.Box(new Vector3(0f, 0.38f, 0f), new Vector3(1.90f, 0.24f, 4.0f), Dark);                     // black cladding along the sills
        b.Box(new Vector3(0f, 0.52f, 2.00f), new Vector3(1.88f, 0.32f, 0.14f), Dark);                 // bumpers
        b.Box(new Vector3(0f, 0.52f, -2.04f), new Vector3(1.88f, 0.32f, 0.14f), Dark);
        b.Box(new Vector3(0f, 0.86f, 1.95f), new Vector3(1.20f, 0.30f, 0.07f), Dark);                 // the big grille
        for (int i = -1; i <= 1; i += 2)
        {
            b.Box(new Vector3(i * 0.76f, 0.90f, 1.97f), new Vector3(0.30f, 0.20f, 0.07f), Lamp);
            b.Box(new Vector3(i * 0.74f, 0.92f, -1.98f), new Vector3(0.28f, 0.34f, 0.07f), TailLamp);
            Wheel(b, i * 0.84f, 0.46f, 1.25f, 0.46f, 0.30f, i);
            Wheel(b, i * 0.84f, 0.46f, -1.25f, 0.46f, 0.30f, i);
            b.Box(new Vector3(i * 0.88f, 0.68f, 1.25f), new Vector3(0.12f, 0.12f, 1.0f), Dark);      // wheel arches
            b.Box(new Vector3(i * 0.88f, 0.68f, -1.25f), new Vector3(0.12f, 0.12f, 1.0f), Dark);
            b.Box(new Vector3(i * 0.93f, 1.10f, 0.52f), new Vector3(0.14f, 0.12f, 0.2f), Dark);      // mirrors
        }
    }

    static void BuildTruck(FacadeBuilder b, int colour)
    {
        Color cab = colour % 2 == 0 ? new Color(0.92f, 0.93f, 0.95f) : new Color(0.92f, 0.78f, 0.22f);
        Color cargo = new Color(0.30f, 0.31f, 0.35f);
        b.Box(new Vector3(0f, 0.48f, 0f), new Vector3(1.9f, 0.26f, 6.2f), Dark);                       // the chassis
        b.Box(new Vector3(0f, 1.65f, -1.05f), new Vector3(2.10f, 2.10f, 4.1f), cargo);               // the cargo box
        b.Box(new Vector3(0f, 2.74f, -1.05f), new Vector3(2.2f, 0.08f, 4.2f), new Color(0.22f, 0.23f, 0.26f));   // its roof rim
        for (int k = 0; k < 9; k++)   // ribs along the sides
        {
            float z = -2.85f + k * 0.45f;
            b.Box(new Vector3(-1.07f, 1.65f, z), new Vector3(0.05f, 1.8f, 0.14f), new Color(0.24f, 0.25f, 0.28f));
            b.Box(new Vector3(1.07f, 1.65f, z), new Vector3(0.05f, 1.8f, 0.14f), new Color(0.24f, 0.25f, 0.28f));
        }
        b.Frustum(0.60f, 1.30f, 1.00f, 3.10f, 1.00f, 3.05f, 2.10f, 2.05f, 0f, cab);                   // the cab, lower
        b.Frustum(1.30f, 2.10f, 1.10f, 3.00f, 1.20f, 2.55f, 2.05f, 1.92f, 0f, Glass);                 // windscreen and windows, slanted
        b.Frustum(2.10f, 2.20f, 1.15f, 2.60f, 1.20f, 2.55f, 2.00f, 1.94f, 0f, cab);                   // the cab roof
        b.Box(new Vector3(0f, 0.52f, 3.18f), new Vector3(2.14f, 0.30f, 0.16f), Dark);                 // bumper
        b.Box(new Vector3(0f, 0.92f, 3.12f), new Vector3(1.0f, 0.12f, 0.06f), Dark);                  // grille slot
        for (int i = -1; i <= 1; i += 2)
        {
            b.Box(new Vector3(i * 0.78f, 0.90f, 3.12f), new Vector3(0.28f, 0.16f, 0.07f), Lamp);
            b.Box(new Vector3(i * 0.96f, 0.82f, -3.12f), new Vector3(0.26f, 0.20f, 0.07f), TailLamp);
            b.Box(new Vector3(i * 1.12f, 1.70f, 2.45f), new Vector3(0.10f, 0.40f, 0.12f), Dark);      // mirrors
            Wheel(b, i * 0.88f, 0.50f, 2.20f, 0.50f, 0.34f, i);
            Wheel(b, i * 0.88f, 0.50f, -1.95f, 0.50f, 0.34f, i);
            Wheel(b, i * 0.88f, 0.50f, -2.60f, 0.50f, 0.34f, i);
        }
    }

    // ---- one vehicle ----

    // Solid parts: centre and size in the vehicle's own space.
    static readonly Vector3[][] BoxCentres =
    {
        new[] { new Vector3(0f, 0.60f, 0f), new Vector3(0f, 1.10f, -0.20f) },
        new[] { new Vector3(0f, 0.75f, 0f), new Vector3(0f, 1.45f, -0.45f) },
        new[] { new Vector3(0f, 0.60f, 0f), new Vector3(0f, 1.10f, -0.20f) },
        new[] { new Vector3(0f, 0.9f, 0f), new Vector3(0f, 1.65f, -1.05f), new Vector3(0f, 1.45f, 2.05f) },
    };
    static readonly Vector3[][] BoxSizes =
    {
        new[] { new Vector3(1.70f, 0.64f, 3.8f), new Vector3(1.45f, 0.55f, 1.6f) },
        new[] { new Vector3(1.85f, 0.85f, 4.0f), new Vector3(1.70f, 0.75f, 2.3f) },
        new[] { new Vector3(1.70f, 0.64f, 3.8f), new Vector3(1.45f, 0.55f, 1.6f) },
        new[] { new Vector3(1.9f, 0.7f, 6.2f), new Vector3(2.1f, 2.1f, 4.1f), new Vector3(2.0f, 1.5f, 2.1f) },
    };

    Transform root;
    MeshFilter mf;
    readonly List<BoxCollider> cols = new List<BoxCollider>();

    public bool Active { get { return root != null && root.gameObject.activeSelf; } }

    public static LowPolyCar Create(Transform parent, string name)
    {
        var c = new LowPolyCar();
        c.root = new GameObject(name).transform;
        c.root.SetParent(parent, false);
        var vis = new GameObject("Body");
        vis.transform.SetParent(c.root, false);
        c.mf = vis.AddComponent<MeshFilter>();
        var mr = vis.AddComponent<MeshRenderer>();
        mr.sharedMaterial = FacadePalette.Mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return c;
    }

    public void Hide() { if (root != null) root.gameObject.SetActive(false); }

    // Puts a vehicle at pos (in the parent's space), facing along z (yaw 0 faces +z, 180 faces -z). It is scaled down to fit when maxLen is shorter.
    public void Place(Vector3 pos, float yaw, int type, int colour, float maxLen)
    {
        float sc = Mathf.Min(1f, maxLen / Lengths[type]);
        root.localPosition = pos;
        root.localEulerAngles = new Vector3(0f, yaw, 0f);
        root.localScale = new Vector3(sc, sc, sc);
        root.gameObject.SetActive(true);
        var vis = mf.transform;
        if (vis.GetComponent<MeshRenderer>().sharedMaterial != FacadePalette.Mat) vis.GetComponent<MeshRenderer>().sharedMaterial = FacadePalette.Mat;
        mf.sharedMesh = MeshFor(type, colour);

        var centres = BoxCentres[type];
        var sizes = BoxSizes[type];
        while (cols.Count < centres.Length)
        {
            var go = new GameObject("Solid" + cols.Count);
            go.transform.SetParent(root, false);
            go.AddComponent<BankSurface>();
            cols.Add(go.AddComponent<BoxCollider>());
        }
        for (int i = 0; i < cols.Count; i++)
        {
            bool on = i < centres.Length;
            cols[i].gameObject.SetActive(on);
            if (!on) continue;
            cols[i].center = centres[i];
            cols[i].size = sizes[i];
        }
    }
}
