Shader "Ian/Seated Character"
{
    Properties
    {
        _MainTex ("Original actor", 2D) = "white" {}
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
            struct Vertex { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 joint : TEXCOORD1; };
            struct Fragment { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 joint : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _SkinUV;
            float _FrontFold;
            Fragment vert(Vertex v)
            {
                Fragment o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.joint = v.joint;
                o.color = v.color;
                return o;
            }
            fixed4 frag(Fragment i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex, i.uv);
                clip(original.a - .88);
                float alpha = i.color.a;
                if (_FrontFold > 0 && abs(i.joint.x) > .075 && i.joint.y > .17 && i.joint.y < .46)
                {
                    // The original hand is folded onto the lap. Keep all shirt and
                    // sleeve pixels opaque, including dark garments against pale upholstery.
                    float3 skin = tex2D(_MainTex, _SkinUV.xy).rgb;
                    float hand = 1 - smoothstep(.34, .51, distance(original.rgb, skin));
                    alpha *= 1 - hand * _FrontFold;
                }
                return fixed4(original.rgb * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
