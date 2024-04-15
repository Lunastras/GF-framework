Shader "Unlit/GfSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _xFollowFactor ("xFollowFactor", Float) = 0.5
        _upDir ("UpDir", Vector) = (0,1,0,0)
        _objectPosition ("The object's position", Vector) = (0,0,0,0)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }
    SubShader
    {
		Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            //"DisableBatching"="True"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
        Lighting Off
        ZWrite Off

        
		
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile_local _ CANCEL_ROTATION

            #include "UnityCG.cginc"
            #include "..\MatrixMath.hlsl"
            #include "..\Quaternion.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _xFollowFactor;
            float4 _objectPosition;
            float4 _upDir;
            float CancelRotation;
            float PixelSnap;

            v2f vert (appdata v)
            {
                #if defined(PIXELSNAP_ON)
                    v.vertex = UnityPixelSnap (v.vertex);
                #endif

                /*
                //cancel rotation of the object
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22));
 
                unity_ObjectToWorld._m00_m10_m20 = float3(scale.x, 0, 0);
                unity_ObjectToWorld._m01_m11_m21 = float3(0, scale.y, 0);
                unity_ObjectToWorld._m02_m12_m22 = float3(0, 0, scale.z);
                
                float3 objCenter = _objectPosition.xyz;//unity_ObjectToWorld._m03_m13_m23;
                float3 upDir = _upDir.xyz;
    
                float3 dirToCamera = normalize(_WorldSpaceCameraPos - objCenter);//v.vertex.xyz;
                float3 rightDir = normalize(cross(upDir, dirToCamera));
                float3x3 mat = LookRotation3x3(dirToCamera, upDir, rightDir);

                float3 vertPos = v.vertex.xyz;
                vertPos = mul(mat, vertPos);

                float angle = angleDeg(upDir, dirToCamera); 
                float auxAngle = 90 + _xFollowFactor * (angle - 90);
                float4 quatRot =  angleDegAxis(auxAngle - angle, rightDir);

                vertPos = quatVec3Mult(quatRot, vertPos);
                */
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
