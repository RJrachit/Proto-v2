using UnityEngine;

// The bike's shadow at night, worked out by a few lines of arithmetic instead of a shadow map (no real-time shadows, so it is free on a phone).
// Each nearby lamp (the street lamps and door lights that NightLamps already knows about) throws one soft stretched blob from the bike,
// pointing away from that lamp. The nearer the lamp, the stronger the blob. How long it is follows from the lamp's height and how far away the bike is.
// The lamps stand alternately on the two sides of the road, so as the bike rides past one the shadow swings from behind it to in front
// while the next lamp's shadow grows in from the other side: a loop of shadows that repeats with every lamp.
public class BikeShadow : MonoBehaviour
{
    const int Count = 3;            // shadows drawn at once (the nearest lamps)
    const float BikeHeight = 2.2f;  // what casts it
    const float MaxReach = 22f;     // lamps further away than this are ignored

    Transform[] quad = new Transform[Count];
    Material[] mat = new Material[Count];
    Renderer[] rend = new Renderer[Count];
    Transform bike;

    // the current picks, kept between frames
    readonly NightLamps.Source[] pick = new NightLamps.Source[Count];
    readonly float[] pickW = new float[Count];

    void Start()
    {
        bike = transform.parent != null ? transform.parent : transform;
        if (!BlobShadow.Available) { enabled = false; return; }
        for (int i = 0; i < Count; i++)
        {
            var q = BlobShadow.Create(null);
            q.name = "BikeLampShadow";
            mat[i] = BlobShadow.NewMat();
            rend[i] = q.GetComponent<Renderer>();
            rend[i].sharedMaterial = mat[i];
            quad[i] = q.transform;
            q.SetActive(false);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < Count; i++) if (quad[i] != null) Destroy(quad[i].gameObject);
    }

    void LateUpdate()
    {
        float night = Mathf.Clamp01(NightLights.Amount * 1.5f);
        if (night < 0.03f || !GameConfig.BlobShadows || bike == null) { Hide(); return; }

        Vector3 bp = bike.position;
        // weigh every lamp near the bike, keep the Count strongest
        for (int i = 0; i < Count; i++) { pick[i] = null; pickW[i] = 0f; }
        var src = NightLamps.Sources;
        for (int i = 0; i < src.Count; i++)
        {
            var s = src[i];
            if (s == null || s.t == null || !s.t.gameObject.activeInHierarchy) continue;
            Vector3 d = bp - s.t.position;
            d.y = 0f;
            float dist = d.magnitude;
            float reach = Mathf.Min(MaxReach, Mathf.Max(6f, s.range * 1.2f));
            if (dist > reach) continue;
            float w = 1f - dist / reach;
            w *= w;
            // insert into the sorted top list
            for (int k = 0; k < Count; k++)
            {
                if (w > pickW[k])
                {
                    for (int m = Count - 1; m > k; m--) { pick[m] = pick[m - 1]; pickW[m] = pickW[m - 1]; }
                    pick[k] = s; pickW[k] = w;
                    break;
                }
            }
        }

        for (int i = 0; i < Count; i++)
        {
            var s = pick[i];
            if (s == null) { quad[i].gameObject.SetActive(false); continue; }
            Vector3 lp = s.t.position;
            Vector3 d = bp - lp;
            d.y = 0f;
            float dist = d.magnitude;
            Vector3 dir = dist > 0.05f ? d / dist : Vector3.forward;

            // shadow length from similar triangles: height of the bike over the height of the lamp above it, times the distance
            float lampH = Mathf.Clamp(lp.y, 2.8f, 6f);
            float len = Mathf.Clamp(dist * BikeHeight / (lampH - BikeHeight) * 0.3f, 0.9f, 2.0f);   // kept short and blobby: a long spoke looked like a clock hand
            // right under the lamp the shadow tucks in under the bike
            float under = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 3f, dist));
            len = Mathf.Lerp(0.9f, len, under);
            float width = Mathf.Lerp(1.15f, 1.0f, under);

            float a = Mathf.Clamp01(0.55f * Mathf.Pow(pickW[i], 0.8f)) * night;
            mat[i].color = new Color(0.02f, 0.03f, 0.07f, a);

            // centred half a length away from the bike (starting just under it), lying flat and turned to point along dir
            Vector3 c = bp + dir * (len * 0.5f - 0.1f);
            c.y = bp.y + 0.08f + i * 0.002f;
            quad[i].position = c;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            quad[i].rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
            quad[i].localScale = new Vector3(width, len + 0.5f, 1f);
            quad[i].gameObject.SetActive(a > 0.01f);
        }
    }

    void Hide()
    {
        for (int i = 0; i < Count; i++) if (quad[i] != null && quad[i].gameObject.activeSelf) quad[i].gameObject.SetActive(false);
    }
}
