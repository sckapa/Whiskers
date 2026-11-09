// Whiskers Rig-ma-roll — CRT screen pass
// Put this material on the UI RawImage that shows your 480x270 RenderTexture
// (or any full-screen quad sampling it). Keeps the art crisp (point-snapped),
// then adds barrel curve, rounded bezel, scanlines aligned to art pixels,
// RGB aperture mask, edge chromatic aberration, rolling band, flicker, vignette.
// Built-in pipeline and URP (unlit, UI-compatible).
Shader "Whiskers/CRTScreen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Game RenderTexture", 2D) = "black" {}
        _SourceRes   ("Source Resolution (art pixels)", Vector) = (480, 270, 0, 0)
        _Strength    ("Overall CRT Strength", Range(0,1)) = 0.85
        _Curvature   ("Barrel Curvature", Range(0,0.3)) = 0.06
        _CornerRadius("Corner Radius", Range(0,0.3)) = 0.06
        _Scanline    ("Scanline Darkness", Range(0,1)) = 0.42
        _Mask        ("RGB Mask Strength", Range(0,0.3)) = 0.07
        _Aberration  ("Edge Aberration (art px)", Range(0,2)) = 0.8
        _Vignette    ("Vignette", Range(0,1)) = 0.55
        _Roll        ("Roll Band Brightness", Range(0,0.3)) = 0.06
        _RollSpeed   ("Roll Band Speed", Float) = 0.11
        _Flicker     ("Flicker", Range(0,0.2)) = 0.05
        _BezelColor  ("Bezel Colour", Color) = (0.0314, 0.0, 0.1294, 1)
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SourceRes, _BezelColor, _Color;
            float _Strength, _Curvature, _CornerRadius, _Scanline, _Mask, _Aberration;
            float _Vignette, _Roll, _RollSpeed, _Flicker;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 scr : TEXCOORD1; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.scr = ComputeScreenPos(o.pos);
                o.color = v.color * _Color;
                return o;
            }

            float2 snapUV(float2 uv) { return (floor(uv * _SourceRes.xy) + 0.5) / _SourceRes.xy; }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c0 = i.uv * 2.0 - 1.0;
                float2 c  = c0 * (1.0 + _Curvature * dot(c0, c0));   // barrel
                float2 uv = c * 0.5 + 0.5;

                // rounded-rect screen edge -> bezel
                float2 qd = abs(c) - (1.0 - _CornerRadius);
                float outside = length(max(qd, 0.0)) + min(max(qd.x, qd.y), 0.0) - _CornerRadius;
                if (outside > 0.0) return fixed4(_BezelColor.rgb, 1) * i.color;

                // point-sampled art, with aberration growing toward the edges
                float ab = _Aberration * dot(c0, c0) * 0.5 / _SourceRes.x;
                float3 base;
                base.r = tex2D(_MainTex, snapUV(uv + float2(ab, 0))).r;
                base.g = tex2D(_MainTex, snapUV(uv)).g;
                base.b = tex2D(_MainTex, snapUV(uv - float2(ab, 0))).b;
                float3 col = base;

                // scanlines: darken half of every art-pixel row
                col *= 1.0 - _Scanline * step(0.5, frac(uv.y * _SourceRes.y));

                // aperture-grille RGB mask on real screen pixels
                float2 scrPx = (i.scr.xy / max(i.scr.w, 1e-5)) * _ScreenParams.xy;
                float m = fmod(floor(scrPx.x), 3.0);
                float3 mask = m < 0.5 ? float3(1 + _Mask, 1 - _Mask, 1 - _Mask)
                            : (m < 1.5 ? float3(1 - _Mask, 1 + _Mask, 1 - _Mask)
                                       : float3(1 - _Mask, 1 - _Mask, 1 + _Mask));
                col *= mask;

                // slow rolling bright band
                float band = frac(uv.y - _Time.y * _RollSpeed) - 0.5;
                col += _Roll * exp(-band * band * 64.0) * float3(1.0, 0.78, 1.0);

                // flicker (occasional dip)
                float n = frac(sin(floor(_Time.y * 20.0) * 12.9898) * 43758.5453);
                col *= 1.0 - _Flicker * step(0.93, n);

                // vignette + inner bezel shadow
                col *= lerp(1.0 - _Vignette, 1.0, 1.0 - smoothstep(0.55, 1.35, length(c0)));
                col *= lerp(0.35, 1.0, smoothstep(0.0, 1.0, saturate(-outside / 0.08)));

                col = lerp(base, col, _Strength);
                return fixed4(col, 1) * i.color;
            }
            ENDCG
        }
    }
    FallBack Off
}
