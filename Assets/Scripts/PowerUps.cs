using UnityEngine;

// Power-ups: timed boosts the player picks up by hitting a floating badge with a paper (like the hearts).
//   Stack     - each throw sends three papers: the aimed one, and two a few degrees to either side.
//   Magnet    - a paper that comes within a few metres of an open basket ring is pulled to its middle (see Paper.MagnetPull).
//   SlowZoom  - slow-mo does not drain, and the aim zoom is stronger.
// Each one runs on its own timer (game time by default: slow-mo stretches it; DEV toggle for real seconds). The HUD shows an icon that drains from the top down (GameUI.Hud).
// Off in the tutorial. A new badge floats up ahead of the bike every 15-25 s, and now and then (about 1 time in 14) within 4 s.
public enum PowerKind { Stack = 0, Magnet = 1, SlowZoom = 2 }

public class PowerUps : MonoBehaviour
{
    public static PowerUps Instance { get; private set; }

    static readonly float[] left = new float[3];     // seconds left
    static readonly float[] total = new float[3];    // the length it started with (so the HUD can show a fraction)

    public static bool Enabled { get { return GameConfig.PowerUpsOn && !Tutorial.Active; } }
    // DEV menu: keep a power-up on all the time (no timer, no badge needed), to test it
    public static readonly bool[] Always = new bool[3];

    public static bool Active(PowerKind k) { return Enabled && (Always[(int)k] || left[(int)k] > 0f); }
    public static float Fraction(PowerKind k) { int i = (int)k; if (Always[i]) return 1f; return total[i] > 0f ? Mathf.Clamp01(left[i] / total[i]) : 0f; }
    public static float SecondsLeft(PowerKind k) { return Always[(int)k] ? 99f : left[(int)k]; }

    // The length of a power-up's timer: the starting length at level 0, then the upgrade table (bought in the shop).
    public static float Seconds(PowerKind k)
    {
        int lv = Progress.PowerLevel(k);
        if (lv > 0) return GameConfig.PowerLevelSec[(int)k][Mathf.Clamp(lv, 0, 5)];
        return k == PowerKind.Stack ? GameConfig.PowerStackSec : (k == PowerKind.Magnet ? GameConfig.PowerMagnetSec : GameConfig.PowerSlowSec);
    }

    // Seconds at a given level (for the shop).
    public static float SecondsAtLevel(PowerKind k, int lv)
    {
        if (lv > 0) return GameConfig.PowerLevelSec[(int)k][Mathf.Clamp(lv, 0, 5)];
        return k == PowerKind.Stack ? GameConfig.PowerStackSec : (k == PowerKind.Magnet ? GameConfig.PowerMagnetSec : GameConfig.PowerSlowSec);
    }

    public static Color KindColor(PowerKind k)
    {
        return k == PowerKind.Stack ? new Color(1.00f, 0.68f, 0.08f) : (k == PowerKind.Magnet ? new Color(0.20f, 0.45f, 1.00f) : new Color(0.62f, 0.32f, 1.00f));
    }

    public static string KindName(PowerKind k)
    {
        return k == PowerKind.Stack ? "STACK" : (k == PowerKind.Magnet ? "MAGNET" : "SLOW-MO+");
    }

    // Picking up the same one again refills its timer (it never adds on top of what is left).
    public static void Activate(PowerKind k)
    {
        int i = (int)k;
        total[i] = Mathf.Max(0.5f, Seconds(k));
        left[i] = total[i];
        var gm = GameManager.Instance;
        if (k == PowerKind.SlowZoom && gm != null) gm.SlowMoMeter = 1f;   // a full meter to start with
    }

    public static void ResetRun()
    {
        for (int i = 0; i < 3; i++) { left[i] = 0f; total[i] = 0f; }
        if (Instance != null) Instance.RestartSpawning();
    }

    // ---- the badges on the road ----

    const int PoolSize = 2;
    PowerUpPickup[] pool;
    float nextIn;

