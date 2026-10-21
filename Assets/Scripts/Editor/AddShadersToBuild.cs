#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// The game creates its objects at run time, so no asset in the project refers to the shaders they use, and Unity
// leaves those shaders out of a phone build (everything draws magenta). This adds them to the build's
// "Always Included Shaders" list automatically before every build. Lives in an Editor folder: editor only.
public class AddShadersToBuild : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    static readonly string[] ShaderNames =
    {
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Simple Lit",
        "Universal Render Pipeline/Unlit",
        "Universal Render Pipeline/Particles/Unlit",
        "Sprites/Default",
        "Unlit/Color",
        "PolyParcel/XRay",
        "PolyParcel/GlowThrough",
        "Standard",
    };

    public void OnPreprocessBuild(BuildReport report)
    {
        AddAll();
    }

    [MenuItem("Tools/Poly Parcel/Add shaders to build now")]
    public static void AddAll()
    {
        var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        if (graphicsSettings == null)
        {
            Debug.LogWarning("AddShadersToBuild: could not open GraphicsSettings.");
            return;
        }

        var so = new SerializedObject(graphicsSettings);
        var list = so.FindProperty("m_AlwaysIncludedShaders");
        int added = 0;

        foreach (var name in ShaderNames)
        {
            var shader = Shader.Find(name);
            if (shader == null) continue;

            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
            }
            if (present) continue;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            added++;
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
        Debug.Log("AddShadersToBuild: added " + added + " shader(s) to Always Included Shaders.");
    }
}
#endif
