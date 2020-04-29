Shader "Custom/GhostOnly_Solid" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_GhostColor ("Ghost Color", Color) = (0, 1, 0, 1)
		_Pow ("Pow Factor", int) = 2
	}
	
	SubShader 
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
		LOD 200
		
		Zwrite Off
        Ztest Always
        Blend SrcAlpha One
		CGPROGRAM
		#pragma surface surf Unlit keepalpha
         
		half4 _GhostColor;
		int _Pow;

		struct Input 
		{
		    float3 viewDir;
		};
		
		fixed4 LightingUnlit(SurfaceOutput s, fixed3 lightDir, fixed atten)
		 {
		     fixed4 c;
		     c.rgb = s.Albedo; 
		     c.a = s.Alpha;
		     return c;
		 }

		void surf (Input IN, inout SurfaceOutput o) 
		{
		    float3 worldNormal = WorldNormalVector(IN, o.Normal);
			o.Albedo = _GhostColor.rgb;
			
			half alpha = 1.0 - saturate(dot (normalize(IN.viewDir), worldNormal));
			alpha = pow(alpha, _Pow);
			o.Alpha = _GhostColor.a * alpha;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
