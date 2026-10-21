using UnityEngine;

// Changes the look of the ride as you go: dawn, day, rain, dusk, then around again.
// Blends the sky colour, sun colour and brightness, and fades rain in and out.
public class DayCycle : MonoBehaviour
{
    struct Phase
    {
        public Color sky;
        public Color light;
        public float intensity;
        public float rain;
        public Color top;       // sky colour high up (the horizon colour is `sky`)
        public Color cloud;
        public Color sun;
        public float sunVisible;
        public float lights;    // how lit the lamps and windows are (0..1)
        public float dark;      // how much the ambient light is dimmed (0..1)
        public Phase(Color sky, Color light, float intensity, float rain, Color top, Color cloud, Color sun, float sunVisible, float lights = 0f, float dark = 0f)
        {
            this.lights = lights;
            this.dark = dark;
            this.sky = sky;
            this.light = light;
            this.intensity = intensity;
            this.rain = rain;
            this.top = top;
            this.cloud = cloud;
            this.sun = sun;
            this.sunVisible = sunVisible;
        }
    }

    // Read by Backdrop (sky, sun, clouds) every frame.
    public static Color SkyHorizon = new Color(0.55f, 0.78f, 0.95f);
    public static Color SkyTop = new Color(0.25f, 0.52f, 0.90f);
    public static Color CloudTint = Color.white;
    public static Color SunTint = new Color(1f, 0.97f, 0.80f);
    public static float SunVisible = 1f;
    public static int ForcePhase = 3;        // -2 = the phone's clock, -1 = follow the ride, 0..4 = hold dawn, day, rain, dusk (default) or night
    public static bool Raining { get { return RainAmount > 0.5f; } }   // the rain phase (an instant switch, for gameplay)
    public static float RainAmount;          // 0..1: how hard it rains right now (the streets get wet; Wetness reads it)
    public static float ShadowAmount = 1f;   // 1 in full day, 0 at dawn, dusk and in rain; read by Look for the sun shadows

    [Tooltip("Distance ridden per phase.")]
    public float distancePerPhase = GameConfig.DistancePerPhase;
    [Tooltip("Rain particles in the rain phase. Off by default: on a phone the specks read as white grain.")]
    public bool enableRain = GameConfig.EnableRain;

    [Tooltip("Sun shadows. Off by default: the low-res shadow map shows as white speckles in the editor and costs fps on phones.")]
    public LightShadows sunShadows = LightShadows.None;

    Phase[] phases;
    Camera cam;
    Light sun;
    ParticleSystem rain;

    void Start()
    {
        cam = Camera.main;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { sun = l; break; }
        }

        // Switch shadows off on EVERY light, not just the sun
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            l.shadows = sunShadows;

        phases = new Phase[]
        {
            new Phase(new Color(1.00f, 0.75f, 0.60f), new Color(1.00f, 0.80f, 0.65f), 0.9f, 0f,
                      new Color(0.55f, 0.62f, 0.85f), new Color(1.00f, 0.85f, 0.80f), new Color(1.00f, 0.75f, 0.45f), 1f, 0f, 0.15f),   // dawn
            new Phase(new Color(0.55f, 0.78f, 0.95f), new Color(1.00f, 0.97f, 0.90f), 1.1f, 0f,
                      new Color(0.25f, 0.52f, 0.90f), Color.white,                    new Color(1.00f, 0.97f, 0.80f), 1f, 0f, 0f),   // day
            new Phase(new Color(0.55f, 0.60f, 0.68f), new Color(0.70f, 0.75f, 0.85f), 0.7f, 1f,
                      new Color(0.40f, 0.45f, 0.55f), new Color(0.62f, 0.65f, 0.70f), new Color(0.80f, 0.80f, 0.80f), 0f, 0f, 0.15f),   // rain
            new Phase(new Color(0.95f, 0.50f, 0.40f), new Color(1.00f, 0.60f, 0.45f), 0.8f, 0f,
                      new Color(0.35f, 0.30f, 0.55f), new Color(1.00f, 0.65f, 0.55f), new Color(1.00f, 0.45f, 0.30f), 1f, 0.5f, 0.4f),   // dusk
            new Phase(new Color(0.025f, 0.04f, 0.10f), new Color(0.40f, 0.50f, 0.90f), 0.12f, 0f,
                      new Color(0.01f, 0.015f, 0.06f), new Color(0.08f, 0.09f, 0.15f), new Color(0.88f, 0.92f, 1.00f), 0.5f, 1f, 1f),   // night
        };

