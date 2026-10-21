using System.Collections.Generic;
using UnityEngine;

// House (partial) - Night: lit lamps, door light and window spill.
// The main part of the class is in House.cs.
public partial class House
{
    // ---- night: lit lamps and windows ----
    readonly List<Transform> lampBulbs = new List<Transform>();
    readonly List<Transform> lampPools = new List<Transform>();
    readonly List<NightLamps.Source> lampSrc = new List<NightLamps.Source>();
    int lampN;
    int doorLamp = -1;     // which lamp slot is the front-door light (it goes off when the house is inactive)
    int nightStep = -1;

    // A glowing bulb at the lamp head and a soft pool of light on the ground under it. Both are only shown at night.
    void LampGlow(Vector3 head, float bulbSize = 0.85f)
    {
        Glow(head, new Vector3(head.x, 0.23f, head.z), 7f, NightLights.Pool, bulbSize, 13f, 4.0f, new Color(1.0f, 0.82f, 0.50f));
    }

    // The light over the front door: a small bulb on the wall and a warm pool on the doorstep. Every house has one.
    void DoorLight(float doorZ)
    {
        float s = side;
        doorLamp = lampN;
        Glow(new Vector3(s * 8.3f, 2.2f, doorZ), new Vector3(s * 7.1f, 0.235f, doorZ), 5.0f, NightLights.DoorPool, 0.3f, 7f, 2.6f, new Color(1.0f, 0.76f, 0.42f));
    }

    void Glow(Vector3 head, Vector3 poolPos, float poolSize, Material poolMat, float bulbSize, float range, float intensity, Color lightColor)
    {
        if (!NightLights.Available) return;
        if (lampN >= lampBulbs.Count)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "LampBulb";
            Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(transform, false);
            var br = b.GetComponent<Renderer>();
            br.sharedMaterial = NightLights.Bulb;
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            br.receiveShadows = false;
            lampBulbs.Add(b.transform);
            lampSrc.Add(NightLamps.Add(b.transform, range, intensity, lightColor));   // a real point light visits it at night

            var p = BlobShadow.Create(transform);
            p.name = "LampPool";
            p.GetComponent<Renderer>().sharedMaterial = NightLights.Pool;
            lampPools.Add(p.transform);
        }
        var src = lampSrc[lampN];   // this slot may be a lamp now and a door light next time
        src.range = range;
        src.intensity = intensity;
        src.color = lightColor;
        var bulb = lampBulbs[lampN];
        bulb.localPosition = head;
        bulb.localScale = Vector3.one * bulbSize;
        var pool = lampPools[lampN];
        pool.localPosition = poolPos;
        pool.localScale = new Vector3(poolSize, poolSize, 1f);
        pool.GetComponent<Renderer>().sharedMaterial = poolMat;
        lampN++;
    }

    void HideLamps()
    {
        for (int i = lampN; i < lampBulbs.Count; i++)
        {
            lampBulbs[i].gameObject.SetActive(false);
            lampPools[i].gameObject.SetActive(false);
        }
    }

    static float Hash01(float x) { return Mathf.Repeat(Mathf.Sin(x * 12.9898f) * 43758.5453f, 1f); }

    // Lamps and windows follow the time of day (NightLights): lamps glow from dusk, most windows light up warm, a few cool.
    void ApplyNight()
    {
        nightStep = NightLights.Step;
        if (!NightLights.Available) return;
        bool lamps = NightLights.LampsOn;
        bool glowOn = NightLights.WindowsOn;
        for (int i = 0; i < shopQuadN; i++) shopQuads[i].gameObject.SetActive(glowOn && (shopQuadLink[i] == null || shopQuadLink[i].Standing));
        // Only the active customer's house has its door and windows lit (shops keep their lit windows); street lamps burn everywhere.
        bool houseLit = (Subscriber && !resolved) || shopKind != 0;
        for (int i = 0; i < lampN; i++)
        {
            bool on = lamps && (i != doorLamp || (Subscriber && !resolved));
            lampBulbs[i].gameObject.SetActive(on);
            lampPools[i].gameObject.SetActive(on);
        }
        LightWindows(groundWindows, 3.1f, true, houseLit);
        LightWindows(upperWindows, 8.7f, false, houseLit);
    }

    readonly List<Transform> winPools = new List<Transform>();

    // light that spills from a lit ground-floor window onto the lawn
    void WindowSpill(int i, bool on, float z)
    {
        while (winPools.Count <= i)
        {
            var p = BlobShadow.Create(transform);
            p.name = "WindowSpill";
            p.GetComponent<Renderer>().sharedMaterial = NightLights.WinPool;
            winPools.Add(p.transform);
        }
        var t = winPools[i];
        if (on)
        {
            t.localPosition = new Vector3(side * 7.5f, 0.235f, z);
            t.localScale = new Vector3(3.2f, 3.2f, 1f);
        }
        t.gameObject.SetActive(on);
    }

    void LightWindows(WindowSlot[] arr, float salt, bool spill, bool allowLit)
    {
        if (arr == null) return;
        bool night = NightLights.WindowsOn && allowLit;
        float wz = transform.position.z;
        for (int i = 0; i < arr.Length; i++)
        {
            var w = arr[i];
            if (w.glass == null || w.own == null || !w.visual.gameObject.activeSelf)
            {
                if (spill) WindowSpill(i, false, 0f);
                continue;
            }
            float h = Hash01(wz * 0.37f + i * 7.13f + salt + side * 5.1f);
            bool lit = night && !w.broken && h < 0.8f;
            if (lit)
                w.glass.sharedMaterial = h < 0.12f ? NightLights.WindowCool : NightLights.WindowWarm;
            else if (w.glass.sharedMaterial != w.own)
                w.glass.sharedMaterial = w.own;
            if (spill) WindowSpill(i, lit, w.visual.localPosition.z);
        }
    }
}
