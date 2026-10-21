using System.Collections.Generic;
using UnityEngine;

// House (partial) - Doorstep path, door parts and door/status colours.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- doorstep path: one run of paving slabs from the footpath all the way to the door ----
    // Built from the lot's box parts for an active customer's house only. Indices are valid until the next Setup.
    // The scoring zones are unchanged: the first part of the run is the path, the last part (x 7.0 to 8.4) is the doormat's area.
    int stoneFirst, stoneCount;         // the slabs, ordered from the footpath to the door
    bool slabsOnPath;                   // the run starts at the footpath (a lawn lot) and not at the doormat's area
    const float PathSweepPeriod = 1.6f; // seconds per run of the glow

    static readonly Color DoorDark = new Color(0.10f, 0.18f, 0.28f);   // the base under the slabs is the path colour pulled towards this
    static readonly Color GlowGreen = new Color(0.20f, 0.90f, 0.35f);  // the glow ends in this at the door
    static Color DoorBase { get { return Color.Lerp(GameConfig.PathColor, DoorDark, 0.6f); } }

    void LayoutDoorParts(float doorZ, bool on)
    {
        stoneCount = 0;
        slabsOnPath = false;
        if (!on) return;
        float s = side;

        float pathW = pathVisualT != null ? pathVisualT.localScale.x : 0f;
        slabsOnPath = pathW >= 1.2f;
        float x0 = slabsOnPath ? (fort && GroundBlock == 1 ? 5.6f : 4.4f - setback) : 7.05f;   // behind a wall the path starts inside it
        float x1 = 8.35f;
        int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 0.65f));
        const float gap = 0.1f;
        float len = ((x1 - x0) - gap * (n + 1)) / n;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + gap + len * 0.5f + i * (len + gap);
            Part(new Vector3(s * x, 0.17f, doorZ), new Vector3(len, 0.03f, 1.3f), GameConfig.PathColor, false);
            if (i == 0) stoneFirst = lotN - 1;
        }
        stoneCount = n;
    }

    void ShowDoorParts(bool on)
    {
        for (int i = 0; i < stoneCount; i++) lotParts[stoneFirst + i].gameObject.SetActive(on);
        if (!on) for (int i = 0; i < slabGlow.Count; i++) slabGlow[i].gameObject.SetActive(false);
    }

    // At night the slabs are as dark as everything else, so the shimmer is drawn on top of them with an unlit overlay: it shows whether or not a light reaches the path.
    readonly List<Transform> slabGlow = new List<Transform>();
    readonly List<Material> slabGlowMat = new List<Material>();

    void ShowSlabGlow(int i, bool night, Color tint, float alpha)
    {
        if (!night) { if (i < slabGlow.Count) slabGlow[i].gameObject.SetActive(false); return; }
        while (slabGlow.Count <= i)
        {
            var q = BlobShadow.Create(transform);
            q.name = "SlabGlow";
            var m = new Material(NightLights.Bulb);   // its own copy, so each slab has its own tint
            q.GetComponent<Renderer>().sharedMaterial = m;
            slabGlow.Add(q.transform);
            slabGlowMat.Add(m);
        }
        var slab = lotParts[stoneFirst + i];
        var t = slabGlow[i];
        t.localPosition = new Vector3(slab.localPosition.x, 0.25f, slab.localPosition.z);
        t.localScale = new Vector3(slab.localScale.x, slab.localScale.z, 1f);
        slabGlowMat[i].color = new Color(tint.r, tint.g, tint.b, alpha);
        t.gameObject.SetActive(alpha > 0.02f);
    }

    // The player's path colour changed (or the house just became active): the slabs take it, the base follows the darker shade.
    void ApplyDoorColors()
    {
        for (int i = 0; i < stoneCount; i++) lotPartR[stoneFirst + i].material.color = GameConfig.PathColor;
    }

    // A glow runs along the slabs from the footpath to the door. It starts as a light shimmer and turns greener as it nears the door.
    void AnimateDoorStones()
    {
        Color baseC = GameConfig.PathColor;
        float head = Mathf.Repeat(Time.time / PathSweepPeriod + phase * 0.159f, 1f) * 1.7f - 0.3f;
        bool night = NightLights.Available && NightLights.Dark > 0.1f;
        for (int i = 0; i < stoneCount; i++)
        {
            float p = (i + 0.5f) / stoneCount;
            float glow = Mathf.Clamp01(1f - Mathf.Abs(head - p) * 3.2f);
            Color tint = Color.Lerp(Color.white, GlowGreen, Mathf.Clamp01((p - 0.1f) / 0.8f));
            lotPartR[stoneFirst + i].material.color = Color.Lerp(baseC, tint, glow * 0.9f);
            ShowSlabGlow(i, night, tint, glow * 0.95f);
        }
    }

    // An active customer: green pad and balcony, red-orange mailbox, a path of slabs to the door (all set in the options).
    void SetStatusPad()
    {
        if (pathRenderer == null && pathVisualT != null) pathRenderer = pathVisualT.GetComponent<Renderer>();
        SetStatus(GameConfig.PadColor);
        if (matRenderer != null) matRenderer.material.color = DoorBase;
        if (mailboxRenderer != null) mailboxRenderer.material.color = GameConfig.MailboxColor;
        if (pathRenderer != null) pathRenderer.material.color = slabsOnPath ? DoorBase : GameConfig.PathColor;
        statusIsPad = true;
        ApplyDoorColors();
        ShowDoorParts(true);
        ReapplyServed();   // (a floor that is already served stays gold)
    }

    // The colour options changed: re-colour what this house shows.
    void RefreshColors()
    {
        colorVersion = GameConfig.ColorVersion;
        if (isPark) { if (padRenderer != null) padRenderer.material.color = GameConfig.ParkColor; }
        else if (statusIsPad) SetStatusPad();
        if (isBeach && padRenderer != null) padRenderer.material.color = GameConfig.SandColor;
    }

    void SetStatus(Color c)
    {
        statusIsPad = false;
        if (pathRenderer == null && pathVisualT != null) pathRenderer = pathVisualT.GetComponent<Renderer>();
        if (pathRenderer != null) pathRenderer.material.color = InactivePath;
        if (padRenderer != null && !paved) padRenderer.material.color = GameConfig.TurfColor;   // a served or missed lawn looks like plain grass
        if (matRenderer != null) matRenderer.material.color = c;
        if (mailboxRenderer != null) mailboxRenderer.material.color = c;
        SetBalconyColors(c);
        if (beachScene == 2 && Subscriber && lgPadR != null) lgPadR.material.color = c;   // the lifeguard hut's pad shows the status
        ShowDoorParts(false);
        UpdateTurf();
    }
}
