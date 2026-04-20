Shader "Silent/Clear Water 3"
{
	Properties
	{
        [CheckDFGTexture]
        [CW3BlendModeSelector(_SrcBlend, _DstBlend, _CustomRenderQueue, _ZWrite, )] _Mode ("__mode", Float) = 0.0
		[HeaderEx(Surface Color)]
		_Color ("Surface Tint Color", Color) = (1,1,1,1)
		[Enum(World Position XZ, 4, UV0, 0, UV1, 1, UV2, 2, UV3, 3)]
		_MainTexUVSrc("Surface Color UV Source", Float) = 4
		_MainTex("Surface Color", 2D) = "white" {}
		_SurfWaveDistort("Surface Wave Distortion Intensity", Range(0, 1)) = 1.0
		_SurfEdgeFade("Surface Edge Fade", Range(0, 1)) = 0.0
        [Space]
		_VertexColorInt("Vertex Colour Intensity", Range(0, 1)) = 0.0
		_VertexAlphaInt("Vertex Alpha Intensity", Range(0, 1)) = 0.0

		[HeaderEx(Normal Waves)]
		[Enum(World Position XZ, 4, UV0, 0, UV1, 1, UV2, 2, UV3, 3)]
		_WaveTexUVSrc("Wave UV Source", Float) = 0.0
		[Normal][SetKeyword(_NORMALMAP)]_BumpMap("Wave Normal Map", 2D) = "bump" {}
		[IfDef(_NORMALMAP)]_BumpMapScale("Wave Normal Scale", Float) = 1.0
		[Enum(World Position XZ, 4, UV0, 0, UV1, 1, UV2, 2, UV3, 3)]
		_WaveTex2UVSrc("2nd Wave UV Source", Float) = 0.0
		[Normal][IfDef(_NORMALMAP)]_BumpMap2("2nd Wave Normal Map", 2D) = "bump" {}
		[IfDef(_NORMALMAP)]_BumpMap2Scale("2nd Wave Normal Scale", Float) = 1.0

		[HeaderEx(Specular)]
		_Metallic("Metalness", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 1
		_ViewSmoothness("Angular Smoothness", Range(0, 1)) = 1

		[HeaderEx(Water Flow Motion)]
		_WindDirection("Wind Direction", Vector) = (3,5,0)
		_WindSpeed("Wind Speed", Range(0, 10)) = 0.1
		_RippleIntensity("Ripple Intensity", Range(0, 1)) = 0.1

		[Enum(World Position XZ, 4, UV0, 0, UV1, 1, UV2, 2, UV3, 3)]
		_FlowMapUVSrc("Flow Map UV Source", Float) = 0
		[SetKeyword(_USE_FLOWMAP)]_FlowMap("Flow Map ", 2D) = "bump" {}
		[IfDef(_USE_FLOWMAP)]_FlowStrength("Flow Strength", Float) = 1.0
		[IfDef(_USE_FLOWMAP)]_FlowSpeed("Flow Speed", Float) = 1.0
		
        [HeaderEx(Projected Caustics)]
        [SetKeyword(_USE_CAUSTICS)][NoScaleOffset]_CausticsTex ("Caustics Texture", 2D) = "white" {}
        [IfDef(_USE_CAUSTICS)]_CausticsStrength ("Caustics Strength", Float) = 1.0
        [IfDef(_USE_CAUSTICS)]_CausticsSpeed ("Caustics Speed", Float) = 0.4
        [IfDef(_USE_CAUSTICS)]_CausticsScale ("Caustics Scale", Float) = 0.2
        [IfDef(_USE_CAUSTICS)]_CausticsMaxDistance ("Caustics Max Distance", Range(1, 1000)) = 30

		[HeaderEx(Caustics Shadow)]
		[SetKeyword(_USE_CAUSTICSHADOW)]_CausticsMap("Shadow Caustics Map", 2D) = "black" {}
        [IfDef(_USE_CAUSTICSHADOW)]_CausticsShadowStrength ("Shadow Caustics Strength", Range(0, 1)) = .25

		[HeaderEx(Vertex Waves)]
		[Toggle(_USE_WAVES)]_VertexWaves("Vertex Displacement Waves", Float) = 0			
		[IfDef(_USE_WAVES)]_WaveSpeed("Wave Speed", Range(0, 10)) = 1
		[IfDef(_USE_WAVES)]_WaveA ("Wave A (XY: Direction)", Vector) = (1,0,0.5,10)
		[IfDef(_USE_WAVES)]_WaveB ("Wave B (Z: Steepness)", Vector) = (0,1,0.25,20)
		[IfDef(_USE_WAVES)]_WaveC ("Wave C (W: Wavelength)", Vector) = (1,1,0.15,10)

		[HeaderEx(Refraction and Reflection)]
		[SetShaderPassToggle(Always, _USE_REFRACTION)]_UseRefraction("Refraction (Enables Grab Pass)", Float) = 1
		_RefractThickness("Water Thickness", Range(0, 1)) = 0.5
		_RefractTransmission("Water Transmission", Range(0, 1)) = 1
		_RefractAbsorptionScale("Water Absorption Scale", Range(0.001, 2)) = 1.0
		_RefractSurfaceCol("Surface Color Power", Range(0, 1)) = 0
		[ToggleUI]_RefractUseColor("Use Custom Absorption Color", Float) = 0
		[Gamma]_RefractColor("Custom Absorption Color", Color) = (0,0,0,1)
		_RefractMaxDepth("Maximum Depth for Absorption", Float ) = 100

		[Space]
		[IfDef(_USE_REFRACTION)][Toggle(_USE_SSR)]_UseSSR("Screen-Space Reflections", Float) = 0
		[Toggle(_USE_MIRROR)]_UseMirror("Mirror Reflections", Float) = 0

		[HeaderEx(Depth)]
		[Toggle(_USE_DEPTHFOG)]_UseDepthFog("Depth Fog", Range(0, 1)) = 1
		[IfDef(_USE_DEPTHFOG)][Enum(Exponential, 0, Exponential Squared, 1, Linear, 2)]
		_FogMode("Depth Fog Mode", Float) = 0
		[IfDef(_USE_DEPTHFOG)]_FogExpDensity("Depth Fog Density (Exponential)", Range(0.001, 1)) = 0.05
		[IfDef(_USE_DEPTHFOG)]_FogLinearDensity("Depth Fog Density (Linear)", Vector) = (20, 200, 0, 0)

		[HeaderEx(Surface Edge Foam)]
		[Enum(World Position XZ, 4, UV0, 0, UV1, 1, UV2, 2, UV3, 3)]
		_FoamTexUVSrc("Foam Map UV Source", Float) = 4
		[SetKeyword(_USE_FOAM)]_FoamTex("Foam Map (RGB)", 2D) = "white" {}
		[IfDef(_USE_FOAM)]_FoamColor("Foam Tint Color", Color) = (1, 1, 1, 1)
		[Space]
		[IfDef(_USE_FOAM)]_FoamFalloff("Foam Layer Falloff", Vector) = (1, 4, 8, 0)
		[IfDef(_USE_FOAM)]_FoamSpeed("Foam Turbulence", Range(0, 10)) = 0.1
		[IfDef(_USE_FOAM)]_FoamRoughness("Foam Shininess", Range(0, 1)) = 0.5
		[Space]
		[IfDef(_USE_FOAM)][Toggle(_USE_FOAM_NORMAL)]_UseFoamNormals("Generate Foam Normals", Float) = 0
		[IfDef(_USE_FOAM)][IfDef(_USE_FOAM_NORMAL)]	_FoamNormalScale("Foam Normals Scale", Range(0, 100)) = 50.0
		[IfDef(_USE_FOAM)][IfDef(_USE_FOAM_NORMAL)]	_FoamNormalSpeed("Foam Normals Speed", Range(0, 10)) = 4.0
		[IfDef(_USE_FOAM)][IfDef(_USE_FOAM_NORMAL)]	_FoamNormalStrength("Foam Normals Intensity", Range(0, 5)) = 2.0

		[HeaderEx(Surface Emission)]
		[NoScaleOffset][SetKeyword(_EMISSION)]_EmissionMap("Emission Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0)
		[Gamma]_Emission("Emission Strength", Float) = 0

		[HeaderEx(Transmission)]
		_SSSCol ("Transmission Color", Color) = (1,1,1,1)
		_SSSDist("Distortion", Range(0, 10)) = 1
		_SSSPow("Power", Range(0.01, 10)) = 1
		_SSSIntensity("Intensity", Range(0, 1)) = 1
		_SSSAmbient("Ambient", Range(0, 1)) = 0
		_SSSShadowStrength("Shadow Strength", Range(0, 1)) = 1

		[HeaderEx(Wrapped Lighting)]
		_ProbeTransmission("Light Probes Wrapping Factor", Range(0, 2)) = 1
		_WrappingFactor("Direct Light Wrapping Factor", Range(0.001, 1)) = 0.01
		[Gamma]_WrappingPowerFactor("Direct Light Wrapping Power Factor", Float) = 1

		[HeaderEx(Distant Fade)]
		_DistantFogOverrideLevel("Distant Fade Fog Strength", Range(0, 1)) = 0
        [NoScaleOffset] _DistantFogOverrideTex ("Distant Fade Fog Texture", Cube) = "black" {}
        _DistantFogOverrideDistance ("Distant Fade Fog Texture Start", Float) = 2000

		[HeaderEx(System)]
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Int) = 0
		[ToggleUI]_BackfaceNormals("Flip Backface Lighting", Float) = 0.0
		[Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)]
	    //_VanishingEnd("Camera Fade End", Range(0, 1)) = 0.1
		[Toggle(_USE_INFINITY)]_UseInfinity("Ignore far clipping plane", Float) = 0

	    [Space]
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
        [KeywordEnum(None, SH, RNM, MonoSH)] _Bakery ("Bakery Mode", Int) = 0
        [HideInInspector]_RNM0("RNM0", 2D) = "black" {}
        [HideInInspector]_RNM1("RNM1", 2D) = "black" {}
        [HideInInspector]_RNM2("RNM2", 2D) = "black" {}
        _ExposureOcclusion("Lightmap Occlusion Sensitivity", Range(0, 1)) = 0.2
        [Toggle(_LIGHTMAPSPECULAR)]_LightmapSpecular("Lightmap Specular", Range(0, 1)) = 1
        _LightmapSpecularMaxSmoothness("Lightmap Specular Max Smoothness", Range(0, 1)) = 0.9
        [Toggle(_VRCLV)]_VRCLV("VRC Light Volumes", Range(0, 1)) = 1
        [IfDef(_VRCLV)] _VRCLVSurfaceBias("Light Volume Surface Bias", Range(0, 0.5)) = 0.05
	    [Space]
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Reflections", Float) = 1.0

        // Blending state
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _CustomRenderQueue ("__rq", Float) = -1.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0

        [NonModifiableTextureData][HideInInspector] _DFG("DFG", 2D) = "white" {}
        [NonModifiableTextureData][HideInInspector] _TANoiseTex ("tanoise Data Texture", 2D) = "white" {}
	}

CGINCLUDE
    #pragma multi_compile_instancing
    #pragma multi_compile_fog

    #include "UnityCG.cginc"

    #if !defined(UNITY_PASS_META) && (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
        #ifdef UNITY_CALC_FOG_FACTOR
            #undef UNITY_CALC_FOG_FACTOR
        #endif
        #define UNITY_CALC_FOG_FACTOR(coord) UNITY_CALC_FOG_FACTOR_RAW(length(pix.fullVectorFromEyeToGeometry))
    #endif

    #include "Lighting.cginc"
    #include "AutoLight.cginc"
    #include "UnityPBSLighting.cginc"
    #include "UnityStandardUtils.cginc"

	// Note: FoliageIndirect depends on these when SHADER_API_D3D11 is defined
	#if defined(SHADER_API_D3D11)
    #include "SharedSamplingLib.hlsl"
    #include "SharedFilteringLib.hlsl"
    #endif

CBUFFER_START(UnityPerMaterialRare)
	uniform float _ExposureOcclusion;
	uniform float _LightmapSpecularMaxSmoothness;
	uniform float _VRCLVSurfaceBias;
CBUFFER_END

	float getLightVolumeSurfaceBias()
	{
		return _VRCLVSurfaceBias;
	}

    #include "WaterUtils.hlsl"
    #include "WaterIndirect.hlsl"

	#include "tanoise/tanoise.cginc"
ENDCG

CGINCLUDE
CBUFFER_START(UnityPerMaterial)
	uniform fixed4 _Color;
	uniform float _Metallic;
	uniform float _Smoothness;
	uniform float _ViewSmoothness;
	uniform sampler2D _MainTex;
	uniform float4 _MainTex_ST; 
	#if defined(_NORMALMAP)
	uniform sampler2D _BumpMap;
	uniform float4 _BumpMap_ST;
	uniform half _BumpMapScale;
	uniform float _WaveTexUVSrc;
	uniform sampler2D _BumpMap2;
	uniform float4 _BumpMap2_ST;
	uniform half _BumpMap2Scale;
	uniform float _WaveTex2UVSrc;
	#endif
	#if defined(_EMISSION)
	uniform sampler2D _EmissionMap;
	#endif
	#if defined(_USE_FOAM)
	uniform sampler2D _FoamTex;
	uniform float _FoamTexUVSrc;
	uniform float4 _FoamTex_ST; 
	uniform float4 _FoamColor;
	uniform float4 _FoamFalloff;
	uniform float _FoamSpeed;
	uniform float _FoamRoughness;
	uniform float _FoamNormalScale;
	uniform float _FoamNormalSpeed;
	uniform float _FoamNormalStrength;
	#endif
	#if defined(_USE_FLOWMAP)
	uniform sampler2D _FlowMap;
	uniform half4 _FlowMap_ST;
	uniform fixed _FlowMapUVSrc;
	uniform float _FlowStrength;
	uniform float _FlowSpeed;
	#endif

	#if defined(_USE_CAUSTICSHADOW)
	uniform sampler2D _CausticsMap;
	uniform float _CausticsShadowStrength;
	#endif

	uniform sampler2D _WindTex;
	uniform float3 _WindDirection;
	uniform float _WindSpeed;

	uniform half _Emission;
	uniform half3 _EmissionColor;

	uniform half _SSSDist;
	uniform half _SSSPow;
	uniform half _SSSIntensity;
	uniform half _SSSAmbient; 
	uniform half _SSSShadowStrength;
	uniform fixed3 _SSSCol;

	uniform float _VertexColorInt;
	uniform float _VertexAlphaInt;

	uniform float _ProbeTransmission;
	uniform float _WrappingFactor;
	uniform float _WrappingPowerFactor;
	uniform float _BackfaceNormals;

	// Vanishing start not needed but kept just in case.
	uniform float _VanishingStart;
	uniform float _VanishingEnd;

	uniform sampler2D _DFG;

	uniform float _MainTexUVSrc;
	uniform float _FoamSize;
	uniform float _FoamSoftness;
	uniform float _RippleIntensity;
	uniform float _VerticalFoam;
	
	#if defined(_USE_REFRACTION)
	// Only true if the GrabPass is active.
	#endif
	uniform half _RefractThickness;
	uniform half _RefractTransmission;
	uniform half _RefractSurfaceCol;
	uniform half _RefractAbsorptionScale;
	uniform half _RefractUseColor;
	uniform half _RefractMaxDepth;
	uniform half4 _RefractColor;
	uniform float _SurfWaveDistort;
	uniform half _FogMode;
	uniform half _FogExpDensity;
	uniform half4 _FogLinearDensity;

	uniform half _WaveSpeed;
	uniform half4 _WaveA;
	uniform half4 _WaveB;
	uniform half4 _WaveC;

    sampler2D _CausticsTex;
    uniform float _CausticsSpeed;
    uniform float _CausticsScale;
    uniform float _CausticsStrength;
    uniform float _CausticsMaxDistance;
    uniform float _EdgeFadeRadius;
    uniform float _EdgeFadeOffset;

    sampler2D _WarpTex;
    uniform float4 _WarpTex_ST;
    uniform float4 _WarpPow;

	uniform float _Mode;
	uniform float _SurfEdgeFade;
	
    TEXTURECUBE(_DistantFogOverrideTex); SAMPLER(sampler_DistantFogOverrideTex);
    half4 _DistantFogOverrideTex_HDR;
	half _DistantFogOverrideDistance;
	half _DistantFogOverrideLevel;
CBUFFER_END

// == Vertex shader section ===================================================

float3 GerstnerWave(float4 parameter, float2 position, float time, inout float3 tangent, inout float3 binormal)
{
    float waveSteepness = parameter.z;
    float waveLength = parameter.w;
	if (waveLength < 0.001) return 0;

    float k = 2.0 * UNITY_PI / waveLength;
    float c = sqrt(9.8 / k);
    float2 d = normalize(parameter.xy);
    float f = k * (dot(d, position) - c * time);
    float a = waveSteepness / k;

    tangent += normalize(float3(
        1.0 - d.x * d.x * (waveSteepness * sin(f)),
        d.x * (waveSteepness * cos(f)),
        -d.x * d.y * (waveSteepness * sin(f))
    ));
    binormal += normalize(float3(
        -d.x * d.y * (waveSteepness * sin(f)),
        d.y * (waveSteepness * cos(f)),
        1.0 - d.y * d.y * (waveSteepness * sin(f))
    ));

    return float3(
        d.x * (a * cos(f)),  // X displacement
        a * sin(f),   		 // Y displacement
        d.y * (a * cos(f))   // Z displacement
    );
}

inline half4 VertexGIForward(appdata_full v, float3 posWorld, half3 normalWorld)
{
    half4 ambientOrLightmapUV = 0;
    // Static lightmaps
    #ifdef LIGHTMAP_ON
        ambientOrLightmapUV.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        ambientOrLightmapUV.zw = 0;
    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
    #elif UNITY_SHOULD_SAMPLE_SH
        #ifdef VERTEXLIGHT_ON
            // Approximated illumination from non-important point lights
            ambientOrLightmapUV.rgb = Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, posWorld, normalWorld);
        #endif

        ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, ambientOrLightmapUV.rgb);
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        ambientOrLightmapUV.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    return ambientOrLightmapUV;
}

