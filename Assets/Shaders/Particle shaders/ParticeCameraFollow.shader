// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/ParticleCameraFollow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _xFollowFactor ("The following factor on the X axis", Float) = 0.5
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
            #include "..\MatrixMath.hlsl"
            #include "..\Dithering\DitheringFunctions.hlsl"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _xFollowFactor;
            float _fadeStartDistance;

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

            void FaceCamera(float3 objCenter, float3 vertPos, float4 gravity4, float xFollowFactor, out float3 finalPos) {
                float3 upDir = gravity4.xyz; 
                upDir -= gravity4.w * objCenter; //if the gravity vector is a position (aka v.uv2.y = 1), then substract, if it is a direction (aka v.uv2.y = 0), don't do anything
                upDir = -normalize(upDir);

                vertPos -= objCenter;
                float3 dirToCamera = normalize(_WorldSpaceCameraPos - objCenter);//v.vertex.xyz;
                float3 rightDir = normalize(cross(upDir, dirToCamera));
                float3x3 mat = LookRotation3x3(dirToCamera, upDir, rightDir);
                vertPos = mul(mat, vertPos);

                float angle = angleDeg(upDir, dirToCamera); 
                float auxAngle = 90 + xFollowFactor * (angle - 90);
                float4 quatRot =  angleDegAxis(auxAngle - angle, rightDir);

                finalPos = quatVec3Mult(quatRot, vertPos) + objCenter;
            }

            v2f vert (appdata v)
            {
                
                float3 objCenter = float3(v.uv.w, v.uv1.x, v.uv1.y); //particle system center
                float4 gravity4 = float4(v.uv1.z,v.uv1.w,v.uv2.x, v.uv2.y); 
                float3 vertPos;

                FaceCamera(objCenter, v.vertex.xyz, gravity4, _xFollowFactor, vertPos);

                v2f o;
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
