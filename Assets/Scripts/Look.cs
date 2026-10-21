using UnityEngine;

// The scene's overall look, all switchable from the DEV menu (Look tab) so the effects can be compared on the phone:
//   - ambient gradient: cool light from the sky, a darker warm ground, so shaded sides are not flat grey
//   - vignette: the screen edges darken a little
//   - sun shadows: soft shadows from the sun in the daytime phase only (fade out at dawn, dusk and rain)
// Added by PrototypeBootstrap.
public class Look : MonoBehaviour
{
    // original ambient settings, restored when the gradient is switched off
    UnityEngine.Rendering.AmbientMode origMode;
    Color origSky, origEquator, origGround;
    float origIntensity;
    bool ambientOn, shadowsApplied;
    Texture2D vig;
    Light sun;

    void Awake()
    {
        gameObject.AddComponent<NightLamps>();   // real point lights on the nearest lamps and door lights at night
        origMode = RenderSettings.ambientMode;
        origSky = RenderSettings.ambientSkyColor;
        origEquator = RenderSettings.ambientEquatorColor;
        origGround = RenderSettings.ambientGroundColor;
        origIntensity = RenderSettings.ambientIntensity;
    }

    static readonly Color GradSky = new Color(0.62f, 0.72f, 0.88f);
    static readonly Color GradEquator = new Color(0.58f, 0.58f, 0.62f);
    static readonly Color GradGround = new Color(0.34f, 0.32f, 0.30f);

    static Color Scale(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

    void Update()
    {
        Prim.TickXRay();
        House.TickBoardGlow();
        if (GameConfig.AmbientGradient != ambientOn)
        {
            ambientOn = GameConfig.AmbientGradient;
            RenderSettings.ambientMode = ambientOn ? UnityEngine.Rendering.AmbientMode.Trilight : origMode;
        }

        // The ambient light dims as the evening comes in. ambientIntensity only affects a skybox, so the gradient colours are scaled directly.
        float dim = Mathf.Lerp(1f, 1f - 0.985f * GameConfig.NightDarkness, NightLights.Dark);
        Color sky = ambientOn ? GradSky : origSky;
        Color eq = ambientOn ? GradEquator : origEquator;
        Color gr = ambientOn ? GradGround : origGround;
        RenderSettings.ambientSkyColor = Scale(sky, dim);
        RenderSettings.ambientEquatorColor = Scale(eq, dim);
        RenderSettings.ambientGroundColor = Scale(gr, dim);
        RenderSettings.ambientIntensity = (ambientOn ? 1f : origIntensity) * dim;
        NightLights.Tick();
        BillboardArt.Tick();
        UpdateShadows();
        // no sun shadows at dusk, dawn or night: the soft contact shadows under things take over and get stronger
        BlobShadow.SetStrength(1f + 0.5f * (1f - DayCycle.ShadowAmount) + 0.8f * Mathf.Clamp01(NightLights.Dark * 1.5f));
    }

    // Soft sun shadows in the daytime phase only. They fade with DayCycle.ShadowAmount, so dawn, dusk and rain have none.
    void UpdateShadows()
    {
        if (sun == null)
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null) return;

        float amount = GameConfig.SunShadows ? DayCycle.ShadowAmount : 0f;
        bool on = amount > 0.03f;
        if (on)
        {
            if (!shadowsApplied)
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowDistance = 45f;
                sun.shadowBias = 0.06f;
                sun.shadowNormalBias = 0.5f;
                try
                {
                    // the URP asset has its own shadow distance
                    var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                    if (asset != null)
                    {
                        var p = asset.GetType().GetProperty("shadowDistance");
                        if (p != null && p.CanWrite) p.SetValue(asset, 45f, null);
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("Could not set the shadow distance: " + e.Message); }
                shadowsApplied = true;
            }
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f * amount;
        }
        else if (shadowsApplied || sun.shadows != LightShadows.None)
        {
            sun.shadows = LightShadows.None;
            QualitySettings.shadows = ShadowQuality.Disable;
            shadowsApplied = false;
        }
    }

    void OnGUI()
    {
        float k = GameConfig.VignetteStrength;
        if (k <= 0.01f || Event.current.type != EventType.Repaint) return;
        if (vig == null) vig = MakeVignette();
        GUI.depth = 2000;   // behind every other screen
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k));
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vig, ScaleMode.StretchToFill);
        GUI.color = Color.white;
    }

    static Texture2D MakeVignette()
    {
        const int n = 128;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = Mathf.SmoothStep(0.45f, 1.0f, d);
                px[y * n + x] = new Color(0.04f, 0.05f, 0.10f, a);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
