Shader "TableNine/BackgroundBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SharpTex ("Sharp Texture", 2D) = "white" {}
        _BlurOffset ("Blur Offset", Float) = 1.0
        _Intensity ("Intensity", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "BlurHorizontal"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurOffset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = float2(_BlurOffset * _MainTex_TexelSize.x, 0.0);
                fixed4 color = tex2D(_MainTex, i.uv) * 0.227027;
                color += tex2D(_MainTex, i.uv + offset) * 0.1945946;
                color += tex2D(_MainTex, i.uv - offset) * 0.1945946;
                color += tex2D(_MainTex, i.uv + offset * 2.0) * 0.1216216;
                color += tex2D(_MainTex, i.uv - offset * 2.0) * 0.1216216;
                color += tex2D(_MainTex, i.uv + offset * 3.0) * 0.054054;
                color += tex2D(_MainTex, i.uv - offset * 3.0) * 0.054054;
                color += tex2D(_MainTex, i.uv + offset * 4.0) * 0.016216;
                color += tex2D(_MainTex, i.uv - offset * 4.0) * 0.016216;
                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "BlurVertical"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurOffset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = float2(0.0, _BlurOffset * _MainTex_TexelSize.y);
                fixed4 color = tex2D(_MainTex, i.uv) * 0.227027;
                color += tex2D(_MainTex, i.uv + offset) * 0.1945946;
                color += tex2D(_MainTex, i.uv - offset) * 0.1945946;
                color += tex2D(_MainTex, i.uv + offset * 2.0) * 0.1216216;
                color += tex2D(_MainTex, i.uv - offset * 2.0) * 0.1216216;
                color += tex2D(_MainTex, i.uv + offset * 3.0) * 0.054054;
                color += tex2D(_MainTex, i.uv - offset * 3.0) * 0.054054;
                color += tex2D(_MainTex, i.uv + offset * 4.0) * 0.016216;
                color += tex2D(_MainTex, i.uv - offset * 4.0) * 0.016216;
                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "Composite"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _SharpTex;
            float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sharp = tex2D(_SharpTex, i.uv);
                fixed4 blurred = tex2D(_MainTex, i.uv);
                return lerp(sharp, blurred, saturate(_Intensity));
            }
            ENDCG
        }
    }

    FallBack Off
}
