Shader "AntonShaderz/TriPlanar"
{
    Properties
    {
        [NoScaleOffset] _Y_Tex ("Y texture", 2D) = "white" {}
        [NoScaleOffset] _Y_Bump ("Y Bump map", 2D) = "bump" {}
        [NoScaleOffset] _X_Tex ("X texture", 2D) = "white" {}
        [NoScaleOffset] _X_Bump ("X Bump map", 2D) = "bump" {}
        [NoScaleOffset] _Z_Tex ("Z texture", 2D) = "white" {}
        [NoScaleOffset] _Z_Bump ("Z Bump map", 2D) = "bump" {}
        _TriplanarBlendSharpness ("Blend Sharpness", range(0.1,150)) = 1
        _TextureScale ("Texture Scale", float) = 1        
        _BumpIntensity ("Normal Intensity", range(0,1)) = 1    
        _Smoothness ("Smoothness", range(0,1)) = 0
        [MaterialToggle] _Metallic("Metallic", Float) = 0 
        _Tint ("Tint", Color) = (1,1,1,1)
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
            
            
            #include "LightPassTriplanar.cginc"          
            
            
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

            #include "LightPassTriplanar.cginc"
            
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
    
    fallback "Diffuse"
}
