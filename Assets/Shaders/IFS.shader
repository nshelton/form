// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SHELTRON/FractalShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorMap ("ColorMap (RGB)", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,10)) = 5.0
        _MarchIter ("MarchIter", Range(0,50)) = 50.0
        _FoldScale ("FoldScale", Range(-5,5)) = 3.5
        _InnerRad ("InnerRad", Range(0,1)) = 3.5
        _IterCount ("IterCount", Range(0,8)) = 3.5
        _Rotation ("Rotation", vector) = (1,1,1,1)
        _ModelToTrackball ("Origin", vector) = (1,1,1,1)
			_boxDim("BoxDim", vector) = (0.1,0.01,1,1)
        _ColorMap ("ColorMap", Range(0,1)) = 0.1
        fs ("fs", Range(-3,4)) = 3.5
        fu ("fu", Range(0,10)) = 3.5
        fftScale ("fftScale", Range(0,1)) = 0.5
        _StepRatio ("stepRatio", Range(0,2)) = 0.5
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
                o.uv = o.vertex.xy/ o.vertex.w;
                o.n = mul(UNITY_MATRIX_M, v.vertex);
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
float4 _boxDim;
float fu;
float fs;
float _palette;
float _StepRatio;
float _scale;

#include "commonDE.cginc"

float4x4 _IterationTransform;


sampler2D _ColorMap;
//#define _FRACTAL tgl;

float4x4 _WorldToModelTransform;
float4x4 _WorldToModelTransform_inv;

float3 _ModelToTrackball;
float _InteractiveTransform;

#define BIGFLOAT 10000.000000

float3x3 getRotation(float4x4 m) { return float3x3( m[0].xyz,  m[1].xyz, m[2].xyz); }

float axes(float3 p) {
    if ( _InteractiveTransform < 0.5)
        return BIGFLOAT;

    float r1 = 0.1;
    float r2 = 0.001;

  return min(min(   length(float2(length(p.xz)-r1, p.y)) - r2,
                    length(float2(length(p.yx)-r1, p.z)) - r2),
                    length(float2(length(p.zy)-r1, p.x)) - r2);

}



float udBox( float3 p, float3 b )
{
  return length(max(abs(p)-b,0.0));
}


float3x3 rotationMatrix(float3 axis, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;

    return float3x3(oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.z * axis.x + axis.y * s,
                oc * axis.x * axis.y + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s,
                oc * axis.z * axis.x - axis.y * s,  oc * axis.y * axis.z + axis.x * s,  oc * axis.z * axis.z + c);
}


float DE(float3 p){
    float3 dim = _boxDim.xyz;

    float3 t = float3(_IterationTransform[0].a, _IterationTransform[1].a, _IterationTransform[2].a);
 

    float d = 1e10;

    float param = _Time.x / 2.;
	float3x3 modelView_s = getRotation(_IterationTransform);
		rotationMatrix(normalize(_Rotation.xyz + float3(cos(param), sin(param), 0.0)), param/2.0);
    
    float scale = fs;

    float s = 1.0;


    for (int i = 0; i < 10; i++){
        if( float(i) > _IterCount)
            break;

        // p = mul(modelView_s,(p-t/s));
        p = mul(modelView_s,p-t/s);

        d = min (d, udBox(p.xyz/s, dim)*s ) ;
        // d = min (d, udBox(p.yxz, dim));
        // d = min (d, udBox(p, _IFSShift));
        p = abs(p);


        // freq *= scale;
        float circle =  fu/10.0 + 0.1 * sin(_Time.x + p.xyz);
        d = min(d, length(p - t) -circle);

        // d = max(d, udBox(p.xyz*scale  , offs)/scale ) ;

        s *= scale;
        // offs *= scale;
        // modelView_s*=scale;
    }


    return d;
}




float query(float3 pos)
{
    float d = BIGFLOAT;
    float actualScale =  length(_WorldToModelTransform[0].xyz);

    pos -= _ModelToTrackball;
    d = min(d, axes(pos));
    pos += _ModelToTrackball;

    d = min(d, DE(pos));
    // d = length(pos) - 0.1;
    // d = min(d, MBOX(pos));
    // d = min(d, cubeThing(pos - float3(0,0,-0.5)));

    return d / actualScale;
}


float3 getNormal(float3 p)
{
    const float d = 0.001;
    return normalize( float3(
        query(p+float3(  d,0.0,0.0))-query(p+float3( -d,0.0,0.0)),
        query(p+float3(0.0,  d,0.0))-query(p+float3(0.0, -d,0.0)),
        query(p+float3(0.0,0.0,  d))-query(p+float3(0.0,0.0, -d)) ));
}


void frag (v2f i, out fixed4 col : SV_Target, out float outDepth : SV_Depth) 

{
    float4 cam_pos = float4(_WorldSpaceCameraPos, 1.0);
    cam_pos =  mul(_WorldToModelTransform, cam_pos);

    float4 dir = float4(normalize(i.n - _WorldSpaceCameraPos), 0.0);
    dir = mul(_WorldToModelTransform, dir);

    float actualScale =  length(_WorldToModelTransform[0].xyz);

    float depth = 0.0;
    float iter = 0;
    float3 position;
    bool hit = false;

    const int ITERS = 50;

    col = float4 ( 0.0, 0.0, 0.0, 1.0);
    for ( int i = 0; i < ITERS; i ++)
    {
        if(i > _MarchIter)
            break;

        position = cam_pos + dir * depth;
        float d = query(position);


        // if ( d <= (  exp(0.6 * depth+1.0) * exp(-_Threshold)) / actualScale  ){  
        float thr = exp(-_Threshold*2.0) * exp(depth *_StepRatio *10.);

        if ( d <=  thr ){  
            hit = true;
            break;
        }

        depth += d ;
        iter++;
    }


    if (hit)
    {
        float3 normal = getNormal(position);
        float ao = 1.0 - (iter / float(_MarchIter));

        // col = float4(fftVal , sin(length(position.xz)/5.), 1.0, 1.0);
        //col = float4(sin(position.xz), 1.0, 1.0);
        col = tex2D(_ColorMap, float2(_palette, 1.0 -ao));
        col.xyz  = ao ;
       // if ( hit)
         // col.xyz  *= abs(dot(dir, normal)) ;

    }
	col.xy = i.uv;
	col.b = 1.0 - col.b;
    // Depth
    float4 pos4 = float4(position, 1.0);
    // pos4 = mul(xf_CS, pos4);
    float4 clip = mul(UNITY_MATRIX_VP, pos4);
    outDepth = 0.1; // clip.z / clip.w;

}

            ENDCG
        }
    }
    FallBack "Diffuse"
}
