using UnityEngine;

// Tutorial (partial) - Long lesson: clearing a whole building (the third step)
// The scripted house of this step is the longest house there is: three storeys, balconies on both upper floors and the building's basket over the
// middle floor's near balcony (SegmentSpawner.Place), and a lawn.
//   1. floorsIntro  A dark overlay with the tower lit up floor by floor, and the words: every floor has to be cleared, and a missed balcony or
//                   ground floor costs a life. The same green "Got it" button. Nothing else on the screen works meanwhile.
//   2. longLesson   The game is slowed. Two circles: where the drag starts (the bike) and where it ends (a long pull, worked out so the arc
//                   comes down on the top floor's balcony). Same rules as the near shot: press on the bike, let go in the green circle.
//   3. afterLong    Speed back to normal while the paper flies. If it missed the balcony, the long shot starts again from the same situation (a fade,
//                   the bike and the house put back, the game slowed again) until a paper lands on the balcony.
//   4. hoopIntro    After a hit: a fade, the bike stands in front of the basket, and a dark overlay lights the basket: a paper through it clears every
//                   balcony at once. The same green "Got it".
//   5. hoopMode     The same slowed lesson with its two circles, now aimed at the basket (a miss starts it again). A dunk: "now clear the ground
//                   floor" (groundPhase) with the ground circled in yellow.
//   The step ends when the ground floor is cleared, a paper fails on the ground, or the bike has passed the house: it moves on either way.
// The main part of the class is in Tutorial.cs.
public partial class Tutorial
{
    bool floorsIntro, longLesson, afterLong, groundPhase, balconyHit;
    bool hoopIntro, hoopMode;        // the basket shot: its overlay, and the lesson (longLesson is on while the circles are up) aimed at the basket
    float hoopAt;                    // when the screen fades to the basket shot after a balcony was cleared
    float longThrowAt;
    float longBikeZ;                 // where the bike stands for the long shot (and again after a miss)
    bool longPlanned;
    float longPower;                 // the power of the lesson's throw (0..1)
    Vector3 longHeading;             // the ground heading of that throw
    const float HoopBack = 4f;       // the bike stands this far back along the road from the basket (the planner uses the game's own ring: nothing is made easier)
    const float LongBack = 3.5f;     // the bike stands this far back along the road from the top floor's balcony (close, so the arc up to it is steep)

    // True during the basket shot: the balconies stop glowing (House reads this) so the board and the ring are all that stands out.
    public static bool HoopFocus { get { return Active && inst != null && inst.hoopMode && !inst.groundPhase; } }

    bool circleLesson { get { return nearLesson || longLesson; } }   // a lesson with a press circle and a release circle

    // ---- start ----

    // The bike is put in front of the tower and the overlay opens (called when the screen is black).
    void BeginFloorsIntro(BikeForward bike, SegmentSpawner sp)
    {
        longBikeZ = sp.TutorialZ(2) + SegmentSpawner.TowerTopZ - LongBack;
        bike.MoveTo(longBikeZ);
        floorsIntro = true;
        longLesson = false; afterLong = false; groundPhase = false; balconyHit = false; hoopMode = false; hoopIntro = false; hoopAt = 0f;
        longPlanned = false;
        nearCached = false;
        ghost = false;
        stepTime = 0f;
    }

    // The bike is put in front of the basket (called when the screen is black): with its overlay the first time, straight into the slowed
    // shot when it starts again after a miss. The house is armed again so the basket can serve the balconies.
    void BeginHoopLesson(BikeForward bike, SegmentSpawner sp, bool withIntro)
    {
        Paper.DestroyAll();
        House h = sp != null ? sp.TutorialHouse(2) : null;
        if (sp != null)
        {
            if (h != null && h.floorHoopZ != null && h.floorHoopZ[1] != null)
                hoopBikeZ = h.floorHoopZ[1].transform.position.z - HoopBack;
            bike.MoveTo(hoopBikeZ);
            sp.TutorialRearm(2);   // after the move: a house behind the bike would count as done
        }
        hoopMode = true;
        floorsIntro = false; afterLong = false; groundPhase = false; balconyHit = false; hoopAt = 0f;
        hoopIntro = withIntro;
        longLesson = !withIntro;
        longPlanned = false;
        nearCached = false;
        praise = null;
        ghost = false;
        stepTime = 0f;
        if (!withIntro) { GameTime.Lesson = LessonSlow; GameTime.Apply(); }
    }

    float hoopBikeZ;

