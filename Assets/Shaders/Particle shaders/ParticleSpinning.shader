
Shader "Unlit/ParticleSpinning"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _fadeStartDistance ("The distance where the object starts to fade", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }
        
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "..\Quaternion.hlsl"
            #include "..\Dithering\DitheringFunctions.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                fixed4 colour : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 colour : COLOR;
                float4 screenCoords : TEXCOORD2;
                float3 viewPosition : TEXCOORD1; 

            };

            

            sampler2D _MainTex;
            float4 _MainTex_ST; 
            float _fadeStartDistance; 

            void CalculateParticleRotation(float3 objCenter, float3 vertPos, float4 gravity4, float spinSpeed, float offset, float bopRange, out float3 finalPos) {
                float time = _Time.y * spinSpeed + offset; 
                float timeSin = sin(time);   

                float3 upDir = gravity4.xyz;
                if(gravity4.w == 1) { //if it's a position, then get the upvec 
                    upDir -= objCenter; 
                    upDir = normalize(upDir);
                }

                vertPos -= objCenter;
                vertPos.y += bopRange * timeSin * 0.5f; 

                timeSin = sin(time * 0.2);
                float timeCos = cos(time * 0.2); 

                float4 rotationQuat = angleRadAxis(timeCos, timeSin, upDir);
                float4 verticalCorrectionQuat = quatFromTo(float3(0,1,0), upDir);
                rotationQuat = quatMult(rotationQuat, verticalCorrectionQuat);
                vertPos = quatVec3Mult(rotationQuat, vertPos);

                finalPos = vertPos + objCenter;
            }
            
            //Get angle axis quaternion on given axisNormalised. sinRadiansHalf and cosRadiansHalf
            v2f vert (appdata v)
            {
                float3 objCenter = float3(v.uv.w, v.uv1.x, v.uv1.y); //particle system center
                float4 gravity4 = float4(v.uv1.z,v.uv1.w,v.uv2.x, v.uv2.y);
                float3 vertPos;
                v2f o;
               
               CalculateParticleRotation(objCenter, v.vertex.xyz, gravity4, 3.0f, 100.0f * v.uv.z, 0.3f, vertPos);

                o.colour = v.colour;
                o.vertex = UnityObjectToClipPos(vertPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenCoords = ComputeScreenPos(o.vertex);
                o.viewPosition = UnityObjectToViewPos(vertPos);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float alphaThreshold, alphaValue;
                GetDitherAlphaThreshold(length(i.viewPosition), _fadeStartDistance, alphaThreshold);
                float4 screenCoords = float4(i.screenCoords.xy / i.screenCoords.w, 0,0);
                GetDither(1, screenCoords, alphaValue);
                alphaValue = step(alphaThreshold, alphaValue);

                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                col *= i.colour;
                col.a *= alphaValue;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}
