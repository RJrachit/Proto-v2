using UnityEngine;

// Phone-specific settings (portrait). Added once by PrototypeBootstrap.
public class MobileSetup : MonoBehaviour
{
    [Tooltip("Lock to portrait. Untick to allow landscape as well.")]
    public bool portraitOnly = true;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;           // phones default to 30
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (portraitOnly)
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }
        else
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.antiAliasing = GameConfig.MsaaSamples > 1 ? GameConfig.MsaaSamples : 0;
        QualitySettings.pixelLightCount = 1;
        ApplyEdgeSmoothing();
    }

    // The URP asset decides the real MSAA level and the render scale (read by reflection, like the other URP settings in this project, so no
    // package reference is needed). The values the asset had are logged, so the Console shows what changed.
    static void ApplyEdgeSmoothing()
    {
        try
        {
            var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (asset == null) { Debug.Log("MobileSetup: no render pipeline asset, MSAA set through the quality settings only."); return; }
            var t = asset.GetType();
            var msaa = t.GetProperty("msaaSampleCount");
            var scale = t.GetProperty("renderScale");
            string was = "";
            if (msaa != null && msaa.CanRead) was += "MSAA x" + msaa.GetValue(asset, null) + " ";
            if (scale != null && scale.CanRead) was += "render scale " + scale.GetValue(asset, null);
            if (msaa != null && msaa.CanWrite) msaa.SetValue(asset, Mathf.Max(1, GameConfig.MsaaSamples), null);
            if (scale != null && scale.CanWrite) scale.SetValue(asset, GameConfig.RenderScale, null);
            Debug.Log("MobileSetup: the render pipeline asset had " + was + "; now MSAA x" + GameConfig.MsaaSamples + ", render scale " + GameConfig.RenderScale);
        }
        catch (System.Exception e) { Debug.LogWarning("Could not set the anti-aliasing: " + e.Message); }
    }
}
