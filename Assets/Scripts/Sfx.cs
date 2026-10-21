using System.Collections.Generic;
using UnityEngine;

// Sound effects. Placeholder sounds are generated in code so the game has audio with no assets.
// To use your own sounds, put clips named whoosh, thwack, chime, miss, bell, pickup, crash, pull and
// windowBreak in Assets/Resources/Sfx/ and they replace the generated ones automatically.
public class Sfx : MonoBehaviour
{
    public static Sfx Instance { get; private set; }

    const int Rate = 22050;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource[] sources;
    int next;

    void Awake()
    {
        Instance = this;
        sources = new AudioSource[6];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }
        Build();
    }

    public static void Play(string key, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null) return;
        AudioClip clip;
        if (!Instance.clips.TryGetValue(key, out clip) || clip == null) return;
        var s = Instance.sources[Instance.next];
        Instance.next = (Instance.next + 1) % Instance.sources.Length;
        s.clip = clip;
        s.volume = volume;
        s.pitch = pitch;
        s.Play();
    }

    void Add(string key, float seconds, System.Func<float, float, float> wave)
    {
        var custom = Resources.Load<AudioClip>("Sfx/" + key);
        if (custom != null) { clips[key] = custom; return; }

        int n = (int)(Rate * seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = wave(i / (float)Rate, i / (float)n);
        var clip = AudioClip.Create(key, n, 1, Rate, false);
        clip.SetData(data, 0);
        clips[key] = clip;
    }

    void Build()
    {
        const float Tau = Mathf.PI * 2f;
        var rnd = new System.Random(7);
        System.Func<float> noise = () => (float)(rnd.NextDouble() * 2.0 - 1.0);

        float lp = 0f;
        Add("whoosh", 0.28f, (t, u) =>
        {
            lp += (noise() - lp) * 0.15f;
            return lp * Mathf.Sin(Mathf.PI * u) * 1.8f;
        });
        Add("thwack", 0.18f, (t, u) =>
            Mathf.Sin(Tau * 140f * t) * Mathf.Exp(-t * 28f) * 0.9f + noise() * Mathf.Exp(-t * 60f) * 0.4f);
        Add("chime", 0.45f, (t, u) =>
            (Mathf.Sin(Tau * 880f * t) + 0.5f * Mathf.Sin(Tau * 1320f * t)) * Mathf.Exp(-t * 7f) * 0.5f);
        Add("miss", 0.22f, (t, u) =>
            Mathf.Sin(Tau * (110f - 40f * u) * t) * Mathf.Exp(-t * 14f) * 0.6f);
        Add("bell", 0.7f, (t, u) =>
            (Mathf.Sin(Tau * 1200f * t) + 0.6f * Mathf.Sin(Tau * 1800f * t) + 0.4f * Mathf.Sin(Tau * 2400f * t))
            * Mathf.Exp(-t * 5f) * 0.35f);
        Add("pickup", 0.3f, (t, u) =>
            Mathf.Sin(Tau * (u < 0.5f ? 660f : 990f) * t) * Mathf.Exp(-(t % 0.15f) * 20f) * 0.5f);
        Add("crash", 0.4f, (t, u) =>
            (noise() * 0.5f + Mathf.Sin(Tau * 70f * t)) * Mathf.Exp(-t * 9f) * 0.8f);
        Add("thunk", 0.2f, (t, u) =>
            Mathf.Sin(Tau * (95f - 35f * u) * t) * Mathf.Exp(-t * 22f) * 0.9f
            + Mathf.Sin(Tau * 1500f * t) * Mathf.Exp(-t * 120f) * 0.25f);
        Add("pull", 0.05f, (t, u) =>
            Mathf.Sin(Tau * 500f * t) * Mathf.Exp(-t * 90f) * 0.5f);
        Add("windowBreak", 0.5f, (t, u) =>
            noise() * Mathf.Exp(-t * 12f) * 0.7f + Mathf.Sin(Tau * 2000f * t) * Mathf.Exp(-t * 30f) * 0.2f);
    }
}