    void Awake()
    {
        Instance = this;
        nextIn = RollGap();
    }

    void RestartSpawning()
    {
        nextIn = RollGap();
        if (pool != null) foreach (var p in pool) if (p != null) p.Hide();
    }

    static float RollGap()
    {
        float lo = Mathf.Min(GameConfig.PowerGapMin, GameConfig.PowerGapMax), hi = Mathf.Max(GameConfig.PowerGapMin, GameConfig.PowerGapMax);
        if (Random.value < GameConfig.PowerQuickChance) return Random.Range(1.5f, 4f);   // now and then one comes soon after the last
        return Random.Range(lo, hi);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.State != GameManager.RunState.Playing || Tutorial.Active || !GameConfig.PowerUpsOn)
        {
            if (pool != null) foreach (var p in pool) if (p != null && p.gameObject.activeSelf) p.Hide();
            return;
        }
        if (gm.Paused) return;

        float dt = GameConfig.PowerGameTime ? Time.deltaTime : Time.unscaledDeltaTime;
        for (int i = 0; i < 3; i++) if (left[i] > 0f) left[i] = Mathf.Max(0f, left[i] - dt);

        nextIn -= dt;
        if (nextIn <= 0f)
        {
            nextIn = RollGap();
            Spawn();
        }
    }

    void Spawn()
    {
        var bike = BikeForward.Instance;
        if (bike == null) return;
        if (pool == null)
        {
            pool = new PowerUpPickup[PoolSize];
            for (int i = 0; i < PoolSize; i++) pool[i] = PowerUpPickup.Create(transform);
        }
        PowerUpPickup free = null;
        foreach (var p in pool) if (p != null && !p.gameObject.activeSelf) { free = p; break; }
        if (free == null) return;   // both badges are still out there: skip this one

        float side = GameConfig.ChaseView ? (Random.value < 0.5f ? 1f : -1f) : -1f;
        var kind = (PowerKind)Random.Range(0, 3);
        Vector3 pos = new Vector3(side * 5f, 2.2f, bike.transform.position.z + Random.Range(45f, 60f));
        free.Show(kind, pos);
    }
}

// The floating badge. A paper that touches it starts that power-up.
public class PowerUpPickup : MonoBehaviour
{
    PowerKind kind;
    Transform visual;
    Vector3 basePos;
    float phase, age;
    bool collected;
    GameObject[] looks;

    public static PowerUpPickup Create(Transform parent)
    {
        var root = new GameObject("PowerUpBadge");
        root.transform.SetParent(parent, false);
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.4f;   // forgiving: the paper only has to pass close by
        var pk = root.AddComponent<PowerUpPickup>();

        var v = new GameObject("Visual");
        v.transform.SetParent(root.transform, false);
        pk.visual = v.transform;
        pk.looks = new GameObject[3];
        for (int k = 0; k < 3; k++)
        {
            var g = new GameObject("Look" + k);
            g.transform.SetParent(v.transform, false);
            BuildLook(g.transform, (PowerKind)k);
            g.SetActive(false);
            pk.looks[k] = g;
        }
        root.SetActive(false);
        return pk;
    }

    static GameObject Glow(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default(Vector3))
    {
        var go = Prim.Make(t, name, parent, pos, scale, c, false, euler);
        Prim.GlowColor(go.GetComponent<Renderer>(), c);
        return go;
    }

