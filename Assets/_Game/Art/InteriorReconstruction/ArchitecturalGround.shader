Shader "Ian/ArchitecturalGround" {
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct input {float4 vertex:POSITION;};
 struct varying {float4 vertex:SV_POSITION;float4 screen:TEXCOORD0;};
 varying vert(input v){varying o;o.vertex=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.vertex);return o;}
 fixed4 frag(varying i):SV_Target {float2 uv=i.screen.xy/i.screen.w;float vignette=saturate(length((uv-.5)*float2(1.15,1.7)));float3 color=lerp(float3(.48,.42,.34),float3(.30,.26,.22),smoothstep(.05,.8,vignette));return fixed4(color,1);}
 ENDCG}
 }
}
