Shader "o3n/Stunner Hair Shader Cutout"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0.2, 0.97)) = 0.5
		_BumpScale("Scale", Range(0.0, 2.0)) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 1.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
		// Hair shader parameters
		_HairHiglightStrength("Overall Highlight Strength", Range(0 , 1)) = 0.35
		_HighlightBias("Highlight Bias", Range(-1 , 1)) = -0.2
		_HighlighWhiteness("Highlight Whiteness", Range(0 , 1)) = 0.3
		_SecondaryHighlightOffset("Secondary Highlight Offset", Range(0 , 1)) = 0.45
		_PrimaryHighlightExponent("Primary Highlight Exponent", Range(10 , 200)) = 60
		_SecondaryHighlightExponent("Secondary Highlight Exponent", Range(10 , 200)) = 50
		_NoiseFactor("Noise Factor", Range(0 , 1)) = 0.2
		_NoiseStrengthU("Noise Strength U", Range(0 , 200)) = 80
		_NoiseStrengthV("Noise Strength V", Range(0 , 200)) = 10
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
		
		Cull Off
	
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
        #pragma surface surf StandardSpecular fullforwardshadows
        #pragma target 3.0
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };
        
        uniform sampler2D _MainTex;
        uniform float _Cutoff;
        uniform sampler2D _BumpMap;
        uniform float _BumpScale;
        
        uniform float _HairHiglightStrength;
        uniform float _HighlightBias;
        uniform float _HighlighWhiteness;
        uniform float _SecondaryHighlightOffset;
        uniform float _PrimaryHighlightExponent;
        uniform float _SecondaryHighlightExponent;
        uniform float _NoiseFactor;
        uniform float _NoiseStrengthU;
        uniform float _NoiseStrengthV;
        
        float3 permute(float3 x) {
            return fmod(x*x*34.0 + x, 289);
        }

        // Snoise function source: https://gist.github.com/fadookie/25adf86ae7e2753d717c
        float snoise(float2 v)
        {
            const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
            float2 i = floor( v + dot(v, C.yy) );
            float2 x0 = v - i + dot(i, C.xx);
            int2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
            float4 x12 = x0.xyxy + C.xxzz;
            x12.xy -= i1;
            i = fmod(i, 289);
            float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0 )) + i.x + float3(0.0, i1.x, 1.0 ));
            float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
            m = m*m ;
            m = m*m ;
            float3 x = 2.0 * frac(p * C.www) - 1.0;
            float3 h = abs(x) - 0.5;
            float3 ox = floor(x + 0.5);
            float3 a0 = x - ox;
            m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
            float3 g;
            g.x = a0.x * x0.x + h.x * x0.y;
            g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            return 130.0 * dot(m, g);
        }
        
        // square root is not applied for calculating sinTH since we pow() it later with exponent. Same effect can be achived with 2x exponent value.
        float kajiyaKay(float3 binormalDir, float3 normalDir, float3 h, float exponent, float shift, float offset)
        {
            float3 tangent = normalize(binormalDir +  normalDir * shift);
            tangent = normalize(float3(tangent.x, tangent.y + offset, tangent.z));
            float dotTH = dot(tangent , h);
            float sinTH = 1.0 - (dotTH * dotTH);
            float dirAtten = smoothstep(-1.0, 0.0, dotTH);
            return dirAtten * pow(sinTH , 2 * exponent);
        }
        
        float3 calculateHighlightComponent(float3 color, float kajiyaKayCompoenent, float lightToWorldNormalFactor) 
        {
            return color * kajiyaKayCompoenent * lightToWorldNormalFactor * _LightColor0 * _LightColor0.a;
        }
        
        float3 getNormalDir(float3 normal, float3 tangent, float3 binormal, float3 worldNormal)
        {
            return normalize(normal.x * tangent + normal.y * binormal + normal.z * worldNormal);
        }
        
        float3 getAlbedoWithHighlight(Input i, float4 mainTex, float3 normal, float noise)
        {
            float3 whiteColor = float3(1,1,1);
            float3 primaryHighlightColor = lerp(mainTex.rgb, whiteColor, _HighlighWhiteness);
            float3 secondaryHighlightColor = lerp(mainTex.rgb, whiteColor, _HighlighWhiteness * 0.3);
            
            float3 worldNormal = WorldNormalVector(i, float3(0, 0, 1));
            float3 worldBinormal = WorldNormalVector(i, float3(0, 1, 0));
            float3 worldTangent = WorldNormalVector(i, float3(1, 0, 0));
            float3 worldLightDir = UnityWorldSpaceLightDir(i.worldPos);
            float3 worldViewDir = UnityWorldSpaceViewDir(i.worldPos);
            float3 h = normalize(worldViewDir + worldLightDir);
            float3 normalDir = getNormalDir(normal, worldTangent, worldBinormal, worldNormal);
            float3 binormalDir = normalize(cross(worldTangent, normalDir));
            
            float primaryKajiyaKayComponent = kajiyaKay(binormalDir, normalDir, h, _PrimaryHighlightExponent, _HighlightBias, noise);
            float secondaryKajiyaKayComponent = kajiyaKay(binormalDir, normalDir, h, _SecondaryHighlightExponent, _HighlightBias, _SecondaryHighlightOffset + noise);
            float cosLightToWorldNormal = clamp(dot(worldLightDir, worldNormal), 0.0 , 1.0 );
            
            float3 primaryHighlightComponent = calculateHighlightComponent(primaryHighlightColor, primaryKajiyaKayComponent, cosLightToWorldNormal);
            float3 secondaryHighlightComponent = calculateHighlightComponent(secondaryHighlightColor, secondaryKajiyaKayComponent, cosLightToWorldNormal);
            
            return mainTex.rgb + _HairHiglightStrength * (primaryHighlightComponent + secondaryHighlightComponent);
        }
        
        void surf(Input i , inout SurfaceOutputStandardSpecular o)
        {
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, i.uv_MainTex), _BumpScale);
            float4 mainTex = tex2D(_MainTex, i.uv_MainTex);
            float noise = mainTex.a * _NoiseFactor * snoise(i.uv_MainTex * float2(_NoiseStrengthU , _NoiseStrengthV));
            o.Alpha = mainTex.a;
            o.Albedo = getAlbedoWithHighlight(i, mainTex, o.Normal, noise);
            clip(o.Alpha - _Cutoff);
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
