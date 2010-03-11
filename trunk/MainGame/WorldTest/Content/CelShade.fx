// Any change made over these constants require that the SkinnedModelEffect also being updated.
#define SHADER20_MAX_BONES 80
#define MAX_LIGHTS 8


// --------------------------------------------------
// Cel Shader 
// --------------------------------------------------
struct PointLight
{
    float3 position;
    float3 color;
};

struct Material
{
    float3 emissiveColor;
    float3 diffuseColor;
    float3 specularColor;
    float specularPower;
};

// Configurations
// -------------------------------------------------
bool diffuseMapEnabled;
bool specularMapEnabled;

// Matrix
// -------------------------------------------------
float4x4 matW  : World;
float4x4 matV	: View;
float4x4 matVI  : ViewInverse;
float4x4 matVP  : ViewProjection;
float4x3 matBones[SHADER20_MAX_BONES];

// Material
// -------------------------------------------------
Material material;

// Light
// -------------------------------------------------
float3 ambientLightColor;
PointLight lights[MAX_LIGHTS];
float4x4 lightview[MAX_LIGHTS];

// Textures and Samplers
// -------------------------------------------------
texture diffuseMap0;
sampler2D diffuseMapSampler = sampler_state {
    texture = <diffuseMap0>;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture normalMap0;
sampler2D normalMapSampler = sampler_state {
    texture = <normalMap0>;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture specularMap0;
sampler2D specularMapSampler = sampler_state {
    texture = <specularMap0>;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

// The texture that contains the celmap
texture CelMap;
sampler2D CelMapSampler = sampler_state
{
	Texture	  = <CelMap>;
	MIPFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MINFILTER = LINEAR;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
};

// The texture that contains the shadow map
texture shadowMap;
sampler2D ShadowMapSampler = sampler_state
{
	Texture	  = <shadowMap>;
	MIPFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MINFILTER = LINEAR;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
};

// Vertex Shaders
// -------------------------------------------------

void StaticModelVS_Light(
	in float4 inPosition		: POSITION,
	in float3 inNormal			: NORMAL,
	in float2 inUV0				: TEXCOORD0,
	
	out float4 outSVPosition	: POSITION,
	out float3 outPosition		: TEXCOORD0,
	out float3 outNormal		: TEXCOORD1,
	out float2 outUV0			: TEXCOORD2,
	out float3 outEyeVector		: TEXCOORD3)
{
	// Transform vertex position and normal
    outPosition = mul(inPosition, matW);
    outSVPosition = mul(float4(outPosition, 1.0f), matVP);
    
    // Transform vertex normal
    outNormal = mul(inNormal, (float3x3)matW);
    
    // Calculate eye vector
    outEyeVector = matVI[3].xyz - outPosition;
    
    // Texture coordinate
    outUV0 = inUV0;
}

void AnimatedModelVS_Light(
    in float4 inPosition		: POSITION,
    in float3 inNormal			: NORMAL,
    in float2 inUV0				: TEXCOORD0,
    in float4 inBoneIndex		: BLENDINDICES0,
    in float4 inBoneWeight		: BLENDWEIGHT0,
    
    out float4 outSVPosition	: POSITION,
    out float3 outPosition		: TEXCOORD0,
    out float3 outNormal		: TEXCOORD1,
    out float2 outUV0			: TEXCOORD2,
    out float3 outEyeVector		: TEXCOORD3)
{
    // Calculate the final bone transformation matrix
    float4x3 matSmoothSkin = 0;
    matSmoothSkin += matBones[inBoneIndex.x] * inBoneWeight.x;
    matSmoothSkin += matBones[inBoneIndex.y] * inBoneWeight.y;
    matSmoothSkin += matBones[inBoneIndex.z] * inBoneWeight.z;
    matSmoothSkin += matBones[inBoneIndex.w] * inBoneWeight.w;
    
    // Combine skin and world transformations
    float4x4 matSmoothSkinWorld = 0;
    matSmoothSkinWorld[0] = float4(matSmoothSkin[0], 0);
    matSmoothSkinWorld[1] = float4(matSmoothSkin[1], 0);
    matSmoothSkinWorld[2] = float4(matSmoothSkin[2], 0);
    matSmoothSkinWorld[3] = float4(matSmoothSkin[3], 1);
    matSmoothSkinWorld = mul(matSmoothSkinWorld, matW);
    
    // Transform vertex position and normal
    outPosition = mul(inPosition, matSmoothSkinWorld);
    outSVPosition = mul(float4(outPosition, 1.0f), matVP);
    
    // Transform vertex normal
    outNormal = mul(inNormal, (float3x3)matSmoothSkinWorld);
    
    // Calculate eye vector
    outEyeVector = matVI[3].xyz - outPosition;
    
    // Texture coordinate
    outUV0 = inUV0;
}


void AnimatedModelVS_LightWithNormal(
	in float4 inPosition		: POSITION,
    in float3 inNormal			: NORMAL,
    in float2 inUV0				: TEXCOORD0,
    in float3 inTangent			: TANGENT0,
    in float3 inBinormal		: BINORMAL0,
    in float4 inBoneIndex		: BLENDINDICES0,
    in float4 inBoneWeight		: BLENDWEIGHT0,

	out float4 outSVPosition	: POSITION,
    out float3 outPosition		: TEXCOORD0,
    out float2 outUV0			: TEXCOORD1,
    out float3 outEyeVector		: TEXCOORD2,
    out float3 outTangent		: TEXCOORD3,
    out float3 outBinormal		: TEXCOORD4,
    out float3 outNormal		: TEXCOORD5
	)
{
    // Calculate the final bone transformation matrix
    float4x3 matSmoothSkin = 0;
    matSmoothSkin += matBones[inBoneIndex.x] * inBoneWeight.x;
    matSmoothSkin += matBones[inBoneIndex.y] * inBoneWeight.y;
    matSmoothSkin += matBones[inBoneIndex.z] * inBoneWeight.z;
    matSmoothSkin += matBones[inBoneIndex.w] * inBoneWeight.w;
    
    // Combine skin and world transformations
    float4x4 matSmoothSkinWorld = 0;
    matSmoothSkinWorld[0] = float4(matSmoothSkin[0], 0);
    matSmoothSkinWorld[1] = float4(matSmoothSkin[1], 0);
    matSmoothSkinWorld[2] = float4(matSmoothSkin[2], 0);
    matSmoothSkinWorld[3] = float4(matSmoothSkin[3], 1);
    matSmoothSkinWorld = mul(matSmoothSkinWorld, matW);
    
    // Transform vertex position and normal
    outPosition = mul(inPosition, matSmoothSkinWorld);
    outSVPosition = mul(float4(outPosition, 1.0f), matVP);

	// Matrix to put world space vectors in tangent space
	//float3x3 tangentSpace = float3x3(inTangent, inBinormal, inNormal);
    //float3x3 toTangentSpace = mul(tangentSpace, (float3x3)matSmoothSkin);
	float3x3 tangentSpace = float3x3(inTangent, inBinormal, inNormal);
	float3x3 tangentToWorld = mul(tangentSpace, (float3x3)matSmoothSkinWorld);
    
	// Calculate eye vector
    float3 eyeVector = matVI[3].xyz - outPosition;
    //outEyeVector = mul(toTangentSpace, eyeVector);
	outEyeVector = eyeVector;
	
	// Tangent base
	outTangent = tangentToWorld[0];
	outBinormal = tangentToWorld[1];
	outNormal = tangentToWorld[2];
	
	// Texture coordinate
	outUV0 = inUV0;
}


// Pixel Shaders
// -------------------------------------------------

float3 PhongShadingPS(
	uniform int lightCount,
	in float3 inPosition,
	in float3 inNormal,
	in float3 inEyeVector, 
	in float3 inDiffuseColor,
	in float3 inSpecularColor,
	in float  inSpecularPower)
{
	float3 diffuseLightColor = ambientLightColor * inDiffuseColor;
	float3 specularLightColor = float3(1,1,1);
	float4 CelColor = float4(0,0,0,0);
	//float4 light_pos = mul(inPosition, lightview[0]);
	//float shadow_test = light_pos.z / light_pos.w;
	//float4 map_val = tex2D(ShadowMapSampler, light_pos.xy);
	
	//if (shadow_test > map_val.z)
	//	return diffuseLightColor;
	//else {
		for (int i = 0; i < lightCount; i++)
		{
			// Light vector
			float3 lightVector = normalize(lights[i].position - inPosition);

			// Diffuse intensity
			float diffuseIntensity = saturate(dot(inNormal, lightVector));
			float3 reflect = normalize(2*diffuseIntensity*inNormal - lightVector);
			float2 Tex = float2(diffuseIntensity, 0);
			CelColor += tex2D(CelMapSampler, Tex);
		
			// Specular intensity
			float specularIntensity = pow(saturate(dot(reflect, inEyeVector)), inSpecularPower);
		
			diffuseLightColor += lights[i].color * diffuseIntensity;
			specularLightColor += lights[i].color * specularIntensity;
		}

		return (diffuseLightColor * inDiffuseColor + specularLightColor * inSpecularColor) * CelColor;
	//}
}

void StaticModelPS_Light(
	uniform int lightCount,
    in float3 inPosition	: TEXCOORD0,
    in float3 inNormal		: TEXCOORD1,
    in float2 inUV0			: TEXCOORD2,
    in float3 inEyeVector	: TEXCOORD3,

	out float4 outColor0	: COLOR0)
{
    // Normalize all input vectors
    float3 position = normalize(inPosition);
	float3 normal = normalize(inNormal);
    float3 eyeVector = normalize(inEyeVector);
    
    // Reads texture diffuse color
    float3 diffuseColor = material.diffuseColor;
    if (diffuseMapEnabled)
        diffuseColor *= tex2D(diffuseMapSampler, inUV0);
    
    // Reads texture specular color
    float3 specularColor = material.specularColor;
    if (specularMapEnabled)
        specularColor *= tex2D(specularMapSampler, inUV0);
       	
    // Calculate final color
    outColor0.a = 1.0f;
	outColor0.rgb = material.emissiveColor + PhongShadingPS(lightCount, position, normal,
		eyeVector, diffuseColor, specularColor, material.specularPower);
}

void animatedModelPS_Light(
	uniform int lightCount,
    in float3 inPosition	: TEXCOORD0,
    in float3 inNormal		: TEXCOORD1,
    in float2 inUV0			: TEXCOORD2,
    in float3 inEyeVector	: TEXCOORD3,

	out float4 outColor0	: COLOR0)
{
    // Normalize all input vectors
    float3 position = normalize(inPosition);
	float3 normal = normalize(inNormal);
    float3 eyeVector = normalize(inEyeVector);
    
    // Reads texture diffuse color
    float3 diffuseColor = material.diffuseColor;
    if (diffuseMapEnabled)
        diffuseColor *= tex2D(diffuseMapSampler, inUV0);
    
    // Reads texture specular color
    float3 specularColor = material.specularColor;
    if (specularMapEnabled)
        specularColor *= tex2D(specularMapSampler, inUV0);
        	
    // Calculate final color
    outColor0.a = 1.0f;
	outColor0.rgb = material.emissiveColor + PhongShadingPS(lightCount, position, normal,
		eyeVector, diffuseColor, specularColor, material.specularPower);
}


void animatedModelPS_LightWithNormal(
	uniform int lightCount,
    in float3 inPosition	: TEXCOORD0,
    in float2 inUV0			: TEXCOORD1,
    in float3 inEyeVector	: TEXCOORD2,
    in float3 inTangent		: TEXCOORD3,
    in float3 inBinormal	: TEXCOORD4,
    in float3 inNormal		: TEXCOORD5,
	
	out float4 outColor0	: COLOR0)
{
    // Normalize all input vectors
	float3 position = normalize(inPosition);
    float3 eyeVector = normalize(inEyeVector);
    
	//float3x3 toTangentSpace = float3x3(
		//normalize(inTangent), normalize(inBinormal), normalize(inNormal));
	float3x3 toTangentSpace = float3x3(inTangent, inBinormal, inNormal);
    
    // Read texture diffuse color
    float3 diffuseColor = material.diffuseColor;
    if (diffuseMapEnabled)
        diffuseColor *= tex2D(diffuseMapSampler, inUV0);
    
    // Reads texture specular color
    float3 specularColor = material.specularColor;
    if (specularMapEnabled)
        specularColor *= tex2D(specularMapSampler, inUV0);
    
    // Read the surface's normal (only use the R and G channels)
    float3 normal = tex2D(normalMapSampler, inUV0);
	normal.xy = normal.xy * 2.0 - 1.0;
	normal.y = -normal.y;
	normal.z = sqrt(1.0 - dot(normal.xy, normal.xy));
    
	// Put normal in world space
	normal = mul(normal, toTangentSpace);

    // Calculate final color
    outColor0.a = 1.0f;
    outColor0.rgb = material.emissiveColor + PhongShadingPS(lightCount, position, normal,
		eyeVector, diffuseColor, specularColor, material.specularPower);
}


// Techniques
// -------------------------------------------------

technique StaticModel
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
	pass p0
	{
		AlphaBlendEnable = FALSE;
		
		VertexShader = compile vs_2_0 StaticModelVS_Light();
        PixelShader = compile ps_2_0 StaticModelPS_Light(1);
	}
}

technique AnimatedModel_NoLight 
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_Light();
        PixelShader = compile ps_2_0 animatedModelPS_Light(0);
    }
}

