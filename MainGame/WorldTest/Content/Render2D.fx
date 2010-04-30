float4x4 World;
float4x4 View;
float4x4 Projection;

float4 reticleColor;

float3 reticlePosition;

float reticleInnerRadius;
float reticleOuterRadius;

void ReticleVertexShader(
	in float4 Position			: POSITION,
	
	out float4 outSVPosition	: POSITION,
	out float3 outPosition		: TEXCOORD0)
{
	outPosition = mul(Position, World);
	outSVPosition = mul(float4(outPosition, 1.0f), mul(View, Projection));
}

void ReticlePixelShader(
	in float3 inPosition		: TEXCOORD0,
	
	out float4 outColor0		: COLOR0)
{
	float distToCenter = distance(inPosition, reticlePosition);
	
	outColor0 = reticleColor;
	
	if (distToCenter < reticleInnerRadius)
	{
		clip(-1);
	}
	else
	{
		float halfReticleBandWidth = (reticleOuterRadius - reticleInnerRadius) * 0.5f;
		float scaleVal = distToCenter - reticleInnerRadius;
		scaleVal = scaleVal / halfReticleBandWidth;
		
		if (scaleVal <= 0.5f)
		{
			outColor0.a = saturate(0.5f + scaleVal);
		}
		else
		{
			outColor0.a = saturate((1 - scaleVal) + 0.5f);
		}
	}
}

technique Reticle
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 ReticleVertexShader();
        PixelShader = compile ps_3_0 ReticlePixelShader();
    }
}
