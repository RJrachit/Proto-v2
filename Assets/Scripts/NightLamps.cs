using System.Collections.Generic;
using UnityEngine;

// Real point lights for the night. The lamps and door lights register themselves as sources; this keeps a small pool of
// point lights parked on the nearest ones, so ground, walls, fences and trees close to a lamp are lit and the rest stay dark.
// It needs the render pipeline's "Additional Lights" to be on (Per Pixel). If they are off, the soft glow on the ground stays as the fallback.
public class NightLamps : MonoBehaviour
{
    public class Source
    {
        public Transform t;
        public float range, intensity;
        public Color color;
        public int slot = -1;
    }

    public static readonly List<Source> Sources = new List<Source>();

    public static Source Add(Transform t, float range, float intensity, Color color)
    {
        var s = new Source { t = t, range = range, intensity = intensity, color = color };
        Sources.Add(s);
        return s;
    }

    const int Count = 10;
    Light[] lights;
    Source[] assigned;
    float nextSort;
    Camera cam;

    static bool checkedReal, real = true;

    // True when the render pipeline draws additional (point) lights per pixel.
    public static bool Real
    {
        get
        {
            if (checkedReal) return real;
            checkedReal = true;
            try
            {
                var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                if (asset == null) { real = false; return real; }
                var t = asset.GetType();
                var p = t.GetProperty("additionalLightsRenderingMode");
                string mode = p != null ? p.GetValue(asset, null).ToString() : "unknown";
                if (mode == "Disabled")
                {
                    var f = t.GetField("m_AdditionalLightsRenderingMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (f != null)
                    {
                        f.SetValue(asset, System.Enum.Parse(f.FieldType, "PerPixel"));
                        var lim = t.GetField("m_AdditionalLightsPerObjectLimit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (lim != null && (int)lim.GetValue(asset) < 4) lim.SetValue(asset, 4);
                        Debug.LogWarning("NightLamps: Additional Lights were Disabled in the URP asset; switched to Per Pixel for this session. For a phone build, set Additional Lights to Per Pixel (limit 4) in the URP asset itself.");
                    }
                    else { real = false; Debug.LogWarning("NightLamps: Additional Lights are Disabled in the URP asset, so lamps cannot light the scene. Set Additional Lights to Per Pixel in the URP asset."); }
                }
                else Debug.Log("NightLamps: additional lights mode = " + mode);
            }
            catch (System.Exception e) { Debug.LogWarning("NightLamps: could not read the URP settings: " + e.Message); }
            return real;
        }
    }

    void Awake()
    {
        Sources.Clear();
        lights = new Light[Count];
        assigned = new Source[Count];
        for (int i = 0; i < Count; i++)
        {
            var go = new GameObject("NightLight" + i);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.shadows = LightShadows.None;
            l.enabled = false;
            lights[i] = l;
        }
    }

    void Update()
    {
        float amt = NightLights.Amount;
        bool on = amt > 0.05f && Real;
        if (!on)
        {
            for (int i = 0; i < Count; i++) { lights[i].enabled = false; assigned[i] = null; }
            return;
        }
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (Time.time >= nextSort) { nextSort = Time.time + 0.2f; Assign(); }

        float k = Mathf.Clamp01(amt * 1.3f) * GameConfig.LampLight;
        for (int i = 0; i < Count; i++)
        {
            var s = assigned[i];
            if (s == null || s.t == null || !s.t.gameObject.activeInHierarchy) { lights[i].enabled = false; assigned[i] = null; continue; }
            var l = lights[i];
            l.transform.position = s.t.position;
            l.range = s.range;
            l.color = s.color;
            l.intensity = s.intensity * k;
            l.enabled = true;
        }
    }

    // Park the lights on the Count nearest active sources. Lights already on a source that is still among the nearest stay put.
    void Assign()
    {
        Vector3 c = cam.transform.position;
        var near = new List<Source>();
        for (int i = Sources.Count - 1; i >= 0; i--)
        {
            var s = Sources[i];
            if (s.t == null) { Sources.RemoveAt(i); continue; }
            if (!s.t.gameObject.activeInHierarchy) continue;
            near.Add(s);
        }
        near.Sort((a, b) => (a.t.position - c).sqrMagnitude.CompareTo((b.t.position - c).sqrMagnitude));
        int n = Mathf.Min(Count, near.Count);

        var free = new List<int>();
        for (int i = 0; i < Count; i++)
        {
            var s = assigned[i];
            bool keep = false;
            if (s != null && s.t != null)
                for (int j = 0; j < n; j++) if (near[j] == s) { keep = true; break; }
            if (!keep) { assigned[i] = null; free.Add(i); }
        }
        for (int j = 0; j < n && free.Count > 0; j++)
        {
            bool has = false;
            for (int i = 0; i < Count; i++) if (assigned[i] == near[j]) { has = true; break; }
            if (has) continue;
            assigned[free[0]] = near[j];
            free.RemoveAt(0);
        }
    }
}
