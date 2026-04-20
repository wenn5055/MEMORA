#ifndef WATER_INDIRECT
#define WATER_INDIRECT

#include "LightVolumes.cginc"

struct Light {
    float4 colorIntensity;  // rgb, pre-exposed intensity
    float3 l;
    float attenuation;
    float NoL;
};

Light ConvertToLight(UnityLight inLight)
{
    Light outLight = (Light)0;
    outLight.colorIntensity = float4(inLight.color, 1.0f);
    outLight.l = inLight.dir;
    return outLight;
}

UnityLight ConvertFromLight(Light outLight)
{
    UnityLight inLight = (UnityLight)0;
    inLight.color = outLight.colorIntensity;
    inLight.dir = outLight.l;
    return inLight;
}

 
// Light probe/SH functions

float luminance(const float3 linearCol) {
    return dot(linearCol, float3(0.2126, 0.7152, 0.0722));
}

float remap_almostIdentity( float x, float m, float n )
{
    if( x>m ) return x;
    const float a = 2.0*n - m;
    const float b = 2.0*m - 3.0*n;
    const float t = x/m;
    return (a*t + b)*t*t + n;
}

#if defined(TARGET_MOBILE)
    // min roughness such that (MIN_PERCEPTUAL_ROUGHNESS^4) > 0 in fp16 (i.e. 2^(-14/4), rounded up)
    #define MIN_PERCEPTUAL_ROUGHNESS 0.089
    #define MIN_ROUGHNESS            0.007921
#else
    #define MIN_PERCEPTUAL_ROUGHNESS 0.045
    #define MIN_ROUGHNESS            0.002025
#endif

/*
// Paper: ZH3: Quadratic Zonal Harmonics, i3D 2024. https://torust.me/ZH3.pdf
// Code based on paper and demo https://www.shadertoy.com/view/Xfj3RK
// https://gist.github.com/pema99/f735ca33d1299abe0e143ee94fc61e73
*/

// L1 radiance = L1 irradiance * PI / Y_1 / AHat_1
// PI / (sqrt(3 / PI) / 2) / ((2 * PI) / 3) = sqrt(3 * PI)
const static float L0IrradianceToRadiance = 2 * sqrt(UNITY_PI);

// L0 radiance = L0 irradiance * PI / Y_0 / AHat_0
// PI / (sqrt(1 / PI) / 2) / PI = 2 * sqrt(PI)
const static float L1IrradianceToRadiance = sqrt(3 * UNITY_PI);

const static float4 L0L1IrradianceToRadiance = float4(L0IrradianceToRadiance, L1IrradianceToRadiance, L1IrradianceToRadiance, L1IrradianceToRadiance);

// Evaluate irradiance in direction normal from the linear SH sh,
// hallucinating the ZH3 coefficient and then using that and linear SH
// for reconstruction.
float SHEvalLinearL0L1_ZH3Hallucinate(float4 sh, float3 normal)
{
    float4 radiance = sh * L0L1IrradianceToRadiance;

    float3 zonalAxis = float3(radiance.w, radiance.y, radiance.z);
    float l1Length = length(zonalAxis);
    zonalAxis /= l1Length;

    float ratio = l1Length / radiance.x;
    float zonalL2Coeff = radiance.x * ratio * (0.08 + 0.6 * ratio); // Curve-fit.

    float fZ = dot(zonalAxis, normal);
    float zhNormal = sqrt(5.0f / (16.0f * UNITY_PI)) * (3.0f * fZ * fZ - 1.0f);

    float result = dot(sh, float4(1, float3(normal.y, normal.z, normal.x)));
    result += 0.25f * zhNormal * zonalL2Coeff;
    return result;
}

float3 SHEvalLinearL0L1_ZH3Hallucinate(float3 normal, float3 L0, 
    float3 L1r, float3 L1g, float3 L1b)
{
    float3 shL0 = L0;
    float3 shL1_1 = float3(L1r.y, L1g.y, L1b.y);
    float3 shL1_2 = float3(L1r.z, L1g.z, L1b.z);
    float3 shL1_3 = float3(L1r.x, L1g.x, L1b.x);

    float3 result = 0.0;
    float4 a = float4(shL0.r, shL1_1.r, shL1_2.r, shL1_3.r);
    float4 b = float4(shL0.g, shL1_1.g, shL1_2.g, shL1_3.g);
    float4 c = float4(shL0.b, shL1_1.b, shL1_2.b, shL1_3.b);
    result.r = SHEvalLinearL0L1_ZH3Hallucinate(a, normal);
    result.g = SHEvalLinearL0L1_ZH3Hallucinate(b, normal);
    result.b = SHEvalLinearL0L1_ZH3Hallucinate(c, normal);
    return result;
}

