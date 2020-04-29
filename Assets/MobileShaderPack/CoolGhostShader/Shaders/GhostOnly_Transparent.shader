Shader "Custom/GhostOnly_Transparent" 
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
        
        sampler2D _MainTex;
		half4 _GhostColor;
		int _Pow;

		struct Input 
		{
		    float3 viewDir;
		    float2 uv_MainTex;
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
		    half4 c = tex2D (_MainTex, IN.uv_MainTex);
		    float3 worldNormal = WorldNormalVector(IN, o.Normal);
			o.Albedo = _GhostColor.rgb;
			
			half alpha = 1.0 - saturate(dot (normalize(IN.viewDir), worldNormal));
			alpha = pow(alpha, _Pow);
			o.Alpha = c.a * _GhostColor.a * alpha;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
