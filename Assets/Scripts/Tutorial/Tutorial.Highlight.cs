using UnityEngine;

// Tutorial (partial) - Highlight
// The main part of the class is in Tutorial.cs.
public partial class Tutorial
{
    // ---- the yellow circle on the target ----

    const int MaxRings = 6;
    readonly LineRenderer[] ringsA = new LineRenderer[MaxRings];
    readonly LineRenderer[] ringsB = new LineRenderer[MaxRings];
    const int RingN = 48;
    static readonly Color RingColor = new Color(1f, 0.9f, 0.15f);
    const int MaxLabels = 6;
    int labelCount;
    readonly Vector3[] labelPos = new Vector3[MaxLabels];
    readonly string[] labelText = new string[MaxLabels];
    readonly bool[] labelHit = new bool[MaxLabels];
    int holeCount;                                           // the intro dims everything except these spots (world centre, radius)
    readonly Vector3[] holeC = new Vector3[MaxRings];
    readonly float[] holeR = new float[MaxRings];

    // True while the intro step is showing: the hit zones glow harder (House reads this).
    public static bool IntroGlow { get { return Active && inst != null && inst.step == 0 && inst.introPhase == 1; } }

    void AddLabel(Vector3 pos, string text, bool hit)
    {
        if (labelCount >= MaxLabels) return;
        labelPos[labelCount] = pos;
        labelText[labelCount] = text;
        labelHit[labelCount] = hit;
        labelCount++;
    }

    LineRenderer MakeRing()
    {
        var go = new GameObject("TutorialRing");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = Mat.Particle();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = RingN;
        lr.widthMultiplier = 0.2f;
        lr.startColor = RingColor;
        lr.endColor = RingColor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.enabled = false;
        return lr;
    }

    // One pulsing circle (with a ripple) on a zone: flat on the ground, or standing up and facing the camera.
    void Ring(int i, HouseTarget zone, bool ground, float r, string label)
    {
        if (zone == null || !zone.gameObject.activeInHierarchy || i >= MaxRings) return;
        if (ringsA[i] == null) { ringsA[i] = MakeRing(); ringsB[i] = MakeRing(); }
        ringFrame[i] = Time.frameCount;
        Vector3 c = zone.transform.position;
        Vector3 ax, ay;
        if (ground)
        {
            c.y = 0.16f;
            ax = Vector3.right; ay = Vector3.forward;
        }
        else
        {
            var cam = Camera.main;
            Vector3 cr = cam != null ? cam.transform.right : Vector3.right;
            ax = new Vector3(cr.x, 0f, cr.z).normalized;
            ay = Vector3.up;
        }
        float t = Time.unscaledTime;
        DrawRing(ringsA[i], c, ax, ay, r * (1f + 0.1f * Mathf.Sin(t * 5f)), 1f);
        float p = (t * 0.9f + i * 0.3f) % 1f;
        DrawRing(ringsB[i], c, ax, ay, r * (1f + 0.9f * p), 1f - p);
        if (label != null) AddLabel(c + (ground ? Vector3.up * 0.3f : Vector3.up * (r + 0.4f)), label, true);
        if (holeCount < MaxRings) { holeC[holeCount] = c; holeR[holeCount] = r; holeCount++; }
    }

