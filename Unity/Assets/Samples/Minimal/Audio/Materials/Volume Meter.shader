Shader "UCLCVE/Samples/Volume Meter Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Volume ("Volume", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Volume;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                if (col.a == 1)
                {
                    if (any(col.rgb > 0)) {
                        if (_Volume < i.uv.y) {
                            col.rgb = float3(0.2, 0.2, 0.2);
                        }
                        else
                        {
                            col.rgb = col.rgb + 0.1;
                        }
                    }
                    else
                    {
                        col.rgb = float3(0.1, 0.1, 0.1);
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}
