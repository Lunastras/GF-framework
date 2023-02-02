Shader "Unlit/ParticleDamageNumbers"
{
    Properties
    {
        _MainTex("Albedo (RGB)", 2DArray) = "white" {}
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

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
                fixed4 colour : COLOR;
            };

            struct v2f
            { 
                float4 uv : TEXCOORD0; //uv.z = startUvXCoord, uv.w = number of digits
                float4 digits : TEXCOORD1; //the digits of the value
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 colour : COLOR;
            };

            
            
            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {             
                const float MAX_VALUE = 9999;
                const int MAX_DECIMALS = 4;
                const float DIGIT_OFFSET = 1.0 / 4.0; //1.0 / MAX_DECIMALS
                const float DIGIT_OFFSET_HALF = (1.0 / 4.0) / 2.0; // DIGIT_OFFSET / 2.0

                float value = round(v.uv.z);
                value = min(value, MAX_VALUE);
                
                v2f o;

                int i = 0, numDigits = 0;
                while(i < MAX_DECIMALS) {
                    o.digits[MAX_DECIMALS - i++ - 1] = value % 10;
                    numDigits += (value > 0);
                    value = floor(value / 10);
                }

                float startUvXCoord = 0.5 - (DIGIT_OFFSET_HALF * (numDigits % 2) + floor(numDigits / 2) * DIGIT_OFFSET);
                o.colour = v.colour;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float4(TRANSFORM_TEX(v.uv, _MainTex), startUvXCoord, MAX_DECIMALS - numDigits);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            } 

            
 
            fixed4 frag (v2f i) : SV_Target
            {
                float DIGIT_OFFSET = 1.0 / 4.0; //1.0 / MAX_DECIMALS
                const int MAX_DECIMALS = 4;

                float startUvXCoord = i.uv.z;
                int startIndex = round(i.uv.w);

                float4 col = float4(0,0,0,0);
                float uvX = i.uv.x - startUvXCoord;
                float digitUvX = (uvX % DIGIT_OFFSET) / DIGIT_OFFSET;
                int digitIndex = uvX / DIGIT_OFFSET;

                if(i.uv.x >= startUvXCoord && i.uv.x <= 1 - startUvXCoord) { 
                    col = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(digitUvX, i.uv.y, i.digits[startIndex + digitIndex]));
                } else {
                    col = float4(startIndex == 1, 0, 0, 0);
                }

                col *= i.colour;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
