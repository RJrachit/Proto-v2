using UnityEngine;

// Slingshot throwing. Press anywhere, pull, release: the paper flies in the exact opposite direction of your pull.
//   Pull DIRECTION sets the heading on the ground (screen left/right = road sideways, screen up/down = along the road).
//   Pull LENGTH sets how far away the paper lands (minDistance .. maxDistance); the launch angle rises with the pull (light = low toss, full = high lob).
// While pulling, time slows (limited meter) and an arc previews the throw.
// A quick tap with no pull rings the bell instead.
// Mouse input doubles as touch input, so this works in the editor and on Android.
[RequireComponent(typeof(BikeForward))]
public class PaperThrower : MonoBehaviour
{
    [Header("Aim")]
    [Tooltip("On: pull back like a slingshot. Off: drag toward where you want the paper to go.")]
    public bool slingshotStyle = GameConfig.SlingshotStyle;
    [Tooltip("Pulls shorter than this (fraction of the short screen edge) cancel the throw.")]
    public float deadZone = GameConfig.DeadZone;
    [Tooltip("Pull length (fraction of the short screen edge) that gives the maximum distance.")]
    public float fullPowerDrag = GameConfig.FullPowerDrag;
    [Tooltip("Landing distance (units from the bike, along the pull direction) for the shortest valid pull.")]
    public float minDistance = GameConfig.MinThrowDistance;
    [Tooltip("Landing distance for a full-length pull.")]
    public float maxDistance = GameConfig.MaxThrowDistance;
    [Tooltip("Launch angle in degrees for a light pull. Lower = flatter and faster.")]
    [Range(10f, 80f)] public float launchAngleLight = GameConfig.LaunchAngleLight;
    [Tooltip("Launch angle in degrees for a full pull. Higher = a higher lob that comes down steeply.")]
    [Range(10f, 80f)] public float launchAngleFull = GameConfig.LaunchAngleFull;
    [Tooltip("Shapes the pull: 1 = linear, above 1 = fine control on short pulls, below 1 = short pulls already go far.")]
    public float powerCurve = GameConfig.PowerCurve;
    [Tooltip("How much of the bike's forward speed the paper keeps.")]
    public float forwardInherit = GameConfig.ForwardInherit;
    // how much of the bike's speed the paper keeps: none while "Cancel the bike's push" is on (the paper then goes exactly the way the pull points, in the world)
    float Inherit { get { return GameConfig.CancelBikePush ? 0f : forwardInherit; } }

    [Header("Aim assist")]
    [Tooltip("Nudges throws that would land close to a marked customer's door, mailbox or balcony so they land on it. The preview shows the assisted arc.")]
    public bool assist = GameConfig.AimAssist;
    [Tooltip("Only throws landing within this many units (along the road) of a target are nudged.")]
    public float assistRange = GameConfig.AssistRange;
    [Range(0f, 1f)] public float assistStrength = GameConfig.AssistStrength;

    [Header("Slow motion while pulling")]
    public float slowScale = GameConfig.SlowMotionScale;
    public float drainPerSecond = GameConfig.SlowMotionDrainPerSecond;
    public float refillPerSecond = GameConfig.SlowMotionRefillPerSecond;

    [Header("Tap = bell")]
    public float tapMaxSeconds = GameConfig.TapMaxSeconds;
    [Tooltip("Movement below this (fraction of the short screen edge) still counts as a tap.")]
    public float tapMaxMove = GameConfig.TapMaxMove;

    bool wasRaining;

    // Wet papers fall short in the rain (never in the tutorial). Only the real throw is shortened: the aim guide still shows the dry arc, so the player has to judge it.
    static bool RainPenaltyOn { get { return DayCycle.Raining && !Tutorial.Active && GameConfig.RainThrowPenalty > 0f; } }

