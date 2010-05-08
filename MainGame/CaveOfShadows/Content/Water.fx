#define MAX_LIGHTS 8

float4x4 xWorld;
float4x4 xView;
float4x4 xProjection;

// TODO: add effect parameters here.

struct PointLight
{
    float3 position;
    float3 color;
};

float xWaveLength;
float xWaveHeight;
float3 xCamPos;
float xTime;
float3 xWindDirection;
float xWindForce;
float3 xLightPosition1;
float3 xLightPosition2;

float3 lightColor1;
float3 lightColor2;

PointLight lights[MAX_LIGHTS];
int lightCount;

float4x4 xReflectionView;
Texture xReflectionMap;

sampler ReflectionSampler = sampler_state { 
	texture = <xReflectionMap> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = mirror; 
	AddressV = mirror;
};

Texture xRefractionMap;

sampler RefractionSampler = sampler_state { 
	texture = <xRefractionMap> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter=LINEAR; 
	AddressU = mirror; 
	AddressV = mirror;
};

Texture xWaterBumpMap;

sampler WaterBumpMapSampler = sampler_state { 
	texture = <xWaterBumpMap> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter=LINEAR; 
	AddressU = mirror; 
	AddressV = mirror;
};

Texture xBarrierMap;

sampler BarrierSampler = sampler_state { 
	texture = <xBarrierMap> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = mirror; 
	AddressV = mirror;
};


//------- Technique: Water --------
struct WVertexToPixel
{
    float4 Position                 : POSITION;
    float4 ReflectionMapSamplingPos    : TEXCOORD1;
    float2 BumpMapSamplingPos        : TEXCOORD2;
    float4 RefractionMapSamplingPos : TEXCOORD3;
    float4 Position3D                : TEXCOORD4;


};

struct WPixelToFrame
{
    float4 Color : COLOR0;
};


WVertexToPixel WaterVS(float4 inPos : POSITION, float2 inTex: TEXCOORD)
{    
    WVertexToPixel Output = (WVertexToPixel)0;

    float4x4 preViewProjection = mul (xView, xProjection);
    float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);
    float4x4 preReflectionViewProjection = mul (xReflectionView, xProjection);
    float4x4 preWorldReflectionViewProjection = mul (xWorld, preReflectionViewProjection);

    Output.Position = mul(inPos, preWorldViewProjection);
    Output.ReflectionMapSamplingPos = mul(inPos, preWorldReflectionViewProjection);
	float2 moveVector = float2(0, xTime*xWindForce);
	Output.BumpMapSamplingPos = (inTex + moveVector)/xWaveLength;

	
	Output.RefractionMapSamplingPos = mul(inPos, preWorldViewProjection);
	Output.Position3D = mul(inPos, xWorld);

    return Output;
}

WPixelToFrame WaterPS(WVertexToPixel PSIn)
{
    WPixelToFrame Output = (WPixelToFrame)0;        
    
    float2 ProjectedTexCoords;
    ProjectedTexCoords.x = PSIn.ReflectionMapSamplingPos.x/PSIn.ReflectionMapSamplingPos.w/2.0f + 0.5f;
    ProjectedTexCoords.y = -PSIn.ReflectionMapSamplingPos.y/PSIn.ReflectionMapSamplingPos.w/2.0f + 0.5f;    

	float4 bumpColor = tex2D(WaterBumpMapSampler, PSIn.BumpMapSamplingPos);
    float2 perturbation = xWaveHeight*(bumpColor.rg - 0.5f)*2.0f;
    float2 perturbatedTexCoords = ProjectedTexCoords + perturbation;
    
    float4 reflectiveColor = tex2D(ReflectionSampler, perturbatedTexCoords); 
    
    float2 ProjectedRefrTexCoords;
	ProjectedRefrTexCoords.x = PSIn.RefractionMapSamplingPos.x/PSIn.RefractionMapSamplingPos.w/2.0f + 0.5f;
	ProjectedRefrTexCoords.y = -PSIn.RefractionMapSamplingPos.y/PSIn.RefractionMapSamplingPos.w/2.0f + 0.5f;    
	
	float2 perturbatedRefrTexCoords = ProjectedRefrTexCoords + perturbation;    
	float4 refractiveColor = tex2D(RefractionSampler, perturbatedRefrTexCoords); 
	
	float3 eyeVector = normalize(xCamPos - PSIn.Position3D);
    float3 normalVector = (bumpColor.rbg-0.5f)*2.0f;
    float fresnelTerm = dot(eyeVector, normalVector);
	
    float4 combinedRefrRefl = lerp(reflectiveColor, refractiveColor, fresnelTerm);
    float4 dullColor = float4(0.3f, 0.3f, 0.5f, 1.0f);
    Output.Color = lerp(combinedRefrRefl, dullColor, 0.2f);
    
    //float specular = (float)0;
    for(int i=0; i<lightCount; i++){
		float3 lightVector1 = lights[i].position - PSIn.Position3D;
		float3 reflectionVector1 = -reflect(normalize(lightVector1), normalVector);
		float specular1 = dot(normalize(reflectionVector1), normalize(eyeVector));
		specular1 = pow(specular1, 200);
		Output.Color.rgb += specular1 * lights[i].color;
    }
	//specular1 = pow(specular1, 200);        
	//Output.Color.rgb += specular1 * lightColor1;
	
	//float3 lightVector2 = xLightPosition2 - PSIn.Position3D;
	//float3 reflectionVector2 = -reflect(normalize(lightVector2), normalVector);
	//float specular2 = dot(normalize(reflectionVector2), eyeVector);
	//specular2 = pow(specular2, 256);
	//Output.Color.rgb += specular2 * lightColor2;

    return Output;
}