    void ClearLongState()
    {
        floorsIntro = false; longLesson = false; afterLong = false; groundPhase = false; balconyHit = false; hoopMode = false; hoopIntro = false; hoopAt = 0f;
        longPlanned = false;
    }

    // ---- the hooks (House and Paper call these) ----

    public static void OnBalcony(int floor)
    {
        if (Active && inst != null) inst.BalconyHit(floor);
    }

    public static void OnGroundCleared()
    {
        if (Active && inst != null) inst.GroundCleared();
    }

    void BalconyHit(int floor)
    {
        if (floor != 2 || hoopMode) return;   // only the top floor's balcony counts (a paper that lands on the middle one is a miss for this lesson)
        if (step != 2 || !afterLong || groundPhase || balconyHit || transitioning || moveKind != 0) return;
        balconyHit = true;
        praise = "Nice!";
        praiseUntil = Time.unscaledTime + 1.4f;
        hoopAt = Time.unscaledTime + 1.5f;   // (a moment to read it: then the screen fades to the basket shot, see UpdateLong)
    }

    // The building's basket was scored. In the basket shot that clears every balcony: on to the ground floor. (A dunk during the balcony shot
    // serves the top floor as well.)
    public void HoopDunk()
    {
        if (step != 2 || !afterLong || groundPhase || transitioning || moveKind != 0) return;
        if (hoopMode)
        {
            groundPhase = true;
            praise = "Swish!";
            praiseUntil = Time.unscaledTime + 1.4f;
        }
        else BalconyHit(2);
    }

    void GroundCleared()
    {
        if (step != 2 || !groundPhase || transitioning || moveKind != 0) return;
        Advance("Complete!");
    }

    void LongThrown()
    {
        longLesson = false;
        afterLong = true;
        longThrowAt = Time.unscaledTime;
        EndLessonTime();      // the speed goes back to normal as soon as the paper is thrown
        ghost = false;
        throws = 1;
    }

    // A paper came to rest without scoring (Paper.Settle): on the ground phase the lesson moves on.
    void LongPaperFailed()
    {
        if (step != 2 || !groundPhase || transitioning || moveKind != 0) return;
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast("Missed houses causes customer complain", 2.4f);
        EndLong(null);
    }

    // The balcony was missed: the long shot starts again from the same situation (like the near shot does).
    void LongMissed()
    {
        if (transitioning || moveKind != 0) return;
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast("Missed houses causes customer complain", 2.4f);
        StartFade(4);
    }

    // The basket was missed: the basket shot starts again from the same situation.
    void HoopMissed()
    {
        if (transitioning || moveKind != 0) return;
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast("Missed. Try again.", 2.4f);
        StartFade(6);
    }

    // The step is over, whatever happened.
    void EndLong(string message)
    {
        if (transitioning || moveKind != 0) return;
        Advance(message);
        if (message == null) jumpAt = Time.unscaledTime + 1.6f;   // (a moment to read what was said)
    }

    // Called every frame while the tower step is on (Tutorial.Update).
    void UpdateLong(GameManager gm, BikeForward bike)
    {
        var sp = gm.Spawner;
        if (sp == null || sp.TutorialHouse(2) == null) return;
        if (!afterLong && !groundPhase) return;

        bool flying = false;
        for (int i = Paper.All.Count - 1; i >= 0; i--)
        {
            var pp = Paper.All[i];
            if (pp != null && pp.InFlight) { flying = true; break; }
        }
        float hz = sp.TutorialZ(2);
        House tower = sp.TutorialHouse(2);
        bool past = bike.transform.position.z > hz + (tower != null ? tower.LotLength * 0.5f : 4f) + 1.5f;
        if (afterLong && !groundPhase)
        {
            if (balconyHit)
            {
                // the balcony is cleared: after a moment the screen fades to the basket shot
                if (hoopAt > 0f && Time.unscaledTime >= hoopAt) { hoopAt = 0f; StartFade(5); }
            }
            else if ((!flying && Time.unscaledTime - longThrowAt > 0.5f) || (past && !flying))
            {
                // the throw is over and it did not clear the balcony (or go through the basket)
                if (hoopMode) HoopMissed(); else LongMissed();
            }
        }
        else if (groundPhase && past && !flying)
        {
            EndLong(null);
        }
    }

    // ---- where the long pull ends ----

