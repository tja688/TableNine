Shader "TableNine/CardFakeShatter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShatterAmount ("Shatter Amount", Range(0, 1)) = 0
        _HitDirection ("Hit Direction", Vector) = (1,0,0,0)
        _ShardScale ("Shard Scale", Float) = 7
        _ScatterStrength ("Scatter Strength", Float) = 0.42
        _CrackWidth ("Crack Width", Range(0, 0.15)) = 0.038
        _ImpactUv ("Impact UV", Vector) = (0.35, 0.5, 0, 0)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _RendererColor;
            float _ShatterAmount;
            float4 _HitDirection;
            float _ShardScale;
            float _ScatterStrength;
            float _CrackWidth;
            float4 _ImpactUv;
            float _EnableExternalAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif
                return o;
            }

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float VoronoiEdgeDistance(float2 uv, float scale, out float2 cellId)
            {
                float2 p = uv * scale;
                float2 ip = floor(p);
                float2 fp = frac(p);

                float minDist = 8.0;
                float minDist2 = 8.0;
                float2 closestCell = float2(0, 0);

                [unroll]
                for (int j = -1; j <= 1; j++)
                {
                    [unroll]
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 neighbor = float2(i, j);
                        float2 seed = Hash22(ip + neighbor);
                        float2 diff = neighbor + seed - fp;
                        float dist = dot(diff, diff);
                        if (dist < minDist)
                        {
                            minDist2 = minDist;
                            minDist = dist;
                            closestCell = ip + neighbor;
                        }
                        else if (dist < minDist2)
                        {
                            minDist2 = dist;
                        }
                    }
                }

                [unroll]
                for (int j2 = -1; j2 <= 1; j2++)
                {
                    [unroll]
                    for (int i2 = -1; i2 <= 1; i2++)
                    {
                        float2 neighbor = float2(i2, j2);
                        float2 neighborCell = ip + neighbor;
                        if (all(closestCell == neighborCell))
                        {
                            continue;
                        }

                        float2 seed = Hash22(neighborCell);
                        float2 diff = neighbor + seed - fp;
                        minDist2 = min(dot(diff, diff), minDist2);
                    }
                }

                cellId = closestCell;
                return sqrt(minDist2) - sqrt(minDist);
            }

            fixed4 SampleSprite(float2 uv, fixed4 tint)
            {
                fixed4 color = tex2D(_MainTex, uv) * tint;
                #if ETC1_EXTERNAL_ALPHA
                color.a = tex2D(_AlphaTex, uv).r;
                #endif
                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float shatter = saturate(_ShatterAmount);
                if (shatter <= 0.001)
                {
                    return SampleSprite(uv, i.color);
                }

                float2 cellId;
                float edge = VoronoiEdgeDistance(uv, _ShardScale, cellId);
                float crack = smoothstep(_CrackWidth * shatter * 0.25, _CrackWidth * shatter, edge);
                if (crack <= 0.01)
                {
                    discard;
                }

                float2 hitDir = _HitDirection.xy;
                if (dot(hitDir, hitDir) < 0.0001)
                {
                    hitDir = float2(1, 0);
                }
                hitDir = normalize(hitDir);

                float2 fromImpact = uv - _ImpactUv.xy;
                float2 scatterDir = fromImpact + hitDir * 0.72;
                if (dot(scatterDir, scatterDir) < 0.0001)
                {
                    scatterDir = hitDir;
                }
                scatterDir = normalize(scatterDir);

                float shardRand = Hash22(cellId).x;
                float scatter = shatter * _ScatterStrength * lerp(0.35, 1.05, shardRand);
                float2 spin = float2(-scatterDir.y, scatterDir.x) * (shardRand - 0.5) * shatter * 0.14;
                float2 sampleUv = uv - scatterDir * scatter - spin;

                fixed4 color = SampleSprite(sampleUv, i.color);
                color.a *= crack;
                return color;
            }
            ENDCG
        }
    }
}
