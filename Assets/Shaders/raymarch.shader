Shader "RayMarchingShader/raymarch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"
            #include "DistanceFunctions.cginc"

            sampler2D _MainTex;
            //Setup
            uniform sampler2D _CameraDepthTexture;
            uniform float4x4 _CamFrustum;
            uniform float4x4 _CamToWorld;
            uniform int _maxIterations;
            uniform float _accuracy;
            uniform float _maxDistance;
            
            //Light
            uniform float3 _lightDir;
            uniform float3 _lightColor;
            uniform float _lightIntensity;
            //Color
            uniform fixed4 _groundColor;
            uniform float _colorIntensity;
            //Shadow
            uniform float2 _shadowDistance;
            uniform float _shadowIntensity;
            uniform float _shadowPenumbra;
            //Ambient Occlusion
            uniform float _ambientOcclusionStepSize;
            uniform float _ambientOcclusionIntensity;
            uniform int _ambientOcclusionIterations;
            //Reflection
            uniform int _reflectionCount;
            uniform float _reflectionIntensity;
            uniform float _envReflectionIntensity;
            uniform samplerCUBE _reflectionCube;
            //Fog
            uniform float3 _fogColor;
            uniform float _fogIntensity;
            uniform float _fogMinDistance;
            uniform float _fogMaxDistance;

            uniform float3 _repeatInterval;

            //Spheres
            uniform float4 _sphere;
            uniform float _sphereSmooth;
            uniform float _sphereRotate;
            uniform int _sphereRepeat;
            uniform fixed4 _sphereColors[8];
            //Mandelbulb
            uniform float4 _mandelbulb;
            uniform int _mandelbulbIterations;
            uniform fixed4 _mandelbulbColor;
            uniform int _mandelbulbExponent;
            uniform int _mandelbulbRepeat;
            //Mandelbox
            uniform float4 _mandelbox;
            uniform int _mandelboxIterations;
            uniform fixed4 _mandelboxColor;
            uniform int _mandelboxRepeat;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                half index = v.vertex.z;
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.ray = _CamFrustum[(int)index].xyz;

                o.ray /= abs(o.ray.z);

                o.ray = mul(_CamToWorld, o.ray);

                return o;
            }

            float3 RotateY(float3 v, float degree)
            {
                float rad = 0.0174532925 * degree;
                float cosY = cos(rad);
                float sinY = sin(rad);
                return float3(cosY * v.x - sinY * v.z, v.y, sinY * v.x + cosY * v.z);
            }

            void applyMod(inout float3 p)
            {
                float modX = repeat(p.x, _repeatInterval.x);
                float modY = repeat(p.y, _repeatInterval.y);
                float modZ = repeat(p.z, _repeatInterval.z);

                //TODO: Noise
            }

            void addMandelbulb(inout float4 result, float3 p, bool mod)
            {
                if (mod) { applyMod(p); }
                else { p.y = -p.y; }
                result = opU(result, float4(_mandelbulbColor.rgb, deMandelbulb(p - _mandelbulb.xyz, _mandelbulb.w, _mandelbulbIterations, _mandelbulbExponent)));
            }

            void addMandelbox(inout float4 result, float3 p, bool mod)
            {
                if (mod) { applyMod(p); }
                else { p.y = -p.y; }
                result = opU(result, float4(_mandelboxColor.rgb, deMandelbox(p - _mandelbox.xyz, _mandelbox.w, _mandelboxIterations)));
            }

            void addEightSphere(inout float4 result, float3 p, bool mod)
            {
                if (mod) { applyMod(p); }
                else { p.y = -p.y; }
                float4 sphere = float4(_sphereColors[0].rgb, sdSphere(p - _sphere.xyz, _sphere.w));
                for (int i = 1; i < 8; i++) 
                {
                    float4 sphereAdd = float4(_sphereColors[i].rgb, sdSphere(RotateY(p, _sphereRotate * i) - _sphere.xyz, _sphere.w));
                    sphere = opUS(sphere, sphereAdd, _sphereSmooth);
                }
                result = opU(result, sphere);
            }

            float4 distanceField(float3 p)
            {
                float4 result = float4(_groundColor.rgb, sdPlane(p, float4(0, 1, 0, 0))); //ground

                addEightSphere(result, p, _sphereRepeat > 0 ? true : false);
                //addMandelbox(result, p, _mandelboxRepeat > 0 ? true : false);
                addMandelbulb(result, p, _mandelbulbRepeat > 0 ? true : false);

                return result;
            }

            float3 getNormal(float3 p)
            {
                const float2 offset = float2(0.001, 0.0);
                float3 n = float3(
                    distanceField(p + offset.xyy).w - distanceField(p - offset.xyy).w,
                    distanceField(p + offset.yxy).w - distanceField(p - offset.yxy).w,
                    distanceField(p + offset.yyx).w - distanceField(p - offset.yyx).w
                );
                return normalize(n);
            }

            float3 applyFog(float3 rgb, float distance, float3 rayDir)
            {
                float fogAmount = lerp(0.0, _fogIntensity, clamp((distance - _fogMinDistance) / (_fogMaxDistance - _fogMinDistance), 0.0, 1.0));
                float sunAmount = max( dot( rayDir, _lightDir ), 0.0 );
                float3 fColor = lerp(_fogColor, _lightColor, pow(sunAmount,8.0));
                return lerp(rgb, fColor, fogAmount);
            }

            float hardShadow(float3 rayOrigin, float3 rayDirection, float minT, float maxT)
            {
                for (float t = minT; t < maxT;)
                {
                    float h = distanceField(rayOrigin + rayDirection * t).w;
                    if (h < 0.001) { return 0.0; }
                    t += h;
                }
                return 1.0;
            }

            float softShadow(float3 rayOrigin, float3 rayDirection, float minT, float maxT, float k)
            {
                float result = 1.0;
                for (float t = minT; t < maxT;)
                {
                    float h = distanceField(rayOrigin + rayDirection * t).w;
                    if (h < 0.001) { return 0.0; }
                    result = min(result, k*h/t);
                    t += h;
                }
                return result;
            }

            float AmbientOcclusion(float3 p, float3 n)
            {
                float step = _ambientOcclusionStepSize;
                float ao = 0.0;
                float dist;
                for (int i = 1; i <= _ambientOcclusionIterations; i++)
                {
                    dist = step * i;
                    ao += max(0.0, (dist - distanceField(p + n * dist).w) / dist);
                }
                return (1.0 - ao * _ambientOcclusionIntensity);
            }

            float3 shading(float3 p, float3 n, fixed3 c)
            {
                float3 result;
                //Diffuse Color
                float3 color = c.rgb * _colorIntensity;
                //Directional light
                float3 light = (_lightColor * dot(-_lightDir, n) * 0.5 + 0.5) * _lightIntensity;
                //Shadows
                float shadow = softShadow(p, -_lightDir, _shadowDistance.x, _shadowDistance.y, _shadowPenumbra) * 0.5 + 0.5;
                shadow = max(0.0, pow(shadow, _shadowIntensity));
                //Ambient Occlusion
                float ao = AmbientOcclusion(p, n);

                result = color * light * shadow * ao;

                return result;
            }

            bool raymarching(float3 rayOrigin, float3 rayDirection, float depth, float maxDistance, int maxIterations, inout float3 p, inout fixed3 distColor)
            {
                bool hit;
                float t = 0; //Afstand richting de ray direction

                for (int i = 0; i < maxIterations; i++)
                {
                    if (t > maxDistance || t >= depth) 
                    {
                        //Draw Environment
                        hit = false;
                        break;
                    }

                    p = rayOrigin + rayDirection * t;
                    //check for hit in distance field
                    float4 distance = distanceField(p);
                    if (distance.w < _accuracy)
                    {
                        distColor = distance.rgb;
                        hit = true;
                        break;
                    }
                    t += distance.w;
                }

                return hit;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
                depth *= length(i.ray);
                fixed3 col = tex2D(_MainTex, i.uv);
                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                fixed4 result;
                float3 hitPosition;
                fixed3 distColor;
                bool hit = raymarching(rayOrigin, rayDirection, depth, _maxDistance, _maxIterations, hitPosition, distColor);
                float3 hp = hitPosition;
                float3 rd = rayDirection;
                if (hit)
                {
                    //Shading
                    float3 n = getNormal(hitPosition);
                    float3 s = shading(hitPosition, n, distColor);
                    result = fixed4(s,1);

                    //Apply skybox reflection
                    result += fixed4(texCUBE(_reflectionCube, n).rgb * _envReflectionIntensity * _reflectionIntensity, 0);

                    //Reflection
                    float distanceMult = 1.0;
                    int iterationDiv = 1;
                    float reflectionInt = _reflectionIntensity;
                    for (int i = 0; i < _reflectionCount; i++)
                    {
                        rayDirection = normalize(reflect(rayDirection, n));
                        rayOrigin = hitPosition + (rayDirection * 0.01);
                        distanceMult *= 0.5;
                        iterationDiv *= 2;
                        hit = raymarching(rayOrigin, rayDirection, _maxDistance, _maxDistance * distanceMult, _maxIterations / iterationDiv, hitPosition, distColor);
                        if (hit)
                        {
                            float3 n = getNormal(hitPosition);
                            float3 s = shading(hitPosition, n, distColor);
                            result += fixed4(s * reflectionInt, 0);
                        }
                        reflectionInt *= 0.5;
                    }
                }
                else
                {
                    result = fixed4(0,0,0,0);
                }

                //Apply fog
                result.rgb = applyFog(result.rgb, length(hp - _WorldSpaceCameraPos), rd);
                
                return fixed4(col * (1.0 - result.w) + result.rgb * result.w, 1.0);
            }
            ENDCG
        }
    }
}