    // The arc has to clear the balcony's rail and come down on its floor (or hit the wall just above it and drop onto it).
    // This tries throws of different power and direction and picks the middle of the range that works, so a pull near the circle succeeds.
    bool PlanLong()
    {
        if (longPlanned) return true;
        var gm = GameManager.Instance;
        var bike = BikeForward.Instance;
        if (gm == null || gm.Spawner == null || bike == null) return false;
        House h = gm.Spawner.TutorialHouse(2);
        if (h == null || h.balconies == null) return false;
        if (ghostThrower == null) ghostThrower = Object.FindFirstObjectByType<PaperThrower>();
        if (ghostThrower == null) return false;

        Vector3 origin = ghostThrower.ThrowPoint;
        LongTarget tgt = default(LongTarget);
        Vector3 target;
        if (hoopMode)
        {
            // the building's basket: a ring over the middle floor's first balcony
            var ring = h.floorHoopZ != null ? h.floorHoopZ[1] : null;
            House.BalconySet under = null;
            for (int i = 0; i < h.balconies.Length; i++)
                if (h.balconies[i] != null && h.balconies[i].on && h.balconies[i].floor == 1) { under = h.balconies[i]; break; }
            if (ring == null || !ring.gameObject.activeInHierarchy || under == null) return false;
            target = ring.transform.position;
            tgt.hoop = true;
            tgt.side = h.side;
            tgt.hx = h.transform.position.x;
            tgt.ringU = h.side * (target.x - tgt.hx);
            tgt.ringY = target.y; tgt.ringZ = target.z;
            tgt.catchHalf = ring.openingHalf > 0f ? ring.openingHalf : GameConfig.HoopOpeningHalf;
            tgt.catchDown = ring.size.y * 0.5f;
            tgt.zb = under.z + h.transform.position.z;        // the balcony beneath (its rail must be cleared)
            tgt.half = under.len * 0.5f + 0.1f;
            tgt.railTop = House.GroundStoreyH + 1.6f;
        }
        else
        {
            House.BalconySet b = null;
            for (int i = 0; i < h.balconies.Length; i++)
                if (h.balconies[i] != null && h.balconies[i].on && h.balconies[i].floor == 2) { b = h.balconies[i]; break; }
            if (b == null || b.slab == null) return false;
            target = b.slab.position;
            float baseY = b.slab.position.y - 0.45f;
            tgt.side = h.side;
            tgt.hx = h.transform.position.x;
            tgt.zb = b.slab.position.z;
            tgt.half = b.len * 0.5f;
            tgt.slabTop = baseY + 0.525f + 0.1f;
            tgt.railTop = baseY + 1.6f;
        }
        Vector3 dir0 = target - origin;
        dir0.y = 0f;
        if (dir0.sqrMagnitude < 0.01f) return false;
        dir0.Normalize();

        // the balcony is a wide target (coarse search); the basket's ring is small, so it is searched finely
        float pMin = hoopMode ? 0.60f : 0.60f, pStep = hoopMode ? 0.005f : 0.02f, psiStep = hoopMode ? 0.5f : 1.5f;
        int pCount = hoopMode ? 60 : 20, psiN = hoopMode ? 24 : 12;
        float dP = hoopMode ? 0.012f : 0.04f, dPsi = hoopMode ? 1.5f : 4f;
        float sumP = 0f, sumPsi = 0f; int cells = 0; float best = 0f;
        for (int pass = 0; pass < 2; pass++)
        {
            // pass 0 finds the best score, pass 1 averages every cell that reaches it (the middle of the plateau)
            for (int pi = 0; pi <= pCount; pi++)
            {
                float p0 = pMin + pi * pStep;
                for (int si = -psiN; si <= psiN; si++)
                {
                    float psi0 = si * psiStep;
                    int ok = 0;
                    for (int a = -1; a <= 1; a++)
                        for (int c = -1; c <= 1; c++)
                        {
                            float p = Mathf.Clamp01(p0 + a * dP);
                            float psi = psi0 + c * dPsi;
                            Vector3 heading = Quaternion.AngleAxis(psi, Vector3.up) * dir0;
                            if (LongHits(ghostThrower.VelocityFor(p, heading), origin, tgt)) ok++;
                        }
                    float score = ok / 9f;
                    if (pass == 0) { if (score > best) best = score; }
                    else if (score >= best - 0.001f) { sumP += p0; sumPsi += psi0; cells++; }
                }
            }
            if (best <= 0f) break;
        }

        float power = cells > 0 ? sumP / cells : 0.75f;
        float psiBest = cells > 0 ? sumPsi / cells : 0f;
        longPower = Mathf.Clamp01(power);
        longHeading = Quaternion.AngleAxis(psiBest, Vector3.up) * dir0;
        longPlanned = true;
        return true;
    }