struct v2f
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
	#ifndef UNITY_PASS_SHADOWCASTER
	    float4 pos : SV_POSITION;
	    float3 normal : NORMAL;
	    #if defined(_NORMALMAP)
	        float3 tangent : TANGENT;
	        float3 bitangent : BITANGENT;
	    #endif
	    UNITY_SHADOW_COORDS(3)
	    UNITY_FOG_COORDS(4)
		// Screen position (xy) and depth (w)
		float4 screenPosition_Depth : SCRPOS;
    #else
	    V2F_SHADOW_CASTER;
	#endif
	float4 wPosAndHue : TEXCOORD0;
	// Packed UV0/UV1
	float4 uv : TEXCOORD1;
	// Packed UV2/UV3
	float4 uv2 : TEXCOORD2;
	float4 color : COLOR;
	float4 ambientOrLightmapUV : LIGHTMAP;
};

v2f vert(appdata_full v)
{
	v2f o;
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	//vertexDataFunc(v);

	#ifdef UNITY_PASS_SHADOWCASTER
		float4 wPosFull = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
		float3 wPos = wPosFull.xyz;
		// Apply waves to world-space position
		#if defined(_USE_WAVES)
		float4 waveScale = float4(1.0, 1.0, 1.0, 1.0);

		float3 dummy;

		// Todo: selector for axis?
		wPos += GerstnerWave(_WaveA * waveScale, wPos.xz, _Time.x * _WaveSpeed, dummy, dummy);
		wPos += GerstnerWave(_WaveB * waveScale, wPos.xz, _Time.x * _WaveSpeed, dummy, dummy);
		wPos += GerstnerWave(_WaveC * waveScale, wPos.xz, _Time.x * _WaveSpeed, dummy, dummy);

		o.wPosAndHue.xyz = wPos;
		
	    o.pos = UnityWorldToClipPos(wPos);
	    TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
		//TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
		#endif
	#else
		float4 wPosFull = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
		float3 wPos = wPosFull.xyz;
		float3 wNormal = UnityObjectToWorldNormal(v.normal);
		float3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
		half tSign = v.tangent.w * unity_WorldTransformParams.w;
		float3 wBitangent = cross(wNormal, wTangent) * tSign; 

		// Apply waves to world-space position
		#if defined(_USE_WAVES)
		float4 waveScale = float4(1.0, 1.0, 1.0, 1.0);

		// Todo: selector for axis?
		wPos += GerstnerWave(_WaveA * waveScale, wPos.xz, _Time.x * _WaveSpeed, wTangent, wBitangent);
		wPos += GerstnerWave(_WaveB * waveScale, wPos.xz, _Time.x * _WaveSpeed, wTangent, wBitangent);
		wPos += GerstnerWave(_WaveC * waveScale, wPos.xz, _Time.x * _WaveSpeed, wTangent, wBitangent);

		//crestFactor = saturate((crestA + crestB + crestC));
		wNormal = normalize(cross(wBitangent, wTangent));
		#endif

		o.wPosAndHue.xyz = wPos;
		o.wPosAndHue.w = 0;

	    o.pos = UnityWorldToClipPos(wPos);

		#if _USE_INFINITY && SHADER_API_D3D11
			// This hack, by Hekky, gives the shader an infinite clipping plane. It is only supported on DX11.
			float4x4 INF_PERSPECTIVE = UNITY_MATRIX_P;
			if (INF_PERSPECTIVE._m20 == 0 && INF_PERSPECTIVE._m21 == 0) {
				// Regular perspective projection matrix
				INF_PERSPECTIVE._m22 = 0.f;
				INF_PERSPECTIVE._m23 = _ProjectionParams.y;
			};
			float4 wPosLocal = float4(wPos, wPosFull.w);
			o.pos = mul(INF_PERSPECTIVE, mul(UNITY_MATRIX_V, wPosLocal)); 
	    #endif

	    o.normal = wNormal;
	    #if defined(_NORMALMAP)
	    	o.tangent = wTangent;
	    	o.bitangent = wBitangent; 
	    #endif
    
	    o.ambientOrLightmapUV = VertexGIForward(v, wPos, o.normal);

		o.screenPosition_Depth = 0;

		// Save the clip space position so we can use it later.
		// This also handles situations where the Y is flipped.
		float2 suv = o.pos * float2( 0.5, 0.5*_ProjectionParams.x);
								
		// Tricky, constants like the 0.5 and the second paramter
		// need to be premultiplied by o.pos.w.
		o.screenPosition_Depth.xy = TransformStereoScreenSpaceTex( suv+0.5*o.pos.w, o.pos.w );
    	COMPUTE_EYEDEPTH(o.screenPosition_Depth.w);
    
	    UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);
        UNITY_TRANSFER_FOG(o,o.pos); 
	#endif
	o.uv.xy = v.texcoord.xy;
	o.uv.zw = v.texcoord1.xy;
	o.uv2.xy = v.texcoord2.xy;
	o.uv2.zw = v.texcoord3.xy;
	o.color = v.color;

	UNITY_TRANSFER_INSTANCE_ID(v, o);
	return o;
}

