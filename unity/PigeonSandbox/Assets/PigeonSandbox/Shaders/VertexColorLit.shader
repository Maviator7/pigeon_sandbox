// Standard lighting driven by baked vertex data: rgb = albedo, a = smoothness, uv.x = metallic.
// MeshBaker writes these so a whole facility or pigeon part draws with one shared material.
Shader "PigeonSandbox/Vertex Color Lit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0

        struct Input
        {
            float4 color : COLOR;
            float metallic;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.metallic = v.texcoord.x;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb;
            o.Smoothness = IN.color.a;
            o.Metallic = IN.metallic;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
