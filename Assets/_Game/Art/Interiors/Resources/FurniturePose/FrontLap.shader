Shader "Ian/Front Seated Clothing"
{
    Properties
    {
        _MainTex ("Indexed trouser and sleeve artwork", 2D) = "white" {}
        _Costume ("Actor costume", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Vertex { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Fragment { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex, _Costume;
            float4 _ClothUV, _SkinUV, _ChinUV;
            float complexionScore(float4 swatch)
            {
                return swatch.a * (swatch.r * .7 + swatch.g * .3 + max(0, swatch.r - swatch.b));
            }
            float4 complexion(float4 a, float4 b)
            {
                return complexionScore(a) > complexionScore(b) ? a : b;
            }
            Fragment vert(Vertex v)
            {
                Fragment o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag(Fragment i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.uv);
                clip(mask.a - .5);
                float2 uv = mask.r < .30 ? _ClothUV.xy : mask.r < .67 ? _ClothUV.zw : _SkinUV.xy;
                fixed4 base = tex2D(_Costume, uv);
                if (mask.r >= .67)
                {
                    base = complexion(base, tex2D(_Costume, _SkinUV.zw));
                    base = complexion(base, tex2D(_Costume, _ChinUV.xy));
                    base = complexion(base, tex2D(_Costume, _ChinUV.zw));
                }
                float light = mask.g * 2;
                float3 ink = base.rgb * light + max(0, light - .82) * .10;
                return fixed4(ink * i.color.rgb, i.color.a);
            }
            ENDCG
        }
    }
}
