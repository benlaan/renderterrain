sampler2D TextureSampler;
float4x4 WorldViewProj;
float3 LightDirection = float3( 0.0f, 0.0f, 1.0f );
float4 Ambient        = float4( 0.3, 0.3, 0.3, 1.0 );
float4 Diffuse        = float4( 0.8, 0.8, 0.8, 1.0 );


struct VS_INPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal   : NORMAL0;
    float4 Color    : COLOR0;
};

struct VS_OUTPUT
{
    float4 Position  : POSITION0;
    float2 Texcoord  : TEXCOORD0;
    float3 Normal    : TEXCOORD1;
    float4 Color     : COLOR0;
};

VS_OUTPUT Transform( VS_INPUT Input )
{
    VS_OUTPUT Output;

    Output.Position = mul( Input.Position, WorldViewProj );
    Output.Texcoord = Input.Texcoord;
    Output.Normal   = normalize( Input.Normal );
    Output.Color    = Input.Color;

    return Output;
}

struct PS_INPUT
{
	float4 Color    : COLOR0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
};

float4 TextureLighting( PS_INPUT Input ) : COLOR0
{
    float  DotProduct   = dot( Input.Normal, LightDirection );
    float4 TotalDiffuse = saturate( Diffuse * DotProduct );
    float4 TextureColor = tex2D( TextureSampler, Input.Texcoord );
    float4 Color = TextureColor * saturate( Ambient + TotalDiffuse );
    Color[3] = Input.Color[3];
    return Color;
};

float4 Wireframe( PS_INPUT Input ) : COLOR0
{
    return float4( 0, 0, 0, 1 );
};

technique Terrain
{
    pass P0
    {
        VertexShader = compile vs_2_0 Transform();
        PixelShader  = compile ps_2_0 TextureLighting();
    }
}

technique Wireframe
{
    pass P0 {
        VertexShader = compile vs_1_1 Transform();
        PixelShader  = compile ps_1_1 Wireframe();
    }
}
