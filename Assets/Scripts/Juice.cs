using UnityEngine;

// "Game feel": particles, hit-stop slow motion, camera kicks, and the matching sounds and vibration.
public class Juice : MonoBehaviour
{
    public static Juice Instance { get; private set; }

    Material particleMaterial;
    float hitUntil;

    void Awake()
    {
        Instance = this;
        particleMaterial = Mat.Particle();
    }

    void Update()
    {
        if (GameTime.Hit < 1f && Time.unscaledTime > hitUntil)
        {
            GameTime.Hit = Mathf.MoveTowards(GameTime.Hit, 1f, Time.unscaledDeltaTime * 3.5f);   // the game eases back to full speed (no sudden snap)
            GameTime.Apply();
        }
    }

    // ---- effects used by the game ----

    public static void Delivery(Vector3 pos, float pitch)
    {
        if (Instance == null) return;
        Instance.Burst(pos + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.25f), 28, 4f);
        Instance.Burst(pos + Vector3.up * 0.3f, Color.white, 14, 2.5f);
        Instance.HitStop(0.12f, 0.55f);   // a short, gentle slow-down
        if (CameraFollow.Instance != null) CameraFollow.Instance.Kick(1f);
        Sfx.Play("thwack");
        Sfx.Play("chime", 0.8f, pitch);
        Haptics.Pulse(25);
    }

    // A trick shot: a big fiery explosion at the ring. A fireball, fast sparks that fall, and slow embers that rise, with a long hit-stop and a hard camera kick.
    public static void TrickShot(Vector3 pos)
    {
        if (Instance == null) return;
        Vector3 c = pos + Vector3.up * 0.4f;
        Instance.Fire(c, 140, 10f, 0.75f, 1.4f, -0.15f);   // the fireball
        Instance.Fire(c, 70, 15f, 0.30f, 1.1f, 0.6f);      // fast sparks thrown out
        Instance.Fire(c, 40, 5f, 1.0f, 1.8f, -0.5f);       // slow big embers drifting up
        Instance.Burst(c, new Color(1f, 0.55f, 0.1f), 30, 7f);
        Instance.Flames(pos, 5f);   // and the ring burns on for a while
        Instance.HitStop(0.25f, 0.2f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Kick(3f);
        Sfx.Play("thwack");
        Sfx.Play("chime", 1f, 1.7f);
        Haptics.Pulse(70);
    }

    // A basket (any swish through a ring): a fiery blast of its own, a little smaller than a trick shot's.
    public static void HoopBlast(Vector3 pos, float pitch)
    {
        if (Instance == null) return;
        Vector3 c = pos + Vector3.up * 0.4f;
        Instance.Fire(c, 90, 8f, 0.65f, 1.2f, -0.15f);     // the fireball
        Instance.Fire(c, 45, 12f, 0.26f, 1.0f, 0.6f);      // fast sparks
        Instance.Fire(c, 24, 4f, 0.85f, 1.5f, -0.5f);      // big embers drifting up
        Instance.Burst(c, new Color(1f, 0.6f, 0.15f), 20, 5f);
        Instance.Flames(pos, 4f);   // the ring burns on for a while
        Instance.HitStop(0.14f, 0.45f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Kick(1.6f);
        Sfx.Play("thwack");
        Sfx.Play("chime", 0.9f, pitch);
        Haptics.Pulse(40);
    }

    // A paper in the leaves: a few leaves of the tree's colour shake loose and drift down.
    public static void Rustle(Vector3 pos, Color leaf, float speed)
    {
        if (Instance == null) return;
        Instance.Burst(pos, leaf, Mathf.Clamp(Mathf.RoundToInt(speed), 6, 16), 1.6f);
        Sfx.Play("thunk", 0.35f, Random.Range(1.5f, 1.9f));
        Haptics.Pulse(8);
    }

    public static void Miss(Vector3 pos)
    {
        if (Instance == null) return;
        Instance.Burst(pos, new Color(0.8f, 0.8f, 0.75f), 8, 1.5f);
        Sfx.Play("miss", 0.7f);
    }

    public static void Crash(Vector3 pos)
    {
        if (Instance == null) return;
        Instance.Burst(pos + Vector3.up, new Color(0.55f, 0.4f, 0.3f), 24, 5f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Kick(1.3f);
        Sfx.Play("crash");
        Haptics.Pulse(60);
    }

    public static void Smash(Vector3 pos, Color color, bool big)
    {
        if (Instance == null) return;
        Instance.Burst(pos + Vector3.up * 0.2f, color, big ? 26 : 10, big ? 4.5f : 3f);
        if (big && CameraFollow.Instance != null) CameraFollow.Instance.Kick(0.6f);
        if (big) Sfx.Play("chime", 0.8f, 1.2f);
        Sfx.Play("thunk", 0.9f, Random.Range(0.9f, 1.2f));
        Haptics.Pulse(big ? 30 : 12);
    }

    public static void WindowBreak(Vector3 pos)
    {
        if (Instance == null) return;
        Instance.Burst(pos, new Color(0.7f, 0.9f, 1f), 30, 4f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Kick(0.8f);
        Sfx.Play("windowBreak");
        Haptics.Pulse(40);
    }

    // ---- building blocks ----

    void HitStop(float seconds, float scale)
    {
        GameTime.Hit = scale;
        GameTime.Apply();
        hitUntil = Time.unscaledTime + seconds;
    }

    // A soft round glow (opaque in the middle, fading to nothing at the edge): flames made of these look like fire, not like confetti.
    Material flameMaterial;
    Material FlameMaterial()
    {
        if (flameMaterial != null) return flameMaterial;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a);   // smooth edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        flameMaterial = Mat.Particle();
        flameMaterial.mainTexture = tex;
        return flameMaterial;
    }

    // The ring on fire: a crown of flames that rises from the ring for a few seconds, then dies down. Three layers: a bright core of tongues,
    // a wider orange-red body that flickers sideways as it climbs, and a few sparks that float up and go out.
    void Flames(Vector3 pos, float seconds)
    {
        FlameLayer(pos, seconds, 0.40f, 60f, 0.45f, 0.30f, 0.55f, 0.50f, 1.6f, 2.6f, 0.15f, true);     // core: small, hot, quick
        FlameLayer(pos, seconds, 0.62f, 45f, 0.85f, 0.55f, 1.00f, 0.95f, 1.0f, 1.8f, 0.55f, false);    // body: big, orange to red, flickering
        Sparks(pos, seconds);
    }

    void FlameLayer(Vector3 pos, float seconds, float ringRadius, float rate, float sizeMin, float lifeMin, float sizeMax, float lifeMax,
                    float riseMin, float riseMax, float flicker, bool hot)
    {
        var go = new GameObject(hot ? "RingFireCore" : "RingFireBody");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.duration = seconds;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));   // thins out as it burns down

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = ringRadius;
        shape.radiusThickness = hot ? 1f : 0.35f;   // the body also burns a little inside the ring
        shape.rotation = new Vector3(90f, 0f, 0f);   // lying flat, like the ring

        var rise = ps.velocityOverLifetime;
        rise.enabled = true;
        rise.space = ParticleSystemSimulationSpace.World;
        rise.x = new ParticleSystem.MinMaxCurve(0f, 0f);   // (x, y and z must all use the same curve mode)
        rise.y = new ParticleSystem.MinMaxCurve(riseMin, riseMax);
        rise.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = ps.noise;   // the flicker: the flames sway as they climb
        noise.enabled = true;
        noise.strength = flicker;
        noise.frequency = 0.9f;
        noise.scrollSpeed = 1.2f;
        noise.damping = true;

        var spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        var colour = ps.colorOverLifetime;
        colour.enabled = true;
        var g = new Gradient();
        if (hot)
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1.00f, 0.97f, 0.75f), 0f), new GradientColorKey(new Color(1.00f, 0.75f, 0.20f), 0.45f), new GradientColorKey(new Color(1.00f, 0.40f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.12f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
        else
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1.00f, 0.60f, 0.12f), 0f), new GradientColorKey(new Color(0.95f, 0.30f, 0.04f), 0.4f), new GradientColorKey(new Color(0.70f, 0.08f, 0.02f), 0.8f), new GradientColorKey(new Color(0.25f, 0.03f, 0.02f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0.55f, 0.65f), new GradientAlphaKey(0f, 1f) });
        colour.color = g;

        var size = ps.sizeOverLifetime;   // a flame swells, then narrows to a tip
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.1f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        go.GetComponent<ParticleSystemRenderer>().material = FlameMaterial();
        ps.Play();
        Destroy(go, seconds + 2.5f);
    }

    // Sparks that float up from the fire and go out.
    void Sparks(Vector3 pos, float seconds)
    {
        var go = new GameObject("RingSparks");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.duration = seconds;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        main.startColor = new Color(1f, 0.8f, 0.3f);
        main.gravityModifier = -0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(16f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 0.7f;

        var colour = ps.colorOverLifetime;
        colour.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.6f), new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colour.color = g;

        go.GetComponent<ParticleSystemRenderer>().material = FlameMaterial();
        ps.Play();
        Destroy(go, seconds + 2.5f);
    }

    // Flames: particles that start white-yellow, turn orange then red then dark, and shrink away as they die.
    void Fire(Vector3 pos, int count, float speed, float size, float life, float gravity)
    {
        var go = new GameObject("Fire");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // it auto-plays; stop before configuring

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startColor = Color.white;
        main.gravityModifier = gravity;
        main.maxParticles = count + 8;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        var colour = ps.colorOverLifetime;
        colour.enabled = true;
        var fire = new Gradient();
        fire.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.00f, 0.80f, 0.25f), 0.00f),   // hot yellow-orange
                new GradientColorKey(new Color(1.00f, 0.45f, 0.05f), 0.30f),   // orange
                new GradientColorKey(new Color(0.90f, 0.10f, 0.02f), 0.65f),   // red
                new GradientColorKey(new Color(0.30f, 0.03f, 0.02f), 1.00f),   // dark red embers
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        colour.color = fire;

        var shrink = ps.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

        go.GetComponent<ParticleSystemRenderer>().material = FlameMaterial();   // (soft glows, not hard squares)
        ps.Play();
        Destroy(go, life + 1f);
    }

    void Burst(Vector3 pos, Color color, int count, float speed)
    {
        var go = new GameObject("Burst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // it auto-plays; stop before configuring

        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.7f;
        main.startSpeed = speed;
        main.startSize = 0.18f;
        main.startColor = color;
        main.gravityModifier = 1f;
        main.maxParticles = count + 8;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        go.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        ps.Play();
        Destroy(go, 2f);
    }
}
