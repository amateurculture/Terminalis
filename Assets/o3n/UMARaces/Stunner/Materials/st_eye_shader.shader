// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "o3n/Stunner Eye Shader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Base Albedo", 2D) = "white" {}
		_IrisTex("Iris Albedo", 2D) = "white" {}
		
		_Glossiness("Glossiness", Range(0.0, 1.0)) = 1.0
		_GlossMapScale("Specular Factor", Range(0.0, 1.0)) = 0.5

		_SpecGlossMap("Specular", 2D) = "white" {}

		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Scale", Range(0.0, 2.0)) = 1.0

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_IrisGlowMask("Iris Glow Mask", 2D) = "white" {}
		_IrisGlowScale("Iris glow scale", Range(0.0, 1.0)) = 0.0
		
		_AnteriorChamberDepth("Anterior Chamber Depth",Range(0.0,2.0)) = 0.1
		_RadiusVisible ("Cornea Radius",Range(0.0,2.0)) = 0.16
		_FadeRatio ("Fade_ratio",Range(0.5,0.99)) = 0.9
		_EyeSizeFactor ("Eye Size Factor",Range(0.0,1.0)) = 0.3
		_FrontNormal ("Front Normal",Vector) = (0, 0, 1, 0)

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
	

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			
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
        #pragma surface surf StandardSpecular fullforwardshadows
        #pragma target 3.0
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            INTERNAL_DATA
        };
        
        uniform sampler2D _MainTex;
        uniform sampler2D _IrisTex;
        uniform sampler2D _SpecGlossMap;
        uniform sampler2D _OcclusionMap;
        uniform float _OcclusionStrength;
        uniform sampler2D _BumpMap;
        uniform float _BumpScale;
        uniform float _Glossiness;
        uniform float _GlossMapScale;
        uniform sampler2D _IrisGlowMask;
        uniform float _IrisGlowScale;
        uniform float _RadiusVisible;
        uniform float _AnteriorChamberDepth;
        uniform float _EyeSizeFactor;
        uniform float _FadeRatio;
        uniform float4 _BlackColor = float4(0,0,0,1);
        fixed4 _FrontNormal;
        
        float2 physicallyBased(float3 viewW, float3 normalW, float mask, float heightW) 
        { 
            float3 refractedW = viewW;
            float cosAlpha = dot(_FrontNormal, -refractedW);
            float dist = heightW / cosAlpha;
            float3 offsetW = dist * refractedW;
            return float2(mask, mask) * offsetW;
        }
        
        float getMask(float distanceSqr)
        {
            return 1 - distanceSqr;
        }
        
        float getHeight(float distanceSqr) {
            float r = distanceSqr / _RadiusVisible * _EyeSizeFactor;
            return _AnteriorChamberDepth * saturate(1.0 - 18.4 * r);
        }
        
        float getDistanceToUvCenter(float2 uv)
        {
            return sqrt(pow(0.5 - uv.x, 2) + pow(0.5 - uv.y, 2));
        }
        
        float getDistanceSqrToUvCenter(float2 uv)
        {
            return pow(0.5 - uv.x, 2) + pow(0.5 - uv.y, 2);
        }
        
        float2 correctOffset(float2 offset, float2 uv)
        {
            float offsetLength = getDistanceToUvCenter(uv + offset);
            if (offsetLength < _RadiusVisible) {
               return offset;
            } else {
                float ratio = _RadiusVisible / offsetLength; 
                return offset * ratio;
            }
        }
        
        float2 getRefractionOffset(Input i, float distanceSqr)
        {
            float mask = getMask(distanceSqr);
            float height = getHeight(distanceSqr);
            float2 offset = physicallyBased(i.viewDir, i.worldNormal, mask, height);
            return correctOffset(offset, i.uv_MainTex);
        }
        
        float getIrisRaito(float2 uv, float distanceSqr)
        {
            float fadeStart = pow(_RadiusVisible * _FadeRatio, 2);
            float fadeRange = pow(_RadiusVisible, 2) - fadeStart;
            if (distanceSqr < fadeStart) {
                return 1;
            } else {
                float inRange = distanceSqr - fadeStart;
                return saturate(1 - inRange / fadeRange);
            }
        }
        
        void surf(Input i , inout SurfaceOutputStandardSpecular o)
        {
            float distanceSqrToCenter = getDistanceSqrToUvCenter(i.uv_MainTex);
            float irisRatio = getIrisRaito(i.uv_MainTex, distanceSqrToCenter);
            
            float2 offsetUv = getRefractionOffset(i, distanceSqrToCenter) + i.uv_MainTex;
            float4 irisTex = lerp(tex2D(_IrisTex, offsetUv), _BlackColor, 1 - irisRatio);
            float4 mainTex = tex2D(_MainTex, i.uv_MainTex);
            o.Albedo = lerp(mainTex.rgb, irisTex.rgb, irisRatio);
            
            o.Alpha = mainTex.a;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, i.uv_MainTex), _BumpScale);
            o.Occlusion = tex2D(_OcclusionMap, i.uv_MainTex) * _OcclusionStrength;
            
            float4 specular = tex2D(_SpecGlossMap, offsetUv);
            float4 smoothness = tex2D(_SpecGlossMap, i.uv_MainTex);
            o.Specular = lerp(specular.rgb, specular.rgb * .25, 1 - irisRatio) * _GlossMapScale;
            o.Smoothness = specular.a * _Glossiness;
            float4 irisGlowMask = tex2D(_IrisGlowMask, offsetUv);
            o.Emission = lerp(o.Albedo * irisGlowMask, 0, saturate((1 - irisRatio) * 2)) * irisGlowMask.a * _IrisGlowScale;
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