// == Fragment shader section =================================================

// Depth texture
// --> _CameraDepthTexture defined in WaterUtils.hlsl

#if (defined(_USE_REFRACTION) || defined(_USE_SSR))
	uniform sampler2D _GrabTexture; uniform float4 _GrabTexture_TexelSize;
	// These variables are only used for refraction
	float4 grabScreenColour(float2 screenPos, float2 offset, float perspectiveDivide) {
		float2 uv = (screenPos.xy + offset) * perspectiveDivide;
		#if UNITY_UV_STARTS_AT_TOP
			if (_CameraDepthTexture_TexelSize.y < 0) {
				uv.y = 1 - uv.y;
			}
		#endif
		uv = (floor(uv * _CameraDepthTexture_TexelSize.zw) + 0.5) *
			abs(_CameraDepthTexture_TexelSize.xy);
		return tex2D(_GrabTexture, uv);
	}
#endif

#if defined(_USE_SSR)
	#include "WaterSSR.hlsl"
#endif

#if defined(_USE_REFRACTION)
#endif

// Mirror reflection
#if defined(_USE_MIRROR)
	sampler2D _ReflectionTex0;
	sampler2D _ReflectionTex1;

	half4 getMirrorSurface(float2 screenPos, float perspectiveDivide)
	{
		float2 uv = screenPos * perspectiveDivide;
		half4 refl = unity_StereoEyeIndex == 0 
		? tex2D(_ReflectionTex0, uv) 
		: tex2D(_ReflectionTex1, uv);
		return refl;
	}
#endif

// Params for wind for sampling textures
struct WindParams
{
	// 2D vector, only used for scrolling textures
	float2 dir;
	// Based on length of (3D) wind direction vector
	float speed;
	float timer;
	float2 anim;
};

WindParams getWindParams()
{
	WindParams wind = (WindParams)0;
	wind.dir = _WindDirection * _WindSpeed;
	wind.speed = length(_WindDirection);
	wind.dir /= wind.speed;
	wind.timer = _Time * wind.speed;
	wind.anim = frac(wind.dir * wind.timer);
	return wind;
};

float2 GetRippleNoise(in float2 position, in float2 timedWindDir)
{
	float2 noise;
	//noise.x = snoise(position * 0.015 + timedWindDir * 0.0005); // large and slower noise 
	//noise.y = snoise(position * 0.1 + timedWindDir * 0.002); // smaller and faster noise
	//noise.x = tanoise3_1d_fast(float3(position * 0.015 + timedWindDir * 0.0005, 1.0));
	//noise.y = tanoise3_1d_fast(float3(1.0, position * 0.015 + timedWindDir * 0.0005));
	noise = tanoise2_hq(position * 0.015 + timedWindDir * 0.0005);
	return saturate(noise);
}

