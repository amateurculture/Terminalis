Shader "CuCat/CuCatShader" {
 Properties {
 _Color ("Main Color", Color) = (1,1,1,1)
 _Cutoff ("Alpha Cutoff", Float) = 0.5
 _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
 _RimColor ("Rim Color", Color) = (0,0,0,0.0)
 _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
 }
  
 SubShader {
 Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
 LOD 250
 Cull off
  
 CGPROGRAM
 #pragma surface surf WrapLambert
 sampler2D _MainTex;
 fixed4 _Color;
 fixed4 _RimColor;
 float _RimPower;
 fixed _Cutoff;
 
 half4 LightingWrapLambert (SurfaceOutput s, half3 lightDir, half atten) {
      half NdotL = dot (s.Normal, lightDir);
      half diff = NdotL * 0.5 + 0.5;
      half4 c;
      c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten * 2);
      c.a = s.Alpha;
      return c;
 	}
  
 struct Input {
 float2 uv_MainTex;
 float3 viewDir;
 };
  
 void surf (Input IN, inout SurfaceOutput o) {
 fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
 c *= _Color;
 
 half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
 o.Emission = _RimColor.rgb * pow (rim, _RimPower);
 
 o.Albedo = c.rgb;
 o.Alpha = c.a;
 clip(o.Alpha - _Cutoff);
 }
 ENDCG
 }
 
 Fallback "Diffuse"
 }
