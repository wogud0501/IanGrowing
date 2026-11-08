Shader "Ian/PixelSoftMask" {
 Properties {
  [PerRendererData] _MainTex("Sprite",2D)="white" {}
  _Color("Tint",Color)=(1,1,1,1)
  [PerRendererData] _RendererColor("Renderer tint",Color)=(1,1,1,1)
  [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend",Float)=10
 }
 SubShader {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Cull Off Lighting Off ZWrite Off Blend SrcAlpha [_DstBlend]
  Pass {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment Frag
   #include "UnitySprites.cginc"
   fixed4 Frag(v2f input):SV_Target {
    float2 d=(floor(input.texcoord*24)/24-.5)*2;
    float alpha=ceil(saturate(1-dot(d,d))*4)/4;
    return fixed4(input.color.rgb,input.color.a*alpha);
   }
   ENDCG
  }
 }
}
