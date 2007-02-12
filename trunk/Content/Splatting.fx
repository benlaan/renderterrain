float4x4 ViewProjection;
float3   LightDirection = float3( 0.0f, 0.0f, 1.0f );
float4   Ambient        = float4( 0.3, 0.3, 0.3, 1.0 );
float4   Diffuse        = float4( 0.8, 0.8, 0.8, 1.0 );

Texture  dirtTexture;
Texture  waterTexture;
Texture  stoneTexture;
Texture  grassTexture;
Texture  alphaTexture;

struct VS_INPUT
{
    float4 Position  : POSITION0;
    float2 Texcoord0 : TEXCOORD0;
    float2 Texcoord1 : TEXCOORD1;
    float3 Normal    : NORMAL0;
};

struct VS_OUTPUT
{
    float4 Position  : POSITION0;
    float2 Texcoord0 : TEXCOORD0;
    float2 Texcoord1 : TEXCOORD1;
    float3 Normal    : TEXCOORD2;
};

struct PS_INPUT
{
    float2 Texcoord0 : TEXCOORD0;
    float2 Texcoord1 : TEXCOORD1;
    float3 Normal    : TEXCOORD2;
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
    
    output.Position  = mul(input.Position, ViewProjection);
    output.Texcoord0 = input.Texcoord0;
    output.Texcoord1 = input.Texcoord1;
    output.Normal = input.Normal;

    return output;  
}

float4 Shader(PS_INPUT input) : COLOR0
{
	
    // sample textures
    vector a = tex2D(alphaTextureSampler, input.Texcoord0);
    vector b = tex2D(waterTextureSampler, input.Texcoord1);
    vector i = tex2D(grassTextureSampler, input.Texcoord1);
    vector j = tex2D(dirtTextureSampler, input.Texcoord1);
    vector k = tex2D(stoneTextureSampler, input.Texcoord1);

    // combine texel colors
    float4 oneminusx = 1.0 - a.x;
    float4 oneminusy = 1.0 - a.y;
    float4 oneminusz = 1.0 - a.z;
    vector l = a.x * i + oneminusx * b;
    vector m = a.y * j + oneminusy * l;
    vector n = a.z * k + oneminusz * m;

    float  DotProduct   = dot(input.Normal, LightDirection);
    float4 TotalDiffuse = saturate(Diffuse * DotProduct);
    float4 Color        = n * saturate(Ambient + TotalDiffuse);
    Color[3]            = n[3];

	return Color;
}

float4 WireFrame(PS_INPUT input) : COLOR0
{
    return 255;
}

technique TextureSplatting
{
    pass Pass0
    {        
        VertexShader = compile vs_1_1 Transform();
        PixelShader = compile  ps_2_0 Shader();
    }
}

technique WireFrame
{
    pass Pass0
    {        
        VertexShader = compile vs_1_1 Transform();
        PixelShader = compile  ps_2_0 WireFrame();
    }
}
