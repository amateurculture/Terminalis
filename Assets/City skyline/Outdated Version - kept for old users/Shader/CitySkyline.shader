// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CitySkyline"
{
	Properties
	{
		[NoScaleOffset]_AtlasDiffuse("Atlas Diffuse", 2D) = "white" {}
		_Albedocolor("Albedo color", Color) = (1,1,1,0)
		_Fresnelcolor("Fresnel color", Color) = (1,1,1,0)
		_Desaturation("Desaturation", Range( 0 , 1)) = 0
		[NoScaleOffset]_cubemap("cubemap", CUBE) = "white" {}
		[NoScaleOffset]_Atlas_Reflectivitymask("Atlas_Reflectivitymask", 2D) = "black" {}
		_FresnelSharpness("Fresnel Sharpness", Range( 0 , 10)) = 0
		_Fresnelintensity("Fresnel intensity", Range( 0 , 5)) = 0
		_reflectionblurriness("reflection blurriness", Range( 0 , 5)) = 0
		_Reflectiondarklevel("Reflection dark level", Float) = 1
		_Reflectionlightlevel("Reflection light level", Float) = 1
		_Reflectionopacity("Reflection opacity", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldRefl;
			INTERNAL_DATA
			float3 worldNormal;
			float3 worldPos;
		};

		uniform float4 _Albedocolor;
		uniform sampler2D _AtlasDiffuse;
		uniform float _Reflectiondarklevel;
		uniform float _Reflectionlightlevel;
		uniform samplerCUBE _cubemap;
		uniform float _reflectionblurriness;
		uniform float _Fresnelintensity;
		uniform float _FresnelSharpness;
		uniform float4 _Fresnelcolor;
		uniform sampler2D _Atlas_Reflectivitymask;
		uniform float _Reflectionopacity;
		uniform float _Desaturation;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			o.Normal = float3(0,0,1);
			float2 uv_AtlasDiffuse = i.uv_texcoord;
			float4 temp_output_60_0 = ( _Albedocolor * tex2D( _AtlasDiffuse, uv_AtlasDiffuse ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 texCUBENode3 = texCUBElod( _cubemap, float4( WorldReflectionVector( i , ase_worldNormal ), _reflectionblurriness) );
			float smoothstepResult29 = smoothstep( _Reflectiondarklevel , _Reflectionlightlevel , texCUBENode3.r);
			float4 blendOpSrc20 = temp_output_60_0;
			float blendOpDest20 = smoothstepResult29;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float fresnelNDotV8 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode8 = ( 0.0 + _Fresnelintensity * pow( 1.0 - fresnelNDotV8, _FresnelSharpness ) );
			float2 uv_Atlas_Reflectivitymask = i.uv_texcoord;
			float4 lerpResult5 = lerp( temp_output_60_0 , ( ( blendOpSrc20 > 0.5 ? ( blendOpDest20 + 2.0 * blendOpSrc20 - 1.0 ) : ( blendOpDest20 + 2.0 * ( blendOpSrc20 - 0.5 ) ) ) + ( fresnelNode8 * _Fresnelcolor ) ) , tex2D( _Atlas_Reflectivitymask, uv_Atlas_Reflectivitymask ).r);
			float4 lerpResult40 = lerp( temp_output_60_0 , lerpResult5 , _Reflectionopacity);
			float3 desaturateVar27 = lerp( lerpResult40.rgb,dot(lerpResult40.rgb,float3(0.299,0.587,0.114)).xxx,_Desaturation);
			o.Albedo = desaturateVar27;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardSpecular keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			# include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				float4 texcoords01 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal( v.normal );
				fixed3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.texcoords01 = float4( v.texcoord.xy, v.texcoord1.xy );
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			fixed4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord.xy = IN.texcoords01.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.worldRefl = -worldViewDir;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandardSpecular o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandardSpecular, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=13701
2567;29;1666;974;2773.438;328.5551;1;True;True
Node;AmplifyShaderEditor.WorldNormalVector;33;-2353.454,-39.64977;Float;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;17;-1980.316,195.4716;Float;False;Property;_reflectionblurriness;reflection blurriness;9;0;0;0;5;0;1;FLOAT
Node;AmplifyShaderEditor.WorldReflectionVector;7;-2128.317,-1.028419;Float;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;10;-551.2914,741.8796;Float;False;Property;_Fresnelintensity;Fresnel intensity;8;0;0;0;5;0;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;30;-997.6306,364.9065;Float;False;Property;_Reflectionlightlevel;Reflection light level;11;0;1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SamplerNode;1;-382.164,-310.4451;Float;True;Property;_AtlasDiffuse;Atlas Diffuse;0;1;[NoScaleOffset];Assets/City skyline/Textures/Atlas Diffuse.tga;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ColorNode;13;-352.0872,-503.4044;Float;False;Property;_Albedocolor;Albedo color;1;0;1,1,1,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SamplerNode;3;-1642.097,53.85769;Float;True;Property;_cubemap;cubemap;5;1;[NoScaleOffset];Assets/City skyline/Textures/cubemap.png;True;0;False;white;Auto;False;Object;-1;MipLevel;Cube;6;0;SAMPLER2D;;False;1;FLOAT3;0,0,0;False;2;FLOAT;1.0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;11;-552.6916,816.0797;Float;False;Property;_FresnelSharpness;Fresnel Sharpness;7;0;0;0;10;0;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;61;-978.3514,244.8224;Float;False;Property;_Reflectiondarklevel;Reflection dark level;11;0;1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;19.71732,-295.7803;Float;False;2;2;0;COLOR;0.0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.ColorNode;14;-169.6916,851.7799;Float;False;Property;_Fresnelcolor;Fresnel color;2;0;1,1,1,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SmoothstepOpNode;29;-634.0902,197.7143;Float;True;3;0;FLOAT;0.0;False;1;FLOAT;0,0,0,0;False;2;FLOAT;1;False;1;FLOAT
Node;AmplifyShaderEditor.FresnelNode;8;-170.4917,703.9797;Float;False;World;4;0;FLOAT3;0,0,0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;3;FLOAT;5.0;False;1;FLOAT
Node;AmplifyShaderEditor.BlendOpsNode;20;148.0749,157.6997;Float;True;LinearLight;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;85.30841,758.2799;Float;False;2;2;0;FLOAT;0,0,0,0;False;1;COLOR;0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleAddOpNode;16;478.275,202.2302;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SamplerNode;2;-25.79769,1117.973;Float;True;Property;_Atlas_Reflectivitymask;Atlas_Reflectivitymask;6;1;[NoScaleOffset];Assets/City skyline/Textures/Atlas_Reflectivitymask.tga;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;62;673.579,439.8029;Float;False;Property;_Reflectionopacity;Reflection opacity;13;0;0;0;1;0;1;FLOAT
Node;AmplifyShaderEditor.LerpOp;5;659.6871,151.0915;Float;True;3;0;COLOR;0.0,0,0,0;False;1;COLOR;0.0,0,0,0;False;2;FLOAT;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.LerpOp;40;981.2404,124.2334;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.RangedFloatNode;28;941.1824,364.5285;Float;False;Property;_Desaturation;Desaturation;4;0;0;0;1;0;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-1265.884,181.1329;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.ColorNode;46;1086.519,-144.1045;Float;False;Property;_Ambiant;Ambiant;12;0;0.190548,0.2527722,0.316,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ColorNode;24;-1614.233,299.686;Float;False;Property;_ReflectioncolorTransparency;Reflection color / Transparency;3;0;0.503,0.503,0.503,0.491;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.DesaturateOpNode;27;1278.743,178.8516;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0.0;False;1;FLOAT3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;1467.744,49.21956;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1909.031,199.6219;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;CitySkyline;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;FLOAT;0.0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;7;0;33;0
WireConnection;3;1;7;0
WireConnection;3;2;17;0
WireConnection;60;0;13;0
WireConnection;60;1;1;0
WireConnection;29;0;3;0
WireConnection;29;1;61;0
WireConnection;29;2;30;0
WireConnection;8;2;10;0
WireConnection;8;3;11;0
WireConnection;20;0;60;0
WireConnection;20;1;29;0
WireConnection;15;0;8;0
WireConnection;15;1;14;0
WireConnection;16;0;20;0
WireConnection;16;1;15;0
WireConnection;5;0;60;0
WireConnection;5;1;16;0
WireConnection;5;2;2;0
WireConnection;40;0;60;0
WireConnection;40;1;5;0
WireConnection;40;2;62;0
WireConnection;23;0;3;0
WireConnection;23;1;24;0
WireConnection;27;0;40;0
WireConnection;27;1;28;0
WireConnection;47;0;46;0
WireConnection;47;1;27;0
WireConnection;0;0;27;0
ASEEND*/
//CHKSM=6E546EDEAB23ED5BC9FCD0C45BD30B52A3D2E75F