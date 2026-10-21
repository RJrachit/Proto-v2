using UnityEngine;
using UnityEngine.Rendering;

// Tiny helper for building the prototype out of Unity's built-in primitives.
public static class Prim
{
    // Referencing these types keeps them in a phone build. Without it Unity's code stripping can remove a collider
    // class and CreatePrimitive then fails with "Can't add component because class '...Collider' doesn't exist".
    static readonly System.Type[] KeepTypes =
    {
        typeof(BoxCollider), typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider),
    };

    // In a URP project, runtime-created primitives get the Built-in "Standard" material, which URP can't draw
    // (magenta in a phone build). Use the pipeline's own default material instead. Does nothing in Built-in projects.
    static Material urpLit;

    public static void FixMaterial(Renderer r)
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null) return;   // Built-in pipeline: the primitive's own material is fine

        if (urpLit == null)
        {
            // Prefer the URP Lit shader by name (added to the build by Editor/AddShadersToBuild).
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) urpLit = new Material(shader);
        }
        if (urpLit != null) r.sharedMaterial = urpLit;
        else if (pipeline.defaultMaterial != null) r.sharedMaterial = pipeline.defaultMaterial;
    }

    // A cylinder primitive comes with a capsule collider, which is the wrong shape for a flat disc: give it a collider that matches the disc.
    public static void RoundCollider(GameObject go)
    {
        var old = go.GetComponent<Collider>();
        if (old != null) Object.Destroy(old);
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        mc.convex = true;
    }

    public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 localPos,
                                  Vector3 scale, Color color, bool keepCollider = false,
                                  Vector3 euler = default(Vector3))
    {
        if (KeepTypes.Length == 0) return null;   // never true; just keeps KeepTypes referenced

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        FixMaterial(r);
        r.material.color = color;
        if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }
}
