// Whiskers Rig-ma-roll — dice-select background vortex
// Procedural swirl, pixel-snapped to a low-res grid (default 480x270),
// Bayer-dithered and quantised to a 7-colour GBA 15-bit palette.
// Works in the Built-in pipeline and URP (unlit). Put it on a full-screen Quad,
// a SpriteRenderer, or a UI RawImage/Image behind your dice.
Shader "Whiskers/VortexBG"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _PixelRes ("Pixel Resolution (art pixels)", Vector) = (480, 270, 0, 0)
        _Speed    ("Speed", Float) = 1
        _Spin     ("Spin Speed", Float) = 0.35
        _Twist    ("Spiral Twist", Float) = 2.4
        _ArmMix   ("Spiral Arm Strength", Range(0,1)) = 0.35
        _CoreSize ("Core Glow Tightness", Float) = 22
        _EdgeDark ("Edge Darkness", Range(0,1)) = 0.58
        // Default = "Neon Night" (RGB555: 2,1,5 / 6,2,11 / 11,3,16 / 17,4,20 / 24,6,23 / 30,12,26 / 31,24,31)
        _C0 ("Palette 0 (darkest)", Color) = (0.0627, 0.0314, 0.1608, 1)
        _C1 ("Palette 1", Color) = (0.1922, 0.0627, 0.3529, 1)
        _C2 ("Palette 2", Color) = (0.3529, 0.0941, 0.5176, 1)
        _C3 ("Palette 3", Color) = (0.5490, 0.1294, 0.6471, 1)
        _C4 ("Palette 4", Color) = (0.7765, 0.1922, 0.7412, 1)
        _C5 ("Palette 5", Color) = (0.9686, 0.3882, 0.8392, 1)
        _C6 ("Palette 6 (core)", Color) = (1.0000, 0.7765, 1.0000, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _PixelRes;
            float _Speed, _Spin, _Twist, _ArmMix, _CoreSize, _EdgeDark;
            fixed4 _C0, _C1, _C2, _C3, _C4, _C5, _C6;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // 4x4 ordered (Bayer) dither, 0..1
            float bayer2(float2 a) { a = floor(a); return frac(dot(a, float2(0.5, a.y * 0.75))); }
            float bayer4(float2 a) { return bayer2(0.5 * a) * 0.25 + bayer2(a); }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 res = _PixelRes.xy;
                float2 fc  = floor(i.uv * res) + 0.5;          // snap to art pixel centre
                float2 p   = (fc - 0.5 * res) / res.y;
                p.y *= 1.35;

                float t = _Time.y * _Speed;
                float r = length(p);
                float a = atan2(p.y, p.x);

                float  sw = a + _Twist * log(r + 0.04) - t * _Spin;
                float2 q  = float2(cos(sw), sin(sw)) * (r * 5.0 + 0.6) + float2(t * 0.05, 0.0);

                [unroll] for (int k = 0; k < 3; k++)
                {
                    float fk = (float)k;
                    q += 0.6 * float2(sin(q.y * 1.4 + t * 0.45 + fk * 1.3),
                                      cos(q.x * 1.2 - t * 0.33 + fk * 2.1));
                }

                float v = 0.5 + 0.5 * sin(q.x + q.y * 0.8);
                v = pow(saturate(v), 1.4);
                float arms = 0.5 + 0.5 * sin(sw * 2.0 + r * 3.0);
                v = lerp(v, arms, _ArmMix);

                float fade = lerp(1.0 - _EdgeDark, 1.0, 1.0 - smoothstep(0.22, 1.15, r));
                float glow = exp(-r * r * _CoreSize) * (0.95 + 0.08 * sin(t * 2.3));
                v = saturate(v * 0.8 * fade + glow);

                float idx = clamp(floor(v * 6.0 + bayer4(fc) * 0.999), 0.0, 6.0);

                fixed3 pal[7] = { _C0.rgb, _C1.rgb, _C2.rgb, _C3.rgb, _C4.rgb, _C5.rgb, _C6.rgb };
                fixed3 col = pal[(int)idx];
                return fixed4(col, 1) * i.color;
            }
            ENDCG
        }
    }
    FallBack Off
}
