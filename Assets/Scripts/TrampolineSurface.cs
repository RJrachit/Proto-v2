using UnityEngine;

// The pad of a trampoline (a round disc: on the lawn, under a reverse basket, or standing upright against a wall). A paper that hits it is thrown
// back (see Paper.OnCollisionEnter). On a hit the pad vibrates like a real trampoline: it dips, springs back past rest and rings down.
public class TrampolineSurface : MonoBehaviour
{
    [Tooltip("Share of the speed into the pad that comes back out.")]
    public float restitution = GameConfig.TrampolineBounce;
    [Tooltip("Share of the sideways speed the paper keeps.")]
    public float tangentKeep = GameConfig.TrampolineSlideKeep;
    [Tooltip("A pad standing upright against a wall: it works from the side (any contact) and sinks into the wall when hit.")]
    public bool vertical;
    public Vector3 pushDir;   // (vertical pad) which way it sinks, in the parent's space
    [Tooltip("The frame around a pad: it throws a paper back just like the pad, but does not vibrate.")]
    public bool still;

    const float Frequency = 6.5f;   // vibrations per second
    const float Damping = 3.2f;     // how fast it rings down
    const float Lifetime = 1.4f;    // the vibration is over after this many seconds
    const float Depth = 0.13f;      // how far the pad dips on a hit of full strength (metres)

    Vector3 baseScale = Vector3.one;
    Vector3 basePos;
    float age = -1f;                // seconds since the hit (< 0: at rest)
    float strength = 1f;

    void Awake()
    {
        baseScale = transform.localScale;
        basePos = transform.localPosition;
    }

    // strength: about 1 for a normal hit, more for a hard one.
    public void Squash(float hit = 1f)
    {
        if (still) return;
        strength = Mathf.Clamp(hit, 0.4f, 1.6f);
        age = 0f;
    }

    void Update()
    {
        if (age < 0f) return;
        age += Time.unscaledDeltaTime;
        if (age >= Lifetime)
        {
            age = -1f;
            transform.localScale = baseScale;
            transform.localPosition = basePos;
            return;
        }
        // a damped wave: down first, then up past rest, smaller each time
        float wave = Mathf.Exp(-Damping * age) * Mathf.Sin(2f * Mathf.PI * Frequency * age);
        Vector3 dir = vertical ? pushDir : Vector3.down;
        transform.localPosition = basePos + dir * (Depth * strength * wave);
        float spread = 1f + 0.035f * strength * wave;   // the disc swells a little as it dips, like a stretched skin
        transform.localScale = new Vector3(baseScale.x * spread, baseScale.y, baseScale.z * spread);
    }
}
