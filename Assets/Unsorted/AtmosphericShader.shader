// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Planet/Atmosphere shader"
{
    Properties
    {
        [NoScaleOffset] _Gradient("Diffraction ramp", 2D) = "white" {}
        _FresnelExponent("Fresnel exponent", Float) = 5
        _TransitionWidth("Transition width", Range(0.1, 0.5)) = 0.15
    }
    SubShader
    {
        Tags{ Queue = Transparent }
        Blend SrcAlpha OneMinusSrcAlpha
 
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}
             
            CGPROGRAM
 
            // pragmas and includes
            #pragma vertex vert
            #pragma fragment frag
             
            #include "UnityCG.cginc"
 
            // user-defined variables
            uniform sampler2D _Gradient;
            uniform float _FresnelExponent;
            uniform float _TransitionWidth;
 
            // unity-defined variables
            uniform float4 _LightColor0;
 
            // constants
            static const float PI = 3.14159265f;
 
            // base input structs
            struct vertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct vertexOutput
            {
                float4 posProjection : SV_POSITION;
                float angleIncidence : ANGLE;
                float4 col : COLOR;
            };
             
            // vertex program
            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;
 
                float3 normalDirection = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                 
                // assuming the object is a sphere, the angles between normals and light determines the positions on the sphere
                o.angleIncidence = acos(dot(lightDirection, normalDirection)) / PI;
                // shade atmosphere according to this ramp function from 0 to 180 degrees
                float shadeFactor = 0.1 * (1 - o.angleIncidence) + 0.9 * (1 - (clamp(o.angleIncidence, 0.5, 0.5 + _TransitionWidth) - 0.5) / _TransitionWidth);
                // the viewer should be able to see further distance through atmosphere towards edges of the sphere
                float angleToViewer = sin(acos(dot(normalDirection, viewDirection)));
                // this ramp funtion lights up edges, especially the very edges of the sphere contour
                float perspectiveFactor = 0.3 + 0.2 * pow(angleToViewer, _FresnelExponent) + 0.5 * pow(angleToViewer, _FresnelExponent * 20);
 
                o.col = _LightColor0 * perspectiveFactor * shadeFactor;
 
                o.posProjection = UnityObjectToClipPos(v.vertex);
 
                return o;
            }
             
            // fragment program
            fixed4 frag (vertexOutput i) : SV_Target
            {
                // tint with gradient texture ramp of 70% brightness value and multiply by 1.4 to re-adjust brightness level
                float2 gradientLevel = float2(i.angleIncidence, 0);
                fixed4 col = i.col * tex2D(_Gradient, gradientLevel) * 1.4;
 
                return col;
            }
 
            ENDCG
        }
    }
    //Fallback "Diffuse"
}