    // The yellow circle(s) on this step's target(s). The intro step circles all three zones of the first house and names them.
    void LateUpdate()
    {
        var gm = GameManager.Instance;
        var bike = BikeForward.Instance;
        bool any = false;
        labelCount = 0;
        holeCount = 0;
        if (Active && gm != null && bike != null && gm.Playing && step <= LastHouseStep && gm.Spawner != null && !(step == 0 && introPhase == 0))
        {
            var sp = gm.Spawner;
            int k = Mathf.Max(1, step);
            House h = sp.TutorialHouse(k);
            float bz = bike.transform.position.z;
            if (h != null && !h.Resolved && bz < sp.TutorialZ(k) + 6f)
            {
                any = true;
                if (step == 0)
                {
                    Ring(0, h.gardenTarget, true, Mathf.Clamp(h.gardenTarget != null ? Mathf.Min(h.gardenTarget.size.x, h.gardenTarget.size.z) * 0.45f : 1f, 0.9f, 1.5f), "Lawn");
                    Ring(1, h.doorstepZone, true, 0.95f, "Doormat");
                    Ring(2, h.mailZone, false, 0.8f, "Mailbox");
                }
                else
                {
                    // the ground floor: every glowing spot on the ground counts, so all of them are circled (until the floor is cleared)
                    int n = 0;
                    // (step 3 is about the hoop alone, so the lawn, doormat and mailbox are not circled there)
                    if (step != 3 && h.ZoneOpen(h.gardenTarget)) Ring(n++, h.gardenTarget, true, Mathf.Clamp(h.gardenTarget != null ? Mathf.Min(h.gardenTarget.size.x, h.gardenTarget.size.z) * 0.45f : 1f, 0.9f, 1.5f), null);
                    if (step != 3 && h.ZoneOpen(h.doorstepZone)) Ring(n++, h.doorstepZone, true, 0.95f, null);
                    if (step != 3 && h.ZoneOpen(h.mailZone)) Ring(n++, h.mailZone, false, 0.8f, null);
                    // step 2 circles the balcony still to clear; step 3 circles only the building's hoop (the balconies are left plain: the hoop clears them)
                    if (step == 2 && h.balconies != null)
                        for (int i = 0; i < h.balconies.Length; i++)
                            if (h.balconies[i].on && h.ZoneOpen(h.balconies[i].zone)) Ring(n++, h.balconies[i].zone, false, 0.9f, null);
                    if (step == 3 && h.floorHoopZ != null && h.floorHoopZ[1] != null && h.ZoneOpen(h.floorHoopZ[1])) Ring(n++, h.floorHoopZ[1], false, 0.8f, null);
                }
            }
        }
        // rings not used this frame are hidden
        for (int i = 0; i < MaxRings; i++)
        {
            bool used = any && ringsA[i] != null && ringsA[i].enabled && RingUsed(i);
            if (!used && ringsA[i] != null) { ringsA[i].enabled = false; ringsB[i].enabled = false; }
        }
    }

    // a ring counts as used this frame if Ring() just switched it on (it is switched off here every frame before being drawn again)
    bool RingUsed(int i) { return ringFrame[i] == Time.frameCount; }
    readonly int[] ringFrame = new int[MaxRings];

    static void DrawRing(LineRenderer lr, Vector3 c, Vector3 ax, Vector3 ay, float r, float alpha)
    {
        for (int i = 0; i < RingN; i++)
        {
            float a = i * Mathf.PI * 2f / RingN;
            lr.SetPosition(i, c + (ax * Mathf.Cos(a) + ay * Mathf.Sin(a)) * r);
        }
        Color col = new Color(RingColor.r, RingColor.g, RingColor.b, alpha);
        lr.startColor = col;
        lr.endColor = col;
        lr.enabled = true;
    }

    void Update()
    {
        if (!Active) return;
        var gm = GameManager.Instance;
        var bike = BikeForward.Instance;
        if (gm == null || bike == null || !gm.Playing) return;

        stepTime += Time.unscaledDeltaTime;
        bike.Hold = step == 0;   // the intro waits: the bike stands still while the zones are explained

        if (transitioning && moveKind == 0 && Time.unscaledTime >= jumpAt) StartFade(1);
        if (moveKind != 0 && Time.unscaledTime - fadeStart >= 0.3f) FinishMove(gm, bike);
        if (transitioning || moveKind != 0 || Time.unscaledTime - fadeStart < 0.8f) return;

        // the caption steps move on by themselves after a while (the button is quicker)
        if (step == 4 && stepTime > 7f) Advance(null);
        else if (step == 5 && stepTime > 8f) Advance(null);

        if (IsHouseStep(step))
        {
            var sp = gm.Spawner;
            int k = step;
            House h = sp != null ? sp.TutorialHouse(k) : null;
            if (h != null)
            {
                float bz = bike.transform.position.z;
                float hz = sp.TutorialZ(k);
                bool flying = Paper.All.Count > 0;
                bool missed = !h.Resolved && bz > hz + 7f;
                bool pastWrong = wrongKind && bz > hz + 6f;
                if ((missed || pastWrong) && !flying) Retry(gm, bike, sp, k);
            }
        }
    }

    // Fades out, puts the bike back in front of the house and re-arms it (see FinishMove).
    void Retry(GameManager gm, BikeForward bike, SegmentSpawner sp, int k)
    {
        retryK = k;
        StartFade(2);
        gm.ShowToast("Let's try that house again", 2.2f);
    }
}