    BikeForward bike;
    LineRenderer line;
    public static float AimPower;
    public static bool AimActive;     // a valid pull is in progress
    public static bool Dragging;      // the finger is down and pulling (with or without slow-mo left)
    public static Vector3 AimPoint;   // where the previewed arc ends (the camera zooms towards it)   // how hard the current pull is (0..1), read by the camera for its aim zoom
    Transform landingOrb;      // glowing ball where the previewed arc ends
    Renderer landingOrbR;
    readonly Gradient arcGradient = new Gradient();
    readonly GradientColorKey[] arcColorKeys = new GradientColorKey[5];
    readonly GradientAlphaKey[] arcAlphaKeys = new GradientAlphaKey[5];
    System.Func<float, float, float> arrowFixedY, arrowGroundY;   // (kept as fields so nothing is allocated every frame)
    LineRenderer dropLine;       // the thin vertical line from the end of the arc down to the end of the arrow
    GroundArrow groundArrow;   // an arrow on the ground under the arc, along the direction of the throw
    Vector2 startPos;
    float pressTime;
    bool aiming;
    bool dragging;
    int lastBucket;
    readonly Vector3[] pts = new Vector3[100];

    Vector3 ThrowOrigin { get { return transform.position + Vector3.up * 1.6f; } }

    // Pull sizes are measured against the SHORT screen edge, so a pull feels the same in portrait and landscape.
    float ScreenShort { get { return Mathf.Min(Screen.width, Screen.height); } }

    // The size every pull length is a fraction of: the short screen edge, but never so big that the full pull needs more than MaxPullMm of finger
    // travel (a tablet). Phones are not affected (their full pull is already under the limit). Public: the tutorial's circles use it too.
    public float RefSize
    {
        get
        {
            float s = ScreenShort;
            float dpi = Screen.dpi;
            if (GameConfig.MaxPullMm > 0f && dpi >= 120f && dpi <= 700f && fullPowerDrag > 0.01f)   // (no believable DPI: the plain fraction)
                s = Mathf.Min(s, GameConfig.MaxPullMm / 25.4f * dpi / fullPowerDrag);
            return s;
        }
    }

    void Awake()
    {
        // the pull shape comes from GameConfig, not from values saved in the scene
        try { GameConfig.SlingshotStyle = PlayerPrefs.GetInt("ctl_sling", GameConfig.SlingshotStyle ? 1 : 0) == 1; } catch (System.Exception) { }   // the control choice from the dev menu
        slingshotStyle = GameConfig.SlingshotStyle;
        launchAngleLight = GameConfig.LaunchAngleLight;
        launchAngleFull = GameConfig.LaunchAngleFull;
        powerCurve = GameConfig.PowerCurve;
        deadZone = GameConfig.DeadZone;
        fullPowerDrag = GameConfig.FullPowerDrag;
        bike = GetComponent<BikeForward>();
        line = gameObject.AddComponent<LineRenderer>();
        line.material = Mat.Particle();
        line.widthMultiplier = 0.26f;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.useWorldSpace = true;

        groundArrow = new GroundArrow(transform);
        var dl = new GameObject("AimDropLine");
        dl.transform.SetParent(transform, false);
        dropLine = dl.AddComponent<LineRenderer>();
        dropLine.material = Mat.Particle();
        dropLine.useWorldSpace = true;
        dropLine.positionCount = 2;
        dropLine.widthMultiplier = 0.05f;
        dropLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dropLine.enabled = false;
        arrowFixedY = (x, z) => GameConfig.ArrowHeight - 0.10f;   // (the arrow's own lift is added on top)
        arrowGroundY = GroundY;

        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "AimLandingOrb";
        Destroy(orb.GetComponent<Collider>());
        landingOrb = orb.transform;
        landingOrb.SetParent(transform, false);
        landingOrbR = orb.GetComponent<Renderer>();
        landingOrbR.sharedMaterial = Mat.Particle();
        landingOrbR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        landingOrbR.receiveShadows = false;
        SetLines(false);
    }

    void OnDestroy() { if (groundArrow != null) groundArrow.Destroy(); }