// From https://github.com/lukis101/VRCUnityStuffs/tree/master/SH
// SH Convolution Functions
// Code adapted from https://blog.selfshadow.com/2012/01/07/righting-wrap-part-2/
///////////////////////////

float3 GeneralWrapSH(float fA) // original unoptimized
{
    // Normalization factor for our model.
    float norm = 0.5 * (2 + fA) / (1 + fA);
    float4 t = float4(2 * (fA + 1), fA + 2, fA + 3, fA + 4);
    return norm * float3(t.x / t.y, 2 * t.x / (t.y * t.z),
        t.x * (fA * fA - t.x + 5) / (t.y * t.z * t.w));
}
float3 GeneralWrapSHOpt(float fA)
{
    const float4 t0 = float4(-0.047771, -0.129310, 0.214438, 0.279310);
    const float4 t1 = float4( 1.000000,  0.666667, 0.250000, 0.000000);

    float3 r;
    r.xyz = saturate(t0.xxy * fA + t0.yzw);
    r.xyz = -r * fA + t1.xyz;
    return r;
}

float3 GreenWrapSHOpt(float fW)
{
    const float4 t0 = float4(0.0, 1.0 / 4.0, -1.0 / 3.0, -1.0 / 2.0);
    const float4 t1 = float4(1.0, 2.0 / 3.0,  1.0 / 4.0,  0.0);

    float3 r;
    r.xyz = t0.xxy * fW + t0.xzw;
    r.xyz = r.xyz * fW + t1.xyz;
    return r;
}

float3 ShadeSH9_wrapped(float3 normal, float3 conv)
{
    float3 x0, x1, x2;
    conv *= float3(1, 1.5, 4); // Undo pre-applied cosine convolution
    //conv *= _Bands.xyz; // debugging

    // Constant (L0)
    // Band 0 has constant part from 6th kernel (band 1) pre-applied, but ignore for performance
    x0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

    // Linear (L1) polynomial terms
    x1.r = (dot(unity_SHAr.xyz, normal));
    x1.g = (dot(unity_SHAg.xyz, normal));
    x1.b = (dot(unity_SHAb.xyz, normal));

    // 4 of the quadratic (L2) polynomials
    float4 vB = normal.xyzz * normal.yzzx;
    x2.r = dot(unity_SHBr, vB);
    x2.g = dot(unity_SHBg, vB);
    x2.b = dot(unity_SHBb, vB);

    // Final (5th) quadratic (L2) polynomial
    float vC = normal.x * normal.x - normal.y * normal.y;
    x2 += unity_SHC.rgb * vC;

    return x0 * conv.x + x1 * conv.y + x2 * conv.z;
}

float3 ShadeSH9_wrappedCorrect(float3 normal, float3 conv)
{
    const float3 cosconv_inv = float3(1, 1.5, 4); // Inverse of the pre-applied cosine convolution
    float3 x0, x1, x2;
    conv *= cosconv_inv; // Undo pre-applied cosine convolution
    //conv *= _Bands.xyz; // debugging

    // Constant (L0)
    x0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    // Remove the constant part from L2 and add it back with correct convolution
    float3 otherband = float3(unity_SHBr.z, unity_SHBg.z, unity_SHBb.z) / 3.0;
    x0 = (x0 + otherband) * conv.x - otherband * conv.z;

    // Linear (L1) polynomial terms
    x1.r = (dot(unity_SHAr.xyz, normal));
    x1.g = (dot(unity_SHAg.xyz, normal));
    x1.b = (dot(unity_SHAb.xyz, normal));

    // 4 of the quadratic (L2) polynomials
    float4 vB = normal.xyzz * normal.yzzx;
    x2.r = dot(unity_SHBr, vB);
    x2.g = dot(unity_SHBg, vB);
    x2.b = dot(unity_SHBb, vB);

    // Final (5th) quadratic (L2) polynomial
    float vC = normal.x * normal.x - normal.y * normal.y;
    x2 += unity_SHC.rgb * vC;

    return x0 + x1 * conv.y + x2 * conv.z;
}

float2 GeneralWrapSH_L0L1(float fA) {
    float norm = 0.5 * (2 + fA) / (1 + fA);
    float4 t = float4(2 * (fA + 1), fA + 2, fA + 3, fA + 4);
    return norm * float2(t.x / t.y, 2 * t.x / (t.y * t.z));
}

float2 GeneralWrapSHOpt_L0L1(float fA) {
    const float4 t0 = float4(-0.047771, -0.129310, 0.214438, 0.279310);
    const float4 t1 = float4( 1.000000,  0.666667, 0.250000, 0.000000);
    float2 r;
    r.xy = saturate(t0.xx * fA + t0.yz); // Adjusted indexing
    r.xy = -r * fA + t1.xy;
    return r;
}

