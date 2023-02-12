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
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        
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
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _xFollowFactor;

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
            };

            v2f vert (appdata v)
            {
                
                float3 objCenter = float3(v.uv.w, v.uv1.x, v.uv1.y); //particle system center

                float3 upDir = float3(v.uv1.z,v.uv1.w,v.uv2.x); 
                upDir -= v.uv2.y * objCenter; //if the gravity vector is a position (aka v.uv2.y = 1), then substract, if it is a direction (aka v.uv2.y = 0), don't do anything
                upDir = normalize(upDir);
                upDir = float3(0,1,0); //delme

                float3 vertPos = v.vertex.xyz - objCenter;
                float3 dirToCamera = normalize(_WorldSpaceCameraPos - objCenter);//v.vertex.xyz;
                float3 rightDir = normalize(cross(upDir, dirToCamera));
                float3x3 mat = LookRotation3x3(dirToCamera, upDir, rightDir);
                vertPos = mul(mat, vertPos);

                float angle = angleDeg(upDir, dirToCamera); 
                float auxAngle = 90 + _xFollowFactor * (angle - 90);
                float4 quatRot =  angleDegAxis(auxAngle - angle, rightDir);

                vertPos = quatVec3Mult(quatRot, vertPos);

                v2f o;
                vertPos += objCenter;
                o.colour = v.colour; 
                o.vertex = UnityObjectToClipPos(vertPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                col *= i.colour;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