    void SetLines(bool on)
    {
        line.enabled = on;
        if (groundArrow != null) groundArrow.SetEnabled(on);
        if (dropLine != null && !on) dropLine.enabled = false;
        if (landingOrbR != null) landingOrbR.enabled = on;
    }

    // Colour along the arc: cool cyan at the hand, through mint, to warm yellow at the far end. A weak pull stops at mint, a full pull reaches yellow.
    static Color ArcColor(float t, float power)
    {
        Color a = new Color(0.35f, 0.95f, 1f);
        Color b = new Color(0.55f, 1f, 0.5f);
        Color c = Color.Lerp(b, new Color(1f, 0.9f, 0.25f), Mathf.Clamp01(power));
        return t < 0.5f ? Color.Lerp(a, b, t * 2f) : Color.Lerp(b, c, (t - 0.5f) * 2f);
    }

    // Semi-transparent, with a bright pulse that keeps running from the hand along the arc: the way the paper will fly.
    void StyleArc(float power)
    {
        float p = Mathf.Repeat(Time.unscaledTime * 1.2f, 1.3f) - 0.15f;     // pulse position, a little beyond both ends
        float[] ts = { 0f, Mathf.Clamp01(p - 0.14f), Mathf.Clamp01(p), Mathf.Clamp01(p + 0.14f), 1f };
        for (int i = 1; i < 5; i++) ts[i] = Mathf.Max(ts[i], ts[i - 1] + 0.002f);
        for (int i = 3; i >= 0; i--) ts[i] = Mathf.Min(ts[i], ts[i + 1] - 0.002f);
        ts[0] = 0f;
        for (int i = 0; i < 5; i++)
        {
            float w = Mathf.Clamp01(1f - Mathf.Abs(ts[i] - p) / 0.14f);
            Color col = Color.Lerp(ArcColor(ts[i], power), Color.white, w * 0.6f);
            arcColorKeys[i] = new GradientColorKey(col, ts[i]);
            arcAlphaKeys[i] = new GradientAlphaKey(Mathf.Lerp(0.5f, 1f, w), ts[i]);
        }
        arcGradient.SetKeys(arcColorKeys, arcAlphaKeys);
        line.colorGradient = arcGradient;
    }

    void OnDisable()
    {
        AimPower = 0f;
        AimActive = false;
        Dragging = false;
        GameTime.Aim = 1f;
        GameTime.Apply();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.Playing) { StopAiming(); wasRaining = false; return; }

        // the moment rain starts (or a run begins in the rain) the player is told that throws now fall short
        bool rainNow = RainPenaltyOn;
        if (rainNow && !wasRaining) gm.ShowToast("Rain! Throws fall short.", 4f);
        wasRaining = rainNow;

        if (!aiming)
        {
            gm.SlowMoMeter = Mathf.Min(1f, gm.SlowMoMeter + refillPerSecond * Time.unscaledDeltaTime);
            if (Input.GetMouseButtonDown(0) && !GameUI.BlocksInput(Input.mousePosition))
            {
                aiming = true;
                dragging = false;
                startPos = Input.mousePosition;
                steadyInit = false;
                pressTime = Time.unscaledTime;
                lastBucket = 0;
            }
            return;
        }

        Vector2 pos = Input.mousePosition;
        dragging = (pos - startPos).magnitude > RefSize * tapMaxMove;
        Dragging = dragging;

        bool valid = false;
        float power = 0f;
        Vector3 v = Vector3.zero;

