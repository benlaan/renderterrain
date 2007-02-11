float4x4 xViewProjection;
float3 LightDirection = float3( 0.0f, 0.0f, 1.0f );
float4 Ambient        = float4( 0.3, 0.3, 0.3, 1.0 );
float4 Diffuse        = float4( 0.8, 0.8, 0.8, 1.0 );

Texture               dirtTexture;
Texture               waterTexture;
Texture               stoneTexture;
Texture               grassTexture;
Texture               alphaTexture;


//struct PixelToFrame
//{
//    float4 Color : COLOR0;
//};

//struct VertexToPixel
//{
//    float4 Position  : POSITION;
//    float4 Texcoord : TEXCOORD0;
//    float3 Normal    : NORMAL0;
//};

struct VS_INPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal   : NORMAL0;
};

struct VS_OUTPUT
{
    float4 Position  : POSITION0;
    float2 Texcoord  : TEXCOORD0;
    float3 Normal    : TEXCOORD1;
    float4 Color     : COLOR0;
};

struct PS_INPUT
{
	float4 Color    : COLOR0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
};

sampler dirtTextureSampler = sampler_state 
{ 
	texture = <dirtTexture>; 
	mipfilter = LINEAR; 
};

sampler waterTextureSampler = sampler_state 
{ 
	texture = <waterTexture>; 
	mipfilter = LINEAR; 
};

sampler grassTextureSampler = sampler_state 
{ 
	texture = <grassTexture>; 
	mipfilter = LINEAR; 
};

sampler stoneTextureSampler = sampler_state 
{ 
	texture = <stoneTexture>; 
	mipfilter = LINEAR; 
};

sampler alphaTextureSampler = sampler_state 
{ 
	texture = <alphaTexture>; 
	mipfilter = LINEAR; 
};

VS_OUTPUT Transform(VS_INPUT input)
{
    VS_OUTPUT output = (VS_OUTPUT)0;
    
    output.Position  = mul(input.Position, xViewProjection);
    output.Texcoord = input.Texcoord;
    output.Normal = input.Normal;

    return output;  
}

float4 Shader(PS_INPUT input) : COLOR0
{
	
    // sample textures
    vector b = tex2D(waterTextureSampler, input.Texcoord);
    vector a = tex2D(alphaTextureSampler, input.Texcoord);
    vector i = tex2D(dirtTextureSampler, input.Texcoord);
    vector j = tex2D(grassTextureSampler, input.Texcoord);
    vector k = tex2D(stoneTextureSampler, input.Texcoord);

    // combine texel colors
    float4 oneminusx = 1.0 - a.x;
    float4 oneminusy = 1.0 - a.y;
    float4 oneminusz = 1.0 - a.z;
    vector l = a.x * i + oneminusx * b;
    vector m = a.y * j + oneminusy * l;
    vector n = a.z * k + oneminusz * m;

    float  DotProduct   = dot( input.Normal, LightDirection );
    float4 TotalDiffuse = saturate( Diffuse * DotProduct );
    float4 Color = n * saturate( Ambient + TotalDiffuse );
    Color[3] = n[3];

	return Color;
}

technique TextureSplatting
{
    pass Pass0
    {        
        VertexShader = compile vs_1_1 Transform();
        PixelShader = compile ps_2_0 Shader();
    }
}