technique AnimatedModel_OneLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_Light();
        PixelShader = compile ps_2_0 animatedModelPS_Light(1);
    }
}

technique AnimatedModel_TwoLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_Light();
        PixelShader = compile ps_2_0 animatedModelPS_Light(2);
    }
}

technique AnimatedModel_FourLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_Light();
        PixelShader = compile ps_2_b animatedModelPS_Light(4);
    }
}

technique AnimatedModel_SixLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_Light();
        PixelShader = compile ps_2_b animatedModelPS_Light(6);
    }
}

technique AnimatedModel_EightLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_Light();
        PixelShader = compile ps_2_b animatedModelPS_Light(8);
    }
}

technique AnimatedModel_OneLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_0 animatedModelPS_LightWithNormal(1);
    }
}

technique AnimatedModel_TwoLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_0 animatedModelPS_LightWithNormal(2);
    }
}

technique AnimatedModel_FourLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_b animatedModelPS_LightWithNormal(4);
    }
}

technique AnimatedModel_SixLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_b animatedModelPS_LightWithNormal(6);
        
    }
}

technique AnimatedModel_EightLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_2_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_b animatedModelPS_LightWithNormal(8);
    }
}

/////////////////////////////////////////////////////////////////////////////////
//
//	The rest of the file contains the shaders that render the black outlines.
//
/////////////////////////////////////////////////////////////////////////////////
// -----------------------------------------------
// Outline Renderer Constants and Textures		 
// -----------------------------------------------
float EdgeWidth = 1.2f;	
float EdgeIntensity = 1;

