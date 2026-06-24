Shader "Custom/WorldSpaceTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileScale ("Tile Scale (Repeats per Meter)", Float) = 1.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        float _TileScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv;
            
            // Project based on the dominant normal direction to handle floors and walls
            float3 absNormal = abs(IN.worldNormal);
            if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
            {
                // Floor/Ceiling: Map XZ
                uv = IN.worldPos.xz;
            }
            else if (absNormal.x > absNormal.z)
            {
                // Wall pointing X: Map ZY
                uv = IN.worldPos.zy;
            }
            else
            {
                // Wall pointing Z: Map XY
                uv = IN.worldPos.xy;
            }

            // Apply texture with the world space tiling scale
            fixed4 c = tex2D(_MainTex, uv * _TileScale);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
