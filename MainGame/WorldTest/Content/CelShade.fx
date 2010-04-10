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
float lightRadii[MAX_LIGHTS];

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

struct VS_INPUT_ANIMATED
{
	float4 Position		: POSITION0;
	float3 Normal		: NORMAL0;
	float2 Texcoord		: TEXCOORD0;
	float4 inBoneIndex	: BLENDINDICES0;
    float4 inBoneWeight	: BLENDWEIGHT0;
};

struct VS_INPUT_STATIC
{
	float4 Position		: POSITION0;
	float3 Normal		: NORMAL0;
	float2 Texcoord		: TEXCOORD0;
};

//Output for the outline vertex shader
struct VS_OUTPUT2
{
	float4 Position			: POSITION0;
	float4 Normal			: TEXCOORD1;
};

//The outline vertex shader
//This tranforms the model and
//"peaks" the surface (scales it out on it's normal) 
VS_OUTPUT2 Outline_Animated(VS_INPUT_ANIMATED Input)
{
	//Here's the important value. It determins the thickness of the outline.
	//The value is completely dependent on the size of the model.
	//My model is very tiny so my outine is very tiny.
	//You may need to increase this or better yet, caluclate it based on the distance
	//between your camera and your model.
	float offset = 0.1;
	
	// Calculate the final bone transformation matrix
    float4x3 matSmoothSkin = 0;
    matSmoothSkin += matBones[Input.inBoneIndex.x] * Input.inBoneWeight.x;
    matSmoothSkin += matBones[Input.inBoneIndex.y] * Input.inBoneWeight.y;
    matSmoothSkin += matBones[Input.inBoneIndex.z] * Input.inBoneWeight.z;
    matSmoothSkin += matBones[Input.inBoneIndex.w] * Input.inBoneWeight.w;
    
    // Combine skin and world transformations
    float4x4 matSmoothSkinWorld = 0;
    matSmoothSkinWorld[0] = float4(matSmoothSkin[0], 0);
    matSmoothSkinWorld[1] = float4(matSmoothSkin[1], 0);
    matSmoothSkinWorld[2] = float4(matSmoothSkin[2], 0);
    matSmoothSkinWorld[3] = float4(matSmoothSkin[3], 1);
    matSmoothSkinWorld = mul(matSmoothSkinWorld, matW);    
	
	float4x4 WorldViewProjection = mul(matSmoothSkinWorld, matVP);
	VS_OUTPUT2 Output;
	Output.Normal			= mul(Input.Normal, matSmoothSkinWorld); //POSSIBLE ERROR: dropped the cast to float3x3
	Output.Position			= mul(Input.Position, WorldViewProjection)+(mul(offset, mul(Input.Normal, WorldViewProjection)));
	
	return Output;
}

// For non-skinned geometry
VS_OUTPUT2 Outline_Static(VS_INPUT_STATIC Input)
{
	//Here's the important value. It determins the thickness of the outline.
	//The value is completely dependent on the size of the model.
	//My model is very tiny so my outine is very tiny.
	//You may need to increase this or better yet, caluclat it based on the distance
	//between your camera and your model.
	float offset = 0.4;
	
	float4x4 WorldViewProjection = mul(matW, matVP);
	
	VS_OUTPUT2 Output;
	Output.Normal			= mul(Input.Normal, matW);
	Output.Position			= mul(Input.Position, WorldViewProjection)+(mul(offset, mul(Input.Normal, WorldViewProjection)));
	
	return Output;
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
	float3 diffuseLightColor = float3(0,0,0);
	float3 specularLightColor = float3(0,0,0);
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
			float3 lightVector = lights[i].position - inPosition;
			float attenuation = saturate(1 - dot(lightVector/lightRadii[i], lightVector/lightRadii[i]));

			lightVector = normalize(lightVector);
			
			// Diffuse intensity
			float diffuseIntensity = saturate(dot(inNormal, lightVector)) * attenuation;
			float3 reflect = normalize(2*diffuseIntensity*inNormal - lightVector);
			float2 Tex = float2(diffuseIntensity, 0);
			CelColor += tex2D(CelMapSampler, Tex);
		
			// Specular intensity
			float specularIntensity = pow(saturate(dot(reflect, inEyeVector)), inSpecularPower) * attenuation;
		
			diffuseLightColor += (lights[i].color * diffuseIntensity);
			specularLightColor += (lights[i].color * specularIntensity);
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
	outColor0.rgb = material.emissiveColor + PhongShadingPS(lightCount, inPosition, normal,
		eyeVector, diffuseColor, material.specularColor, material.specularPower);
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
	outColor0.rgb = material.emissiveColor + PhongShadingPS(lightCount, inPosition, normal,
		eyeVector, diffuseColor, material.specularColor, material.specularPower);
}

