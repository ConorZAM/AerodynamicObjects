using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ArrowSettings
{
    //public static float scale = 1f;
    public static float arrowAlpha = 0.5f;
    public static Color liftColour = new Color(0, 1, 0, arrowAlpha);
    public static Color dragColour = new Color(1, 0, 0, arrowAlpha);
    public static Color windColour = new Color(0, 1, 1, arrowAlpha);

    public static void ToFadeMode(this Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
