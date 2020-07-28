Shader "o3n/Stunner Standard Specular Shader With Skin" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_SpecGlossMap("Specular", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_DetailNormalMap("Normal Map", 2D) = "bump" {}
		_MaskTex ("Mask Texture (R - Skin)", 2D) = "white" {}
        _BRDFTex ("Brdf Map", 2D) = "gray" {}
        _BeckmannTex ("BeckmannTex", 2D) = "gray" {}
        _SpecPow ("Specularity", Range(0, 1)) = 0.1
        _GlossPow ("Smoothness", Range(0, 1)) = 1.0
        _AmbientContribution ("Ambience", Range(0, 1)) = 1
    }
    SubShader {
        Tags
		{
			"Queue" = "AlphaTest"
			"RenderType" = "TransparentCutout"
		}
        LOD 300

		Cull Back
        
        /*
		FORWARD PASS IS HANDLED WITH SURFACE SHADER PART AT THE END OF THIS SUBSHADER
		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend Zero One
			ZWrite [_ZWrite]
			
			Cull Off

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertBase
			#pragma fragment fragBase
			#include "UnityStandardCoreForward.cginc"
			
			ENDCG
		}*/
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			Cull Off

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual
			
			Cull Off

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _PARALLAXMAP
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }
			
			Cull Off

			CGPROGRAM
			#pragma target 3.0
			#pragma exclude_renderers nomrt


			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile ___ LIGHTMAP_ON
			#pragma multi_compile ___ DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile ___ DYNAMICLIGHTMAP_ON

			#pragma vertex vertDeferred
			#pragma fragment fragDeferred

			#include "UnityStandardCore.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		} 
        CGPROGRAM
        #pragma surface surf StandardSkin fullforwardshadows vertex:vert
        #pragma target 3.0
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"

		struct SurfaceOutputStandardSkin {
	   		fixed3 Albedo;
	    	half Specular;
	    	fixed3 Normal;
	    	half3 Emission;
	    	half Smoothness;
	    	half Occlusion;
	    	fixed Alpha;
			fixed Skin;
		};

        struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
            float2 texcoord1 : TEXCOORD1;
            float2 texcoord2 : TEXCOORD2;  
        };
            
        struct Input {
            float2 uv_MainTex;
            float3 viewDir;
            float3 coords0;
            float3 coords1;
        };
            
        sampler2D _MainTex;
        sampler2D _BumpMap;
		sampler2D _SpecGlossMap;
        sampler2D _OcclusionMap;
		sampler2D _DetailNormalMap;
		sampler2D _MaskTex;

        float _SpecPow;
        float _GlossPow;
        float _AmbientContribution;
		float _Cutoff;
            
        sampler2D _BRDFTex;
        sampler2D _BeckmannTex;
        
        void vert (inout appdata v, out Input o)
        {
            
            UNITY_INITIALIZE_OUTPUT(Input, o);	
    
            TANGENT_SPACE_ROTATION;
            o.coords0 = mul(rotation, UNITY_MATRIX_IT_MV[0].xyz);
            o.coords1 = mul(rotation, UNITY_MATRIX_IT_MV[1].xyz);
        }

		float Fresnel(float3 _half, float3 view, float f0) {
			float base = 1.0 - dot(view, _half);
            float exponential = pow(base, 5.0);
            return exponential + f0 * (1.0 - exponential);
		}

		half SpecularKSK(sampler2D beckmannTex, float3 normal, float3 light, float3 view, float roughness) {
				
			const float _specularFresnel = 1.08;
					
            half3 _half = view + light;
            half3 halfn = normalize(_half);

            half ndotl = max(dot(normal, light), 0.0);
            half ndoth = max(dot(normal, halfn), 0.0);

            half ph = pow(2.0 * tex2D(beckmannTex, float2(ndoth, roughness)).r, 10.0);
            half f = lerp(0.25, Fresnel(halfn, view, 0.028), _specularFresnel);
            half ksk = max(ph * f / dot(_half, _half), 0.0);
            
            return ndotl * ksk;   
		}

		half3 Skin_BRDF_PBS (SurfaceOutputStandardSkin s, float oneMinusReflectivity, half3 viewDir, UnityLight light, UnityIndirect gi)
		{
			half3 normalizedLightDir = normalize(light.dir);
            viewDir = normalize(viewDir);

            float3 occl = light.color.rgb * s.Occlusion;
            half specular = (s.Specular * SpecularKSK(_BeckmannTex, s.Normal, normalizedLightDir, viewDir , s.Smoothness) );

            float dotNL = dot(s.Normal, normalizedLightDir);
            float2 brdfUV = float2(dotNL * 0.5 + 0.5, 0.7 * dot(light.color, fixed3(0.2126, 0.7152, 0.0722)));
            half3 brdf = tex2D( _BRDFTex, brdfUV ).rgb;

            half nv = DotClamped (s.Normal, viewDir);
            half grazingTerm = saturate(1-s.Smoothness + (1-oneMinusReflectivity));
            
            half3 color = s.Albedo * (_AmbientContribution * gi.diffuse + occl * brdf) 
                        + specular * light.color
                        + gi.specular * FresnelLerp (specular, grazingTerm, nv) * _AmbientContribution * 0.1; // reduced this effect to 10% to get rid of bright rim effect
            // reflections
            color += BRDF3_Indirect(0, s.Specular, gi, grazingTerm, 0);
            return color;
		}

		half4 BRDF3_Unity_PBS__ (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi)
		{
			half3 reflDir = reflect (viewDir, normal);

			half nl = saturate(dot(normal, light.dir));
			half nv = saturate(dot(normal, viewDir));

			// Vectorize Pow4 to save instructions
			half2 rlPow4AndFresnelTerm = Pow4 (half2(dot(reflDir, light.dir), 1-nv));  // use R.L instead of N.H to save couple of instructions
			half rlPow4 = rlPow4AndFresnelTerm.x; // power exponent must match kHorizontalWarpExp in NHxRoughness() function in GeneratedTextures.cpp
			half fresnelTerm = rlPow4AndFresnelTerm.y;

			half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));

			half3 color = BRDF3_Direct(diffColor, specColor, rlPow4, smoothness);
			color *= light.color * nl;
			color += BRDF3_Indirect(diffColor, specColor, gi, grazingTerm, fresnelTerm);

			return half4(color, 1);
		}

		inline half4 LightingStandardSkin (SurfaceOutputStandardSkin s, half3 viewDir, UnityGI gi)
		{
			s.Normal = normalize(s.Normal);

			half oneMinusReflectivity;
			half3 specColor;
			s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, oneMinusReflectivity);

			half outputAlpha;
			s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, outputAlpha);
				
			half4 color = half4(0.0, 0.0, 0.0, 1.0);
			if (s.Skin > 0.5) {
				color.rgb = Skin_BRDF_PBS(s, oneMinusReflectivity, viewDir, gi.light, gi.indirect);
			} else {
				color.rgb = BRDF3_Unity_PBS__ (s.Albedo, s.Specular, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
			}
			color.a = outputAlpha;			
			return color;
		}

		inline void LightingStandardSkin_GI (SurfaceOutputStandardSkin s, UnityGIInput data, inout UnityGI gi)
		{
			gi = UnityGlobalIllumination (data, s.Occlusion, s.Smoothness, s.Normal);
		}

        void surf (Input IN, inout SurfaceOutputStandardSkin o) {
            
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
            if (c.a < _Cutoff) {
				discard;
			}
			// Albedo
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			
			// Normal
            float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			float3 detailNormal = UnpackNormal(tex2D(_DetailNormalMap, IN.uv_MainTex));
			o.Normal = BlendNormals(normal, detailNormal);
                                
            // SPECULAR / GLOSS / Occlusion
			half4 occlusion = tex2D (_OcclusionMap, IN.uv_MainTex);
			half4 specular = tex2D (_SpecGlossMap, IN.uv_MainTex);
            o.Specular = specular.rgb * _SpecPow;
            o.Smoothness = specular.a * _GlossPow;
            o.Occlusion = occlusion.rgb;

			// SET SKIN MASK
			half4 skinFilter = tex2D(_MaskTex, IN.uv_MainTex);
			o.Skin = skinFilter.r;
        }
        ENDCG
    }
    
    SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			
			Cull Off

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertBase
			#pragma fragment fragBase
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			Cull Off
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#include "UnityStandardCoreForward.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual
			
			Cull Off

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}
    
    FallBack "VertexLit"
}
