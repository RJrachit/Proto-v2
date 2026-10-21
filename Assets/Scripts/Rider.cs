using System.Collections.Generic;
using UnityEngine;

// The newspaper boy on his bicycle, built from primitives and animated by a few lines of arithmetic (no animation clips, no rig).
//   - a proper bike: thin frame tubes, spoked wheels that spin with the speed, cranks and pedals that turn, a saddle, handlebar and a paper satchel
//   - a person SITTING on the saddle: hips on the seat, torso leaning forward, head in a cap, arms down to the grips, feet on the pedals
//   - the legs pedal (a two-bone solver puts each knee in the right place for the pedal's position), the body bobs a little
//   - when a paper is thrown, the arm on the side it flies to winds up behind the head and swings out, then goes back to the bar
// Everything lives in the bike's local space: +z forward, +x right, y up. The wheels' diameter is 0.9.
public class Rider : MonoBehaviour
{
    // ---- shared with the rest of the game ----
    public static Material FrameMat;   // every frame tube shares it, so the bike colour (the shop skins) changes them all at once
    public static bool HasFrame { get { return FrameMat != null; } }
    public static void Tint(Color c) { if (FrameMat != null) FrameMat.color = c; }

    // set by PaperThrower when a paper leaves the hand
    public static float LastThrowTime = -10f;
    public static float LastThrowSide = 1f;   // +1 the paper flew to the right, -1 to the left

    // ---- the body's measurements ----
    const float ThighL = 0.46f, ShinL = 0.46f, UpperArmL = 0.42f, ForeArmL = 0.42f;
    const float CrankR = 0.18f, WheelR = 0.45f;
    static readonly Vector3 BB = new Vector3(0f, 0.42f, -0.05f);          // bottom bracket: where the cranks turn
    static readonly Vector3 HipC = new Vector3(0f, 1.04f, -0.42f);        // on the saddle
    const float HipX = 0.11f, PedalX = 0.17f, ShoulderX = 0.22f;
    static readonly Vector3 ShoulderBase = new Vector3(0f, 1.57f, -0.20f);
    static readonly Vector3 GripBase = new Vector3(0.30f, 1.10f, 0.52f);

    // ---- parts that move ----
    Transform upper;                       // everything above the hips: bobs a little
    Transform wheelF, wheelB;
    readonly Transform[] thigh = new Transform[2], shin = new Transform[2], shoe = new Transform[2];
    readonly Transform[] pedal = new Transform[2], crankArm = new Transform[2];
    readonly Transform[] upperArm = new Transform[2], foreArm = new Transform[2], hand = new Transform[2];   // [0] left, [1] right

    BikeForward bike;
    float speed, wheelDeg, crank;

    readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();

    // Builds the whole bike and rider under `bike`; returns a frame renderer (the game wants one to hold on to).
    public static Renderer Build(Transform bike)
    {
        var go = new GameObject("Rider");
        go.transform.SetParent(bike, false);
        var r = go.AddComponent<Rider>();
        return r.Construct();
    }

    // ---- building ----

    Material MatOf(Color c)
    {
        int key = (Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) | Mathf.RoundToInt(c.b * 255f);
        Material m;
        if (mats.TryGetValue(key, out m) && m != null) return m;
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var tr = tmp.GetComponent<Renderer>();
        Prim.FixMaterial(tr);
        m = new Material(tr.sharedMaterial);
        m.color = c;
        Destroy(tmp);
        mats[key] = m;
        return m;
    }

