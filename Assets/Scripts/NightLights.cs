using UnityEngine;

// Shared state and materials for the night: how lit the street lamps and windows should be, and the glowing materials they use.
// DayCycle sets Amount (0 = no lights, 1 = full night) and Dark (how much to dim the ambient light). Look calls Tick every frame.
// The glow uses the flat unlit sprite material, so it stays bright in the dark without any emission setup (which can be stripped from phone builds).
public static class NightLights
{
    public static float Amount;   // 0..1, how much the lamps and windows are lit
    public static float Dark;     // 0..1, how dark the ambient light gets

    // Quantised, so houses only re-light their windows a few times while the evening comes in.
    public static int Step { get { return Mathf.RoundToInt(Amount * 8f); } }
    public static bool LampsOn { get { return Amount > 0.05f; } }
    public static bool WindowsOn { get { return Amount > 0.15f; } }
    public static bool Available { get { return BlobShadow.Available; } }

    static Material bulb, warm, cool, pool, doorPool, winPool;

    // Unity-aware null checks (not ??): a material destroyed when Play mode ended must be rebuilt, not reused.
    public static Material Bulb { get { if (bulb == null) bulb = Make(new Color(1.0f, 0.95f, 0.65f), false); return bulb; } }
    public static Material WindowWarm { get { if (warm == null) warm = Make(new Color(1.0f, 0.82f, 0.42f), false); return warm; } }
    public static Material WindowCool { get { if (cool == null) cool = Make(new Color(0.80f, 0.90f, 1.0f), false); return cool; } }
    // the light on the ground under a lamp
    public static Material Pool { get { if (pool == null) pool = Make(new Color(1.0f, 0.88f, 0.55f, 0f), true); return pool; } }

    // neon sign colours (unlit, always bright)
    static readonly Color[] neonColors =
    {
        new Color(0.45f, 0.95f, 1.00f), new Color(1.00f, 0.50f, 0.85f), new Color(0.55f, 1.00f, 0.60f), new Color(0.72f, 0.58f, 1.00f),
    };
    static Material[] neon;
    public static int NeonCount { get { return neonColors.Length; } }
    public static Material Neon(int i)
    {
        if (neon == null) neon = new Material[neonColors.Length];
        i = Mathf.Abs(i) % neonColors.Length;
        if (neon[i] == null) neon[i] = Make(neonColors[i], false);
        return neon[i];
    }

    // the light under a door lamp (warmer and stronger) and the light that spills from a lit window onto the lawn
    public static Material DoorPool { get { if (doorPool == null) doorPool = Make(new Color(1.0f, 0.80f, 0.45f, 0f), true); return doorPool; } }
    public static Material WinPool { get { if (winPool == null) winPool = Make(new Color(1.0f, 0.85f, 0.50f, 0f), true); return winPool; } }

    static Material Make(Color c, bool soft)
    {
        var m = Mat.Particle();
        if (soft) m.mainTexture = BlobShadow.GlowTex();
        m.color = c;
        return m;
    }

    public static void Tick()
    {
        if (!Available) return;
        var c = Pool.color;
        // with real lamp lights the ground glow is only a faint extra; without them it is the fallback
        float k = Mathf.Clamp01(Amount * 1.2f) * (NightLamps.Real ? 0.28f : 1f);
        c.a = 0.34f * k;
        Pool.color = c;
        var d = DoorPool.color;
        d.a = 0.38f * k;
        DoorPool.color = d;
        var w = WinPool.color;
        w.a = 0.22f * k;
        WinPool.color = w;
    }
}