// Material inputs used to generate the params 
struct WaterInputs
{
	// RGB: Colour reflected from surface
	// A: Surface opacity (obscures elements below it)
	float4 surfColor;
	// XYZ: Surface normal direction (tangent space)
	float3 normalTangent;

	// Surface smoothness. 
	float smoothness;
	// View-dependant smoothness
	float viewSmoothness;
	// Metalness of water, for something like mercury
	float metallic;
	// Ambient occlusion applied to water, typically provided through vertex colours
	float occlusion;
};

float3 FlowUVW (float2 uv, float2 flowVector, float time, bool flowB) 
{
	float phaseOffset = flowB ? 0.5 : 0;
	float progress = frac(time + phaseOffset);
	float3 uvw;
	uvw.xy = uv - flowVector * progress;
	uvw.z = 1 - abs(1 - 2 * progress);
	return uvw;
};

struct Texcoords
{
	float2 uv[5];
	float2 ripples;
};

Texcoords getTexcoords(float4 i_uv, float4 i_uv2, float3 worldPos)
{
	Texcoords tc = (Texcoords)0;
	tc.uv[0] = i_uv.xy;
	tc.uv[1] = i_uv.zw;
	tc.uv[2] = i_uv2.xy;
	tc.uv[3] = i_uv2.zw;
	tc.uv[4] = worldPos.xz;
	
	// Default to mesh UVs for effects like the sin offset that don't look good
	// when applied using world-space position.  
    float2 uv = tc.uv[0];
    float4 uvDDXY = float4(ddx(uv), ddy(uv)); 
    float2 uvDeriv = float2(length(uvDDXY.xz), length(uvDDXY.yw)); 

	// Wobbly ripple effect, like Quake
	tc.ripples = float2(sin(_Time.y + uv.y * UNITY_PI),
		cos(_Time.y + uv.x * UNITY_PI)) * _RippleIntensity * 0.1;

	return tc;
};

WaterInputs getWaterInputs(const Texcoords tc, float3 worldPos, float4 vertCol, WindParams wind)
{
	WaterInputs mat;

	// Flowmap setup
	#if defined(_USE_FLOWMAP)
		float2 flowMapUVs = applyScaleOffset(tc.uv[_FlowMapUVSrc], _FlowMap_ST);
		float2 flowMap = tex2D(_FlowMap, flowMapUVs) * 2 - 1;
		flowMap *= _FlowStrength;
		float rippleNoise = tanoise3_1d(worldPos + wind.timer);
	#endif

	// Wave normal setup	
	#if defined(_NORMALMAP)
		float2 waveUVs = applyScaleOffset(tc.uv[_WaveTexUVSrc], _BumpMap_ST);
		float waveScale = _BumpMapScale * 0.1;
		float2 wave2UVs = applyScaleOffset(tc.uv[_WaveTex2UVSrc], _BumpMap2_ST);
		float wave2Scale = _BumpMap2Scale * 0.1;
		#if defined(_USE_FLOWMAP)
			// When using a flowmap, the wind direction is ignored, as the flowmap sets the direction. 
			waveUVs += tc.ripples;
			float3 waveFlowUVsA = FlowUVW(waveUVs, flowMap, _FlowSpeed * _Time.x + rippleNoise, false);
			mat.normalTangent = UnpackScaleNormal(tex2D (_BumpMap, waveFlowUVsA.xy), waveScale) * waveFlowUVsA.z;
			float3 waveFlowUVsB = FlowUVW(waveUVs, flowMap, _FlowSpeed * _Time.x + rippleNoise, true);
			mat.normalTangent += UnpackScaleNormal(tex2D (_BumpMap, waveFlowUVsB.xy), waveScale) * waveFlowUVsB.z;
		#else
			waveUVs += tc.ripples + wind.anim;
			mat.normalTangent = UnpackScaleNormal(tex2D (_BumpMap, waveUVs), waveScale);
			wave2UVs += tc.ripples + wind.anim;
			mat.normalTangent += UnpackScaleNormal(tex2D (_BumpMap2, wave2UVs), wave2Scale);
		#endif
	#else
		mat.normalTangent = float3(0, 0, 1);
	#endif

	// Surface colour setup
	float2 surfUVs = applyScaleOffset(tc.uv[_MainTexUVSrc], _MainTex_ST);
	surfUVs += tc.ripples + wind.anim;
	surfUVs += mat.normalTangent * _SurfWaveDistort;

	float4 texCol = tex2D(_MainTex, surfUVs);
	
    float4 vColInt = float4(_VertexColorInt.xxx, _VertexAlphaInt);
    vertCol = (1-vColInt) + saturate(vertCol) * vColInt;

    mat.surfColor = texCol * _Color * vertCol;

	mat.smoothness = _Smoothness;
	mat.viewSmoothness = _ViewSmoothness;
	mat.metallic = _Metallic;
	mat.occlusion = vertCol.a;

	return mat;
}


SSSParams getSSSParams(float attenuation)
{
	SSSParams params = (SSSParams)0;
	params.distortion = _SSSDist;
	params.power = _SSSPow;
	params.scale = _SSSIntensity;
	params.ambient = _SSSAmbient;
	params.shadowStrength = _SSSShadowStrength;
	params.lightAttenuation = attenuation;
	return params;
}

// Holds world-space information derived from the scene's depth buffer.
struct SceneDepthInfo
{
    float eyeDepthWorld;                 // Distance in meters from the camera to the background geometry.
    float3 worldPosEyeHit;               // The world-space position of that background geometry.
    float distToSurface;                 // The world-space distance between the water surface and the background.
    float3 dirToSurface;                 // The world-space direction vector from the water to the background.
    float waterDepth;                    // The vertical distance (Y-axis) between the water and the background.
};

// Parameters for water to pass into the lighting function
struct WaterPixel
{
	float3 diffuseColor;
	float3 specColor;
	float3 worldNormal;
	float alpha;
	// SSS colour
	float3 transmission; 

	float perceptualRoughness;
	float reflectance;
	// How visible the refraction is. Different from SSS thickness. 
	float thickness;

	float3 fullVectorFromEyeToGeometry;
	float3 worldSpaceDirection;
	float3 viewDir;
	float3 worldPos;
	float3 geometricNormal;

	float NdotV;
	float3 dfg;
};

WaterPixel getPixelParams(const WaterInputs mat, float3x3 tangentToWorld, float3 worldPos,
	const float isFacing)
{
	WaterPixel pix;

	pix.fullVectorFromEyeToGeometry = worldPos - _WorldSpaceCameraPos;
	pix.worldSpaceDirection = normalize( pix.fullVectorFromEyeToGeometry );
	pix.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
	pix.worldPos = worldPos;

	// This is a float in case MSAA doesn't like it.
	const float flipBackfaceNormal = (_BackfaceNormals != 0 && !isFacing);

	#if defined(_NORMALMAP)
        if (flipBackfaceNormal) 
        {
            tangentToWorld = -tangentToWorld;
        }
		
		pix.geometricNormal = normalize(tangentToWorld[2]);
        	    
        tangentToWorld = transpose(tangentToWorld);
        pix.worldNormal = normalize(mul(tangentToWorld, mat.normalTangent));
	#else        	    
		pix.worldNormal = normalize(flipBackfaceNormal ? -tangentToWorld[2] : tangentToWorld[2]);
		pix.geometricNormal = pix.worldNormal;
	#endif


	pix.NdotV = abs(dot(pix.worldNormal, pix.viewDir)) + 1e-5;

	float smoothness = lerp(mat.smoothness, mat.viewSmoothness, pix.NdotV);

	float metallic = mat.metallic;
	float materialor = 1.333; // Water's IOR!

	pix.reflectance = iorToF0(max(1.0, materialor), 1.0);

    pix.diffuseColor = computeDiffuseColor(mat.surfColor, mat.metallic);
    pix.specColor = computeF0(mat.surfColor, mat.metallic, pix.reflectance);
	pix.alpha = mat.surfColor.a;

	// todo
	// Name is incorrect but this serves a similar purpose to Filament's refraction
	// where Fd *= (1.0 - pixel.transmission); and our diffuse is lerped against 
	pix.thickness = max(0.0, _RefractTransmission - mat.surfColor.a);
	pix.transmission = _SSSCol;
	
	pix.perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	pix.perceptualRoughness = IsotropicNDFFiltering(pix.worldNormal, pix.perceptualRoughness);
	
    pix.dfg = tex2Dlod(_DFG, float4(float2(pix.NdotV, pix.perceptualRoughness), 0, 0));

	return pix;
}

struct WaterLighting
{
    UnityLight light;
    UnityIndirect indirect;
    half bakedOcclusion;
    half attenuation; 
    SSSParams sssData;
};