// How sensitive should the edge detection be to tiny variations in the input data?
// Smaller settings will make it pick up more subtle edges, while larger values get
// rid of unwanted noise.

float NormalThreshold = 0.5;
float DepthThreshold = 0.1;

// How dark should the edges get in response to changes in the input data?

float NormalSensitivity = 1;
float DepthSensitivity = 10;

// Pass in the current screen resolution.
float2 ScreenResolution;

texture SceneTexture;

sampler SceneSampler : register(s0) = sampler_state
{
    Texture = (SceneTexture);
    
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

// This texture contains normals (in the color channels) and depth (in alpha)
// for the main scene image. Differences in the normal and depth data are used
// to detect where the edges of the model are.

texture NormalDepthTexture;

sampler NormalDepthSampler : register(s1) = sampler_state
{
    Texture = (NormalDepthTexture);
    
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

// --------------------------------------
// Structs
// --------------------------------------

struct VertexShaderInput
{
    float4 Position				: POSITION0;
    float4 Color				: COLOR0;
    float3 Normal				: NORMAL0;
    float2 TextureCoordinate	: TEXCOORD0;
    float4 inBoneIndex			: BLENDINDICES0;
    float4 inBoneWeight			: BLENDWEIGHT0;
};

struct VertexShaderInputStatic
{
	float4 Position				: POSITION0;
    float4 Color				: COLOR0;
    float3 Normal				: NORMAL0;
    float2 TextureCoordinate	: TEXCOORD0;
};

struct NormalDepthVertexShaderOutput
{
    float4 Position				: POSITION0;
    float4 Color				: COLOR0;
};

// ---------------------------------------
// Vertex Shaders
// ---------------------------------------

NormalDepthVertexShaderOutput Static_NormalDepthVertexShader(VertexShaderInputStatic input)
{
	NormalDepthVertexShaderOutput output;
	
	// Apply camera matrices to the input position.
    output.Position = mul(input.Position, matW);
    output.Position = mul(output.Position, matVP);
    
    float3 worldNormal = mul(input.Normal, (float3x3)matW);

    // The output color holds the normal, scaled to fit into a 0 to 1 range.
    output.Color.rgb = (worldNormal + 1) / 2;

    // The output alpha holds the depth, scaled to fit into a 0 to 1 range.
    output.Color.a = output.Position.z / output.Position.w;
    
    return output;    
}

NormalDepthVertexShaderOutput NormalDepthVertexShader(VertexShaderInput input)
{
    NormalDepthVertexShaderOutput output;
    
    // Calculate the final bone transformation matrix
    float4x3 matSmoothSkin = 0;
    matSmoothSkin += matBones[input.inBoneIndex.x] * input.inBoneWeight.x;
    matSmoothSkin += matBones[input.inBoneIndex.y] * input.inBoneWeight.y;
    matSmoothSkin += matBones[input.inBoneIndex.z] * input.inBoneWeight.z;
    matSmoothSkin += matBones[input.inBoneIndex.w] * input.inBoneWeight.w;
    
    // Combine skin and world transformations
    float4x4 matSmoothSkinWorld = 0;
    matSmoothSkinWorld[0] = float4(matSmoothSkin[0], 0);
    matSmoothSkinWorld[1] = float4(matSmoothSkin[1], 0);
    matSmoothSkinWorld[2] = float4(matSmoothSkin[2], 0);
    matSmoothSkinWorld[3] = float4(matSmoothSkin[3], 1);
    matSmoothSkinWorld = mul(matSmoothSkinWorld, matW);

    // Apply camera matrices to the input position.
    output.Position = mul(input.Position, matSmoothSkinWorld);
    output.Position = mul(output.Position, matVP);
    
    
    float3 worldNormal = mul(input.Normal, (float3x3)matSmoothSkinWorld);

    // The output color holds the normal, scaled to fit into a 0 to 1 range.
    output.Color.rgb = (worldNormal + 1) / 2;

    // The output alpha holds the depth, scaled to fit into a 0 to 1 range.
    output.Color.a = output.Position.z / output.Position.w;
    
    return output;    
}

// --------------------------------------------
// Pixel Shaders
// --------------------------------------------

float4 NormalDepthPixelShader(float4 color : COLOR0) : COLOR0
{
    return color;
}

float4 OutlineShader(float2 texCoord : TEXCOORD0) : COLOR0
{
    // TODO: add your pixel shader code here.

    // Look up the original color from the main scene.
    float3 scene = tex2D(SceneSampler, texCoord);
    
    // Apply the edge detection filter?
    // Look up four values from the normal/depth texture, offset along the
    // four diagonals from the pixel we are currently shading.
    float2 edgeOffset = EdgeWidth / ScreenResolution;
    
    float4 n1 = tex2D(NormalDepthSampler, texCoord + float2(-1, -1) * edgeOffset);
    float4 n2 = tex2D(NormalDepthSampler, texCoord + float2( 1,  1) * edgeOffset);
    float4 n3 = tex2D(NormalDepthSampler, texCoord + float2(-1,  1) * edgeOffset);
    float4 n4 = tex2D(NormalDepthSampler, texCoord + float2( 1, -1) * edgeOffset);

    // Work out how much the normal and depth values are changing.
    float4 diagonalDelta = abs(n1 - n2) + abs(n3 - n4);

    float normalDelta = dot(diagonalDelta.xyz, 1);
    float depthDelta = diagonalDelta.w;
    
    // Filter out very small changes, in order to produce nice clean results.
    normalDelta = saturate((normalDelta - NormalThreshold) * NormalSensitivity);
    depthDelta = saturate((depthDelta - DepthThreshold) * DepthSensitivity);

    // Does this pixel lie on an edge?
    float edgeAmount = saturate(normalDelta + depthDelta) * EdgeIntensity;
    
    // Apply the edge detection result to the main scene color.
    scene *= (1 - edgeAmount);

    return float4(scene, 1);
}

// -----------------------------------------
// Techniques
// -----------------------------------------

technique Outlines
{
    pass p0
    {
        // TODO: set renderstates here.

        PixelShader = compile ps_2_0 OutlineShader();
    }
}

technique NormalDepth
{
    pass p0
    {
        VertexShader = compile vs_1_1 NormalDepthVertexShader();
        PixelShader = compile ps_1_1 NormalDepthPixelShader();
    }
}

technique StaticNormalDepth
{
    pass p0
    {
        VertexShader = compile vs_1_1 Static_NormalDepthVertexShader();
        PixelShader = compile ps_1_1 NormalDepthPixelShader();
    }
}

// ------------------------------------------------------------
// Shadow Mapping
// ------------------------------------------------------------

struct SMapVertexToPixel
{
    float4 Position     : POSITION;
    float4 Position2D    : TEXCOORD0;
};

struct SMapPixelToFrame
{
    float4 Color : COLOR0;
};

SMapVertexToPixel ShadowMapVertexShader( float4 inPos : POSITION)
{
    SMapVertexToPixel Output = (SMapVertexToPixel)0;

	//get working only for 1 light for right now
    Output.Position = mul(inPos, lightview[0]);
    Output.Position2D = Output.Position;

    return Output;
}

SMapPixelToFrame ShadowMapPixelShader(SMapVertexToPixel PSIn)
{
    SMapPixelToFrame Output = (SMapPixelToFrame)0;            

    Output.Color = PSIn.Position2D.z/PSIn.Position2D.w;

    return Output;
}

technique ShadowMap
{
    pass Pass0
    {
        VertexShader = compile vs_2_0 ShadowMapVertexShader();
        PixelShader = compile ps_2_0 ShadowMapPixelShader();
    }
}