#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "UnityPBSLighting.cginc"
#pragma multi_compile_fwdbase;
#pragma multi_compile_fwdadd;

struct mesh_data
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
};

struct interpolators
{
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 tangent : TEXCOORD2;
    float3 bitangent : TEXCOORD3;
    float3 worldPos : TEXCOORD4;
    float4 pos : SV_POSITION;
    LIGHTING_COORDS(5, 6)
};

sampler2D _MainTex;
float3 _Tint;
float3 _Ambient;
float4 _MainTex_ST;
sampler2D _BumpMap;
float _BumpIntensity;
float _Smoothness;
float _Metallic;

interpolators vert(mesh_data v)
{
    interpolators o;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.bitangent = cross(o.normal, o.tangent) * (v.tangent.w * unity_WorldTransformParams.w);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    TRANSFER_VERTEX_TO_FRAGMENT(o) //lighting
    return o;
}

fixed4 frag(interpolators i) : SV_Target
{
    float3 tangentSpaceNormal = UnpackNormal(tex2D(_BumpMap, i.uv));

    tangentSpaceNormal = normalize(lerp(float3(0, 0, 1), tangentSpaceNormal, _BumpIntensity));

    float3x3 matrixTangentToWorld = {
        i.tangent.x, i.bitangent.x, i.normal.x,
        i.tangent.y, i.bitangent.y, i.normal.y,
        i.tangent.z, i.bitangent.z, i.normal.z
    };

    float attenuation = LIGHT_ATTENUATION(i);

    //Lembertian lighting model

    float3 N = normalize(mul(matrixTangentToWorld, tangentSpaceNormal)); //Normal mapped/Bump mapped
    //float3 N = normalize(i.normal);
    float3 L = normalize(UnityWorldSpaceLightDir(i.worldPos)); //Direction from surface to light

    float3 lambert = saturate(dot(N, L));

    float3 diffuse = (lambert * attenuation) * _LightColor0.xyz;

    #ifdef UNITY_PASS_FORWARDADD

    #else
    diffuse += _Ambient;
    #endif


    //Blinn-Phong reflection model

    float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
    //float3 R = reflect(-L, N); //Replace H with R for simple Phong reflection, and "specular = saturate(dot(H,N));"
    float3 H = normalize(L + V);
    float3 specular = saturate(dot(H, N)) * (lambert > 0);
    float specularExponent = exp2(_Smoothness * 12) + 2;
    specular = pow(specular, specularExponent) * _Smoothness * 2 * attenuation; // specular exponent
    


    // sample the texture
    fixed4 col = tex2D(_MainTex, i.uv);

    if(_Metallic < 1)
    {
        specular *= _LightColor0.xyz;
    }else
    {
        specular *= col * 3;
    }

    float3 diffuseCol = col * diffuse * _Tint;


    return float4(diffuseCol + specular, 1);
}