    struct LongTarget { public int side; public float hx, zb, half, slabTop, railTop; public bool hoop; public float ringU, ringY, ringZ, catchHalf, catchDown; }

    const float RailU = 6.88f, WallU = 8.42f;   // the balcony rail's road side and the wall, measured out from the road along the house's side

    // Does a throw from here with this launch velocity end up on the balcony floor?
    static bool LongHits(Vector3 v, Vector3 origin, LongTarget t)
    {
        if (t.hoop) return HoopHits(v, origin, t);
        Vector3 pos = origin, vel = v;
        const float dt = 0.02f;
        bool crossed = false;
        for (int i = 0; i < 400; i++)
        {
            vel += Physics.gravity * dt;
            vel *= Mathf.Exp(-GameConfig.AirDrag * dt);
            Vector3 prev = pos;
            pos += vel * dt;
            float u0 = t.side * (prev.x - t.hx), u1 = t.side * (pos.x - t.hx);
            if (!crossed && u0 < RailU && u1 >= RailU)
            {
                float f = (RailU - u0) / (u1 - u0);
                float yy = prev.y + (pos.y - prev.y) * f, zz = prev.z + (pos.z - prev.z) * f;
                if (Mathf.Abs(zz - t.zb) > t.half || yy < t.railTop) return false;   // beside the balcony, or into the rail
                crossed = true;
            }
            if (crossed)
            {
                if (prev.y >= t.slabTop && pos.y < t.slabTop && vel.y < 0f)
                {
                    float f = (prev.y - t.slabTop) / (prev.y - pos.y);
                    float xx = u0 + (u1 - u0) * f, zz = prev.z + (pos.z - prev.z) * f;
                    return Mathf.Abs(zz - t.zb) < t.half - 0.15f && xx > 7.0f && xx < 8.4f;
                }
                if (u1 >= WallU)
                {
                    float f = (WallU - u0) / (u1 - u0);
                    float yy = prev.y + (pos.y - prev.y) * f, zz = prev.z + (pos.z - prev.z) * f;
                    return Mathf.Abs(zz - t.zb) < t.half - 0.15f && yy > t.slabTop && yy < t.slabTop + 6f;   // it drops down the wall onto the floor
                }
            }
            if (pos.y < 0f) return false;
        }
        return false;
    }

    // Does a throw from here come down through the basket? It has to clear the rail of the balcony under it, then drop into the ring's catch zone.
    static bool HoopHits(Vector3 v, Vector3 origin, LongTarget t)
    {
        Vector3 pos = origin, vel = v;
        const float dt = 0.02f;
        bool crossed = false;
        for (int i = 0; i < 500; i++)
        {
            vel += Physics.gravity * dt;
            vel *= Mathf.Exp(-GameConfig.AirDrag * dt);
            Vector3 prev = pos;
            pos += vel * dt;
            float u0 = t.side * (prev.x - t.hx), u1 = t.side * (pos.x - t.hx);
            if (!crossed && u0 < RailU && u1 >= RailU)
            {
                float f = (RailU - u0) / (u1 - u0);
                float yy = prev.y + (pos.y - prev.y) * f, zz = prev.z + (pos.z - prev.z) * f;
                if (Mathf.Abs(zz - t.zb) < t.half && yy < t.railTop + 0.1f) return false;   // into the rail
                crossed = true;
            }
            if (Mathf.Abs(u1 - t.ringU) < t.catchHalf && Mathf.Abs(pos.z - t.ringZ) < t.catchHalf && Mathf.Abs(pos.y - t.ringY) < t.catchDown && vel.y < -0.5f) return true;
            if (u1 >= WallU || pos.y < 0f) return false;
        }
        return false;
    }