float3 ShadeSH_L0L1_wrapped(float3 normal, float2 conv_l0l1, 
    float3 L0, float3 L1r, float3 L1g, float3 L1b) {
    float3 x0, x1;
    // Undo pre-applied cosine convolution for L0 and L1
    conv_l0l1 *= float2(1.0, 1.5);

    // Constant (L0)
    x0 = L0;

    // Linear (L1) polynomial terms
    x1.r = dot(L1r.xyz, normal);
    x1.g = dot(L1g.xyz, normal);
    x1.b = dot(L1b.xyz, normal);

    return x0 * conv_l0l1.x + x1 * conv_l0l1.y;
}

// 

#if UNITY_LIGHT_PROBE_PROXY_VOLUME
// normal should be normalized, w=1.0
half3 Irradiance_SampleProbeVolume (half4 normal, float3 worldPos)
{
    const float transformToLocal = unity_ProbeVolumeParams.y;
    const float texelSizeX = unity_ProbeVolumeParams.z;

    //The SH coefficients textures and probe occlusion are packed into 1 atlas.
    //-------------------------
    //| ShR | ShG | ShB | Occ |
    //-------------------------

    float3 position = (transformToLocal == 1.0f) ? mul(unity_ProbeVolumeWorldToObject, float4(worldPos, 1.0)).xyz : worldPos;
    float3 texCoord = (position - unity_ProbeVolumeMin.xyz) * unity_ProbeVolumeSizeInv.xyz;
    texCoord.x = texCoord.x * 0.25f;

    // We need to compute proper X coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between RGB coefficients
    float texCoordX = clamp(texCoord.x, 0.5f * texelSizeX, 0.25f - 0.5f * texelSizeX);

    // sampler state comes from SHr (all SH textures share the same sampler)
    texCoord.x = texCoordX;
    half4 SHAr = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

    texCoord.x = texCoordX + 0.25f;
    half4 SHAg = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

    texCoord.x = texCoordX + 0.5f;
    half4 SHAb = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

    // Linear + constant polynomial terms
    half3 x1;

        x1.r = dot(SHAr, normal);
        x1.g = dot(SHAg, normal);
        x1.b = dot(SHAb, normal);

    return x1;
}
#endif



#if defined(_VRCLV)
half3 Irradiance_SampleVRCLightVolume(half3 normal, float3 worldPos, float transmission, out Light o_Light)
{
    o_Light = (Light)0;
    
    // Fetch Spherical Harmonics (SH) components from the VRC Light Volume
    float3 L0, L1r, L1g, L1b;
    LightVolumeSH(worldPos, L0, L1r, L1g, L1b, normal * getLightVolumeSurfaceBias());

    L1r *= transmission;
    L1g *= transmission;
    L1b *= transmission;

    // Compute irradiance using the SH components
    half3 irradiance = 0.0;
    
    float2 sh_conv = GeneralWrapSHOpt_L0L1(transmission);
    irradiance = ShadeSH_L0L1_wrapped(normal.xyz, sh_conv, L0, L1r, L1g, L1b);

    float3 nL1x; float3 nL1y; float3 nL1z;
    nL1x = float3(L1r[0], L1g[0], L1b[0]);
    nL1y = float3(L1r[1], L1g[1], L1b[1]);
    nL1z = float3(L1r[2], L1g[2], L1b[2]);
    float3 dominantDir = float3(luminance(nL1x), luminance(nL1y), luminance(nL1z));

    half L0_lum = max(FLT_EPS, luminance(L0));
    half L1_mag = length(dominantDir);
    half directionality = saturate(L1_mag / L0_lum);
    o_Light.l = dominantDir / L1_mag;
    
    float3 directionalColor = float3(dot(L1r, o_Light.l), dot(L1g, o_Light.l), dot(L1b, o_Light.l));
    float3 Li = L0 + max(0, directionalColor);

    o_Light.colorIntensity = float4(Li, 1.0);
    o_Light.attenuation = directionality;
    o_Light.NoL = saturate(dot(normal, o_Light.l));

    return irradiance;
}

half3 Irradiance_SampleVRCLightVolumeAdditive(half3 normal, float3 worldPos, out Light o_Light)
{
    // Fetch Spherical Harmonics (SH) components from the VRC Light Volume
    float3 L0, L1r, L1g, L1b;
    LightVolumeAdditiveSH(worldPos, L0, L1r, L1g, L1b, normal * getLightVolumeSurfaceBias());

    // Compute irradiance using the SH components
    half3 irradiance = 0.0;

    // Doesn't support non-linear evaluation, so just evaluate normally. 
        irradiance.r = dot(L1r, normal.xyz) + L0.r;
        irradiance.g = dot(L1g, normal.xyz) + L0.g;
        irradiance.b = dot(L1b, normal.xyz) + L0.b;
    
    // Add derived light to existing derived light
    float3 nL1x; float3 nL1y; float3 nL1z;
    nL1x = float3(L1r[0], L1g[0], L1b[0]);
    nL1y = float3(L1r[1], L1g[1], L1b[1]);
    nL1z = float3(L1r[2], L1g[2], L1b[2]);
    float3 dominantDir = float3(luminance(nL1x), luminance(nL1y), luminance(nL1z));

    half L0_lum = max(FLT_EPS, luminance(L0));
    half L1_mag = length(dominantDir);
    half directionality = saturate(L1_mag / L0_lum);
    o_Light.l = dominantDir / L1_mag;
    
    float3 directionalColor = float3(dot(L1r, o_Light.l), dot(L1g, o_Light.l), dot(L1b, o_Light.l));
    float3 Li = L0 + max(0, directionalColor);

    o_Light.colorIntensity = float4(Li, 1.0);
    o_Light.attenuation = directionality;
    o_Light.NoL = saturate(dot(normal, o_Light.l));

    return irradiance;
}
#endif

