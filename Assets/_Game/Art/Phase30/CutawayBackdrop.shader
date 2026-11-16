Shader "Ian/Presentation/CutawayBackdrop" {
 Properties { _Center("Center",Color)=(.16,.18,.22,1) _Edge("Edge",Color)=(.08,.10,.14,1) }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct input {float4 vertex:POSITION;};struct varying {float4 vertex:SV_POSITION;float4 screen:TEXCOORD0;};
 float4 _Center,_Edge;
 varying vert(input v){varying o;o.vertex=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.vertex);return o;}
 fixed4 frag(varying i):SV_Target {float2 uv=i.screen.xy/i.screen.w;float2 pixel=floor(uv*_ScreenParams.xy/4);float grain=frac(sin(dot(pixel,float2(12.9898,78.233)))*43758.5453);float v=saturate(length((uv-.5)*float2(1.15,1.7)));float3 c=lerp(_Center.rgb,_Edge.rgb,smoothstep(.05,.85,v));c+=(grain-.5)*.012;return fixed4(c,1);}
 ENDCG}
 }
}
