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
            #include "..\Quaternion.inc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _xFollowFactor;

            /*Props to allista from the kerbal space program forum for this incredible function*/
            float angleRadHalf(float3 a, float3 b)
            {
                float3 abm = a * length(b);
                float3 bam = b * length(a);
                return atan2(length(abm - bam), length(abm + bam));
            }

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
                v2f o;
                
                float3 upDir = float3(v.uv1.z,v.uv1.w,v.uv2.x);
                if(v.uv2.y == 1) {
                    upDir -= v.vertex.xyz;
                    upDir = normalize(upDir);
                }

                float3 center = float3(v.uv.w, v.uv1.x, v.uv1.y);
                float3 objectPos = v.vertex.xyz - center;
                float3 dirToCamera = _WorldSpaceCameraPos - v.vertex.xyz;

                float halfPi = 1.57079632679;
                float angle = 2 * angleRadHalf(upDir, dirToCamera); 
                float auxAngleHalf = 0.5 * (halfPi + _xFollowFactor * (angle - halfPi) - angle);

                float4 quatRot =  angleAxis(cos(auxAngleHalf), sin(auxAngleHalf), float3(0, 1, 0));
               // objectPos = quatVec3Mult(quatRot, objectPos);

                objectPos += center;
                o.colour = v.colour;
                o.vertex = UnityObjectToClipPos(objectPos);
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