// Encapsulates the setup of direct and indirect lighting from Unity's GI systems.
WaterLighting GetSurfaceLighting(WaterPixel pix, v2f i)
{
    WaterLighting lit = (WaterLighting)0;
	lit.bakedOcclusion = 1.0;
    
    UNITY_LIGHT_ATTENUATION(atten, i, pix.worldPos);
    lit.attenuation = atten;

    UnityGIInput gi = InitialiseUnityGIInput(pix.worldPos, pix.viewDir);
    gi.light.dir = normalize(UnityWorldSpaceLightDir(pix.worldPos));
    gi.light.color = lit.attenuation * _LightColor0.rgb;
    gi.atten = lit.attenuation;
    #if (defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON))
        gi.lightmapUV = i.ambientOrLightmapUV;
        gi.ambient = 0;
    #else
        gi.ambient = i.ambientOrLightmapUV.rgb;
        gi.lightmapUV = 0;
    #endif

    // Create and store sssData in the output struct
    lit.sssData = getSSSParams(lit.attenuation);

    // Diffuse GI
    half bakedAtten = 1.0;
    UnityGI baseGI = UnityGI_Base_local(gi, bakedAtten, /*out*/ lit.bakedOcclusion, 
        pix.worldNormal, pix.perceptualRoughness, pix.transmission, _ExposureOcclusion, _LightmapSpecularMaxSmoothness,
        _ProbeTransmission, lit.sssData);
    
    lit.indirect = baseGI.indirect;

    #if !defined(UNITY_PASS_FORWARDADD)
        lit.light = baseGI.light;
        lit.attenuation *= bakedAtten;
    #else
        lit.light = gi.light;
    #endif

    // Specular GI 
    float3 reflectionDir = reflect(-pix.viewDir, pix.worldNormal);
    lit.indirect.specular += UnityGI_prefilteredRadiance(gi, pix.perceptualRoughness, reflectionDir);

    return lit;
}

#if defined(_USE_FOAM)
// Seperated for shadow support. 
float getFoamLevel(const Texcoords tc, const WindParams wind, float3 dirToSurface, float3 normal, float vertexFoam)
{
	float localHeight = length(dirToSurface * normal);

	float2 foamUVs = applyScaleOffset(tc.uv[_FoamTexUVSrc], _FoamTex_ST);
	foamUVs += wind.anim + tc.ripples;
	// Foam ripple by wave effect -- removed, foam is over waves
	//foamUVs -= saturate(localHeight) * mat.normalTangent * (dirToSurface.xz / localHeight);
	// Wave parallax effect, so it "sticks" to surfaces -- removed, looks weird
	//foamUVs -= saturate(dirToSurface.xz * -0.2);

	// Sample the foam texture from 4 offsets with it moving in 4 directions to give it a chaotic look.
	// This is applied over the base UV movement, which follows the wind direction. 
	float2 foamTimer = frac(_Time.x * _FoamSpeed) * float2(1, -1);
	float4 intervalOffsets = float4(0, 0.125, 0.25, 0.5);
	
	float4 texFoamA = tex2D(_FoamTex, foamUVs + intervalOffsets.xy + foamTimer.xx);
	float4 texFoamB = tex2D(_FoamTex, foamUVs + intervalOffsets.ww + foamTimer.xy);
	float4 texFoamC = tex2D(_FoamTex, foamUVs + intervalOffsets.yx + foamTimer.yx);
	float4 texFoamD = tex2D(_FoamTex, foamUVs + intervalOffsets.zz + foamTimer.yy);

	float4 foamBase = (texFoamA+texFoamB+texFoamC+texFoamD) * 0.25;

	float outFoam = 0.0;
	{
		float4 foamEdge = foamBase;
		// Apply a different falloff to the different layers of the foam map. 
		foamEdge *= saturate(1.0 - localHeight * _FoamFalloff);
		foamEdge.b *= 0.5;
		localHeight = saturate(localHeight + 0.01);

		float foamFlat = dot(foamEdge.rgb, 1.0);

		// Make sure the foam always breaks at the shore
		float foamLimit = saturate(localHeight * 100);
		float edgeMask = saturate( 5 * localHeight / foamFlat);
		edgeMask = min(saturate(edgeMask * foamLimit - 0.25), foamLimit);
		foamEdge *= edgeMask;

		outFoam += dot(foamEdge.rgb, 1.0);
	}
	
	{
		// Apply a different falloff to the different layers of the foam map. 
		foamBase *= saturate(vertexFoam * _FoamFalloff);
		foamBase.b *= 0.5;

		float foamFlat = dot(foamBase.rgb, 1.0);

		// Make sure the foam always breaks at the shore
		float foamLimit = saturate(localHeight * 100);
		float edgeMask = saturate( 5 * localHeight / foamFlat);
		edgeMask = min(saturate(edgeMask * foamLimit - 0.25), foamLimit);
		foamBase *= edgeMask;

		outFoam += dot(foamBase.rgb, 1.0);
	}
	return outFoam;
}


// Calculates foam mask and normal distortion using out parameters.
void getFoamProperties(const Texcoords tc, const WindParams wind, const WaterPixel pix, const SceneDepthInfo depthInfo, float vertexFoam, 
	out float outFoamMask, out float3 outFoamNormalContribution)
{
    outFoamMask = getFoamLevel(tc, wind, depthInfo.dirToSurface, pix.worldNormal, vertexFoam) * _FoamColor.a;

    outFoamNormalContribution = float3(0,0,0);
    #if defined(_USE_FOAM_NORMAL)
        float4 noiseInput = float4(tc.uv[_FoamTexUVSrc] * _FoamNormalScale, 
			_Time.y * _FoamNormalSpeed, 0.0);
        float4 noise = tanoise4(noiseInput);
        outFoamNormalContribution = (noise.rgb * 2.0 - 1.0) * _FoamNormalStrength;
    #endif
}
#endif

#if defined(_USE_CAUSTICSHADOW)
float3 getCausticsShadow(float3 depthWorldPos)
{
	float4 noise = tanoise4(float4(depthWorldPos + _SinTime.xyz + _CosTime.zyx, _Time.x));

	float3 posToLight = normalize(_WorldSpaceLightPos0.xyz - depthWorldPos);

	float3 causticsPos = dot(depthWorldPos + noise, posToLight) * posToLight;
	causticsPos.xy = 0.5 * causticsPos.xz + causticsPos.y * 0.1;
	
    float2 uv1 = causticsPos + _Time.y * _CausticsSpeed;
    float2 uv2 = causticsPos * -1.0 + _Time.y * _CausticsSpeed * 0.5;

    float3 causticsTex1 = tex2D(_CausticsMap, uv1).rgb;
    float3 causticsTex2 = tex2D(_CausticsMap, uv2).rgb;
	
    float3 caustics = min(causticsTex1, causticsTex2);
    
    return caustics;
}
#endif

#if defined(_USE_CAUSTICS)
float3 getCausticProjected(float3 worldPosition, float edgeFadeMask, float distFade)
{
	float4 noise = tanoise4(float4(worldPosition + _SinTime.xyz + _CosTime.zyx, _Time.x));

	float3 posToLight = normalize(_WorldSpaceLightPos0.xyz - worldPosition);

	float3 causticsPos = dot(worldPosition + noise, posToLight) * posToLight * _CausticsScale;
	causticsPos.xy = 0.5 * causticsPos.xz + causticsPos.y * 0.1;
	
    float2 uv1 = causticsPos + _Time.y * _CausticsSpeed;
    float2 uv2 = causticsPos * -1.0 + _Time.y * _CausticsSpeed * 0.5;

	const float causticsMaxMipLevel = 8;
    float3 causticsTex  = tex2Dlod(_CausticsTex, float4(uv1,0, distFade*causticsMaxMipLevel)).rgb;
    float3 causticsTex2 = tex2Dlod(_CausticsTex, float4(uv2,0, distFade*causticsMaxMipLevel)).rgb;

    float3 caustics = min(causticsTex, causticsTex2) * edgeFadeMask;
    return caustics;
}
#endif

// Calculates the transmission factor 'T' based on water depth and color.
float3 CalculateAbsorption(SceneDepthInfo depthInfo, Refraction ray)
{
	const float3 waterAbsorptionReal = float3(0.811f, 0.0067f, 0.00166f);
    // Define the base absorption color of water
    float3 waterAbsorption = _RefractUseColor ? (1.0 - _RefractColor.rgb) : waterAbsorptionReal;
    
    // Scale absorption by user settings and water depth
    float absorptionUserTweak = 1.0f / _RefractAbsorptionScale;
    float3 refrAbsorption = waterAbsorption * min(depthInfo.waterDepth, _RefractMaxDepth) * absorptionUserTweak;

    // The final transmission factor 'T' is how much light makes it through the medium.
    // ray.d is the distance the refracted ray traveled through the medium.
    float3 T = min(1.0, exp(-refrAbsorption * ray.d));
    return T;
}

