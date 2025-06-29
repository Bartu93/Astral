Shader "Unlit/WaterWithEdgeFoam"
{
    Properties
    {
        _Color("Main Color", Color) = (0.2, 0.5, 0.8, 1)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamIntensity("Foam Intensity", Range(0, 1)) = 0.5
        _FoamDepthThreshold("Foam Depth Threshold", Range(0, 0.05)) = 0.02
    }
    SubShader
    {
        Tags{ "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 scrPos : TEXCOORD2;
            };

            sampler2D _CameraDepthTexture;
            float4 _Color;
            float4 _FoamColor;
            float _FoamIntensity;
            float _FoamDepthThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0));
                o.scrPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample depth and calculate depth difference
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)));
                float depthDiff = abs(sceneDepth - i.scrPos.w);

                // Apply foam only at depth differences within threshold
                float edgeFoam = smoothstep(_FoamDepthThreshold * 0.5, _FoamDepthThreshold, depthDiff) * _FoamIntensity;

                // Blend foam color along detected edges
                float4 baseColor = _Color;
                float4 foamColor = _FoamColor * edgeFoam;

                // Combine base color with foam
                return lerp(baseColor, foamColor, edgeFoam);
            }
            ENDCG
        }
    }
}
