Shader "Hidden/BrightnessShader" {
 Properties {
 _MainTex ("Base (RGB)", 2D) = "white" {}
 _brightness ("Brightness", Range (0, 100)) = 100
 }
 SubShader {
 Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 
 #include "UnityCG.cginc"
 
 uniform sampler2D _MainTex;
 uniform float _brightness;

 float4 frag(v2f_img i) : COLOR {
 float4 result = tex2D(_MainTex, i.uv) * (_brightness/100);

 return result;
 }
 ENDCG
 }
 }
}