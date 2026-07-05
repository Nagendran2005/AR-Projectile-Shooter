Shader "Custom/PulseHighlight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _HighlightColor ("Highlight Color", Color) = (1, 1, 0, 1) // Defaults to Yellow
        _PulseSpeed ("Pulse Speed", Float) = 1.0 // 1.0 = exactly 1 pulse per second
        _MinEmission ("Min Emission", Float) = 0.5
        _MaxEmission ("Max Emission", Float) = 4.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _HighlightColor;
            float _PulseSpeed;
            float _MinEmission;
            float _MaxEmission;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Base texture sample
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 6.28318530718 is 2 * PI. 
                // Multiplying this by time means a Pulse Speed of 1.0 equals exactly 1 full pulse cycle per second.
                float frequency = _Time.y * _PulseSpeed * 6.28318530718;
                
                // sin() outputs between -1.0 and 1.0. 
                // This shifts and scales it smoothly into a 0.0 to 1.0 range.
                float pulse = (sin(frequency) + 1.0) * 0.5;
                
                // Smoothly interpolate between your min and max brightness settings using the pulse wave
                float currentIntensity = lerp(_MinEmission, _MaxEmission, pulse);
                
                // Combine the base texture/color with the pulsing HDR highlight color
                fixed4 finalColor = col * _HighlightColor * currentIntensity;

                // Apply fog if necessary
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
}