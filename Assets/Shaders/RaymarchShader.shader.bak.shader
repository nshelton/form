// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SHELTRON/FractalShaderBackup" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorMap ("ColorMap (RGB)", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,15)) = 5.0
        _MarchIter ("MarchIter", Range(0,50)) = 50.0
        _FoldScale ("FoldScale", Range(-5,5)) = 3.5
        _InnerRad ("InnerRad", Range(0,1)) = 3.5
        _IterCount ("IterCount", Range(0,8)) = 3.5
        _Rotation ("Rotation",Vector) = (1,1,1,1)
        _ModelToTrackball ("Origin",Vector) = (1,1,1,1)
        _IFSShift ("IFS Shift",Vector) = (1,1,1,1)
        _ColorMap ("ColorMap", Range(0,1)) = 0.1
        fs ("fs", Range(-3,4)) = 3.5
        fu ("fu", Range(0,10)) = 3.5
        fftScale ("fftScale", Range(0,1)) = 0.5
        _StepRatio ("stepRatio", Range(0,1)) = 0.5
        _palette ("palette", Range(0,1)) = 0.2
        _scale ("scale", Range(0,1)) = 1.0

    }
    SubShader {
        Cull Off
        Pass
        {
            CGPROGRAM
            // use "vert" function as the vertex shader
            #pragma vertex vert
            #pragma target 4.0
            #include "commonDE.cginc"

            // use "frag" function as the pixel (fragment) shader
            #pragma fragment frag
            struct appdata
            {
                float4 vertex : POSITION; // vertex position
                float2 uv : TEXCOORD0; // texture coordinate
            };

            // vertex shader outputs ("vertex to fragment")
            struct v2f
            {
                float2 uv : TEXCOORD0; // texture coordinate
                float3 n : NORMAL; 
                float4 vertex : SV_POSITION; // clip space position
            };

            // vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.n = mul(UNITY_MATRIX_M, v.vertex)  ;
                return o;
            }



// -------------DO IT LIVE--------------------------------------- 
float _FoldScale;
float _Threshold;
float _InnerRad;
float _IterCount;
float _someParameter;
float _MarchIter;
float4 _Rotation;
float4 _IFSShift;
float fu;
float fs;
float _palette;
float _StepRatio;
float _scale;


sampler2D _ColorMap;
//#define _FRACTAL tgl;

float4x4 _FractalModelView;
float3 _ModelToTrackball;
float3 _WorldToModel;
float _InteractiveTransform;

#define BIGFLOAT 1.000000

float axes(float3 p) {
    if ( _InteractiveTransform < 0.5)
        return BIGFLOAT;

    float r1 = 0.005;
    float r2 = 0.001;

  return min(min(   length(float2(length(p.xz)-r1, p.y)) - r2,
                    length(float2(length(p.yx)-r1, p.z)) - r2),
                    length(float2(length(p.zy)-r1, p.x)) - r2);

}

float query(float3 pos)
{
    float d = BIGFLOAT;
    // scale about roataion point
    pos -= _ModelToTrackball;

    pos /= _scale;
    d = min(d, axes(pos));

    pos += _ModelToTrackball;


    // pos += ;

    d = min(d, cubeThing(pos - float3(0,0,-0.5)));


    return d * _scale ;
}

    float3 getNormal(float3 p)
    {
        const float d = 0.001;
        return normalize( float3(
            query(p+float3(  d,0.0,0.0))-query(p+float3( -d,0.0,0.0)),
            query(p+float3(0.0,  d,0.0))-query(p+float3(0.0, -d,0.0)),
            query(p+float3(0.0,0.0,  d))-query(p+float3(0.0,0.0, -d)) ));
    }


float3x3 getRotation(float4x4 m) { return float3x3( m[0].xyz,  m[1].xyz, m[2].xyz); }


void frag (v2f i, out fixed4 col : SV_Target, out float outDepth : SV_Depth) 

{
    // this rotates from world to trackball frame, about the trackball center
    float3x3 trackballRotation = getRotation(_FractalModelView);
    float3 cam_pos = _WorldSpaceCameraPos;
    // cam_pos += _WorldToModel;

    // rotate around trackballRotation center
    cam_pos = mul( cam_pos - _ModelToTrackball, trackballRotation) + _ModelToTrackball;

    // invert worldToModel ( it is actually model to)
    cam_pos += mul(-_WorldToModel, trackballRotation);
    
    // cam_pos += _WorldToModel;


    float3 dir = normalize(i.n - _WorldSpaceCameraPos);
    dir = mul(dir, trackballRotation);

    float depth = 0.01;
    float iter = 0;
    float3 position;
    bool hit = false;

    const int ITERS = 50;

    col = float4 ( 0.0, 0.0, 0.0, 1.0);
    for ( int i = 0; i < ITERS; i ++)
    {
        position = cam_pos + dir * depth;
        float d = query(position);

        if ( d <= exp(-_Threshold) ){  
            hit = true;
            break;
        }

        depth += d * _StepRatio ;
        iter++;
    }


    if (hit)
    {
        float3 normal = getNormal(position);
        float ao = 1.0 - (iter / float(ITERS));

        // col = float4(fftVal , sin(length(position.xz)/5.), 1.0, 1.0);
        //col = float4(sin(position.xz), 1.0, 1.0);
        col = tex2D(_ColorMap, float2(_palette, 1.0 -ao));
        col.xyz  *= ao ;
       // if ( hit)
         // col.xyz  *= abs(dot(dir, normal)) ;

    }

    // Depth
    float4 pos4 = float4(position, 1.0);
    // pos4 = mul(xf_CS, pos4);
    float4 clip = mul(UNITY_MATRIX_VP, pos4);
    outDepth = 0.9; // clip.z / clip.w;

}

            ENDCG
        }
    }
    FallBack "Diffuse"
}
