

void sphereFold(inout vec3 z, inout float dz) {

	float fixedRadius2 = fixedRad2;
	float minRadius2  = minRad2;

	float r2 = dot(z,z);
	if (r2 < minRadius2) { 
		// linear inner scaling
		float temp = (fixedRadius2/minRadius2);
		z *= temp;
		dz*= temp;
	} else if (r2 < fixedRadius2) { 
		// this is the actual sphere inversion
		float temp =(fixedRadius2/r2);
		z *= temp;
		dz*= temp;
	}
}
 
void boxFold(inout vec3 z, inout float dz) {
	float foldingLimit = foldingLimit;
	z = clamp(z, -foldingLimit, foldingLimit) * 2.0 - z;
}

vec2 DE(vec3 z)
{
	vec3 offset = z;
	float dr = 1.0;

	float Scale = SCALE;
	float iter = 0.0;

	for (int n = 0; n < MAX_ORBIT; n++) {
		boxFold(z,dr);       // Reflect
		sphereFold(z,dr);    // Sphere Inversion
 		
        z=Scale*z + offset;  // Scale & Translate
        dr = dr*abs(Scale)+1.0;
        iter++;
        if (abs(dr) > 1000000.)
        	break;
	}
	float r = length(z);

	return vec2(iter, r/abs(dr));
}


//----------------------------------------------------------------------------------------
float Map(vec3 pos) 
{

	if ( type == 0.0 )
		return DE(pos).y;

	vec4 p = vec4(pos,1);
	vec4 p0 = p;  // p.w is the distance estimate

	for (int i = 0; i < 11; i++)
	{
		p.xyz = clamp(p.xyz, -1.0, 1.0) * 2.0 - p.xyz;

		// sphere folding: 	
		float r2 = dot(p.xyz, p.xyz);

		//if (r2 < minRad2) p /= minRad2; else if (r2 < 1.0) p /= r2;
		p *= clamp(max(minRad2/r2, minRad2), 0.0, 1.0);

		// scale, translate
		p = p*scale + p0;
	}
	
	return ((length(p.xyz) - abs(SCALE) + 1.0) / p.w);
}
