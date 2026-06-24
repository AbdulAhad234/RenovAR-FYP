Shader "Custom/OcclusionMask"
{
    Properties { }

    SubShader
    {
        // Render BEFORE transparent objects, AFTER opaque ones
        Tags
        {
            "Queue" = "Geometry-1"
            "RenderType" = "Opaque"
        }

        // Write to depth buffer, but render NOTHING to screen
        ColorMask 0          // Don't write any color
        ZWrite On            // DO write to depth buffer (this is the key!)
        ZTest LEqual

        Pass { }
    }
}