// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "SHELTRON/Totem" {
    Properties {
		_ColorMap("ColorMap",  2D) = "white" {}
		_NoiseTex("NoiseTex",  2D) = "white" {}
		_Threshold("Threshold", Range(0,10)) = 5.0
		_MarchIter("MarchIter", Range(0,50)) = 50.0
		_IterCount("Fractal Level", Range(0,8)) = 3.5
			_falloff("threhsold_falloff", Range(0,10)) = 5.0
			_fovea("fovea", Range(0,3)) = 1
			_PrimitiveDimension("Box Size", vector) = (0.01,0.01, 1, 0)
	}
    SubShader {
        Cull Off
        Pass
        {
            CGPROGRAM
			#pragma target 4.0
			#include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

			struct VertexIn{
	        	float4 position  : POSITION; 
				float3 normal    : NORMAL; 
				float4 texcoord  : TEXCOORD0; 
				float4 tangent   : TANGENT;
			};


			struct VertexOut {
				float4 pos    	: POSITION; 
				float3 normal 	: NORMAL; 
				float4 uv     	: TEXCOORD0; 
				float3 ro     	: TEXCOORD1;
				float3 rd     	: TEXCOORD2;
			};

			float _boundingSphereRadius;

            // vertex shader
            VertexOut vert (VertexIn v)
            {
                VertexOut o;
				o.normal = v.normal;
 		        // scale the actual coords
				float4 pos = v.position;
				//pos.xyz *= _boundingSphereRadius;
				// for vertex on screen draw position (MVP)
		        o.pos = UnityObjectToClipPos(  pos);

				// we're gonna raymarch in object coordinates
		        o.ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
		        o.rd = normalize(pos - o.ro);
				o.uv = o.pos ;

		        return o;


            }



// Meta Parameters
float _Threshold;
float _IterCount;
float _MarchIter;
float _falloff;
float _fovea;
sampler2D _ColorMap;
sampler2D _NoiseTex;

//#include "commonDE.cginc"

float4x4 _IterationTransform;


float3 _PrimitiveDimension;
float _InteractiveTransform;

#define BIGFLOAT 1.000000

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

	//float3 dim = pow(_PrimitiveDimension, float3(2.0, 2.0, 2.0));
	float3 dim = float3(0.01, 0.01, 1.0);

	float3 t = float3(_IterationTransform[0].a, _IterationTransform[1].a, _IterationTransform[2].a);

	float scale = 1.1;
    float d = 1e10;


    for (int i = 0; i < 10; i++){
        if( float(i) > _IterCount)
            break;

        // p = mul(modelView_s,(p-t/s));
        p = mul(_IterationTransform, float4(p - t, 0.0));

        //d = min (d, udBox(p.xyz/s, dim)*s ) ;
        d = min (d, udBox(p.yxz, dim));
        // d = min (d, udBox(p, _IFSShift));
        p = abs(p);

        // freq *= scale;
		float circle = 0.05  * (0.5 + 0.5 * sin(_Time.g + p.xyz));
        d = min(d, length(p - t/6.0) -circle);

        // d = max(d, udBox(p.xyz*scale  , offs)/scale ) ;

		t *= scale;
		dim *= scale;
        //_IterationTransform*=scale;
    }


    return d;
}
 

float3 getNormal(float3 p, float thr)
{
    const float d = thr;
    return normalize( float3(
        DE(p+float3(  d,0.0,0.0))- DE(p+float3( -d,0.0,0.0)),
		DE(p+float3(0.0,  d,0.0))- DE(p+float3(0.0, -d,0.0)),
		DE(p+float3(0.0,0.0,  d))- DE(p+float3(0.0,0.0, -d)) ));
}



float compute_depth(float3 modelSpace)
{
	// position in clipspace
	float4 clipSpace = UnityObjectToClipPos(float4(modelSpace, 1.0));

#if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
	return ((clipSpace.z / clipSpace.w) + 1.0) * 0.5;
#else
	return clipSpace.z / clipSpace.w;
#endif
}

void frag (VertexOut i, out fixed4 col : SV_Target, out float outDepth : SV_Depth) 

{
	float3 cam_pos = i.ro;
	float3 dir = i.rd;

	float4 noise = tex2D(_NoiseTex, dir.xy + _Time.a );
	//dir = normalize(i.rd + float3(noise.xy/100., 0.0));
  
	float4 clip = UnityObjectToClipPos(float4(cam_pos + dir, 1.0));
	float fovea = length(clip.xy / clip.w);
	fovea = exp(-fovea*fovea / _fovea) ;

	float depth = 0.0;
    float iter = 0;
    float3 position;
    bool hit = false;
	const float maxDepth = 10.0;
	float thr;

    const int MAX_ITERS = 50;
    col = float4 ( 0.0, 0.0, 0.0, 0.0);

    for ( int i = 0; i < MAX_ITERS; i ++)
    {
        if(i > _MarchIter * sqrt(fovea))
            break;

        position = cam_pos + dir * depth;
        float d = DE(position);

		thr = exp(-_Threshold*2.0) * (1.0 - fovea) * depth*_falloff;

        if ( d <=  thr ){  
            hit = true;
            break;
        }

		if (depth > maxDepth)
			break;

        depth += d ;
        iter++;
    }


    if (hit)
    {
        float3 normal = getNormal(position, thr / 2.0);
        float ao = 1.0 - (iter / float(_MarchIter));

        //col = float4(fftVal , sin(length(position.xz)/5.), 1.0, 1.0);
        //col = float4(sin(position.xz), 1.0, 1.0);
         col = tex2D(_ColorMap, float2(0.5,  ao));
		//col = float4(ao, ao, ao, ao);
        //col.xyz *= dot(dir, -normal);
		 col *= ao;
    }

	//col.r = fovea;
	 

	// outDepth = compute_depth(position);
	//
}

            ENDCG
        }
    }
    FallBack "Diffuse"
}