half3 Irradiance_SphericalHarmonicsUnity (half3 normal, float3 worldPos, float transmission)
{
    half3 ambient_contrib = 0.0;
    float3 sh_conv = GeneralWrapSHOpt(transmission);

    #if UNITY_LIGHT_PROBE_PROXY_VOLUME
        if (unity_ProbeVolumeParams.x == 1.0)
            ambient_contrib = Irradiance_SampleProbeVolume(half4(normal, 1.0), worldPos);
        else
    		ambient_contrib += ShadeSH9_wrappedCorrect(normal, sh_conv);
    #else
    	ambient_contrib += ShadeSH9_wrappedCorrect(normal, sh_conv);
    #endif

    ambient_contrib = max(half3(0, 0, 0), ambient_contrib);

    return ambient_contrib;
}

// Lightmap function

float4 SampleShadowmaskBicubic(float2 uv)
{
    #ifdef SHADER_API_D3D11
        float width, height;
        unity_ShadowMask.GetDimensions(width, height);

        float4 unity_ShadowMask_TexelSize = float4(width, height, 1.0/width, 1.0/height);

        return SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(unity_ShadowMask, samplerunity_ShadowMask),
            uv, unity_ShadowMask_TexelSize);
    #else
        return UNITY_SAMPLE_TEX2D_SAMPLER(unity_ShadowMask, unity_ShadowMask, uv);
    #endif
}

float4 SampleLightmapBicubic(float2 uv)
{
    #ifdef SHADER_API_D3D11
        float width, height;
        unity_Lightmap.GetDimensions(width, height);

        float4 unity_Lightmap_TexelSize = float4(width, height, 1.0/width, 1.0/height);

        return SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
            uv, unity_Lightmap_TexelSize);
    #else
        return UNITY_SAMPLE_TEX2D_SAMPLER(unity_Lightmap, unity_Lightmap, uv);
    #endif
}

float4 SampleLightmapDirBicubic(float2 uv)
{
    #ifdef SHADER_API_D3D11
        float width, height;
        unity_LightmapInd.GetDimensions(width, height);

        float4 unity_LightmapInd_TexelSize = float4(width, height, 1.0/width, 1.0/height);

        return SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(unity_LightmapInd, samplerunity_Lightmap),
            uv, unity_LightmapInd_TexelSize);
    #else
        return UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, uv);
    #endif
}

float4 SampleDynamicLightmapBicubic(float2 uv)
{
    #ifdef SHADER_API_D3D11
        float width, height;
        unity_DynamicLightmap.GetDimensions(width, height);

        float4 unity_DynamicLightmap_TexelSize = float4(width, height, 1.0/width, 1.0/height);

        return SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
            uv, unity_DynamicLightmap_TexelSize);
    #else
        return UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicLightmap, unity_DynamicLightmap, uv);
    #endif
}

float4 SampleDynamicLightmapDirBicubic(float2 uv)
{
    #ifdef SHADER_API_D3D11
        float width, height;
        unity_DynamicDirectionality.GetDimensions(width, height);

        float4 unity_DynamicDirectionality_TexelSize = float4(width, height, 1.0/width, 1.0/height);

        return SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
            uv, unity_DynamicDirectionality_TexelSize);
    #else
        return UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
    #endif
}


#if (defined(_BAKERY_RNM) || defined(_BAKERY_SH))
#define USING_BAKERY
#if defined(SHADER_API_D3D11)
SAMPLER(sampler_RNM0);
TEXTURE2D_HALF(_RNM0);
TEXTURE2D_HALF(_RNM1);
TEXTURE2D_HALF(_RNM2);
#else
sampler2D _RNM0;
sampler2D _RNM1;
sampler2D _RNM2;
#endif
#endif

