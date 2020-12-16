float sdSphere(float3 p, float s)
{
	return length(p) - s;
}

float sdPlane(float3 p, float4 n)
{
    return dot(p, n.xyz) + n.w;
}

float sdBox(float3 p, float3 scale)
{
    float3 d = abs(p) - scale;
	return min(max(d.x, max(d.y, d.z)), 0.0) +
		length(max(d, 0.0));
}

float sdRoundBox(float3 p, float3 b, float r)
{
    float3 q = abs(p) - b;
    return min(max(q.x, max(q.y, q.z)), 0.0) + length(max(q, 0.0));
}

float deMandelbulb(float3 p, float w, int numIterations, float exponent)
{
	float3 c = p;
	float dr = 1.0;
	float r = 0.0;

	for (int index = 0; index < numIterations; index++)
	{
		r = length(c);
		if (r > 1.5) { break; }

		//To polar
		float theta = acos(c.z / r);
		float psi = atan2(c.y, c.x);
		dr = pow(r, exponent - 1.0) * exponent * dr + 1.0;

		//Scale & Rotate
		float zr = pow(r, exponent * w);
		theta *= exponent * w;
		psi *= exponent * w;

		//To cartesian
		c.x = zr * cos(theta) * cos(psi);
		c.y = zr * cos(theta) * sin(psi);
		c.z = zr * sin(theta);

		c += p;
	}
	return 0.5 * log(r) * r / dr;
}

// MANDEL BOX

// Sphere Inversion
void sphereFold(inout float3 z, inout float dz) {
	float r2 = dot(z,z);
	if (r2<5.0) { 
		// Linear inner scaling
		float temp = (10.0 / 5.0);
		z *= temp;
		dz *= temp;
	} else if (r2 < 10.0) { 
		// Sphere inversion
		float temp = (10.0 / r2);
		z *= temp;
		dz *= temp;
	}
}

// Reflect
void boxFold(inout float3 z, inout float dz) {
	z = clamp(z, -1.0, 1.0) * 2.0 - z;
}

float deMandelbox(in float3 p, in float w, int numIterations)
{
	float3 offset = p;
	float dr = 1.0;
	for (int n = 0; n < numIterations; n++) {
		boxFold(p, dr);
		sphereFold(p, dr);
		p = w * p + offset;  // Scale & Translate
		dr = dr * abs(w) + 1.0;
	}
	float r = length(p);
	return r / abs(dr);
}

// BOOLEAN OPERATORS

// Union
float4 opU(float4 d1, float4 d2)
{
	return (d1.w < d2.w) ? d1 : d2;
}

// Subtraction
float4 opS(float4 d1, float4 d2)
{
	float dist = max(-d1.w, d2.w);
	float3 color = lerp(d2.rgb, d1.rgb, 0.5);
	return float4(color, dist);
}

// Intersection
float opI(float d1, float d2)
{
	return max(d1, d2);
}

// Mod Position Axis
float repeat(inout float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(-p + halfsize, size) - halfsize;
	return c;
}

// SMOOTH BOOLEAN OPERATORS

float4 opUS(float4 d1, float4 d2, float k)
{
    float h = clamp(0.5 + 0.5 * (d2.w - d1.w) / k, 0.0, 1.0);
	float3 color = lerp(d2.rgb, d1.rgb, h);
	float distance = lerp(d2.w, d1.w, h) - k * h * (1.0 - h);
	return float4(color, distance);
}

float opSS(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 + d1) / k, 0.0, 1.0);
    return lerp(d2, -d1, h) + k * h * (1.0 - h);
}

float opIS(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) + k * h * (1.0 - h);
}