void animatedModelGrayPS_Light(
	uniform int lightCount,
    in float3 inPosition	: TEXCOORD0,
    in float3 inNormal		: TEXCOORD1,
    in float2 inUV0			: TEXCOORD2,
    in float3 inEyeVector	: TEXCOORD3,

	out float4 outColor0	: COLOR0)
{
    // Normalize all input vectors
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
	outColor0.rgb = material.emissiveColor + PhongShadingPS(lightCount, inPosition, normal,
		eyeVector, diffuseColor, specularColor, material.specularPower);
		
	float luma = (outColor0.r * 0.3 + outColor0.g * 0.59 + outColor0.b * 0.11);
	outColor0.r = luma;
	outColor0.g = luma;
	outColor0.b = luma;
}

/*
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
*/

//This is the ouline pixel shader. It just outputs unlit black.
float4 Black() : COLOR
{
   return float4(0.0f, 0.0f, 0.0f, 1.0f);
}


// Techniques
// -------------------------------------------------

technique StaticModel_OneLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
	pass p0
	{
		AlphaBlendEnable = FALSE;
		
		VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(1);
        CullMode = CW;
	}
	
	pass p1
	{
		VertexShader = compile vs_2_0 Outline_Static();
		PixelShader  = compile ps_2_0 Black();
		CullMode = CCW;
	}
}

technique StaticModel_TwoLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(2);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_2_0 Outline_Static();
		PixelShader  = compile ps_2_0 Black();
		CullMode = CCW;
    }
}

technique StaticModel_ThreeLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(3);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Static();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CCW;
    }
}

technique StaticModel_FourLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(4);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Static();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CCW;
    }
}

technique StaticModel_FiveLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(5);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Static();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CCW;
    }
}

technique StaticModel_SixLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(6);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Static();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CCW;
    }
}

technique StaticModel_SevenLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(7);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Static();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CCW;
    }
}

technique StaticModel_EightLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 StaticModelVS_Light();
        PixelShader = compile ps_3_0 StaticModelPS_Light(8);
        CullMode = CW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Static();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CCW;
    }
}

technique AnimatedModel_NoLight 
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(0);
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_OneLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(1);
        CullMode = CCW;
    }
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_OneLight_Gray
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelGrayPS_Light(1);
        CullMode = CCW;
    }
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_TwoLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(2);
        CullMode = CCW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_ThreeLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(3);
        CullMode = CCW;
    }
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_FourLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(4);
        CullMode = CCW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_FiveLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(5);
        CullMode = CCW;
    }
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_SixLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(6);
        CullMode = CCW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_SevenLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(7);
        CullMode = CCW;
    }
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}

technique AnimatedModel_EightLight
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_Light();
        PixelShader = compile ps_3_0 animatedModelPS_Light(8);
        CullMode = CCW;
    }
    
    pass p1
    {
		VertexShader = compile vs_3_0 Outline_Animated();
		PixelShader  = compile ps_3_0 Black();
		CullMode = CW;
    }
}
/*
technique AnimatedModel_OneLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_3_0 animatedModelPS_LightWithNormal(1);
    }
}

technique AnimatedModel_TwoLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_0"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_3_0 animatedModelPS_LightWithNormal(2);
    }
}

technique AnimatedModel_FourLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_b animatedModelPS_LightWithNormal(4);
    }
}

technique AnimatedModel_SixLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_b animatedModelPS_LightWithNormal(6);
        
    }
}

technique AnimatedModel_EightLightWithNormal
< string vertexShaderProfile = "VS_2_0"; string pixelShaderProfile = "PS_2_b"; >
{
    pass p0
    {
		AlphaBlendEnable = FALSE;
		
        VertexShader = compile vs_3_0 AnimatedModelVS_LightWithNormal();
        PixelShader = compile ps_2_b animatedModelPS_LightWithNormal(8);
    }
}
*/

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
        VertexShader = compile vs_3_0 ShadowMapVertexShader();
        PixelShader = compile ps_3_0 ShadowMapPixelShader();
    }
}