inline float3 DecodeDirectionalLightmapSpecular(half3 color, half4 dirTex, half3 normalWorld, 
    const bool isRealtimeLightmap, fixed4 realtimeNormalTex, out Light o_light)
{
    o_light = (Light)0;
    o_light.colorIntensity = float4(color, 1.0);
    o_light.l = dirTex.xyz * 2 - 1;

    // The length of the direction vector is the light's "directionality", i.e. 1 for all light coming from this direction,
    // lower values for more spread out, ambient light.
    half directionality = max(0.001, length(o_light.l));
    o_light.l /= directionality;

    #ifdef DYNAMICLIGHTMAP_ON
    if (isRealtimeLightmap)
    {
        // Realtime directional lightmaps' intensity needs to be divided by N.L
        // to get the incoming light intensity. Baked directional lightmaps are already
        // output like that (including the max() to prevent div by zero).
        half3 realtimeNormal = realtimeNormalTex.xyz * 2 - 1;
        o_light.colorIntensity /= max(0.125, dot(realtimeNormal, o_light.l));
    }
    #endif

    // Split light into the directional and ambient parts, according to the directionality factor.
    o_light.colorIntensity = o_light.colorIntensity * directionality;
    o_light.attenuation = directionality;
    o_light.NoL = saturate(dot(normalWorld, o_light.l));

    return color * lerp(1.0, o_light.NoL, directionality);
}

#if defined(USING_BAKERY) && defined(LIGHTMAP_ON)
// needs specular variant?
float3 DecodeRNMLightmap(half3 color, half2 lightmapUV, half3 normalTangent, float3x3 tangentToWorld, out Light o_light)
{
    const float rnmBasis0 = float3(0.816496580927726f, 0, 0.5773502691896258f);
    const float rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
    const float rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);

    float3 irradiance;
    o_light = (Light)0;

    #ifdef SHADER_API_D3D11
        float width, height;
        _RNM0.GetDimensions(width, height);

        float4 rnm_TexelSize = float4(width, height, 1.0/width, 1.0/height);
        
        float3 rnm0 = DecodeLightmap(SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(_RNM0, sampler_RNM0), lightmapUV, rnm_TexelSize));
        float3 rnm1 = DecodeLightmap(SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(_RNM1, sampler_RNM0), lightmapUV, rnm_TexelSize));
        float3 rnm2 = DecodeLightmap(SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(_RNM2, sampler_RNM0), lightmapUV, rnm_TexelSize));
    #else
        float3 rnm0 = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(_RNM0, _RNM0, lightmapUV));
        float3 rnm1 = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(_RNM1, _RNM0, lightmapUV));
        float3 rnm2 = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(_RNM2, _RNM0, lightmapUV));
    #endif

    normalTangent.g *= -1;

    irradiance =  saturate(dot(rnmBasis0, normalTangent)) * rnm0
                + saturate(dot(rnmBasis1, normalTangent)) * rnm1
                + saturate(dot(rnmBasis2, normalTangent)) * rnm2;

    #if defined(_LIGHTMAPSPECULAR)
    float3 dominantDirT = rnmBasis0 * luminance(rnm0) +
                          rnmBasis1 * luminance(rnm1) +
                          rnmBasis2 * luminance(rnm2);

    float3 dominantDirTN = normalize(dominantDirT);
    float3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
                       saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
                       saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;                        

    o_light.l = normalize(mul(tangentToWorld, dominantDirT));
    half directionality = max(FLT_EPS, length(o_light.l));
    o_light.l /= directionality;

    // Split light into the directional and ambient parts, according to the directionality factor.
    o_light.colorIntensity = float4(specColor * directionality, 1.0);
    o_light.attenuation = directionality;
    o_light.NoL = saturate(dot(normalTangent, dominantDirTN));
    #endif

    return irradiance;
}