        if (enableRain) { BuildRain(); new GameObject("RainSurface").AddComponent<RainSurface>(); }   // drops splashing on the ground and dripping off roofs and trees
    }

    void BuildRain()
    {
        var go = new GameObject("Rain");
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // emit downwards
        rain = go.AddComponent<ParticleSystem>();
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // it auto-plays; stop before configuring

        var main = rain.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 20f;
        main.startSize = 0.07f;
        main.startColor = new Color(0.75f, 0.85f, 1f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(40f, 40f, 1f);

        var emission = rain.emission;
        emission.rateOverTime = 0f;

        go.GetComponent<ParticleSystemRenderer>().material = Mat.Particle();
        rain.Play();
    }

    // The phone's local time decides the look: night 19:30 to 04:30, dawn around 5:30 to 7, day 7 to 17, dusk 17:00 to 19:30, with soft changes in between.
    // Phases: 0 dawn, 1 day, 2 rain, 3 dusk, 4 night.
    static void ClockPhase(out int a, out int b, out float f)
    {
        var now = System.DateTime.Now;
        float h = now.Hour + now.Minute / 60f + now.Second / 3600f;
        if (h < 4.5f || h >= 19.5f) { a = 4; b = 4; f = 0f; }
        else if (h < 5.5f) { a = 4; b = 0; f = (h - 4.5f) / 1f; }
        else if (h < 7f) { a = 0; b = 1; f = (h - 5.5f) / 1.5f; }
        else if (h < 17f) { a = 1; b = 1; f = 0f; }
        else if (h < 18.5f) { a = 1; b = 3; f = (h - 17f) / 1.5f; }
        else { a = 3; b = 4; f = (h - 18.5f) / 1f; }
        f = Mathf.SmoothStep(0f, 1f, f);
    }

    void Update()
    {
        var bike = BikeForward.Instance;
        if (bike == null || cam == null || phases == null) return;

        int a, b;
        float f;
        if (ForcePhase == -2) ClockPhase(out a, out b, out f);
        else
        {
            float t = ForcePhase >= 0 ? ForcePhase : Mathf.Max(0f, bike.transform.position.z) / distancePerPhase;
            a = (int)t % phases.Length;
            b = (a + 1) % phases.Length;
            f = Mathf.SmoothStep(0f, 1f, t - Mathf.Floor(t));
        }
        Phase pa = phases[a];
        Phase pb = phases[b];

        cam.backgroundColor = Color.Lerp(pa.sky, pb.sky, f);
        SkyHorizon = cam.backgroundColor;
        SkyTop = Color.Lerp(pa.top, pb.top, f);
        CloudTint = Color.Lerp(pa.cloud, pb.cloud, f);
        SunTint = Color.Lerp(pa.sun, pb.sun, f);
        SunVisible = Mathf.Lerp(pa.sunVisible, pb.sunVisible, f);
        NightLights.Amount = Mathf.Lerp(pa.lights, pb.lights, f);
        NightLights.Dark = Mathf.Lerp(pa.dark, pb.dark, f);
        ShadowAmount = Mathf.Lerp(a == 1 ? 1f : 0f, b == 1 ? 1f : 0f, f);
        RainAmount = Mathf.Lerp(pa.rain, pb.rain, f);
        if (sun != null)
        {
            sun.color = Color.Lerp(pa.light, pb.light, f);
            sun.intensity = Mathf.Lerp(pa.intensity, pb.intensity, f);
        }

        if (rain != null)
        {
            var emission = rain.emission;
            emission.rateOverTime = 500f * Mathf.Lerp(pa.rain, pb.rain, f);
            rain.transform.position = cam.transform.position + new Vector3(0f, 14f, 10f);
        }
    }
}
