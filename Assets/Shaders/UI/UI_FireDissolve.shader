Shader "ChasingLight/UI_FireDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BurnProgress ("Burn Progress", Range(0, 1.2)) = 0.0
        _BurnColor ("Burn Edge Color", Color) = (1.0, 0.45, 0.05, 1.0)
        _BurnEdgeWidth ("Burn Edge Width", Range(0.005, 0.2)) = 0.06
        _BurnCenter ("Burn Center (UV)", Vector) = (0.5, 0.5, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _BurnProgress;
            fixed4 _BurnColor;
            float _BurnEdgeWidth;
            float4 _BurnCenter;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                if (_BurnProgress <= 0.001)
                {
                    discard;
                    return fixed4(0, 0, 0, 0);
                }

                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Distance from burn center
                float2 uvOffset = IN.texcoord - _BurnCenter.xy;
                float dist = length(uvOffset) * 1.45;

                // Procedural organic flame turbulence
                float flameNoise = sin(IN.texcoord.x * 28.0 + _BurnProgress * 6.0) * cos(IN.texcoord.y * 28.0) * 0.035
                                 + sin(IN.texcoord.x * 56.0 - IN.texcoord.y * 42.0) * 0.015;

                float effectiveDist = dist + flameNoise;
                float burnFront = _BurnProgress * 1.55;

                // Not yet burned through -> transparent to show game underneath
                if (effectiveDist > burnFront)
                {
                    discard;
                    return fixed4(0, 0, 0, 0);
                }

                // Fiery edge rim
                float edgeDist = burnFront - effectiveDist;
                if (edgeDist < _BurnEdgeWidth)
                {
                    float edgeFactor = 1.0 - (edgeDist / _BurnEdgeWidth);
                    // Golden-orange burning emission on the edge
                    color.rgb = lerp(color.rgb, _BurnColor.rgb * 1.6, edgeFactor);
                    color.a = max(color.a, edgeFactor);
                }

                return color;
            }
            ENDCG
        }
    }
}