float3 DecodeSHLightmap(half3 L0, half2 lightmapUV, half3 normalWorld, out Light o_light)
{
    float3 irradiance;
    o_light = (Light)0;

    #ifdef SHADER_API_D3D11
        float width, height;
        _RNM0.GetDimensions(width, height);

        float4 rnm_TexelSize = float4(width, height, 1.0/width, 1.0/height);
        
        float3 nL1x = SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(_RNM0, sampler_RNM0), lightmapUV, rnm_TexelSize);
        float3 nL1y = SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(_RNM1, sampler_RNM0), lightmapUV, rnm_TexelSize);
        float3 nL1z = SampleTexture2DBicubicFilter(TEXTURE2D_ARGS(_RNM2, sampler_RNM0), lightmapUV, rnm_TexelSize);
    #else
        float3 nL1x = UNITY_SAMPLE_TEX2D_SAMPLER(_RNM0, _RNM0, lightmapUV);
        float3 nL1y = UNITY_SAMPLE_TEX2D_SAMPLER(_RNM1, _RNM0, lightmapUV);
        float3 nL1z = UNITY_SAMPLE_TEX2D_SAMPLER(_RNM2, _RNM0, lightmapUV);
    #endif

    nL1x = nL1x * 2 - 1;
    nL1y = nL1y * 2 - 1;
    nL1z = nL1z * 2 - 1;
    float3 L1x = nL1x * L0 * 2;
    float3 L1y = nL1y * L0 * 2;
    float3 L1z = nL1z * L0 * 2;

    #ifdef BAKERY_SHNONLINEAR
        float lumaL0 = dot(L0, float(1));
        float lumaL1x = dot(L1x, float(1));
        float lumaL1y = dot(L1y, float(1));
        float lumaL1z = dot(L1z, float(1));
        float lumaSH = shEvaluateDiffuseL1Geomerics_local(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWorld);

        irradiance = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
        float regularLumaSH = dot(irradiance, 1);
        irradiance *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));
    #else
        irradiance = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
    #endif

    #if defined(_LIGHTMAPSPECULAR)
    float3 dominantDir = float3(luminance(nL1x), luminance(nL1y), luminance(nL1z));

    o_light.l = dominantDir;
    half directionality = max(FLT_EPS, length(o_light.l));
    o_light.l /= directionality;

    // Split light into the directional and ambient parts, according to the directionality factor.
    o_light.colorIntensity = float4(irradiance * directionality, 1.0);
    o_light.attenuation = directionality;
    o_light.NoL = saturate(dot(normalWorld, o_light.l));
    #endif

    return irradiance;
}
#endif

#if defined(_BAKERY_MONOSH)
float3 DecodeMonoSHLightmap(half3 L0, half2 lightmapUV, half3 normalWorld, out Light o_light)
{
    o_light = (Light)0;

    float3 dominantDir = SampleLightmapDirBicubic (lightmapUV);

    float3 nL1 = dominantDir * 2 - 1;
    float3 L1x = nL1.x * L0 * 2;
    float3 L1y = nL1.y * L0 * 2;
    float3 L1z = nL1.z * L0 * 2;

    float3 sh;

#if BAKERY_SHNONLINEAR
    float lumaL0 = dot(L0, 1);
    float lumaL1x = dot(L1x, 1);
    float lumaL1y = dot(L1y, 1);
    float lumaL1z = dot(L1z, 1);
    float lumaSH = shEvaluateDiffuseL1Geomerics_local(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWorld);

    sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
    float regularLumaSH = dot(sh, 1);

    sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));
#else
    sh = L0 + normalWorld.x * L1x + normalWorld.y * L1y + normalWorld.z * L1z;
#endif

    #if defined(_LIGHTMAPSPECULAR)
    dominantDir = nL1;

    o_light.l = dominantDir;
    half directionality = max(FLT_EPS, length(o_light.l));
    o_light.l /= directionality;

    // Split light into the directional and ambient parts, according to the directionality factor.
    o_light.colorIntensity = float4(L0 * directionality, 1.0);
    o_light.attenuation = directionality;
    o_light.NoL = saturate(dot(normalWorld, o_light.l));
    #endif

    return sh;
}
#endif

float IrradianceToExposureOcclusion(float3 irradiance, float occlusionPow)
{
    return saturate(length(irradiance + FLT_EPS) * occlusionPow);
}

// Workaround for Unity bug with lightmap sampler not being defined
// https://issuetracker.unity3d.com/issues/shader-cannot-find-frag-surf-and-vert-surf-and-throws-errors-in-the-console-and-inspector
fixed UnitySampleBakedOcclusion_local (float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
            fixed4 rawOcclusionMask = SampleShadowmaskBicubic(lightmapUV.xy);
        #else
            fixed4 rawOcclusionMask = fixed4(1.0, 1.0, 1.0, 1.0);
            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
                else
                    rawOcclusionMask = SampleShadowmaskBicubic(lightmapUV.xy);
            #else
                rawOcclusionMask = SampleShadowmaskBicubic(lightmapUV.xy);
            #endif
        #endif
        return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));

    #else

        //In forward dynamic objects can only get baked occlusion from LPPV, light probe occlusion is done on the CPU by attenuating the light color.
        fixed atten = 1.0f;
        #if defined(UNITY_INSTANCING_ENABLED) && defined(UNITY_USE_SHCOEFFS_ARRAYS)
            // ...unless we are doing instancing, and the attenuation is packed into SHC array's .w component.
            atten = unity_SHC.w;
        #endif

        #if UNITY_LIGHT_PROBE_PROXY_VOLUME && !defined(LIGHTMAP_ON) && !UNITY_STANDARD_SIMPLE
            fixed4 rawOcclusionMask = atten.xxxx;
            if (unity_ProbeVolumeParams.x == 1.0)
                rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
            return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));
        #endif

        return atten;
    #endif
}


