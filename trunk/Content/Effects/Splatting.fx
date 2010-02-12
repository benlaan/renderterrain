float4x4 ViewProjection;
float3   LightDirection = float3( 0.0f, 0.0f, 1.0f );
float4   Ambient        = float4( 0.3, 0.3, 0.3, 1.0 );
float4   Diffuse        = float4( 0.8, 0.8, 0.8, 1.0 );

Texture  forestTexture;
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

struct MTVertexToPixel
{
    float4 Position         : POSITION;    
    float4 Color            : COLOR0;
    float3 Normal            : TEXCOORD0;
    float2 TextureCoords    : TEXCOORD1;
    float4 LightDirection    : TEXCOORD2;
    float4 TextureWeights    : TEXCOORD3;
};

sampler forestTextureSampler = sampler_state 
{ 
    texture = <forestTexture>; 
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
    vector i = tex2D(forestTextureSampler, input.Texcoord1);
    vector j = tex2D(grassTextureSampler, input.Texcoord1);
    vector k = tex2D(stoneTextureSampler, input.Texcoord1);

    // combine texel colors
    vector l = a.x * i + (1.0 - a.x) * b;
    vector m = a.y * j + (1.0 - a.y) * l;
    vector n = a.z * k + (1.0 - a.z) * m;

    float  DotProduct   = dot(input.Normal, LightDirection);
    float4 TotalDiffuse = saturate(Diffuse * DotProduct);
    float4 Color        = n * saturate(Ambient + TotalDiffuse);
    Color[3]            = n[3];

    return Color;

//    return n;
}

struct MTPixelToFrame
{
    float4 Color : COLOR0;
};


MTPixelToFrame MultiTexturedPS(MTVertexToPixel PSIn)
{
    MTPixelToFrame Output = (MTPixelToFrame)0;        
    
    float lightingFactor = 1;
    //if (xEnableLighting)
        lightingFactor = saturate(saturate(dot(PSIn.Normal, -PSIn.LightDirection)) + Ambient);

    Output.Color = tex2D(waterTextureSampler, PSIn.TextureCoords)*PSIn.TextureWeights.x;
    Output.Color += tex2D(grassTextureSampler, PSIn.TextureCoords)*PSIn.TextureWeights.y;
    Output.Color += tex2D(forestTextureSampler, PSIn.TextureCoords)*PSIn.TextureWeights.z;
    Output.Color += tex2D(stoneTextureSampler, PSIn.TextureCoords)*PSIn.TextureWeights.w;    
	Output.Color *= lightingFactor;
    return Output;
}

MTVertexToPixel MultiTexturedVS( float4 inPos : POSITION, float3 inNormal: NORMAL, float2 inTexCoords: TEXCOORD0, float4 inTexWeights: TEXCOORD1)
{    
    MTVertexToPixel Output = (MTVertexToPixel)0;
    
    Output.Position = mul(inPos, ViewProjection);
    Output.Normal = normalize(inNormal);
    Output.TextureCoords = inTexCoords;
    Output.LightDirection.xyz = -LightDirection;
    Output.LightDirection.w = 1;    
    Output.TextureWeights = inTexWeights;
    
    return Output;    
}

float4 WireFrame(PS_INPUT input) : COLOR0
{
    return 255;
}

technique TextureSplatting
{
    pass Pass0
    {        
//        VertexShader = compile vs_1_1 Transform();
//        PixelShader = compile  ps_2_0 Shader();

		VertexShader = compile vs_1_1 MultiTexturedVS();
        PixelShader = compile ps_2_0 MultiTexturedPS();
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
