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
    public static Vector3 AimPoint;   // where the previewed arc ends (the camera zooms towards it)   // how hard the current pull is (0..1), read by the camera for its aim zoom
    Transform landingOrb;      // glowing ball where the previewed arc ends
    Renderer landingOrbR;
    readonly Gradient arcGradient = new Gradient();
    readonly GradientColorKey[] arcColorKeys = new GradientColorKey[5];
    readonly GradientAlphaKey[] arcAlphaKeys = new GradientAlphaKey[5];
    LineRenderer groundLine;   // straight line on the ground under the arc, along the direction of the throw
    Vector2 startPos;
    float pressTime;
    bool aiming;
    bool dragging;
    int lastBucket;
    readonly Vector3[] pts = new Vector3[100];

    Vector3 ThrowOrigin { get { return transform.position + Vector3.up * 1.6f; } }

    // Pull sizes are measured against the SHORT screen edge, so a pull feels the same in portrait and landscape.
    float RefSize { get { return Mathf.Min(Screen.width, Screen.height); } }

    void Awake()
    {
        // the pull shape comes from GameConfig, not from values saved in the scene
        launchAngleLight = GameConfig.LaunchAngleLight;
        launchAngleFull = GameConfig.LaunchAngleFull;
        powerCurve = GameConfig.PowerCurve;
        bike = GetComponent<BikeForward>();
        line = gameObject.AddComponent<LineRenderer>();
        line.material = Mat.Particle();
        line.widthMultiplier = 0.26f;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.useWorldSpace = true;

        var gl = new GameObject("GroundAimLine");
        gl.transform.SetParent(transform, false);
        groundLine = gl.AddComponent<LineRenderer>();
        groundLine.material = Mat.Particle();
        groundLine.widthMultiplier = 0.22f;
        groundLine.useWorldSpace = true;
        groundLine.positionCount = 2;
        groundLine.numCapVertices = 4;

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

    void SetLines(bool on)
    {
        line.enabled = on;
        if (groundLine != null) groundLine.enabled = on;
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
        GameTime.Aim = 1f;
        GameTime.Apply();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.Playing) { StopAiming(); wasRaining = false; return; }

        // the moment rain starts (or a run begins in the rain) the player is told that throws now fall short
        bool rainNow = RainPenaltyOn;
        if (rainNow && !wasRaining) gm.ShowToast("Rain! Wet papers fall " + Mathf.RoundToInt(GameConfig.RainThrowPenalty * 100f) + "% short", 4f);
        wasRaining = rainNow;

        if (!aiming)
        {
            gm.SlowMoMeter = Mathf.Min(1f, gm.SlowMoMeter + refillPerSecond * Time.unscaledDeltaTime);
            if (Input.GetMouseButtonDown(0) && !GameUI.BlocksInput(Input.mousePosition))
            {
                aiming = true;
                dragging = false;
                startPos = Input.mousePosition;
                pressTime = Time.unscaledTime;
                lastBucket = 0;
            }
            return;
        }

        Vector2 pos = Input.mousePosition;
        dragging = (pos - startPos).magnitude > RefSize * tapMaxMove;

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
            if (dragging && valid)
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
    float PullCap(Vector2 dir)
    {
        float cap = fullPowerDrag * RefSize;
        if (dir.sqrMagnitude < 1e-6f) return cap;
        dir.Normalize();
        float margin = RefSize * 0.04f;
        Rect safe = Screen.safeArea;
        float room = float.MaxValue;
        if (dir.x > 0.001f) room = Mathf.Min(room, (safe.xMax - margin - startPos.x) / dir.x);
        else if (dir.x < -0.001f) room = Mathf.Min(room, (safe.xMin + margin - startPos.x) / dir.x);
        if (dir.y > 0.001f) room = Mathf.Min(room, (safe.yMax - margin - startPos.y) / dir.y);
        else if (dir.y < -0.001f) room = Mathf.Min(room, (safe.yMin + margin - startPos.y) / dir.y);
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
        Vector3 heading = transform.right * aim.x + transform.forward * aim.y;
        var view = CameraFollow.Instance;
        if (view != null && !view.firstPerson) heading = Quaternion.Euler(0f, view.yaw, 0f) * heading;   // follow the camera's turn: in the side view, screen right is along the road
        heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : transform.forward;

        float distance = Mathf.Lerp(minDistance, maxDistance, power);
        if (wet) distance *= 1f - GameConfig.RainThrowPenalty;   // a wet paper lands short
        float th = Mathf.Lerp(launchAngleLight, launchAngleFull, power) * Mathf.Deg2Rad;   // the harder the pull, the higher the arc
        float speed = SpeedForDistance(distance, th);

        Vector3 v = bike.Velocity * forwardInherit
                  + heading * speed * Mathf.Cos(th)
                  + Vector3.up * speed * Mathf.Sin(th);
        if (valid && assist && !Tutorial.Active) v = ApplyAssist(v);   // the tutorial teaches the plain throw
        return v;
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
                q += w * dt;
                total = k;
                if (q.y < 0.05f) break;
            }
        }
        int limit = Mathf.Max(2, Mathf.CeilToInt(total * Mathf.Clamp01(Tutorial.Active ? 1f : GameConfig.PreviewShare)));   // (the tutorial always shows the whole arc)

        Vector3 p = ThrowOrigin;
        int n = 0;
        pts[n++] = p;
        for (int i = 1; i <= 170 && i <= limit && n < pts.Length; i++)
        {
            Vector3 prev = p;
            v += Physics.gravity * dt;
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

        // The same line on the ground: from under the hand to where the arc ends, so the direction of the throw reads clearly.
        // It carries the same depth taper, and a faint colour so the arc stays the main cue.
        Vector3 g0 = new Vector3(ThrowOrigin.x, 0.07f, ThrowOrigin.z);
        Vector3 g1 = new Vector3(pts[n - 1].x, 0.07f, pts[n - 1].z);
        groundLine.SetPosition(0, g0);
        groundLine.SetPosition(1, g1);
        Color gc = valid ? new Color(0.6f, 0.95f, 1f, 0.4f) : new Color(1f, 1f, 1f, 0.2f);
        groundLine.startColor = gc;
        groundLine.endColor = gc;
        groundLine.widthCurve = wc;

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
        if (!gm.TryUsePaper()) { gm.ShowToast("No papers!"); return; }

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
        if (GameConfig.ThrowBoost > 1.01f) paper.BeginBoost(v, FlightTime(v), GameConfig.ThrowBoost, GameConfig.ThrowBoostShare);   // the first part of the arc is flown faster
        else rb.AddForce(v, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);

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
