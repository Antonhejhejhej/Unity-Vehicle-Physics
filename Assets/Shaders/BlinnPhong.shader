Shader "AntonShaderz/BlinnPhong"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _BumpMap ("Bump map", 2D) = "bump" {}
        _BumpIntensity ("Normal Intensity", range(0,1)) = 1        
        _Smoothness ("Smoothness", range(0,1)) = 0
        [MaterialToggle] _Metallic("Metallic", Float) = 0 
        _Ambient ("Ambient Light Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}

        //Base
        Pass
        {
            Tags { "LightMode"="ForwardBase"}
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "LightPassBump.cginc"
            
            ENDCG
        }
        
        //Additive
        Pass
        {
            Tags { "LightMode"="ForwardAdd"}
            Blend One One //source*1 + dest*1
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd

            #include "LightPassBump.cginc"
            
            ENDCG
        }
        
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        } 
        
        
    }
}