        if (dragging)
        {
            gm.SlowMoMeter = Mathf.Max(0f, gm.SlowMoMeter - drainPerSecond * Time.unscaledDeltaTime);
            GameTime.Aim = gm.SlowMoMeter > 0f ? slowScale : 1f;
            GameTime.Apply();

            v = ComputeVelocity(pos, out valid, out power);
            AimPower = power;
            SetLines(true);
            DrawPreview(v, valid, power);

            int bucket = (int)(power * 5f);
            if (valid && bucket != lastBucket)
            {
                lastBucket = bucket;
                Sfx.Play("pull", 0.5f, 0.8f + power * 0.8f);
                Haptics.Pulse(8);
            }
        }
        else
        {
            SetLines(false);
            AimActive = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            bool tap = !dragging && Time.unscaledTime - pressTime < tapMaxSeconds;
            if (dragging && valid && !Tutorial.AllowRelease(pos))
            {
                // the tutorial's near-shot lesson: the pull must end in the circle, so this one is not thrown
            }
            else if (dragging && valid)
            {
                bool v2; float p2;
                Throw(ComputeVelocity(pos, out v2, out p2, RainPenaltyOn));   // the throw itself carries the rain penalty; the preview above did not
            }
            else if (tap) gm.RingBell();
            StopAiming();
        }
    }

    // The longest pull (in pixels) that still counts, in the direction the finger is pulling: the comfortable pull (fullPowerDrag), or less when
    // the screen edge is closer than that, so full power can always be reached without the finger leaving the screen.
    float PullCap(Vector2 dir) { return PullCapFrom(startPos, dir); }

    float PullCapFrom(Vector2 start, Vector2 dir)
    {
        float cap = fullPowerDrag * RefSize;
        if (dir.sqrMagnitude < 1e-6f) return cap;
        dir.Normalize();
        float margin = ScreenShort * 0.04f;   // (the real screen edge, not the capped size)
        Rect safe = Screen.safeArea;
        float room = float.MaxValue;
        if (dir.x > 0.001f) room = Mathf.Min(room, (safe.xMax - margin - start.x) / dir.x);
        else if (dir.x < -0.001f) room = Mathf.Min(room, (safe.xMin + margin - start.x) / dir.x);
        if (dir.y > 0.001f) room = Mathf.Min(room, (safe.yMax - margin - start.y) / dir.y);
        else if (dir.y < -0.001f) room = Mathf.Min(room, (safe.yMin + margin - start.y) / dir.y);
        float floor = Mathf.Min(cap, Mathf.Max(MinCapFraction(), deadZone + 0.05f) * RefSize);
        return Mathf.Clamp(room, floor, cap);
    }

    static float MinCapFraction() { return GameConfig.MinFullPowerDrag; }

    // Pull direction -> heading, pull length -> landing distance, pull -> launch angle, then angle + distance -> speed (solved below).
    Vector3 ComputeVelocity(Vector2 current, out bool valid, out float power, bool wet = false)
    {
        Vector2 d = current - startPos;
        Vector2 aim = slingshotStyle ? -d : d;           // throw opposite to the drag

        float length = aim.magnitude / RefSize;
        valid = length > deadZone;
        float capFraction = PullCap(d) / RefSize;   // the comfortable full pull, or less near a screen edge
        power = Mathf.Pow(Mathf.InverseLerp(deadZone, capFraction, length), powerCurve);

        // Heading on the ground: screen X -> road sideways, screen Y -> along the road (turned with the camera, see below)
        if (aim.sqrMagnitude > 1e-4f) aim = SteadyDir(aim.normalized, length) * aim.magnitude;   // (a short pull's heading is smoothed, see SteadyDir)
        Vector3 heading = transform.right * aim.x + transform.forward * aim.y;
        var view = CameraFollow.Instance;
        if (view != null && !view.firstPerson) heading = Quaternion.Euler(0f, view.yaw, 0f) * heading;   // follow the camera's turn: in the side view, screen right is along the road
        heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : transform.forward;
        if (GameConfig.ScreenTrueAim && aim.sqrMagnitude > 1e-4f) heading = ScreenTrueHeading(aim, heading);

        float distance = Mathf.Lerp(minDistance, maxDistance, power);
        if (wet) distance *= 1f - GameConfig.RainThrowPenalty;   // a wet paper lands short
        float th = Mathf.Lerp(launchAngleLight, launchAngleFull, power) * Mathf.Deg2Rad;   // the harder the pull, the higher the arc
        float speed = SpeedForDistance(distance, th);

        Vector3 v = bike.Velocity * Inherit
                  + heading * speed * Mathf.Cos(th)
                  + Vector3.up * speed * Mathf.Sin(th);
        if (valid && assist && !Tutorial.Active) v = ApplyAssist(v);   // the tutorial teaches the plain throw
        return v;
    }

    // The ground heading that LOOKS exactly opposite the drag on the screen. The camera is not straight behind the thrower and looks down the road in
    // perspective, so "screen up = along the road" drifts a little sideways; here the screen direction of the throw is followed from the thrower's own
    // screen spot down to the ground instead. (The camera and the thrower stay where they are.) fallback: the plain heading, when the ray misses the ground.
    Vector3 ScreenTrueHeading(Vector2 aimPx, Vector3 fallback)
    {
        var cam = Camera.main;
        if (cam == null) return fallback;
        Vector3 o = ThrowOrigin;
        Vector3 sp = cam.WorldToScreenPoint(o);
        if (sp.z <= 0.1f) return fallback;
        Vector2 step = aimPx.normalized * 60f;
        Ray ray = cam.ScreenPointToRay(new Vector3(sp.x + step.x, sp.y + step.y, 0f));
        var plane = new Plane(Vector3.up, new Vector3(0f, o.y, 0f));
        float t;
        if (!plane.Raycast(ray, out t)) return fallback;
        Vector3 h = ray.GetPoint(t) - o;
        h.y = 0f;
        return h.sqrMagnitude > 0.0001f ? h.normalized : fallback;
    }

    // A short pull turns the heading a lot for a tiny wobble of the finger, so the heading is smoothed the shorter the pull is
    // (GameConfig.AimSteady, below AimSteadyBelow). The smoothing runs once per frame: the preview and the throw itself see the same heading.
    Vector2 steadyDir;
    int steadyFrame = -1;
    bool steadyInit;

    Vector2 SteadyDir(Vector2 raw, float length)
    {
        if (steadyFrame == Time.frameCount) return steadyDir;
        steadyFrame = Time.frameCount;
        if (!steadyInit || GameConfig.AimSteady <= 0f) { steadyDir = raw; steadyInit = true; return steadyDir; }
        float shortness = 1f - Mathf.Clamp01(length / Mathf.Max(0.02f, GameConfig.AimSteadyBelow));
        float k = GameConfig.AimSteady * shortness;                         // 0 = follow the finger, towards 1 = slow and steady
        float a = 1f - Mathf.Pow(k, Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.1f) * 60f);   // the blend per frame (1 - k at 60 fps)
        Vector2 m = Vector2.Lerp(steadyDir, raw, a);
        steadyDir = m.sqrMagnitude > 1e-6f ? m.normalized : raw;
        return steadyDir;
    }

    static readonly RaycastHit[] groundHits = new RaycastHit[8];

    // The height of the highest surface under a spot (the thrower, papers and the like do not count). The ray starts at 0.8 m, below roofs and
    // inside a car or a fence, so the line never climbs onto those.
    float GroundY(float x, float z)
    {
        int cnt = Physics.RaycastNonAlloc(new Vector3(x, 0.8f, z), Vector3.down, groundHits, 1.2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        for (int i = 0; i < cnt; i++)
        {
            var c = groundHits[i].collider;
            if (c == null || c.GetComponentInParent<PaperThrower>() != null || c.GetComponent<Paper>() != null) continue;
            if (groundHits[i].point.y > best) best = groundHits[i].point.y;
        }
        return best > float.NegativeInfinity ? best : 0f;
    }

    // ---- for the tutorial's lessons (they work out where a pull has to end) ----

    // Where a paper leaves the hand.
    public Vector3 ThrowPoint { get { return ThrowOrigin; } }

    // The launch velocity for a throw of the given power (0..1) along a ground heading, made exactly as ComputeVelocity makes it (dry, no assist).
    public Vector3 VelocityFor(float power, Vector3 heading)
    {
        heading.y = 0f;
        heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : transform.forward;
        float distance = Mathf.Lerp(minDistance, maxDistance, power);
        float th = Mathf.Lerp(launchAngleLight, launchAngleFull, power) * Mathf.Deg2Rad;
        float speed = SpeedForDistance(distance, th);
        return bike.Velocity * Inherit + heading * speed * Mathf.Cos(th) + Vector3.up * speed * Mathf.Sin(th);
    }

    // The pull length (as a fraction of the short screen edge) that gives this power, for a drag starting at startPx (y up) in direction dragDir.
    public float PullFractionFor(float power, Vector2 startPx, Vector2 dragDir)
    {
        float capFraction = PullCapFrom(startPx, dragDir) / RefSize;
        float t = Mathf.Pow(Mathf.Clamp01(power), 1f / Mathf.Max(0.1f, powerCurve));
        return deadZone + t * (capFraction - deadZone);
    }

    // Launch speed so a paper thrown from ThrowOrigin's height at angle th lands `distance` away on the ground.
    // Range(v) = v*cos(th) * (v*sin(th) + sqrt((v*sin(th))^2 + 2*g*h)) / g, which only grows with v, so bisect.
    float SpeedForDistance(float distance, float th)
    {
        float g = -Physics.gravity.y;
        float h = ThrowOrigin.y;
        float lo = 0.5f, hi = 60f;
        for (int i = 0; i < 24; i++)
        {
            float v = (lo + hi) * 0.5f;
            float vy = v * Mathf.Sin(th);
            float range = v * Mathf.Cos(th) * (vy + Mathf.Sqrt(vy * vy + 2f * g * h)) / g;
            if (range < distance) lo = v; else hi = v;
        }
        return (lo + hi) * 0.5f;
    }

    // Aim assist: follow the arc (the same way the physics will). If it passes through the height and distance of a
    // customer's door, mailbox or balcony but is a bit off along the road, nudge the forward speed so it lines up.
    // Windows are never assisted. Only the forward speed changes, so the height and distance you chose are kept.
    Vector3 ApplyAssist(Vector3 v)
    {
        Vector3 p = ThrowOrigin;
        Vector3 vel = v;
        const float dt = 0.02f;
        float t = 0f;
        for (int i = 0; i < 170; i++)
        {
            vel += Physics.gravity * dt;
            vel *= Mathf.Exp(-GameConfig.AirDrag * dt);
            p += vel * dt;
            t += dt;
            if (p.y < 0f) break;

            foreach (var house in House.Customers)
            {
                if (house == null || house.Resolved || house.zones == null || house.Blocked) continue;
                foreach (var zone in house.zones)
                {
                    if (zone == null || zone.kind == HouseTarget.Kind.Window || zone.kind == HouseTarget.Kind.Breakable) continue;
                    if (!zone.gameObject.activeInHierarchy) continue;
                    if (!house.ZoneOpen(zone)) continue;   // served balconies, floors and ground floor need no help

                    Vector3 c = zone.transform.position;
                    Vector3 half = zone.size * 0.5f;
                    // the vertical margin covers one simulation step, so thin zones are not skipped over
                    if (Mathf.Abs(p.x - c.x) > half.x || Mathf.Abs(p.y - c.y) > half.y + 0.25f) continue;

                    float err = Vector3.Dot(c - p, transform.forward);   // positive = target is further along the road
                    // A long target (the lawn) needs no nudge while the paper is already well inside it.
                    float slack = zone.size.z * 0.25f;
                    float over = Mathf.Max(0f, Mathf.Abs(err) - slack) * Mathf.Sign(err);
                    float range = assistRange;
                    float strength = assistStrength;
                    if (Mathf.Abs(err) < range)
                        return v + transform.forward * (over / t) * strength;
                }
            }
        }
        return v;
    }

    void DrawPreview(Vector3 v, bool valid, float power)
    {
        if (valid) StyleArc(power);
        else
        {
            Color c = new Color(1f, 1f, 1f, 0.25f);
            line.startColor = c;
            line.endColor = c;
        }

        const float dt = 0.02f;   // same step as the physics, so the preview matches the real throw
        // How much of the arc to show (GameConfig.PreviewShare: 25 / 50 / 75 / 100 %), counted on the free flight to the ground.
        int total = 0;
        {
            Vector3 q = ThrowOrigin, w = v;
            for (int k = 1; k <= 170; k++)
            {
                w += Physics.gravity * dt;
                w *= Mathf.Exp(-GameConfig.AirDrag * dt);
                q += w * dt;
                total = k;
                if (q.y < 0.05f) break;
            }
        }
        int limit = Mathf.Max(2, Mathf.CeilToInt(total * Mathf.Clamp01(Tutorial.Active ? GameConfig.TutorialPreviewShare : GameConfig.PreviewShare)));   // (the how-to-play lessons have their own share)

        Vector3 p = ThrowOrigin;
        int n = 0;
        pts[n++] = p;
        for (int i = 1; i <= 170 && i <= limit && n < pts.Length; i++)
        {
            Vector3 prev = p;
            v += Physics.gravity * dt;
            v *= Mathf.Exp(-GameConfig.AirDrag * dt);
            p += v * dt;

            // The line stops at the first thing the paper would touch (a wall, a backboard, the ground).
            // It does not show the bounce: that is for the player to judge.
            Vector3 step = p - prev;
            RaycastHit hit;
            bool blocked = Physics.Raycast(prev, step.normalized, out hit, step.magnitude,
                                           Physics.AllLayers, QueryTriggerInteraction.Ignore);
            bool end = blocked || p.y < 0.05f;
            if (end) pts[n++] = blocked ? hit.point : new Vector3(p.x, 0.05f, p.z);
            else if (i % 2 == 0 || i == limit) pts[n++] = p;
            if (end) break;
        }
        line.positionCount = n;
        line.SetPositions(pts);

        // Depth cue: the line gets thinner towards the far end when it goes away from the camera and fatter when it comes towards it.
        // Across the screen stays even. (The camera may be turned sideways, so "away" follows the camera, not the road.)
        var view = CameraFollow.Instance;
        Vector3 away = (view != null && !view.firstPerson) ? Quaternion.Euler(0f, view.yaw, 0f) * Vector3.forward : transform.forward;
        float depth = Mathf.Clamp(Vector3.Dot(pts[n - 1] - ThrowOrigin, away) / Mathf.Max(1f, maxDistance), -1f, 1f);
        float endWidth = depth >= 0f ? 1f - 0.8f * depth : 1f + 1.8f * -depth;
        var wc = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, endWidth));
        line.widthCurve = wc;

        // The arrow on the ground: from under the hand to where the arc ends, so the direction of the throw reads clearly. It follows the ground
        // it runs over (pavement, kerbs, steps, court paint stand higher than the lawn), so it is never hidden under it.
        groundArrow.Draw(new Vector2(ThrowOrigin.x, ThrowOrigin.z), new Vector2(pts[n - 1].x, pts[n - 1].z), GameConfig.ArrowHeight > 0f ? arrowFixedY : arrowGroundY, valid, power);

        // the drop line: from the end of the arc straight down to the end of the arrow
        if (dropLine != null)
        {
            Vector3 top = pts[n - 1];
            float bottomY = GameConfig.ArrowHeight > 0f ? GameConfig.ArrowHeight : GroundY(top.x, top.z) + 0.1f;
            bool show = GameConfig.AimDropLine && valid && top.y - bottomY > 0.3f;
            dropLine.enabled = show;
            if (show)
            {
                dropLine.SetPosition(0, top);
                dropLine.SetPosition(1, new Vector3(top.x, bottomY, top.z));
                dropLine.startColor = new Color(1f, 1f, 1f, 0.15f);   // white, faint at the top, a little stronger where it meets the arrow
                dropLine.endColor = new Color(1f, 1f, 1f, 0.50f);
            }
        }

        AimPoint = pts[n - 1];
        AimActive = valid;

        // glowing ball where the arc ends, breathing gently
        if (landingOrbR != null)
        {
            landingOrbR.enabled = valid;
            landingOrb.position = pts[n - 1];
            float s = 0.5f + 0.1f * Mathf.Sin(Time.unscaledTime * 8f);
            landingOrb.localScale = Vector3.one * s;
            Color oc = ArcColor(1f, power);
            oc.a = 0.65f;
            landingOrbR.sharedMaterial.color = oc;
        }
    }

    // How long a paper launched with velocity v takes to fall to the ground, with normal gravity (the arc's own time).
    float FlightTime(Vector3 v)
    {
        float g = -Physics.gravity.y;
        return (v.y + Mathf.Sqrt(v.y * v.y + 2f * g * Mathf.Max(0f, ThrowOrigin.y))) / g;
    }

    void Throw(Vector3 v)
    {
        var gm = GameManager.Instance;
        if (!gm.TryUsePaper()) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Paper";
        go.transform.position = ThrowOrigin;
        go.transform.localScale = new Vector3(0.45f, 0.12f, 0.32f);
        var paperRenderer = go.GetComponent<Renderer>();
        Prim.FixMaterial(paperRenderer);
        paperRenderer.material.color = Color.white;

        var rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var paper = go.AddComponent<Paper>();
        go.AddComponent<ParcelGlow>();   // the parcel glows white, and the light shows through obstacles
        if (GameConfig.ThrowBoost > 1.01f) paper.BeginBoost(v, FlightTime(v), GameConfig.ThrowBoost, GameConfig.ThrowBoostShare);   // the first part of the arc is flown faster
        else rb.AddForce(v, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);

        // Console check: how far this arc should reach on plain physics (the same as the aim line), and the time state it was thrown in
        {
            Vector3 q = ThrowOrigin, w = v;
            for (int k = 0; k < 200 && q.y > 0.05f; k++) { w += Physics.gravity * 0.02f; w *= Mathf.Exp(-GameConfig.AirDrag * 0.02f); q += w * 0.02f; }
            Vector3 flat = q - ThrowOrigin; flat.y = 0f;
            paper.ThrowFrom = ThrowOrigin;
            paper.ThrowPredicted = flat.magnitude;
            paper.ThrowNote = "[timeScale " + Time.timeScale.ToString("0.00") + ", aim " + GameTime.Aim.ToString("0.00") + ", slow-mo left " + gm.SlowMoMeter.ToString("0.00") + "]";
        }

        Tutorial.OnThrow();
        Rider.LastThrowTime = Time.time;   // the rider's arm swings to the side the paper flies to
        Rider.LastThrowSide = Vector3.Dot(v, transform.right) >= 0f ? 1f : -1f;

        Sfx.Play("whoosh", 0.8f, Random.Range(0.95f, 1.1f));
        Haptics.Pulse(12);
    }

    void StopAiming()
    {
        if (!aiming) return;
        aiming = false;
        dragging = false;
        Dragging = false;
        AimPower = 0f;
        AimActive = false;
        SetLines(false);
        GameTime.Aim = 1f;
        GameTime.Apply();
    }

    // The slingshot band: a line from where you pressed to where your finger is now.
    void OnGUI()
    {
        if (!aiming || !dragging || Event.current.type != EventType.Repaint) return;

        GUI.matrix = Matrix4x4.identity;
        Vector2 a = new Vector2(startPos.x, Screen.height - startPos.y);
        Vector2 b = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float length = (b - a).magnitude;
        if (length < 8f) return;
        // the band stops at the longest pull that counts (the finger can go on, the band does not)
        length = Mathf.Min(length, PullCap(new Vector2(b.x - a.x, -(b.y - a.y))));
        b = a + (b - a).normalized * length;

        Texture2D white = Texture2D.whiteTexture;
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

        GUI.color = new Color(1f, 1f, 1f, 0.8f);
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - 3f, length, 6f), white);
        GUI.matrix = Matrix4x4.identity;
        GUI.DrawTexture(new Rect(a.x - 12f, a.y - 12f, 24f, 24f), white);
        GUI.color = Color.white;
    }
}
