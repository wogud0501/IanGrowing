Shader "Ian/UI/SpecimenInk"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil operation", Float) = 0
        _StencilWriteMask ("Stencil write mask", Float) = 255
        _StencilReadMask ("Stencil read mask", Float) = 255
        _ColorMask ("Color mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            struct Attributes
            {
                float4 position : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 clipPosition : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.localPosition = input.position.xy;
                output.clipPosition = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 texel = (tex2D(_MainTex, input.uv) + _TextureSampleAdd) * input.color;
                fixed value = dot(texel.rgb, fixed3(0.2126, 0.7152, 0.0722));
                #ifdef UNITY_UI_CLIP_RECT
                    texel.a *= UnityGet2DClipping(input.localPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(texel.a - 0.001);
                #endif
                return fixed4(value, value, value, texel.a);
            }
            ENDCG
        }
    }
}