// Occlusion is applied in the shader; output baked attenuation for SSS instead
inline UnityGI UnityGI_Base_local(UnityGIInput data, out half bakedAtten, inout half occlusion, 
	half3 normalWorld, float perceptualRoughness, half3 transmission, float exposureOcclusion, float maxSmoothness,
    float probeTransmission, SSSParams sssData)
{
    UnityGI o_gi;
    ResetUnityGI(o_gi);

    o_gi.indirect.diffuse = data.ambient;

    float3 irradianceForAO = 1.0;

    // Base pass with Lightmap support is responsible for handling ShadowMask / blending here for performance reason
    bakedAtten = 1.0;

    #if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
        bakedAtten = UnitySampleBakedOcclusion_local(data.lightmapUV.xy, data.worldPos);
        float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
        float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
        data.atten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
    #endif

    o_gi.light = data.light;
    o_gi.light.color *= data.atten;

	Light filamentLight = (Light)0;

    #if UNITY_SHOULD_SAMPLE_SH
        #if defined(_VRCLV)
            o_gi.indirect.diffuse = Irradiance_SampleVRCLightVolume(normalWorld, data.worldPos, probeTransmission, filamentLight);
            irradianceForAO = o_gi.indirect.diffuse;
        #else
    	o_gi.indirect.diffuse = Irradiance_SphericalHarmonicsUnity(normalWorld, data.worldPos, probeTransmission);
    #endif
    #endif

    #if defined(LIGHTMAP_ON)
        // Baked lightmaps
        half4 bakedColorTex = SampleLightmapBicubic (data.lightmapUV.xy);
        half3 bakedColor = DecodeLightmap(bakedColorTex);

        #ifdef DIRLIGHTMAP_COMBINED
            fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, data.lightmapUV.xy);

            // Bakery's MonoSH mode replaces the regular directional lightmap
            #if defined(_BAKERY_MONOSH)
                o_gi.indirect.diffuse += DecodeMonoSHLightmap (bakedColor, data.lightmapUV.xy, normalWorld, filamentLight);
                irradianceForAO = o_gi.indirect.diffuse;

                #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
                    o_gi.indirect.diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (o_gi.indirect.diffuse, bakedAtten, bakedColorTex, normalWorld);
                #endif
            #else

				o_gi.indirect.diffuse += DecodeDirectionalLightmapSpecular (bakedColor, bakedDirTex, normalWorld, false, 0, filamentLight);
				irradianceForAO = o_gi.indirect.diffuse;

				#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
					ResetUnityLight(o_gi.light);
					o_gi.indirect.diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (o_gi.indirect.diffuse, data.atten, bakedColorTex, normalWorld);
				#endif
            #endif

        #else // not directional lightmap

            #if defined(USING_BAKERY)
                #if defined(_BAKERY_RNM)
                // bakery rnm mode
                o_gi.indirect.diffuse = DecodeRNMLightmap(bakedColor, data.lightmapUV.xy, tangentNormal, tangentToWorld, filamentLight);
                #endif

                #if defined(_BAKERY_SH)
                // bakery sh mode
                o_gi.indirect.diffuse = DecodeSHLightmap(bakedColor, data.lightmapUV.xy, normalWorld, filamentLight);
                #endif

                irradianceForAO = o_gi.indirect.diffuse;

                #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
                    o_gi.indirect.diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap(o_gi.indirect.diffuse, bakedAtten, bakedColorTex, normalWorld);
                #endif

            #else
           		o_gi.indirect.diffuse += bakedColor;

                irradianceForAO = o_gi.indirect.diffuse;

				#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
					ResetUnityLight(o_gi.light);
					o_gi.indirect.diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap(o_gi.indirect.diffuse, data.atten, bakedColorTex, normalWorld);
				#endif
			#endif
        #endif
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        // Dynamic lightmaps
        fixed4 realtimeColorTex = SampleDynamicLightmapBicubic(data.lightmapUV.zw);
        half3 realtimeColor = DecodeRealtimeLightmap (realtimeColorTex);
		irradianceForAO += realtimeColor;

        #ifdef DIRLIGHTMAP_COMBINED
            half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, data.lightmapUV.zw);
            o_gi.indirect.diffuse += DecodeDirectionalLightmap (realtimeColor, realtimeDirTex, normalWorld);
        #else
            o_gi.indirect.diffuse += realtimeColor;
        #endif
    #endif

    #if defined(_LIGHTMAPSPECULAR)
        // o_gi.light stores the data for the main light, so a seperate light must be output for specular
        // or else it must be calculated here.
        half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
        
        // remap smoothness to clamp at max smoothness without a hard clamp
        roughness = remap_almostIdentity(roughness, 1.0 - maxSmoothness, 1.0 - maxSmoothness - MIN_PERCEPTUAL_ROUGHNESS);

        float focus = saturate(length(filamentLight.l));
        half3 halfDir = Unity_SafeNormalize(normalize(filamentLight.l) + data.worldViewDir);
        half nh = saturate(dot(normalWorld, halfDir));
        half spec = GGXTerm(nh, roughness);
        float3 sh = filamentLight.NoL * filamentLight.colorIntensity;

        o_gi.indirect.specular += max(spec * sh, 0.0);
    #endif

	// Apply subsurface and add to indirect.diffuse
	float3 sssLighting = getSubsurfaceScatteringLight(sssData, filamentLight.l, normalWorld, 
	data.worldViewDir, transmission) * filamentLight.colorIntensity;
	o_gi.indirect.diffuse += sssLighting;
    
    // VRC Light Volumes also have an additive component which can be added over lightmapping.
    #if defined(_VRCLV) && !UNITY_SHOULD_SAMPLE_SH
        Light volumeLight = (Light)0;
        float3 lvColor = Irradiance_SampleVRCLightVolumeAdditive(normalWorld, data.worldPos, volumeLight);
        o_gi.indirect.diffuse += lvColor;
        irradianceForAO += lvColor; // Messy workaround to avoid lightmap affecting LV - fix later.

        sssLighting = getSubsurfaceScatteringLight(sssData, volumeLight.l, normalWorld,
            data.worldViewDir, transmission) * volumeLight.colorIntensity;
        o_gi.indirect.diffuse += sssLighting;

        #if defined(_LIGHTMAPSPECULAR)
            focus = volumeLight.attenuation;
            halfDir = Unity_SafeNormalize(normalize(volumeLight.l) + data.worldViewDir);
            nh = saturate(dot(normalWorld, halfDir));
            spec = GGXTerm(nh, roughness);
            sh = volumeLight.NoL * volumeLight.colorIntensity;

            o_gi.indirect.specular += max(spec * sh, 0.0);
        #endif
    #endif
    
	occlusion *= IrradianceToExposureOcclusion(irradianceForAO, 1.0/exposureOcclusion);

    return o_gi;
}