    // The two circles of the long shot (screen pixels, origin bottom left): the press on the bike and the end of the pull.
    bool ComputeLongGeometry(out Vector2 startPx, out Vector2 releasePx, out float startR, out float releaseR)
    {
        startPx = releasePx = Vector2.zero;
        startR = releaseR = 0f;
        if (!PlanLong() || ghostThrower == null) return false;

        bool portrait = Screen.height > Screen.width;
        float scale = portrait ? Screen.width / 540f : Screen.height / 720f;
        float w = Screen.width / scale, hh = Screen.height / scale;
        Vector2 cGui = BikeGuiPos(w, hh, scale);

        // the heading in screen axes, the way PaperThrower.ComputeVelocity reads the pull (the other way round)
        float yaw = 0f;
        var view = CameraFollow.Instance;
        if (view != null && !view.firstPerson) yaw = view.yaw;
        Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * longHeading;
        Transform axes = ghostThrower.transform;
        Vector2 aim = new Vector2(Vector3.Dot(local, axes.right), Vector3.Dot(local, axes.forward));
        if (aim.sqrMagnitude < 0.0001f) return false;
        aim.Normalize();
        Vector2 drag = ghostThrower.slingshotStyle ? -aim : aim;       // y up

        startPx = new Vector2(cGui.x * scale, Screen.height - cGui.y * scale);
        float frac = ghostThrower.PullFractionFor(longPower, startPx, drag);
        float lenGui = frac * Mathf.Min(Screen.width, Screen.height) / scale;
        Vector2 rGui = cGui + new Vector2(drag.x, -drag.y) * lenGui;
        releasePx = new Vector2(rGui.x * scale, Screen.height - rGui.y * scale);
        startR = StartRadiusUi * scale;
        releaseR = ReleaseRadiusUi * scale;
        return true;
    }

    // ---- drawing ----

