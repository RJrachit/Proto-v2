using UnityEngine;

// Camera with two modes.
//   First person (optional, off by default): camera at the rider's eye looking down the road, the bike hidden, and only a handlebar
//   (cross bar, grips, stem, bell) fixed in the lower part of the screen.
//   Third person: fixed-angle chase camera.
// The field of view adapts to the screen shape, so portrait phones still see the houses at the sides.
// Pushes in briefly on hits (Kick) and widens the view during a hot streak.
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;

    [Header("Mode")]
    public bool firstPerson = false;

    [Header("Third person")]
    public Vector3 offset = GameConfig.CameraOffset;
    public float pitch = GameConfig.CameraPitch;
    [Tooltip("Turns the camera sideways (degrees). 0 = looking straight down the road.")]
    public float yaw = GameConfig.CameraYaw;
    public float baseFov = GameConfig.CameraFov;
    public float hotFovBonus = GameConfig.CameraHotStreakFovBonus;
    [Tooltip("The view is widened until at least this many degrees are visible across the screen (matters in portrait).")]
    public float minHorizontalFov = GameConfig.CameraMinHorizontalFov;
    public float maxVerticalFov = 135f;
    [Tooltip("Side view: how strongly the camera centres on a flying paper (1 = fully, 0 = off).")]
    public float flightFollow = GameConfig.CameraFlightFollow;
    public float flightMaxAhead = GameConfig.CameraFlightMaxAhead;

    [Header("First person")]
    [Tooltip("Eye position relative to the bike. The paper is thrown from y = 1.6, so keep it close.")]
    public Vector3 eyeOffset = new Vector3(0f, 1.7f, 0.2f);
    [Tooltip("Slight downward tilt so you see the road ahead and the house fronts.")]
    public float fpPitch = 10f;
    public float fpFov = 85f;
    public float fpMinHorizontalFov = 80f;
    [Tooltip("Cap on the vertical FOV, to avoid a fisheye look in portrait.")]
    public float fpMaxVerticalFov = 100f;
    [Tooltip("Gentle bob while riding. 0 = off.")]
    public float bobAmount = 0.03f;

    [Header("First person handlebar")]
    [Tooltip("Position relative to the camera. Lower y = lower on screen, smaller z = bigger.")]
    public Vector3 handlebarPos = new Vector3(0f, -0.48f, 0.95f);
    [Tooltip("Tilts the bar slightly toward you.")]
    public float handlebarTilt = 8f;

    Camera cam;
    float kick;
    float kickShown, kickVel;   // the kick as the camera uses it: eased in, so a hit never makes the view jump
    float lookAhead;
    float aimZoom;
    float aimZoomVel;
    float baseFovNow;
    Vector2 aimCentre;          // where on the screen the aim zoom is centred (-1..1)
    Vector3 aimPointLast;
    [Header("Aim zoom")]
    [Tooltip("The view narrows by this share while you aim in slow-mo, at a light pull.")]
    public float aimZoomLight = GameConfig.AimZoomLight;
    [Tooltip("... and at a full pull (smaller, so the far landing stays on screen).")]
    public float aimZoomFull = GameConfig.AimZoomFull;
    float focusZ, focusTimer;
    bool lastFirstPerson;
    bool visualApplied;
    Transform handlebar;
    Renderer stemRenderer;
    int lastSkin = -1;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        GameConfig.ChaseView = PlayerPrefs.GetInt(ModeKey, 0) == 1;
        ApplyBuiltInView();
        LoadView();
    }

    // ---- the saved third-person view (set up in the DEV menu's Camera tab; kept on the device between runs) ----

    const string ModeKey = "camview.mode";
    static string ViewKey { get { return GameConfig.ChaseView ? "camview.chase." : "camview."; } }   // each view keeps its own saved camera

    public bool HasSavedView { get { return PlayerPrefs.GetInt(ViewKey + "saved", 0) == 1; } }

    public void SaveView()
    {
        PlayerPrefs.SetFloat(ViewKey + "x", offset.x);
        PlayerPrefs.SetFloat(ViewKey + "y", offset.y);
        PlayerPrefs.SetFloat(ViewKey + "z", offset.z);
        PlayerPrefs.SetFloat(ViewKey + "pitch", pitch);
        PlayerPrefs.SetFloat(ViewKey + "yaw", yaw);
        PlayerPrefs.SetFloat(ViewKey + "fov", baseFov);
        PlayerPrefs.SetFloat(ViewKey + "minh", minHorizontalFov);
        PlayerPrefs.SetInt(ViewKey + "saved", 1);
        PlayerPrefs.Save();
    }

    void LoadView()
    {
        if (!HasSavedView) return;
        offset = new Vector3(PlayerPrefs.GetFloat(ViewKey + "x", offset.x), PlayerPrefs.GetFloat(ViewKey + "y", offset.y), PlayerPrefs.GetFloat(ViewKey + "z", offset.z));
        pitch = PlayerPrefs.GetFloat(ViewKey + "pitch", pitch);
        yaw = PlayerPrefs.GetFloat(ViewKey + "yaw", yaw);
        baseFov = PlayerPrefs.GetFloat(ViewKey + "fov", baseFov);
        minHorizontalFov = PlayerPrefs.GetFloat(ViewKey + "minh", minHorizontalFov);
    }

    // Put the camera at one of the preset views (not saved until "Save camera").
    public void ApplyView(Vector3 off, float pitchDeg, float yawDeg, float fov, float minHorizontal)
    {
        offset = off;
        pitch = pitchDeg;
        yaw = yawDeg;
        baseFov = fov;
        minHorizontalFov = minHorizontal;
    }

    // The built-in camera of the current view (side or chase).
    void ApplyBuiltInView()
    {
        if (GameConfig.ChaseView) ApplyView(GameConfig.ChaseOffset, GameConfig.ChasePitch, GameConfig.ChaseYaw, GameConfig.ChaseFov, GameConfig.ChaseMinHorizontalFov);
        else ApplyView(GameConfig.CameraOffset, GameConfig.CameraPitch, GameConfig.CameraYaw, GameConfig.CameraFov, GameConfig.CameraMinHorizontalFov);
    }

    // Forget the saved camera of this view and go back to its built-in one (GameConfig).
    public void ResetView()
    {
        PlayerPrefs.SetInt(ViewKey + "saved", 0);
        PlayerPrefs.Save();
        ApplyBuiltInView();
    }

    // Switch between the side view and the chase view (the road must be rebuilt after: the side view leaves out the right-hand houses).
    public void SetChase(bool chase)
    {
        GameConfig.ChaseView = chase;
        PlayerPrefs.SetInt(ModeKey, chase ? 1 : 0);
        PlayerPrefs.Save();
        ApplyBuiltInView();
        LoadView();
    }

    public void Kick(float amount) { kick = Mathf.Max(kick, amount); }

    // Vertical FOV: at least baseV, widened if needed to show minHorizontal degrees across the screen, never above cap.
    float Fov(float baseV, float minHorizontal, float cap)
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float fromWidth = 2f * Mathf.Atan(Mathf.Tan(minHorizontal * 0.5f * Mathf.Deg2Rad) / aspect) * Mathf.Rad2Deg;
        return Mathf.Min(Mathf.Max(baseV, fromWidth), cap);
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (!visualApplied || lastFirstPerson != firstPerson)
        {
            ApplyBikeVisibility();
            lastFirstPerson = firstPerson;
            visualApplied = true;
        }

        // keep the stem in the selected bike colour
        if (stemRenderer != null && lastSkin != Progress.SelectedSkin)
        {
            lastSkin = Progress.SelectedSkin;
            stemRenderer.material.color = Progress.Skins[lastSkin].color;
        }

        kick = Mathf.MoveTowards(kick, 0f, Time.unscaledDeltaTime * GameConfig.KickDecay);
        kickShown = Mathf.SmoothDamp(kickShown, kick, ref kickVel, 0.14f, float.MaxValue, Time.unscaledDeltaTime);
        var gm = GameManager.Instance;
        bool hot = gm != null && gm.HotStreak && gm.Playing;

        float fov = firstPerson ? Fov(fpFov, fpMinHorizontalFov, fpMaxVerticalFov)
                                : Fov(baseFov, minHorizontalFov, maxVerticalFov);
        // a little zoom while aiming (real time, so it stays smooth while the game slows); eases out when you let go.
        // It follows the pull itself, NOT the slow-mo meter: a zoom that came and went with the meter made the same pull look longer or
        // shorter on screen (the zoom magnifies the gap between the bike and the landing ball), which read as the throw changing.
        bool aimingSlow = gm != null && gm.Playing && PaperThrower.Dragging;
        // the zoom is smoothed by its own spring (quick in, a slower glide out) so it never jumps when you let go
        bool slowPower = PowerUps.Active(PowerKind.SlowZoom);
        float wantZoom = aimingSlow ? Mathf.Lerp(aimZoomLight, aimZoomFull, Mathf.Clamp01(PaperThrower.AimPower)) * (slowPower ? GameConfig.PowerZoomMult : 1f) : 0f;
        if (!aimingSlow) wantZoom = 0f;
        else if (!PaperThrower.AimActive) wantZoom = aimZoom;     // a pull that is too short to throw: hold the zoom as it is
        bool zoomFresh = aimZoom < 0.003f;
        aimZoom = Mathf.SmoothDamp(aimZoom, wantZoom, ref aimZoomVel, wantZoom > aimZoom ? 0.12f : (slowPower ? 0.9f : 0.5f), float.MaxValue, Time.unscaledDeltaTime);
        if (aimingSlow && PaperThrower.AimActive) aimPointLast = PaperThrower.AimPoint;
        float targetFov = fov + (hot ? hotFovBonus : 0f) - kickShown * GameConfig.KickFov;
        baseFovNow = baseFovNow <= 0f ? targetFov : Mathf.Lerp(baseFovNow, targetFov, Time.unscaledDeltaTime * 6f);
        cam.fieldOfView = baseFovNow;   // (the aim zoom is applied at the end, as a zoom into the aim point: the camera itself does not move)

        if (firstPerson)
        {
            float bob = 0f;
            if (gm != null && gm.Playing) bob = Mathf.Sin(Time.time * 9f) * bobAmount;
            transform.position = target.position + eyeOffset + Vector3.up * bob + Vector3.forward * kickShown * 0.2f;
            transform.rotation = Quaternion.Euler(fpPitch, 0f, 0f);
            cam.nearClipPlane = 0.05f;

            if (handlebar != null)
            {
                handlebar.localPosition = handlebarPos;
                handlebar.localRotation = Quaternion.Euler(handlebarTilt, 0f, 0f);
            }
        }
        else
        {
            // side view: while a paper is flying (and a moment after it lands) centre the view on it, so you can watch the throw finish even
            // though the bike keeps riding. The bike is never allowed to leave the screen: the camera only slides as far as keeps it in view.
            float wantAhead = 0f;
            if (flightFollow > 0f && Mathf.Abs(yaw) > 45f)
            {
                bool flying = false;
                for (int i = Paper.All.Count - 1; i >= 0; i--)
                {
                    var pp = Paper.All[i];
                    if (pp == null || !pp.InFlight) continue;
                    focusZ = pp.transform.position.z;
                    flying = true;
                    break;
                }
                if (flying) focusTimer = 1.4f; else focusTimer -= Time.unscaledDeltaTime;
                if (focusTimer > 0f)
                {
                    float vf = cam.fieldOfView * Mathf.Deg2Rad;
                    float hf = 2f * Mathf.Atan(Mathf.Tan(vf * 0.5f) * cam.aspect);
                    float halfWidth = Mathf.Abs(offset.x) * Mathf.Tan(hf * 0.5f);          // half the road length visible beside the bike
                    float limit = Mathf.Min(flightMaxAhead, Mathf.Max(0f, 0.85f * halfWidth - offset.z));
                    wantAhead = Mathf.Clamp((focusZ - (target.position.z + offset.z)) * flightFollow, 0f, limit);
                }
            }
            lookAhead = Mathf.Lerp(lookAhead, wantAhead, Time.unscaledDeltaTime * (wantAhead > lookAhead ? 5f : 2f));
            transform.position = target.position + offset + Vector3.forward * (kickShown * GameConfig.KickPush + lookAhead);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            cam.nearClipPlane = 0.3f;
        }

        // Aim zoom: magnify the view around the point where the arc ends, without moving the camera. That point stays where it is on the
        // screen and everything around it grows away from it. (Done on the projection matrix, so the camera, the bike's place on screen
        // and the framing of the road stay exactly as they were.)
        cam.ResetProjectionMatrix();
        if (aimZoom > 0.002f)
        {
            // the centre follows the aim point while you pull, and stays where it was once you let go (the zoom unwinds around it)
            if (aimingSlow && PaperThrower.AimActive)
            {
                Vector3 vp = cam.WorldToViewportPoint(aimPointLast);
                Vector2 want = vp.z > 0.1f ? new Vector2(Mathf.Clamp(vp.x * 2f - 1f, -0.9f, 0.9f), Mathf.Clamp(vp.y * 2f - 1f, -0.9f, 0.9f)) : Vector2.zero;
                aimCentre = zoomFresh ? want : Vector2.Lerp(aimCentre, want, Time.unscaledDeltaTime * 10f);
            }
            float s = 1f / (1f - Mathf.Clamp(aimZoom, 0f, 0.5f));
            Matrix4x4 m = cam.projectionMatrix;
            m[0, 0] *= s;
            m[1, 1] *= s;
            m[0, 2] += (s - 1f) * aimCentre.x;
            m[1, 2] += (s - 1f) * aimCentre.y;
            cam.projectionMatrix = m;
        }
    }

    // Hide the bike and rider in first person; show only a handlebar fixed to the camera.
    void ApplyBikeVisibility()
    {
        foreach (var r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (r is LineRenderer) continue;          // keep the aim arc visible
            r.enabled = !firstPerson;
        }

        if (firstPerson && handlebar == null) BuildHandlebar();
        if (handlebar != null) handlebar.gameObject.SetActive(firstPerson);
    }

    void BuildHandlebar()
    {
        var root = new GameObject("Handlebar");
        handlebar = root.transform;
        handlebar.SetParent(transform, false);
        handlebar.localPosition = handlebarPos;
        handlebar.localRotation = Quaternion.Euler(handlebarTilt, 0f, 0f);

        Color metal = new Color(0.20f, 0.20f, 0.22f);
        Color grip = new Color(0.08f, 0.08f, 0.08f);

        // cross bar (cylinder rotated to lie sideways)
        Prim.Make(PrimitiveType.Cylinder, "Bar", handlebar, Vector3.zero,
                  new Vector3(0.05f, 0.45f, 0.05f), metal, false, new Vector3(0f, 0f, 90f));

        // stem going down toward the front wheel, tinted with the selected bike colour
        var stem = Prim.Make(PrimitiveType.Cylinder, "Stem", handlebar, new Vector3(0f, -0.12f, 0.03f),
                             new Vector3(0.05f, 0.12f, 0.05f), metal);
        stemRenderer = stem.GetComponent<Renderer>();

        // grips at both ends
        Prim.Make(PrimitiveType.Cylinder, "GripL", handlebar, new Vector3(-0.42f, 0f, 0f),
                  new Vector3(0.07f, 0.09f, 0.07f), grip, false, new Vector3(0f, 0f, 90f));
        Prim.Make(PrimitiveType.Cylinder, "GripR", handlebar, new Vector3(0.42f, 0f, 0f),
                  new Vector3(0.07f, 0.09f, 0.07f), grip, false, new Vector3(0f, 0f, 90f));

        // small bell on the bar (matches "tap = bell")
        Prim.Make(PrimitiveType.Sphere, "Bell", handlebar, new Vector3(0.22f, 0.07f, 0f),
                  new Vector3(0.09f, 0.07f, 0.09f), new Color(0.85f, 0.85f, 0.9f));
    }
}