// Calculates the final color seen through the water, including refraction, fog, and caustics.
float3 GetBelowSurfaceColor(v2f i, WaterPixel pix, SceneDepthInfo depthInfo, Refraction ray, bool isFacing)
{
	// float4 refrPos = UnityWorldToClipPos(float4(ray.position, 1.0));
	// Using the output ray looks good but has bad artifacts, so just shift based on normals instead.
	float refrScale = 0.1 * saturate(100 * depthInfo.distToSurface); // TODO: Make user tweakable
	float4 refrPos = UnityWorldToClipPos(float4(pix.worldPos + (pix.geometricNormal - pix.worldNormal) * refrScale, 1.0));
	refrPos.xy = refrPos * float2( 0.5, 0.5*_ProjectionParams.x);
	refrPos.xy = TransformStereoScreenSpaceTex( refrPos.xy+0.5*refrPos.w, refrPos.w );
    refrPos.xy = abs(refrPos.xy);
    
    float3 refractionColor;
    UnityGIInput unityData = InitialiseUnityGIInput(pix.worldPos, pix.viewDir);

	float3 ambientSpecularCol = UnityGI_prefilteredRadiance(unityData, 1, ray.direction);

    #if defined(_USE_REFRACTION)
        float refrPerspectiveDivide = 1.0f / refrPos.w;
	    refractionColor = grabScreenColour(refrPos.xy, 0, refrPerspectiveDivide);
    #else
        // Fallback for when GrabPass isn't used (e.g., on mobile)
		// Roughness remapping so that an IOR of 1.0 means no microfacet refraction and an IOR
		// of 1.5 has full microfacet refraction
		// float perceptualRoughnessRefr = lerp(pix.perceptualRoughness, 0.0, saturate(etaIR * 3.0 - 2.0));

		// Reflection probes in Unity are not very representative of deep water, especially if they're
		// box projected. Instead, we use them as an approximation of the average lighting and later
		// fade towards the absorption colour like fog. 
        float perceptualRoughnessRefr = lerp(pix.perceptualRoughness, 0.0, saturate(ray.etaIR * 3.0 - 2.0));
        refractionColor = UnityGI_prefilteredRadiance(unityData, perceptualRoughnessRefr, ray.direction);
    #endif

    #if defined(_USE_CAUSTICS)
        float causticsDistFade = saturate(depthInfo.eyeDepthWorld / _CausticsMaxDistance);
        float surfaceEdgeFade = saturate(depthInfo.distToSurface);
		float3 caustics = getCausticProjected(depthInfo.worldPosEyeHit, surfaceEdgeFade, causticsDistFade);
		
		refractionColor += refractionColor * (_LightColor0 + ambientSpecularCol) * caustics * _CausticsStrength * (1 - causticsDistFade);
	#endif

    #if defined(_USE_DEPTHFOG)
        float fogFac = 1;
        // When looking at a backface (i.e. we're underwater) base the fog level on 
		// distance to the camera instead of distance to the surface below. 
        float fogDistance = !isFacing ? length(pix.fullVectorFromEyeToGeometry) : depthInfo.distToSurface;

		switch ((int)_FogMode)
		{
			case 0: fogFac = exp2(-_FogExpDensity * fogDistance); break; // Exponential
			case 1: fogFac = exp2(-_FogExpDensity * _FogExpDensity * fogDistance * fogDistance); break; // Exponential Squared
			case 2: fogFac = saturate((_FogLinearDensity.y - fogDistance) / (_FogLinearDensity.y - _FogLinearDensity.x)); break; // Linear
		};
		refractionColor = lerp(ambientSpecularCol, refractionColor, fogFac);
	#endif
    
    return refractionColor;
}

half4 BRDF_New_PBS (const WaterPixel pix, const WaterLighting lit, float3 Ft)
{
    float roughness = PerceptualRoughnessToRoughness(pix.perceptualRoughness);
	
	UnityIndirect gi = lit.indirect;

	gi.specular *= SpecularAO_Lagarde(pix.NdotV, lit.bakedOcclusion, roughness);

    half3 f0 = 0.16 * pix.reflectance + pix.specColor;

    float3 energyCompensation = 1.0 + f0 * (1.0 / pix.dfg.y - 1.0);

    half clampedRoughness = max(roughness, 0.002f);

    float3 halfDir = Unity_SafeNormalize (float3(lit.light.dir) + pix.viewDir);

    float nl = saturate(dot(pix.worldNormal, lit.light.dir));
    float nh = saturate(dot(pix.worldNormal, halfDir));

    half lv = saturate(dot(lit.light.dir, pix.viewDir));
    half lh = saturate(dot(lit.light.dir, halfDir));

    // Diffuse term
    half diffuseTerm = DisneyDiffuse(pix.NdotV, nl, lh, pix.perceptualRoughness) * nl;

    // Diffuse wrapping
    diffuseTerm = pow(saturate((diffuseTerm + _WrappingFactor) /
     (1.0f + _WrappingFactor)), _WrappingPowerFactor) * (_WrappingPowerFactor + 1) / 
    (2 * (1 + _WrappingFactor));

	float3 sssLighting = getSubsurfaceScatteringLight(lit.sssData, lit.light.dir, 
	_BackfaceNormals ? -pix.worldNormal : pix.worldNormal, 
	pix.viewDir, pix.transmission) ;

    float3 reflDir = reflect(-pix.viewDir, pix.worldNormal);
    float horizon = min(1 + dot(reflDir, pix.worldNormal), 1);

    float specularTerm = 0;

    half3 F = F_Schlick(lh, f0);
    half D = D_GGX(nh, clampedRoughness);
    half V = V_SmithGGXCorrelated(pix.NdotV, nl, clampedRoughness);

    F *= energyCompensation;

    specularTerm = max(0, (D * V) * F) * UNITY_PI * nl;

#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif

    // To provide true Lambert lighting, we need to be able to kill specular completely.
    specularTerm *= any(pix.specColor) ? 1.0 : 0.0;

	// Ref: This is what foliage uses, but water is not "solid"
	// float3 Fd = pix.diffuseColor * (gi.diffuse + lit.light.color * diffuseTerm + _LightColor0 * sssLighting);

	float3 Fd = pix.diffuseColor * (lit.light.color * diffuseTerm + _LightColor0 * sssLighting) * (1.0 - pix.thickness);
	Fd += lerp(pix.diffuseColor * gi.diffuse, Ft, pix.thickness);
	
	float3 Fr = specularTerm * lit.light.color * lit.bakedOcclusion;
	Fr += gi.specular * lerp(pix.dfg.xxx, pix.dfg.yyy, f0);

    //half3 color = Fr + lerp(Fd, Ft, pix.thickness);
    half3 color = Fr + Fd;

    return half4(color, 1);
}


// UNITY_SHADER_NO_UPGRADE
// == Main pass section =======================================================
#ifndef UNITY_PASS_SHADOWCASTER
// Samples the depth buffer to calculate distances to background geometry.
SceneDepthInfo GetSceneDepthInfo(v2f i, WaterPixel pix)
{
    SceneDepthInfo info = (SceneDepthInfo)0;

    // Compute projective scaling factor.
	// perspectiveFactor is 1.0 for the center of the screen, and goes above 1.0 toward the edges
	float perspectiveDivide = 1.0f / i.pos.w;
    float perspectiveFactor = length(pix.fullVectorFromEyeToGeometry * perspectiveDivide);
    float2 screenUV = i.screenPosition_Depth.xy * perspectiveDivide;
    
    // eyeDepthWorld is in meters.
    info.eyeDepthWorld = GetLinearZFromZDepth_WorksWithMirrors(screenDepthClamped(screenUV), i.screenPosition_Depth.xy) * perspectiveFactor;
    info.worldPosEyeHit = _WorldSpaceCameraPos + info.eyeDepthWorld * pix.worldSpaceDirection;
    info.distToSurface = distance(info.worldPosEyeHit, pix.worldPos); 
    info.dirToSurface = (info.worldPosEyeHit - pix.worldPos);
    // waterDepth is positive when viewed from above water, negative from below
    info.waterDepth = pix.worldPos.y - info.worldPosEyeHit.y; 

    return info;
}

