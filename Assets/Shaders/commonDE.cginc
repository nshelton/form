
#define PI      3.1415926535897932384626433832795
float  modc(float  a, float  b) { return a - b * floor(a/b); }
float2 modc(float2 a, float2 b) { return a - b * floor(a/b); }
float3 modc(float3 a, float3 b) { return a - b * floor(a/b); }
float4 modc(float4 a, float4 b) { return a - b * floor(a/b); }


float2 rot2D (float2 q, float a)
{
  return q * cos (a) + q.yx * sin (a) * float2 (-1., 1.);
}



// distance function from Hartverdrahtet
// ( http://www.pouet.net/prod.php?which=59086 )
float hartverdrahtet(float3 f)
{
	float3 cs = 0.05 * float3(_IterationTransform[0].a, _IterationTransform[1].a, _IterationTransform[2].a);
   
    //cs += sin(time)*0.2;

    float v=1.;
    for(int i=0; i<12; i++){
        f=2.*clamp(f,-cs,cs)-f;
        float c=max(fs/dot(f,f),1.);
        f*=c;
        v*=c;
        f+=fc;
    }
    float z=length(f.xy)-fu;
    return fd*max(z,abs(length(f.xy)*f.z)/sqrt(dot(f,f)))/abs(v);
}


void sphereFold(inout float3 z, inout float dz) {

	float fixedRadius2 =  fu;
	float minRadius = _InnerRad;

	float Scale = fs;
	float offset = _boxDim;


	float r2 = dot(z,z);
	if (r2 < minRadius) { 
		// linear inner scaling
		float temp = (fixedRadius2/minRadius);
		z *= temp;
		dz*= temp;
	} else if (r2 < fixedRadius2) { 
		// this is the actual sphere inversion
		float temp =(fixedRadius2/r2);
		z *= temp;
		dz*= temp;
	}
}
 
void boxFold(inout float3 z, inout float dz) {
	float foldingLimit = _FoldScale;

	z = clamp(z, -foldingLimit, foldingLimit) * 2.0 - z;
}


float MBOX(float3 z)
{
	float Scale = fs - 5.;
	float3 offset = z;
	float dr = 1.0;
	for (int n = 0; n < 10; n++) {
		if(n > _IterCount)
			break;
		boxFold(z,dr);       // Reflect
		sphereFold(z,dr);    // Sphere Inversion
 		
                z=Scale*z + offset;  // Scale & Translate
                dr = dr*abs(Scale)+1.0;
	}
	float r = length(z);
	return r/abs(dr);
}

float fftField(float3 pos) {
	float3 c = 0.1;
	float3 q = pos;

	float dist = length(pos);

	q = modc(pos, c) - 0.5*c;   
	// float fftVal = tex2D(_FFTTex, pos2Tex(pos.xz) ).x;
	// fftVal = logfft(fftVal);
	// fftVal = pow(fftVal, 0.3) / 3.0;

	return length(q) - 0.02 *  ( 0.5 + 0.5 * dot(float3(1.0, 1.0, 1.0), sin( 40.0 *  pos)));
}


float cubeThing(float3 p) {
	p.xyz += 0.5* p * sin(_Time.z + 50.0*p.x) * sin(_Time.z + 57.0*p.y) * sin(_Time.z + 38.0*p.z);

	float3 b = float3(0.05, 0.06, 0.05 );

	float3 d = abs(p) - b;
	return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0)) *0.4 ;
}

