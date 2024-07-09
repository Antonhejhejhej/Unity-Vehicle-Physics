#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "UnityPBSLighting.cginc"
#pragma multi_compile_fwdbase;
#pragma multi_compile_fwdadd_fullshadows



struct mesh_data
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct interpolators
{
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float4 pos : SV_POSITION;
    LIGHTING_COORDS(3, 4)
};

sampler2D _Y_Tex;
sampler2D _Y_Bump;
sampler2D _X_Tex;
sampler2D _X_Bump;
sampler2D _Z_Tex;
sampler2D _Z_Bump;
float _TextureScale;
float _TriplanarBlendSharpness;
float _BumpIntensity;
float3 _Tint;
float3 _Ambient;
float4 _MainTex_ST;
float _Smoothness;
float _Metallic;


float3 rnmBlendUnpacked(float3 n1, float3 n2)
{
    n1 += float3(0, 0, 1);
    n2 *= float3(-1, -1, 1);
    return n1 * dot(n1, n2) / n1.z - n2;
}

interpolators vert(mesh_data v)
{
    interpolators o;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    TRANSFER_VERTEX_TO_FRAGMENT(o); //lighting
    return o;
}

fixed4 frag(interpolators i) : SV_Target
{
    float attenuation = LIGHT_ATTENUATION(i);
    float3 N = normalize(i.normal); //World space normal
    float3 L = normalize(UnityWorldSpaceLightDir(i.worldPos)); //Direction from surface to light


    //TRIPLANAR

    // Determine UV per axis
    half2 yUV = i.worldPos.xz / _TextureScale;
    half2 xUV = i.worldPos.zy / _TextureScale;
    half2 zUV = i.worldPos.xy / _TextureScale;
    // Sample textures
    half3 yDiff = tex2D(_Y_Tex, yUV);
    half3 xDiff = tex2D(_X_Tex, xUV);
    half3 zDiff = tex2D(_Z_Tex, zUV);
    
    half3 blendWeights = pow(abs(N), _TriplanarBlendSharpness);
    
    blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);
    
    fixed3 color = xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;    


    // Tangent space normal maps
    half3 tangentNormalX = UnpackNormal(tex2D(_X_Bump, xUV));
    half3 tangentNormalY = UnpackNormal(tex2D(_Y_Bump, yUV));
    half3 tangentNormalZ = UnpackNormal(tex2D(_Z_Bump, zUV));
    
    half3 absVertNormal = abs(N);
    tangentNormalX = normalize(lerp(float3(0,0,1), rnmBlendUnpacked(half3(N.zy, absVertNormal.x), tangentNormalX), _BumpIntensity));
    tangentNormalY = normalize(lerp(float3(0,0,1), rnmBlendUnpacked(half3(N.xz, absVertNormal.y), tangentNormalY), _BumpIntensity));
    tangentNormalZ = normalize(lerp(float3(0,0,1), rnmBlendUnpacked(half3(N.xy, absVertNormal.z), tangentNormalZ), _BumpIntensity));
    
    half3 axisSign = sign(N);
    tangentNormalX.z *= axisSign.x;
    tangentNormalY.z *= axisSign.y;
    tangentNormalZ.z *= axisSign.z;
    
    half3 normalX = half3(0.0, tangentNormalX.yx);
    half3 normalY = half3(tangentNormalY.x, 0.0, tangentNormalY.y);
    half3 normalZ = half3(tangentNormalZ.xy, 0.0);
    
    half3 worldNormal = normalize(
        normalX.xyz * blendWeights.x +
        normalY.xyz * blendWeights.y +
        normalZ.xyz * blendWeights.z +
        N
    );


    //Lambertian lighting model            

    const float3 lambert = saturate(dot(worldNormal, L));

    float3 diffuse = (lambert * attenuation) * _LightColor0.xyz;

    #ifdef UNITY_PASS_FORWARDADD

    #else
    diffuse += _Ambient;
    #endif


    //Blinn-Phong reflection model

    const float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
    //float3 R = reflect(-L, N); //Replace H with R for simple Phong reflection, and "specular = saturate(dot(H,N));"
    const float3 H = normalize(L + V);
    float3 specular = saturate(dot(H, worldNormal)) * (lambert > 0);
    const float specularExponent = exp2(_Smoothness * 12) + 2;
    specular = pow(specular, specularExponent) * _Smoothness * 2 * attenuation; // specular exponent
    if(_Metallic < 1)
    {
        specular *= _LightColor0.xyz;
    }else
    {
        specular *= color * 3;
    }
    

    

    float3 diffuseCol = color * diffuse * _Tint;


    return float4(diffuseCol + specular, 1);
}