    // A rounded rectangle, filled or only its outline (Unity's own border drawing: the corners are really round, whatever the size).
    static void RoundFill(Rect r, float radius, Color c)
    {
        float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) * 0.5f);
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, new Vector4(rad, rad, rad, rad));
    }

    static void RoundOutline(Rect r, float thick, float radius, Color c)
    {
        float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) * 0.5f);
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, new Vector4(thick, thick, thick, thick), new Vector4(rad, rad, rad, rad));
    }

    // The dark overlay's window: a soft-edged, round-cornered hole (dark all round, clear in the middle, feathered between).
    // The texture is the corner of that frame; Frame9 stretches it to any size.
    static Texture2D frameTex;
    const float FrameB = 32f, FrameRadius = 14f;      // texture border (UI units when drawn) and the radius of the hole's corners

    static Texture2D FrameTexture()
    {
        if (frameTex != null) return frameTex;
        const int n = 64;
        frameTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        frameTex.wrapMode = TextureWrapMode.Clamp;
        frameTex.filterMode = FilterMode.Bilinear;
        frameTex.hideFlags = HideFlags.HideAndDontSave;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - n * 0.5f), dy = Mathf.Abs(y + 0.5f - n * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - FrameRadius;                // distance outside the hole
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / (n * 0.5f - FrameRadius))) * DimAlpha;
                px[y * n + x] = new Color32(6, 8, 20, (byte)(a * 255f));
            }
        frameTex.SetPixels32(px);
        frameTex.Apply(false);
        return frameTex;
    }

    // Dims the whole screen except a soft window over `hole` (UI units).
    static void DimWithWindow(float w, float h, Rect hole, float strength = 1f)
    {
        float pad = FrameB - FrameRadius;
        Rect r = new Rect(hole.x - pad, hole.y - pad, hole.width + 2f * pad, hole.height + 2f * pad);
        Color dim = new Color(6f / 255f, 8f / 255f, 20f / 255f, DimAlpha * strength);
        Fill(new Rect(0f, 0f, w, Mathf.Max(0f, r.y)), dim);
        Fill(new Rect(0f, r.yMax, w, Mathf.Max(0f, h - r.yMax)), dim);
        Fill(new Rect(0f, r.y, Mathf.Max(0f, r.x), r.height), dim);
        Fill(new Rect(r.xMax, r.y, Mathf.Max(0f, w - r.xMax), r.height), dim);

        var tex = FrameTexture();
        GUI.color = new Color(1f, 1f, 1f, strength);
        float b = FrameB, w2 = Mathf.Max(0f, r.width - 2f * b), h2 = Mathf.Max(0f, r.height - 2f * b);
        const float m = 0.015f;
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, b, b), tex, new Rect(0f, 0.5f, 0.5f, 0.5f));
        GUI.DrawTextureWithTexCoords(new Rect(r.xMax - b, r.y, b, b), tex, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - b, b, b), tex, new Rect(0f, 0f, 0.5f, 0.5f));
        GUI.DrawTextureWithTexCoords(new Rect(r.xMax - b, r.yMax - b, b, b), tex, new Rect(0.5f, 0f, 0.5f, 0.5f));
        if (w2 > 0f)
        {
            GUI.DrawTextureWithTexCoords(new Rect(r.x + b, r.y, w2, b), tex, new Rect(0.5f - m, 0.5f, 2f * m, 0.5f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x + b, r.yMax - b, w2, b), tex, new Rect(0.5f - m, 0f, 2f * m, 0.5f));
        }
        if (h2 > 0f)
        {
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y + b, b, h2), tex, new Rect(0f, 0.5f - m, 0.5f, 2f * m));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - b, r.y + b, b, h2), tex, new Rect(0.5f, 0.5f - m, 0.5f, 2f * m));
        }
    }

    // The screen rectangle (GUI units) of one floor of the tower: 0 = ground floor (with the lawn in front), 1 = middle, 2 = top.
    bool FloorRect(House h, int f, float scale, out Rect r)
    {
        r = new Rect(0f, 0f, 0f, 0f);
        var cam = Camera.main;
        if (cam == null || h == null) return false;
        float y0 = f <= 0 ? 0f : House.GroundStoreyH + (f - 1) * House.StoreyH;
        float y1 = House.GroundStoreyH + f * House.StoreyH;
        float uNear = f <= 0 ? 3.2f : 6.9f, uFar = 8.45f;
        float L = Mathf.Max(3f, h.LotLength);
        float zLo = -L * 0.5f, zHi = L * 0.5f;
        if (f >= 1 && h.balconies != null)
        {
            // an upper floor is lit where its balconies are (the middle one spans both of its balconies)
            float lo = 1e9f, hi = -1e9f;
            for (int i = 0; i < h.balconies.Length; i++)
            {
                var bs = h.balconies[i];
                if (bs == null || !bs.on || bs.floor != f) continue;
                lo = Mathf.Min(lo, bs.z - bs.len * 0.5f); hi = Mathf.Max(hi, bs.z + bs.len * 0.5f);
            }
            if (hi > lo) { zLo = lo - 0.15f; zHi = hi + 0.15f; }
        }
        Vector3 hp = h.transform.position;
        float minX = 1e9f, minY = 1e9f, maxX = -1e9f, maxY = -1e9f;
        for (int a = 0; a < 8; a++)
        {
            float u = (a & 1) == 0 ? uNear : uFar;
            float dz = (a & 2) == 0 ? zLo : zHi;
            float y = (a & 4) == 0 ? y0 : y1;
            Vector3 s = cam.WorldToScreenPoint(new Vector3(hp.x + h.side * u, y, hp.z + dz));
            if (s.z <= 0.1f) return false;
            float gx = s.x / scale, gy = (Screen.height - s.y) / scale;
            minX = Mathf.Min(minX, gx); maxX = Mathf.Max(maxX, gx);
            minY = Mathf.Min(minY, gy); maxY = Mathf.Max(maxY, gy);
        }
        float sw = Screen.width / scale;
        minX = Mathf.Clamp(minX, 6f, sw - 12f); maxX = Mathf.Clamp(maxX, minX + 6f, sw - 6f);   // (a long house may run past the edge of the screen)
        r = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    // The overlay of the tower: it stands in a soft window, the ground and every balcony are outlined and lit one after the other, then the card
    // with the words and the green button.
    void DrawFloorsIntro(float w, float h, float scale, float insetT, float insetB)
    {
        cardRectScreen = new Rect(0f, 0f, Screen.width, Screen.height);   // every press belongs to this screen
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);

        var gm = GameManager.Instance;
        House tower = gm != null && gm.Spawner != null ? gm.Spawner.TutorialHouse(2) : null;

        var fr = new Rect[3];
        bool ok = tower != null;
        for (int f = 0; f < 3 && ok; f++) ok = FloorRect(tower, f, scale, out fr[f]);

        Rect all = new Rect(0f, 0f, 0f, 0f);
        if (ok)
        {
            all = fr[0];
            for (int f = 1; f < 3; f++) all = Rect.MinMaxRect(Mathf.Min(all.xMin, fr[f].xMin), Mathf.Min(all.yMin, fr[f].yMin), Mathf.Max(all.xMax, fr[f].xMax), Mathf.Max(all.yMax, fr[f].yMax));
            all = Rect.MinMaxRect(all.xMin - 10f, all.yMin - 10f, all.xMax + 10f, all.yMax + 10f);
            DimWithWindow(w, h, all);
        }
        else Fill(new Rect(0f, 0f, w, h), new Color(6f / 255f, 8f / 255f, 20f / 255f, DimAlpha));

        // what has to be cleared: the ground, then every balcony, each in its own outline, lit one after the other (no names, no numbers)
        if (ok)
        {
            var targets = new System.Collections.Generic.List<Rect>();
            targets.Add(fr[0]);
            if (tower.balconies != null)
                for (int i = 0; i < tower.balconies.Length; i++)
                {
                    Rect br;
                    var bs = tower.balconies[i];
                    if (bs != null && bs.on && BalconyRect(tower, bs, scale, out br)) targets.Add(br);
                }
            float t = Time.unscaledTime;
            int lit = (int)(t / 1.1f) % targets.Count;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
            for (int k = 0; k < targets.Count; k++)
            {
                Rect r = targets[k];
                r = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
                if (k == lit)
                {
                    // a soft glow round the lit one, then a tint and a bright outline
                    for (int g = 3; g >= 1; g--)
                        RoundOutline(new Rect(r.x - g * 3f, r.y - g * 3f, r.width + g * 6f, r.height + g * 6f), 3f, 12f + g * 3f,
                                     new Color(RingGold.r, RingGold.g, RingGold.b, (0.10f + 0.07f * pulse) / g));
                    RoundFill(r, 12f, new Color(RingGold.r, RingGold.g, RingGold.b, 0.14f + 0.10f * pulse));
                    RoundOutline(r, 3f, 12f, RingGold);
                }
                else RoundOutline(r, 2f, 12f, new Color(1f, 1f, 1f, 0.45f));
            }
        }
        GUI.color = Color.white;

        // the card goes on whichever side of the tower has more room
        float cw = Mathf.Min(w - 32f, portraitWidthCap(w));
        float ch = 252f;
        bool towerHigh = ok && all.center.y < h * 0.5f;
        float cy = towerHigh ? h - ch - 24f - insetB : Mathf.Max(insetT + 30f, h * 0.06f);
        Rect card = new Rect((w - cw) * 0.5f, cy, cw, ch);
        DrawNote(card, "Clear every balcony", "Clear all balconies and the ground. Miss one and you lose a life.", 100f);
        if (GreenButton(new Rect(card.center.x - 110f, card.yMax - 82f, 220f, 54f), "Got it"))
        {
            floorsIntro = false;
            longLesson = true;
            nearCached = false;
            stepTime = 0f;
            GameTime.Lesson = LessonSlow;
            GameTime.Apply();
            cardRectScreen = new Rect(0f, 0f, 0f, 0f);
        }
    }

    // The screen rectangle (GUI units) round one balcony: its floor, its rail and the space above the floor.
    bool BalconyRect(House h, House.BalconySet b, float scale, out Rect r)
    {
        r = new Rect(0f, 0f, 0f, 0f);
        var cam = Camera.main;
        if (cam == null || h == null || b == null) return false;
        float baseY = House.GroundStoreyH + (b.floor - 1) * House.StoreyH;
        Vector3 hp = h.transform.position;
        float minX = 1e9f, minY = 1e9f, maxX = -1e9f, maxY = -1e9f;
        for (int a = 0; a < 8; a++)
        {
            float u = (a & 1) == 0 ? 6.9f : 8.5f;
            float dz = (a & 2) == 0 ? b.z - b.len * 0.5f - 0.1f : b.z + b.len * 0.5f + 0.1f;
            float y = (a & 4) == 0 ? baseY + 0.3f : baseY + 2.0f;
            Vector3 s = cam.WorldToScreenPoint(new Vector3(hp.x + h.side * u, y, hp.z + dz));
            if (s.z <= 0.1f) return false;
            float gx = s.x / scale, gy = (Screen.height - s.y) / scale;
            minX = Mathf.Min(minX, gx); maxX = Mathf.Max(maxX, gx);
            minY = Mathf.Min(minY, gy); maxY = Mathf.Max(maxY, gy);
        }
        r = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    // The screen rectangle (GUI units) round the building's basket: the ring, its board and a little wall around them.
    bool HoopRect(House h, float scale, out Rect r)
    {
        r = new Rect(0f, 0f, 0f, 0f);
        var cam = Camera.main;
        var ring = h != null && h.floorHoopZ != null ? h.floorHoopZ[1] : null;
        if (cam == null || ring == null || !ring.gameObject.activeInHierarchy) return false;
        Vector3 c = ring.transform.position, hp = h.transform.position;
        float minX = 1e9f, minY = 1e9f, maxX = -1e9f, maxY = -1e9f;
        for (int a = 0; a < 8; a++)
        {
            float u = (a & 1) == 0 ? 7.2f : 8.5f;
            float dz = (a & 2) == 0 ? -1.1f : 1.1f;
            float y = (a & 4) == 0 ? c.y - 0.8f : c.y + 1.5f;
            Vector3 s = cam.WorldToScreenPoint(new Vector3(hp.x + h.side * u, y, c.z + dz));
            if (s.z <= 0.1f) return false;
            float gx = s.x / scale, gy = (Screen.height - s.y) / scale;
            minX = Mathf.Min(minX, gx); maxX = Mathf.Max(maxX, gx);
            minY = Mathf.Min(minY, gy); maxY = Mathf.Max(maxY, gy);
        }
        r = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    // During the basket shot: the colours of everything are dimmed, except the board and the ring (no ring drawn round them).
    // Lighter than the overlay's dark, so the bike, the circles and the flight of the paper can still be followed.
    void DrawHoopDim(float w, float h, float scale)
    {
        GUI.color = Color.white;
        Fill(new Rect(0f, 0f, w, h), new Color(6f / 255f, 8f / 255f, 20f / 255f, DimAlpha * 0.40f));   // (no lit window: the board's own glow is what stands out)
    }

    // The basket overlay: everything dark but the basket, which glows, and the card with the words and the green button.
    void DrawHoopIntro(float w, float h, float scale, float insetT, float insetB)
    {
        cardRectScreen = new Rect(0f, 0f, Screen.width, Screen.height);   // every press belongs to this screen
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);

        var gm = GameManager.Instance;
        House tower = gm != null && gm.Spawner != null ? gm.Spawner.TutorialHouse(2) : null;
        Rect hr;
        bool ok = HoopRect(tower, scale, out hr);
        Fill(new Rect(0f, 0f, w, h), new Color(6f / 255f, 8f / 255f, 20f / 255f, DimAlpha * 0.7f));   // (no lit window: the board glows by itself)
        GUI.color = Color.white;

        float cw = Mathf.Min(w - 32f, portraitWidthCap(w));
        float ch = 222f;
        bool basketHigh = ok && hr.center.y < h * 0.5f;
        float cy = basketHigh ? h - ch - 24f - insetB : Mathf.Max(insetT + 30f, h * 0.06f);
        Rect card = new Rect((w - cw) * 0.5f, cy, cw, ch);
        DrawNote(card, "Clear all balconies at once", "Score the basket to clear every balcony.", 70f);
        if (GreenButton(new Rect(card.center.x - 110f, card.yMax - 82f, 220f, 54f), "Got it"))
        {
            hoopIntro = false;
            longLesson = true;
            nearCached = false;
            stepTime = 0f;
            GameTime.Lesson = LessonSlow;
            GameTime.Apply();
            cardRectScreen = new Rect(0f, 0f, 0f, 0f);
        }
    }

    // The green button (white text with a dark outline, like the menu buttons).
    bool GreenButton(Rect b, string text)
    {
        return UiTheme.Tinted(b, text, 30, UiTheme.Tone.Green);
    }

    // After the long throw: the ground-floor prompt, and the praise.
    void DrawLongAfter(float w, float h, float scale, float insetT)
    {
        cardRectScreen = new Rect(0f, 0f, 0f, 0f);
        extraRectScreen = new Rect(0f, 0f, 0f, 0f);
        float top = Mathf.Max(insetT + 30f, h * 0.10f);

        if (groundPhase && !transitioning)
        {
            float cw = Mathf.Min(w - 32f, 480f);
            Rect card = new Rect((w - cw) * 0.5f, top + 6f, cw, 150f);
            DrawNote(card, "Yard area", "Hit any glowing spot.", 62f);
            cardRectScreen = new Rect(card.x * scale, card.y * scale, card.width * scale, card.height * scale);
        }

        if (!string.IsNullOrEmpty(praise) && Time.unscaledTime < praiseUntil)
        {
            float a = Mathf.Clamp01((praiseUntil - Time.unscaledTime) * 2.5f);
            float rise = (1f - Mathf.Clamp01((praiseUntil - Time.unscaledTime) / 1.4f)) * 30f;
            Color keep = praiseS.normal.textColor;
            praiseS.normal.textColor = new Color(keep.r, keep.g, keep.b, a);
            UiTheme.Label(new Rect(0f, top + 150f - rise, w, 64f), praise, praiseS, 2f);
            praiseS.normal.textColor = keep;
        }
    }
}