float4 frag(v2f i, uint isFacing : SV_IsFrontFace) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );
	float3 worldPos = i.wPosAndHue.xyz;

    float3x3 tangentToWorld;
	#if defined(_NORMALMAP)
		tangentToWorld[0] = i.tangent.xyz;
		tangentToWorld[1] = i.bitangent.xyz;
		tangentToWorld[2] = i.normal.xyz;
	#else
		tangentToWorld[0] = float3(1, 0, 0);
		tangentToWorld[1] = float3(0, 1, 0);
		tangentToWorld[2] = i.normal.xyz;
	#endif

	WindParams wind = getWindParams();
	Texcoords tc = getTexcoords(i.uv, i.uv2, worldPos);
	WaterInputs mat = getWaterInputs(tc, worldPos, i.color, wind);
	WaterPixel pix = getPixelParams(mat, tangentToWorld, worldPos, isFacing);
    WaterLighting lit = GetSurfaceLighting(pix, i);
    
    // Early exit for opaque mode
    if (_Mode == 1.0 ) // Opaque mode
    {
        lit.bakedOcclusion *= mat.occlusion;

		// Combine lit and reflection
		float3 col = BRDF_New_PBS(pix, lit, float3(0, 0, 0)); // No refraction/underwater in opaque mode

		#ifdef UNITY_PASS_FORWARDADD
		return float4(col * lit.attenuation, 1.0 );
		#else
		return float4(col, 1.0);
		#endif
	}

	// Todo: Change below water processing based on isDepthAvailable();
    SceneDepthInfo depthInfo = GetSceneDepthInfo(i, pix);

	// When refraction is active, depth params need to match the refracted underwater scene. 
    Refraction ray;
	// Air's Index of refraction is 1.000277 at STP but everybody uses 1.0
	const float airIor = 1.0;
	// Water's IOR is 1.333. This is water, right? 
	// Switch between 1.333 (water IOR) and f0ToIor(pixel.f0.g) based on metalness
    float materialor = lerp(1.333, f0ToIor(pix.reflectance), mat.metallic);
    float etaIR = airIor / materialor;
    float etaRI = materialor / airIor;
    refractionSolidSphere(pix.worldPos, etaIR, etaRI, _RefractThickness * saturate(depthInfo.waterDepth), pix.worldNormal, -pix.viewDir, ray);
    
    // Increase water transmission where the water thickness is thin, as determined by the depth
    float surfaceEdgeFade = saturate(depthInfo.distToSurface);
	pix.alpha *= lerp(1.0, surfaceEdgeFade, _SurfEdgeFade);
    pix.thickness = lerp(1.0, pix.thickness, surfaceEdgeFade);

    #if defined(_USE_FOAM)
        float foamMask;
        float3 foamNormalDistortion;
        getFoamProperties(tc, wind, pix, depthInfo, i.wPosAndHue.w, /*out*/ foamMask, /*out*/ foamNormalDistortion);

        if (foamMask > 0.001)
        {
			// Todo: Is there a better way to blend this?
            pix.diffuseColor = lerp(pix.diffuseColor, _FoamColor.rgb, saturate(foamMask * 3));
            pix.perceptualRoughness = lerp(pix.perceptualRoughness, _FoamRoughness, foamMask);
            pix.worldNormal = normalize(pix.worldNormal + foamNormalDistortion * foamMask * foamMask);
            pix.alpha = saturate(pix.alpha + foamMask);
            
            // Filter the edge of the foam from the roughness so there is no specular shine there
            float specEdgeMaskA = (1.0 - saturate(length(pix.worldNormal * depthInfo.dirToSurface.y * 25)));
            float specEdgeMaskB = (1.0 - saturate(foamMask*200));
            pix.perceptualRoughness += specEdgeMaskA*specEdgeMaskB;
            // It seems like it would make sense to apply the foam mask to the refraction thickness,
	        // but because foam is very light, it looks really ugly depending on light conditions.
            pix.thickness *= lerp(1.0, 0.0, foamMask);
        }
    #endif


	#if defined (_USE_INFINITY)
		// When infinite clip plane is used, the water can appear above objects it intersects with. 
		if (depthInfo.dirToSurface.y > 0) clip (-1);
	#endif

    #if !defined(UNITY_PASS_FORWARDADD)
    float3 belowSurfaceColor = GetBelowSurfaceColor(i, pix, depthInfo, ray, isFacing);
    float3 T = CalculateAbsorption(depthInfo, ray);

    // base color changes the amount of light passing through the boundary
    belowSurfaceColor *= lerp(1, pix.diffuseColor, _RefractSurfaceCol * surfaceEdgeFade);
	// fresnel from the first interface
	belowSurfaceColor *= 1.0 - (pix.specColor * pix.dfg.z);
	belowSurfaceColor *= T;
	#else
	float3 belowSurfaceColor = 0;
	#endif

	lit.bakedOcclusion *= surfaceEdgeFade;
	lit.bakedOcclusion *= mat.occlusion;

	float3 reflectionDir = reflect(-pix.viewDir, pix.worldNormal);

	// Indirect specular can be from the reflection probe, SSR, or a mirror. 
	float3 indirectSpecular = lit.indirect.specular;
	
	#if defined(_USE_SSR)
		float4 ssrNoise;
		ssrNoise.x = interleavedGradientNoise(i.pos.xy);
		ssrNoise.y = interleavedGradientNoise(i.pos.xy+0.333);
		ssrNoise.z = interleavedGradientNoise(i.pos.xy+0.666);
		ssrNoise.z = interleavedGradientNoise(i.pos.xy+0.888);
		float ssrRdotV = saturate(0.95 * dot(reflectionDir, -pix.viewDir.xyz) + 0.05);

		float ssrThreshold = saturate((pix.perceptualRoughness - 0.5) * -10);

		#define SSR_FALLOFF_START 0.6666667
		ssrRdotV = saturate(ssrRdotV / SSR_FALLOFF_START);
		ssrRdotV = ssrRdotV * ssrRdotV * (3 - 2 * ssrRdotV); 
		ssrThreshold *= ssrRdotV;
		
		SSRData ssr_data = GetSSRData(
			worldPos.xyz, pix.viewDir, reflectionDir,
			pix.geometricNormal, pix.perceptualRoughness,
			ssrRdotV, interleavedGradientNoise(i.pos.xy));
		
		float4 ssrResult = 0.0;

		if (ssrThreshold > 0.008) 
		{
			ssrResult += getSSRColor(ssr_data);
		}

		ssrResult *= ssrThreshold;
		// SSR reflections will override baked reflection probes and baked specular...
		indirectSpecular = lerp(indirectSpecular, ssrResult, saturate(ssrResult.a));
		lit.bakedOcclusion = max(lit.bakedOcclusion, ssrResult.a);
	#endif

	#if defined(_USE_MIRROR)
		// If the normals are super bent, we could conceivably sample from the probe instead of the mirror,
		// but in practise, the visual difference is minimal. 
		float mirrorScale = 1 * saturate(depthInfo.distToSurface);
		float4 mirrorPos = UnityWorldToClipPos(float4(worldPos + (pix.geometricNormal - pix.worldNormal) * mirrorScale, 1.0));

		mirrorPos = ComputeNonStereoScreenPos(mirrorPos);

		mirrorPos.xy = abs(mirrorPos.xy);

		float mirrorPerspectiveDivide = 1.0f / mirrorPos.w;
		float mirrorPerspectiveFactor = length( pix.fullVectorFromEyeToGeometry * mirrorPerspectiveDivide );
		float4 mirrorReflection = getMirrorSurface(mirrorPos.xy, mirrorPerspectiveDivide);
		// Backfaces are never valid for mirrors
		if (isFacing) indirectSpecular = mirrorReflection;
	#endif

	// Add our combined specular to the derived specular. 
    #if !defined(UNITY_PASS_FORWARDADD)
	lit.indirect.specular = indirectSpecular;
	#endif

	/* Notes
	/  In Filament, refraction is applied at the last step before Fd (diffuse) and Fr (specular)
	/  light are combined. First the base colour is applied to the refraction (as above,
	/  and then the refraction applies like so; color.rgb += Fr + lerp(Fd, Ft, pixel.transmission);
	*/

	float3 col = BRDF_New_PBS(pix, lit, belowSurfaceColor);
	
	UNITY_APPLY_FOG(i.fogCoord, col); 

	// Prepare the alpha value; this is used either for alpha blending, or for blending in the
	// refraction. 
	float outAlpha = saturate(pix.alpha);
	#if defined(_USE_REFRACTION)
	outAlpha = 1.0;
	#endif

	// Distant fog override for distance fade to skybox
	if (_DistantFogOverrideLevel > 0.01)
	{
		float4 distantCol = SAMPLE_TEXTURECUBE(_DistantFogOverrideTex, sampler_DistantFogOverrideTex, -pix.viewDir);
		float fogStart = _DistantFogOverrideDistance;
		float fogEnd = _ProjectionParams.y; // Far clip plane
		float fadeValue = saturate((fogEnd - length(pix.fullVectorFromEyeToGeometry)) / (fogEnd - fogStart)) * _DistantFogOverrideLevel;
		distantCol.rgb = DecodeHDR(distantCol, _DistantFogOverrideTex_HDR);
		col.rgb = lerp(col,distantCol, fadeValue);
	};

	//UNITY_APPLY_FOG(i.fogCoord, col); 
	#ifdef UNITY_PASS_FORWARDADD
	return float4(col * lit.attenuation, 1.0 * lit.attenuation);
	#else
	return float4(col, outAlpha);
	#endif
}
#else
// == Shadowcaster section ====================================================
float4 frag(v2f i) : SV_Target
{    
	UNITY_SETUP_INSTANCE_ID(i);
	// We want to handle the shadowcasting logic like this.
	// Water should be transparent. It can't write to the depth buffer. However,
	// it should cast shadows if the user wants. Add that in later.
	
	float3 worldPos = i.wPosAndHue.xyz;

	WindParams wind = getWindParams();

	Texcoords tc = getTexcoords(i.uv, i.uv2, worldPos);

	bool isShadowPass = dot(unity_LightShadowBias, 1);

	if (isShadowPass)
	{
		float alpha = 0;
		
		#if defined(_USE_FOAM)
		float foamMask = getFoamLevel(tc, wind, 0, 0, 0);
		alpha += (foamMask * 10);
		#endif

		#if defined(_USE_CAUSTICSHADOW)
		// We can add in some caustics, but they're pretty limited. 
		// Shadows only darken, instead of lighten. 
		// Fade them out as they're distant from the camera to avoid artifacts. 
		float dist = distance(worldPos, _WorldSpaceCameraPos)/2;
		float3 caustics = getCausticsShadow(worldPos) * 10;

		// Given a texture with white (lit) caustic patterns, only those regions should be fully bright. 
		alpha += (1.0 - caustics) * _CausticsShadowStrength;
		#endif

		float4 noise = tanoise4(float4(worldPos + _SinTime.xyz + _CosTime.zyx, _Time.x));
		
		float dither = RDitherPattern(i.pos.xy + _SinTime.x);
		
		clip(alpha - dither);
	}
	else
	{
		clip(-1);
	}


	SHADOW_CASTER_FRAGMENT(i)
}
#endif
	ENDCG

	SubShader
	{
		// PC
		LOD 300
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" }
		
		// On PC, GrabPass is OK.
		GrabPass
		{
			Tags { "LightMode" = "Always" }
			// _GrabTexture is chosen for optimisation reasons, but it may be
			// used by other shaders. If you see strange problems with the refraction
			// in your scene missing objects, check the Frame Debugger to make sure
			// _GrabTexture is being created after the opaque pass has ended. 
			"_GrabTexture"
		}

		Cull [_CullMode]
		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
			CGPROGRAM

			#pragma target 5.0
            #pragma exclude_renderers gles gles3
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_fragment _EMISSION
			#pragma shader_feature_local_fragment _USE_FOAM // foam
			#pragma shader_feature_local_fragment _USE_FOAM_NORMAL
			#pragma shader_feature_local_fragment _USE_REFRACTION 
			#pragma shader_feature_local_fragment _USE_FLOWMAP 
			#pragma shader_feature_local_fragment _USE_DEPTHFOG
			#pragma shader_feature_local_fragment _ _USE_MIRROR _USE_SSR
			#pragma shader_feature_local_vertex _USE_WAVES
			#pragma shader_feature_local _USE_CAUSTICS
			#pragma shader_feature_local _USE_INFINITY

			#pragma multi_compile _ LOD_FADE_CROSSFADE // LOD fading
			
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _GLOSSYREFLECTIONS_OFF
			
            #pragma shader_feature_local_fragment _LIGHTMAPSPECULAR
            #pragma shader_feature_local_fragment _ _BAKERY_RNM _BAKERY_SH _BAKERY_MONOSH
            #pragma shader_feature_local_fragment _VRCLV

			#ifndef UNITY_PASS_FORWARDBASE
			#define UNITY_PASS_FORWARDBASE
			#endif

			#pragma multi_compile_fwdbase

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}

		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
            ZWrite [_ZWrite]
            ZTest LEqual
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
			CGPROGRAM
			#pragma target 5.0
            #pragma exclude_renderers gles gles3
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _USE_FOAM // foam
			#pragma shader_feature_local_fragment _USE_FOAM_NORMAL
			#pragma shader_feature_local_fragment _USE_FLOWMAP 
			#pragma shader_feature_local_vertex _USE_WAVES
			#pragma shader_feature_local _USE_INFINITY

			#pragma multi_compile _ LOD_FADE_CROSSFADE // LOD fading
			
			#ifndef UNITY_PASS_FORWARDADD
			#define UNITY_PASS_FORWARDADD
			#endif

			#pragma multi_compile_fwdadd_fullshadows

			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "LightMode" = "ShadowCaster" }
			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			Blend One Zero
			CGPROGRAM
			#pragma target 5.0
            #pragma exclude_renderers gles gles3
			#pragma shader_feature_local_fragment _USE_FOAM // foam
			#pragma shader_feature_local_fragment _USE_CAUSTICSHADOW
			#pragma shader_feature_local_vertex _USE_WAVES
			#pragma shader_feature_local _USE_INFINITY

			#pragma multi_compile _ LOD_FADE_CROSSFADE // LOD fading

			#ifndef UNITY_PASS_SHADOWCASTER
			#define UNITY_PASS_SHADOWCASTER
			#endif

			#pragma multi_compile_shadowcaster

			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		// UsePass "Standard/META"

		// Deferred fallback for baking
		// UsePass "Standard/DEFERRED"
	}

	SubShader
	{
		// Quest
		LOD 150
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" }

		// On Quest, GrabPass is not OK. We ignore it even if set.

		Cull [_CullMode]
		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
			CGPROGRAM

			#pragma target 4.0
            #pragma exclude_renderers d3d11
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_fragment _EMISSION
			// #pragma shader_feature_local_fragment _USE_FOAM // foam requires depth -- not supported here
			// #pragma shader_feature_local_fragment _USE_REFRACTION // refraction -- not supported here
			#pragma shader_feature_local_fragment _USE_FLOWMAP 
			// #pragma shader_feature_local_fragment _USE_DEPTHFOG // requires depth -- not supported here
			#pragma shader_feature_local_vertex _USE_WAVES

			#pragma multi_compile _ LOD_FADE_CROSSFADE // LOD fading
			
			#pragma shader_feature_local_fragment  _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment  _GLOSSYREFLECTIONS_OFF
            #pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED
            #pragma shader_feature_local_fragment _LIGHTMAPSPECULAR
            #pragma shader_feature_local_fragment _ _BAKERY_RNM _BAKERY_SH _BAKERY_MONOSH
            #pragma shader_feature_local_fragment _VRCLV

			#ifndef UNITY_PASS_FORWARDBASE
			#define UNITY_PASS_FORWARDBASE
			#endif

			#define TARGET_MOBILE

			#pragma multi_compile_fwdbase

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}

		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
            ZWrite [_ZWrite]
            ZTest LEqual
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
			CGPROGRAM
			#pragma target 4.0
            #pragma exclude_renderers d3d11
			#pragma shader_feature_local _NORMALMAP
			// #pragma shader_feature_local_fragment _USE_FOAM // foam requires depth -- not supported here
			#pragma shader_feature_local_fragment _USE_FLOWMAP 
			#pragma shader_feature_local_vertex _USE_WAVES

			#pragma multi_compile _ LOD_FADE_CROSSFADE // LOD fading

            #pragma skip_variants SHADOWS_SOFT
			
			#ifndef UNITY_PASS_FORWARDADD
			#define UNITY_PASS_FORWARDADD
			#endif

			#define TARGET_MOBILE

			#pragma multi_compile_fwdadd_fullshadows

			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "LightMode" = "ShadowCaster" }
			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			Blend One Zero
			CGPROGRAM
			#pragma target 4.0
            #pragma exclude_renderers d3d11
			#pragma shader_feature_local_fragment _USE_CAUSTICSHADOW
			#pragma shader_feature_local_vertex _USE_WAVES
			// #pragma shader_feature_local_fragment _USE_FOAM // foam requires depth -- not supported here

			#pragma multi_compile _ LOD_FADE_CROSSFADE // LOD fading

            #pragma skip_variants SHADOWS_SOFT

			#ifndef UNITY_PASS_SHADOWCASTER
			#define UNITY_PASS_SHADOWCASTER
			#endif

			#define TARGET_MOBILE

			#pragma multi_compile_shadowcaster

			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		// UsePass "Standard/META"

		// Deferred fallback for baking
		// UsePass "Standard/DEFERRED" 
	}
CustomEditor "SilentClearWater.Unity.ClearWaterInspector"
}