technique Water
{
    pass Pass0
    {
        VertexShader = compile vs_3_0 WaterVS();
        PixelShader = compile ps_3_0 WaterPS();
    }
}

/////////////////////////////////////////////////////////////////////////
// Barrier
/////////////////////////////////////////////////////////////////////////

struct BVertexToPixel
{
    float4 Position                 : POSITION;
    float2 BarrierMapSamplingPos    : TEXCOORD1;
    float2 BumpMapSamplingPos        : TEXCOORD2;
    float4 Position3D                : TEXCOORD3;
};

struct BPixelToFrame
{
    float4 Color : COLOR0;
};

BVertexToPixel BarrierVS(float4 inPos : POSITION, float2 inTex: TEXCOORD)
{    
    BVertexToPixel Output = (BVertexToPixel)0;

    float4x4 preViewProjection = mul (xView, xProjection);
    float4x4 preWorldViewProjection = mul (xWorld, preViewProjection);

    Output.Position = mul(inPos, preWorldViewProjection);
    Output.BarrierMapSamplingPos = inTex;
	float2 moveVector = float2(0, xTime*xWindForce);
	Output.BumpMapSamplingPos = (inTex + moveVector)/xWaveLength;
	
	Output.Position3D = mul(inPos, xWorld);

    return Output;
}

BPixelToFrame BarrierPS(BVertexToPixel PSIn)
{
    WPixelToFrame Output = (WPixelToFrame)0;        
    
	float4 bumpColor = tex2D(WaterBumpMapSampler, PSIn.BumpMapSamplingPos);
    float2 perturbation = xWaveHeight*(bumpColor.rg - 0.5f)*2.0f;
    float2 perturbatedTexCoords = PSIn.BarrierMapSamplingPos + perturbation; 
       
	float4 barrierColor = tex2D(BarrierSampler, perturbatedTexCoords); 
	
	float3 eyeVector = normalize(xCamPos - PSIn.Position3D);
    float3 normalVector = (bumpColor.rbg-0.5f)*2.0f;
    float fresnelTerm = dot(eyeVector, normalVector);
	
    float4 combinedColor = lerp(barrierColor, barrierColor + float4(0.1,0.1,0.1,0.0), fresnelTerm);
    
    //for(int i=0; i<lightCount; i++){
	//	float3 lightVector1 = lights[i].position - PSIn.Position3D;
	//	float3 reflectionVector1 = -reflect(normalize(lightVector1), normalVector);
	//	float specular1 = dot(normalize(reflectionVector1), normalize(eyeVector));
	//	specular1 = pow(specular1, 200);
	//	Output.Color.rgb += specular1 * lights[i].color;
    //}
	Output.Color.rgb = combinedColor.rgb;
	Output.Color.a = 0.7;
    return Output;
}

technique Barrier
{
    pass Pass0
    {
		AlphaBlendEnable = TRUE;
        VertexShader = compile vs_3_0 BarrierVS();
        PixelShader = compile ps_3_0 BarrierPS();
        Cullmode = none;
    }
}