    // A round badge: a dark outline, a coloured disc and the picture on it, all unlit so it is bright in any light. It is turned to the camera every frame.
    static void BuildLook(Transform p, PowerKind k)
    {
        var flat = new Vector3(90f, 0f, 0f);
        Color outline = new Color(0.07f, 0.08f, 0.16f);
        Color c = PowerUps.KindColor(k);
        Color white = Color.white;
        Glow(PrimitiveType.Cylinder, "Outline", p, Vector3.zero, new Vector3(1.9f, 0.05f, 1.9f), outline, flat);
        Glow(PrimitiveType.Cylinder, "Disc", p, Vector3.zero, new Vector3(1.62f, 0.09f, 1.62f), c, flat);
        if (k == PowerKind.Stack)
        {
            Glow(PrimitiveType.Cube, "P1", p, new Vector3(0.00f, -0.30f, 0f), new Vector3(0.84f, 0.20f, 0.24f), white);
            Glow(PrimitiveType.Cube, "P2", p, new Vector3(0.05f, 0.00f, 0f), new Vector3(0.84f, 0.20f, 0.24f), white, new Vector3(0f, 0f, 7f));
            Glow(PrimitiveType.Cube, "P3", p, new Vector3(-0.04f, 0.30f, 0f), new Vector3(0.84f, 0.20f, 0.24f), white, new Vector3(0f, 0f, -6f));
        }
        else if (k == PowerKind.Magnet)
        {
            Color red = new Color(1f, 0.15f, 0.12f);
            Glow(PrimitiveType.Cube, "ArmL", p, new Vector3(-0.30f, 0.06f, 0f), new Vector3(0.24f, 0.70f, 0.24f), red);
            Glow(PrimitiveType.Cube, "ArmR", p, new Vector3(0.30f, 0.06f, 0f), new Vector3(0.24f, 0.70f, 0.24f), red);
            Glow(PrimitiveType.Cube, "Base", p, new Vector3(0f, -0.30f, 0f), new Vector3(0.84f, 0.24f, 0.24f), red);
            Glow(PrimitiveType.Cube, "TipL", p, new Vector3(-0.30f, 0.34f, 0f), new Vector3(0.24f, 0.22f, 0.26f), white);
            Glow(PrimitiveType.Cube, "TipR", p, new Vector3(0.30f, 0.34f, 0f), new Vector3(0.24f, 0.22f, 0.26f), white);
        }
        else
        {
            Color navy = new Color(0.10f, 0.08f, 0.30f);
            Glow(PrimitiveType.Cylinder, "Face", p, Vector3.zero, new Vector3(1.06f, 0.12f, 1.06f), white, flat);
            Glow(PrimitiveType.Cube, "Minute", p, new Vector3(0f, 0.17f, 0f), new Vector3(0.09f, 0.40f, 0.20f), navy);
            Glow(PrimitiveType.Cube, "Hour", p, new Vector3(0.12f, 0f, 0f), new Vector3(0.28f, 0.09f, 0.20f), navy);
            Glow(PrimitiveType.Sphere, "Hub", p, Vector3.zero, new Vector3(0.14f, 0.14f, 0.26f), navy);
        }
    }

    public void Show(PowerKind k, Vector3 pos)
    {
        kind = k;
        for (int i = 0; i < looks.Length; i++) looks[i].SetActive(i == (int)k);
        basePos = pos;
        transform.position = pos;
        phase = Random.value * 6.28f;
        age = 0f;
        collected = false;
        gameObject.SetActive(true);
    }

    public void Hide() { gameObject.SetActive(false); }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 2.2f + phase) * 0.18f);
        var cam = Camera.main;
        if (cam != null && visual != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            if (f.sqrMagnitude > 0.01f) visual.rotation = Quaternion.LookRotation(f.normalized) * Quaternion.Euler(0f, Mathf.Sin(Time.time * 2f + phase) * 14f, 0f);
        }
        float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 5f + phase);
        if (visual != null) visual.localScale = Vector3.one * pulse;

        // gone once the bike has passed it, or it has hung about for too long
        var bike = BikeForward.Instance;
        if ((bike != null && bike.transform.position.z > basePos.z + 8f) || age > 40f) Hide();
    }

    // Called by a paper that touches the badge.
    public void Collect()
    {
        if (collected) return;
        collected = true;
        PowerUps.Activate(kind);
        Juice.PowerUp(transform.position, PowerUps.KindColor(kind));
        var gm = GameManager.Instance;
        if (gm != null) gm.ShowToast(PowerUps.KindName(kind) + "!", 1.4f);
        Hide();
    }
}
