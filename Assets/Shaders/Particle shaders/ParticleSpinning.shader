// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/ParticleSpinning"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            //Get angle axis quaternion on given axisNormalised. sinRadiansHalf and cosRadiansHalf
            v2f vert (appdata v)
            {
                float3 objCenter = float3(v.uv.w, v.uv1.x, v.uv1.y); //particle system center

                v2f o;
                float time = _Time.y * 3.0f + 100.0f * v.uv.z; 
                float timeSin = sin(time);   

                float3 upDir = float3(v.uv1.z,v.uv1.w,v.uv2.x);
                if(v.uv2.y == 1) { //if it's a position, then get
                    upDir -= objCenter; //this actually doesn't work and distorts the model, but it won't be very noticeable
                    upDir = normalize(upDir);
                }

                float3 vertPos = v.vertex.xyz - objCenter;

                vertPos.y += 0.3f * timeSin; 

                timeSin = sin(time * 0.2);
                float timeCos = cos(time * 0.2); 

                float4 rotationQuat = angleRadAxis(timeCos, timeSin, upDir);
                float4 verticalCorrectionQuat = quatFromTo(float3(0,1,0), upDir);
                rotationQuat = quatMult(rotationQuat, verticalCorrectionQuat);
                vertPos = quatVec3Mult(rotationQuat, vertPos);

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
