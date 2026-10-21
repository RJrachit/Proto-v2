using System.Collections.Generic;
using UnityEngine;

// A cow standing in the middle of the road. The bike can't steer, so the answer is to tap and ring the bell
// when it is close enough (see GameManager.bellRange). Ride into it and you lose a life.
public class RoadHazard : MonoBehaviour
{
    public static readonly List<RoadHazard> Active = new List<RoadHazard>();

    enum HazardState { Idle, Fleeing }

    HazardState state;
    float fleeDirection;
    bool resolved;
    Vector3 baseLocal;

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    public void Init(Vector3 localPosition) { baseLocal = localPosition; }

    // Called by SegmentSpawner every time the segment is (re)placed.
    public void Setup(bool present)
    {
        state = HazardState.Idle;
        resolved = false;
        transform.localPosition = baseLocal;
        gameObject.SetActive(present);
    }

    void Update()
    {
        var bike = BikeForward.Instance;
        var gm = GameManager.Instance;
        if (bike == null || gm == null || !gm.Playing) return;

        if (state == HazardState.Fleeing)
        {
            transform.position += new Vector3(fleeDirection * 9f * Time.deltaTime, 0f, 0f);
            return;
        }

        float dz = transform.position.z - bike.transform.position.z;

        if (dz > 0f && dz < 30f && PlayerPrefs.GetInt("HintBell", 0) == 0)
        {
            PlayerPrefs.SetInt("HintBell", 1);
            gm.ShowToast("Cow ahead! Tap to ring the bell", 3f);
        }

        if (!resolved && Mathf.Abs(dz) < 1.4f)
        {
            resolved = true;
            gm.HitHazard("Hit a cow!", transform.position);
            Flee();
        }
    }

    void Flee()
    {
        state = HazardState.Fleeing;
        fleeDirection = Random.value < 0.5f ? -1f : 1f;
    }

    // Scares every idle hazard within range ahead of the bike. Returns how many.
    public static int ScareAhead(Vector3 bikePosition, float range)
    {
        int count = 0;
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var h = Active[i];
            float dz = h.transform.position.z - bikePosition.z;
            if (h.state == HazardState.Idle && dz > 0f && dz <= range)
            {
                h.Flee();
                count++;
            }
        }
        return count;
    }
}