// Reflection Probes

UnityGIInput InitialiseUnityGIInput(float3 worldPos, float3 worldViewDir)
{
    UnityGIInput d;
    d.worldPos = worldPos;
    d.worldViewDir = -worldViewDir;
    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
      d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
      d.boxMax[0] = unity_SpecCube0_BoxMax;
      d.probePosition[0] = unity_SpecCube0_ProbePosition;
      d.boxMax[1] = unity_SpecCube1_BoxMax;
      d.boxMin[1] = unity_SpecCube1_BoxMin;
      d.probePosition[1] = unity_SpecCube1_ProbePosition;
    #endif
    return d;
}

half3 Unity_GlossyEnvironment_local (UNITY_ARGS_TEXCUBE(tex), half4 hdr, Unity_GlossyEnvironmentData glossIn)
{
    half perceptualRoughness = glossIn.roughness /* perceptualRoughness */ ;

    // Unity derivation
    perceptualRoughness = perceptualRoughness*(1.7 - 0.7 * perceptualRoughness);
    
    half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
    half3 R = glossIn.reflUVW;
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);

    return DecodeHDR(rgbm, hdr);
}

inline half3 UnityGI_prefilteredRadiance(const UnityGIInput data, const float perceptualRoughness, const float3 r)
{
    half3 specular;

    Unity_GlossyEnvironmentData glossIn = (Unity_GlossyEnvironmentData)0;
    glossIn.roughness = perceptualRoughness;
    glossIn.reflUVW = r;

    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
        // we will tweak reflUVW in glossIn directly (as we pass it to Unity_GlossyEnvironment twice for probe0 and probe1), so keep original to pass into BoxProjectedCubemapDirection
        half3 originalReflUVW = glossIn.reflUVW;
        glossIn.reflUVW = BoxProjectedCubemapDirection (originalReflUVW, data.worldPos, data.probePosition[0], data.boxMin[0], data.boxMax[0]);
    #endif

    #ifdef _GLOSSYREFLECTIONS_OFF
        specular = unity_IndirectSpecColor.rgb;
    #else
        half3 env0 = Unity_GlossyEnvironment_local (UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn);
        #ifdef UNITY_SPECCUBE_BLENDING
            const float kBlendFactor = 0.99999;
            float blendLerp = data.boxMin[0].w;
            UNITY_BRANCH
            if (blendLerp < kBlendFactor)
            {
                #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                    glossIn.reflUVW = BoxProjectedCubemapDirection (originalReflUVW, data.worldPos, data.probePosition[1], data.boxMin[1], data.boxMax[1]);
                #endif

                half3 env1 = Unity_GlossyEnvironment_local (UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1,unity_SpecCube0), data.probeHDR[1], glossIn);
                specular = lerp(env1, env0, blendLerp);
            }
            else
            {
                specular = env0;
            }
        #else
            specular = env0;
        #endif
    #endif

    return specular;
}

#endif // WATER_INDIRECT