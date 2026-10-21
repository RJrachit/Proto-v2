using UnityEngine;

// Builds the simple unlit material used by particles and the aim line.
// Shader.Find only works in a phone build if the shader was included in it; otherwise it returns null and
// `new Material(null)` throws. This tries several shaders, then falls back to the material of a built-in primitive,
// which is always in the build.
public static class Mat
{
    static readonly string[] Candidates =
    {
        "Sprites/Default",
        "Universal Render Pipeline/Particles/Unlit",
        "Particles/Standard Unlit",
        "Legacy Shaders/Particles/Alpha Blended",
        "Unlit/Color",
    };

    public static Material Particle()
    {
        foreach (var name in Candidates)
        {
            var shader = Shader.Find(name);
            if (shader != null && shader.isSupported) return new Material(shader);
        }

        Debug.LogWarning("Mat: no unlit shader found in this build, using the default primitive material.");
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var gr = g.GetComponent<Renderer>();
        Prim.FixMaterial(gr);
        var m = new Material(gr.sharedMaterial);
        Object.Destroy(g);
        return m;
    }
}
