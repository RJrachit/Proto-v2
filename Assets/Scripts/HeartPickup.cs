using UnityEngine;

// A floating heart. Hit it with a thrown paper to win a life.
// It can hang anywhere: over a balcony, a porch roof, a garage, a car, a stall, a tree, or low over a lawn,
// on any house, including ones with no customer. One per road segment at most (SegmentSpawner places it).
public class HeartPickup : MonoBehaviour
{
    Transform visual;
    Vector3 basePos;
    float phase;
    bool collected;

    public static HeartPickup Create(Transform parent)
    {
        var root = new GameObject("Heart");
        root.transform.SetParent(parent, false);

        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.9f;   // forgiving: the paper only has to pass close by

        var heart = root.AddComponent<HeartPickup>();

        // A flat heart from three primitives: a square turned on its corner plus two round lobes.
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(root.transform, false);
        visualGo.transform.localScale = Vector3.one * 1.6f;
        heart.visual = visualGo.transform;
        Color red = new Color(0.95f, 0.15f, 0.25f);
        Prim.Make(PrimitiveType.Cube, "HeartBody", visualGo.transform, Vector3.zero,
                  new Vector3(0.5f, 0.5f, 0.3f), red, false, new Vector3(0f, 0f, 45f));
        Prim.Make(PrimitiveType.Sphere, "HeartLobeL", visualGo.transform, new Vector3(-0.177f, 0.177f, 0f),
                  new Vector3(0.5f, 0.5f, 0.3f), red);
        Prim.Make(PrimitiveType.Sphere, "HeartLobeR", visualGo.transform, new Vector3(0.177f, 0.177f, 0f),
                  new Vector3(0.5f, 0.5f, 0.3f), red);

        root.SetActive(false);
        return heart;
    }

    public void Show(Vector3 worldPos)
    {
        basePos = worldPos;
        transform.position = worldPos;
        phase = Random.value * 6.28f;
        collected = false;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 2f + phase) * 0.15f);
        visual.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
    }

    // Called by a paper that touches the heart.
    public void Collect()
    {
        if (collected) return;
        collected = true;
        var gm = GameManager.Instance;
        if (gm != null) gm.CollectHeart(transform.position);
        Hide();
    }
}
