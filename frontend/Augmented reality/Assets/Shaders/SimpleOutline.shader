Shader "Custom/SimpleOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 0, 1) // Green by default
        _OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+1" }
        Pass
        {
            Name "OUTLINE"
            Cull Front // Render the inside of the mesh
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                // Extrude vertex along normal
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor; // Solid green
            }
            ENDCG
        }
    }
}