    Transform Make(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default(Vector3))
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        Destroy(g.GetComponent<Collider>());
        var t = g.transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localEulerAngles = euler;
        t.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return t;
    }

    // A cube stretched from a to b. tx = thickness in the plane, ty = thickness across it (when sideUp, ty is the width along x).
    static void Place(Transform t, Vector3 a, Vector3 b, float tx, float ty, bool sideUp = false)
    {
        Vector3 d = b - a;
        float len = Mathf.Max(0.001f, d.magnitude);
        Vector3 dir = d / len;
        Vector3 up = sideUp || Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        t.localPosition = (a + b) * 0.5f;
        t.localRotation = Quaternion.LookRotation(dir, up);
        t.localScale = new Vector3(tx, ty, len + 0.5f * Mathf.Min(tx, ty));
    }

    Transform Bar(string name, Transform parent, Vector3 a, Vector3 b, float tx, float ty, Material m, bool sideUp = false)
    {
        var t = Make(PrimitiveType.Cube, name, parent, Vector3.zero, Vector3.one, m);
        Place(t, a, b, tx, ty, sideUp);
        return t;
    }

    Transform Wheel(string name, Vector3 pos, Material tire, Material rim, Material spoke)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(transform, false);
        pivot.localPosition = pos;
        Make(PrimitiveType.Cylinder, "Tire", pivot, Vector3.zero, new Vector3(0.9f, 0.035f, 0.9f), tire, new Vector3(0f, 0f, 90f));
        Make(PrimitiveType.Cylinder, "Rim", pivot, Vector3.zero, new Vector3(0.70f, 0.042f, 0.70f), rim, new Vector3(0f, 0f, 90f));
        Make(PrimitiveType.Cube, "SpokeA", pivot, Vector3.zero, new Vector3(0.09f, 0.66f, 0.03f), spoke);
        Make(PrimitiveType.Cube, "SpokeB", pivot, Vector3.zero, new Vector3(0.09f, 0.03f, 0.66f), spoke);
        Make(PrimitiveType.Cube, "SpokeC", pivot, Vector3.zero, new Vector3(0.09f, 0.47f, 0.03f), spoke, new Vector3(45f, 0f, 0f));
        Make(PrimitiveType.Cube, "SpokeD", pivot, Vector3.zero, new Vector3(0.09f, 0.47f, 0.03f), spoke, new Vector3(-45f, 0f, 0f));
        Make(PrimitiveType.Cylinder, "Hub", pivot, Vector3.zero, new Vector3(0.12f, 0.06f, 0.12f), spoke, new Vector3(0f, 0f, 90f));
        return pivot;
    }

    Renderer Construct()
    {
        LastThrowTime = -10f;
        Color skinC = new Color(0.95f, 0.76f, 0.60f), shirtC = new Color(0.94f, 0.92f, 0.84f), vestC = new Color(0.20f, 0.55f, 0.38f);
        Color pantsC = new Color(0.20f, 0.24f, 0.40f), shoeC = new Color(0.16f, 0.14f, 0.15f), hairC = new Color(0.30f, 0.18f, 0.10f);
        Color darkC = new Color(0.11f, 0.11f, 0.12f), metalC = new Color(0.62f, 0.64f, 0.68f), seatC = new Color(0.30f, 0.20f, 0.13f);
        Color bagC = new Color(0.78f, 0.66f, 0.42f), bagDark = new Color(0.60f, 0.48f, 0.28f), paperC = new Color(0.97f, 0.97f, 0.95f);

        var frameM = new Material(MatOf(Progress.Skins[Progress.SelectedSkin].color));   // an instance of its own, so recolouring it touches only the frame
        frameM.color = Progress.Skins[Progress.SelectedSkin].color;
        FrameMat = frameM;
        Material dark = MatOf(darkC), metal = MatOf(metalC);

        // wheels
        wheelF = Wheel("WheelFront", new Vector3(0f, WheelR, 0.8f), dark, MatOf(new Color(0.78f, 0.80f, 0.84f)), metal);
        wheelB = Wheel("WheelBack", new Vector3(0f, WheelR, -0.8f), dark, MatOf(new Color(0.78f, 0.80f, 0.84f)), metal);

        // frame tubes
        Vector3 head = new Vector3(0f, 0.92f, 0.56f), seat = new Vector3(0f, 0.95f, -0.40f);
        Vector3 hubF = new Vector3(0f, WheelR, 0.8f), hubB = new Vector3(0f, WheelR, -0.8f);
        var top = Bar("Frame", transform, seat + new Vector3(0f, 0f, 0.02f), head, 0.07f, 0.07f, frameM);
        Bar("DownTube", transform, head, BB, 0.08f, 0.08f, frameM);
        Bar("SeatTube", transform, BB, seat, 0.07f, 0.07f, frameM);
        Bar("ChainStay", transform, BB, hubB, 0.05f, 0.05f, frameM);
        Bar("SeatStay", transform, seat, hubB, 0.05f, 0.05f, frameM);
        Bar("Fork", transform, head + new Vector3(0f, 0.04f, 0f), hubF, 0.06f, 0.06f, frameM);

        // saddle, stem, handlebar, grips, bell
        Make(PrimitiveType.Cube, "Saddle", transform, new Vector3(0f, 1.0f, -0.42f), new Vector3(0.17f, 0.07f, 0.34f), MatOf(seatC));
        Bar("SeatPost", transform, seat, new Vector3(0f, 1.0f, -0.40f), 0.04f, 0.04f, metal);
        Bar("Stem", transform, head, new Vector3(0f, 1.10f, 0.52f), 0.05f, 0.05f, metal);
        Make(PrimitiveType.Cylinder, "Bar", transform, new Vector3(0f, 1.10f, 0.52f), new Vector3(0.04f, 0.30f, 0.04f), metal, new Vector3(0f, 0f, 90f));
        Make(PrimitiveType.Cylinder, "GripL", transform, new Vector3(-0.31f, 1.10f, 0.52f), new Vector3(0.06f, 0.06f, 0.06f), dark, new Vector3(0f, 0f, 90f));
        Make(PrimitiveType.Cylinder, "GripR", transform, new Vector3(0.31f, 1.10f, 0.52f), new Vector3(0.06f, 0.06f, 0.06f), dark, new Vector3(0f, 0f, 90f));
        Make(PrimitiveType.Sphere, "Bell", transform, new Vector3(0.10f, 1.16f, 0.52f), new Vector3(0.09f, 0.07f, 0.09f), metal);
        Make(PrimitiveType.Cylinder, "Chainring", transform, new Vector3(0.07f, BB.y, BB.z), new Vector3(0.30f, 0.012f, 0.30f), dark, new Vector3(0f, 0f, 90f));

        // cranks and pedals (placed every frame)
        for (int i = 0; i < 2; i++)
        {
            crankArm[i] = Bar("Crank", transform, BB, BB + Vector3.up * 0.1f, 0.04f, 0.04f, metal);
            pedal[i] = Make(PrimitiveType.Cube, "Pedal", transform, BB, new Vector3(0.10f, 0.03f, 0.08f), dark);
        }

        // the paper satchel on the rack, left side, with rolled papers sticking out
        Make(PrimitiveType.Cube, "Satchel", transform, new Vector3(-0.28f, 0.82f, -0.62f), new Vector3(0.20f, 0.34f, 0.44f), MatOf(bagC));
        Make(PrimitiveType.Cube, "SatchelFlap", transform, new Vector3(-0.28f, 0.99f, -0.62f), new Vector3(0.22f, 0.04f, 0.46f), MatOf(bagDark));
        Make(PrimitiveType.Cylinder, "Roll", transform, new Vector3(-0.31f, 1.07f, -0.72f), new Vector3(0.07f, 0.12f, 0.07f), MatOf(paperC), new Vector3(8f, 0f, 6f));
        Make(PrimitiveType.Cylinder, "Roll", transform, new Vector3(-0.25f, 1.08f, -0.62f), new Vector3(0.07f, 0.13f, 0.07f), MatOf(paperC), new Vector3(0f, 0f, -8f));
        Make(PrimitiveType.Cylinder, "Roll", transform, new Vector3(-0.29f, 1.07f, -0.52f), new Vector3(0.07f, 0.12f, 0.07f), MatOf(paperC), new Vector3(-8f, 0f, 2f));
        Bar("Rack", transform, new Vector3(0f, 0.95f, -0.50f), new Vector3(0f, 0.70f, -0.80f), 0.04f, 0.04f, metal);
        Make(PrimitiveType.Cube, "RackTop", transform, new Vector3(0f, 0.72f, -0.70f), new Vector3(0.46f, 0.03f, 0.34f), metal);

        // ---- the person ----
        Make(PrimitiveType.Cube, "Pelvis", transform, HipC, new Vector3(0.40f, 0.20f, 0.30f), MatOf(pantsC));

        upper = new GameObject("Upper").transform;
        upper.SetParent(transform, false);
        Vector3 neck = new Vector3(0f, 1.62f, -0.22f);
        Bar("Torso", upper, HipC + new Vector3(0f, 0.04f, 0f), neck + new Vector3(0f, -0.06f, 0f), 0.27f, 0.42f, MatOf(shirtC), true);
        Make(PrimitiveType.Cube, "Shoulders", upper, new Vector3(0f, 1.58f, -0.22f), new Vector3(0.58f, 0.16f, 0.24f), MatOf(shirtC));
        Bar("Vest", upper, new Vector3(0f, 1.50f, -0.40f), new Vector3(0f, 1.12f, -0.56f), 0.05f, 0.40f, MatOf(vestC), true);   // a green vest over the back
        Bar("Strap", upper, new Vector3(0.20f, 1.58f, -0.375f), new Vector3(-0.20f, 1.12f, -0.575f), 0.04f, 0.07f, MatOf(darkC), true);
        Make(PrimitiveType.Cylinder, "Neck", upper, new Vector3(0f, 1.69f, -0.21f), new Vector3(0.12f, 0.06f, 0.12f), MatOf(skinC));
        Make(PrimitiveType.Sphere, "Head", upper, new Vector3(0f, 1.86f, -0.17f), new Vector3(0.34f, 0.36f, 0.34f), MatOf(skinC));
        Make(PrimitiveType.Sphere, "Hair", upper, new Vector3(0f, 1.82f, -0.29f), new Vector3(0.30f, 0.22f, 0.12f), MatOf(hairC));
        Make(PrimitiveType.Sphere, "Cap", upper, new Vector3(0f, 1.96f, -0.18f), new Vector3(0.37f, 0.22f, 0.37f), MatOf(vestC));
        Make(PrimitiveType.Cube, "Visor", upper, new Vector3(0f, 1.92f, 0.01f), new Vector3(0.28f, 0.03f, 0.18f), MatOf(vestC));

        for (int i = 0; i < 2; i++)
        {
            thigh[i] = Make(PrimitiveType.Cube, "Thigh", transform, Vector3.zero, Vector3.one, MatOf(pantsC));
            shin[i] = Make(PrimitiveType.Cube, "Shin", transform, Vector3.zero, Vector3.one, MatOf(pantsC));
            shoe[i] = Make(PrimitiveType.Cube, "Shoe", transform, Vector3.zero, new Vector3(0.12f, 0.09f, 0.26f), MatOf(shoeC));
            upperArm[i] = Make(PrimitiveType.Cube, "UpperArm", upper, Vector3.zero, Vector3.one, MatOf(shirtC));
            foreArm[i] = Make(PrimitiveType.Cube, "ForeArm", upper, Vector3.zero, Vector3.one, MatOf(skinC));
            hand[i] = Make(PrimitiveType.Sphere, "Hand", upper, Vector3.zero, new Vector3(0.11f, 0.11f, 0.11f), MatOf(skinC));
        }

        Pose(0f, -10f);
        return top.GetComponent<Renderer>();
    }

    // ---- animating ----

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (bike == null) bike = BikeForward.Instance;
        var gm = GameManager.Instance;
        float target = 0f;
        if (bike != null && gm != null && gm.Playing && !bike.Hold) target = bike.Velocity.magnitude;
        speed = Mathf.MoveTowards(speed, target, dt * 12f);
        wheelDeg = Mathf.Repeat(wheelDeg + speed / WheelR * Mathf.Rad2Deg * dt, 360f);
        crank = Mathf.Repeat(crank + speed * 1.1f * dt, Mathf.PI * 2f);
        Pose(crank, Time.time - LastThrowTime);
    }

    static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    // Where the throwing hand is, t seconds after the paper left it: from the grip, back up behind the head, out to the side, back to the grip.
    static Vector3 ThrowHand(float sg, float t)
    {
        Vector3 a = new Vector3(GripBase.x * sg, GripBase.y, GripBase.z);
        Vector3 b = new Vector3(0.40f * sg, 1.74f, -0.40f);
        Vector3 c = new Vector3(0.80f * sg, 1.50f, 0.50f);
        if (t < 0.14f) return Vector3.Lerp(a, b, Smooth(t / 0.14f));
        if (t < 0.30f) return Vector3.Lerp(b, c, Smooth((t - 0.14f) / 0.16f));
        return Vector3.Lerp(c, a, Smooth((t - 0.30f) / 0.45f));
    }

    // Two-bone solver: the joint (knee or elbow) between a and b. b is pulled in if it is out of reach. `hint` says which way the joint bends.
    static Vector3 Joint(Vector3 a, ref Vector3 b, float l1, float l2, Vector3 hint)
    {
        Vector3 d = b - a;
        float dist = d.magnitude;
        float max = l1 + l2 - 0.005f;
        if (dist > max) { d = d / dist * max; dist = max; b = a + d; }
        dist = Mathf.Max(dist, 0.1f);
        Vector3 dir = d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
        float h = (dist * dist + l1 * l1 - l2 * l2) / (2f * dist);
        float r = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - h * h));
        Vector3 perp = hint - dir * Vector3.Dot(hint, dir);
        perp = perp.sqrMagnitude < 1e-6f ? Vector3.up : perp.normalized;
        return a + dir * h + perp * r;
    }

    void Pose(float crankAngle, float sinceThrow)
    {
        wheelF.localRotation = Quaternion.Euler(wheelDeg, 0f, 0f);
        wheelB.localRotation = Quaternion.Euler(wheelDeg, 0f, 0f);

        float bob = Mathf.Sin(crankAngle * 2f) * 0.012f;
        upper.localPosition = new Vector3(0f, bob, 0f);
        Vector3 bobV = new Vector3(0f, bob, 0f);

        for (int i = 0; i < 2; i++)
        {
            float sg = i == 0 ? -1f : 1f;

            // pedals and legs: the left and right pedal are half a turn apart
            float a = crankAngle + (i == 0 ? 0f : Mathf.PI);
            Vector3 pedalPos = new Vector3(sg * PedalX, BB.y + CrankR * Mathf.Cos(a), BB.z + CrankR * Mathf.Sin(a));
            pedal[i].localPosition = pedalPos;
            Place(crankArm[i], new Vector3(sg * 0.10f, BB.y, BB.z), new Vector3(sg * 0.10f, pedalPos.y, pedalPos.z), 0.04f, 0.04f);

            Vector3 hip = new Vector3(sg * HipX, HipC.y, HipC.z);
            Vector3 ankle = pedalPos + new Vector3(0f, 0.08f, -0.04f);
            Vector3 knee = Joint(hip, ref ankle, ThighL, ShinL, new Vector3(sg * 0.25f, 0.1f, 1f));
            Place(thigh[i], hip, knee, 0.15f, 0.15f);
            Place(shin[i], knee, ankle, 0.12f, 0.12f);
            shoe[i].localPosition = ankle + new Vector3(0f, -0.03f, 0.05f);

            // arms: shoulder down to the grip, or through the throw
            Vector3 shoulder = new Vector3(sg * ShoulderX, ShoulderBase.y, ShoulderBase.z);
            Vector3 grip = new Vector3(sg * GripBase.x, GripBase.y, GripBase.z);
            Vector3 handPos = grip;
            bool throwing = (LastThrowSide >= 0f) == (i == 1) && sinceThrow >= 0f && sinceThrow < 0.8f;
            if (throwing) handPos = ThrowHand(sg, sinceThrow);
            handPos -= bobV;                       // the shoulders ride up and down with the body, the hands stay on the bar
            Vector3 elbow = Joint(shoulder, ref handPos, UpperArmL, ForeArmL, new Vector3(sg * 0.7f, -1f, -0.2f));
            Place(upperArm[i], shoulder, elbow, 0.11f, 0.11f);
            Place(foreArm[i], elbow, handPos, 0.09f, 0.09f);
            hand[i].localPosition = handPos;
        }
    }
}
