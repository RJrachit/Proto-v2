using UnityEngine;

// Put this on a collider that should hold a paper that lands on it (a table top): the first landing keeps only a small
// share of the paper's sliding speed, so it stays where it lands instead of skidding off the edge.
public class StickySurface : MonoBehaviour
{
    [Range(0f, 1f)] public float skidKeep = 0.08f;
}
