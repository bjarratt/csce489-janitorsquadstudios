float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 VI;

float time_0_X;
float waveSpeed=0.014f; //0.034f
float noiseSpeed=0.018f;
float fadeBias=0.8f;
float4 waterColor;

texture Noise_tex;
texture skyBox_Tex;


sampler Noise = sampler_state
{
   Texture = (Noise_tex);
   ADDRESSU = WRAP;
   ADDRESSV = WRAP;
   ADDRESSW = WRAP;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
};
sampler skyBox = sampler_state
{
   Texture = (skyBox_Tex);
   ADDRESSU = CLAMP;
   ADDRESSV = CLAMP;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
};

struct VertexShaderOutput
{
    float4 Pos : POSITION;
    float3 pos :TEXCOORD0;
    float3 normal: TEXCOORD1;
    float3 vVec: TEXCOORD2;
    
};

VertexShaderOutput VertexShaderFunction( float4 Position : POSITION,float3 normal: NORMAL,float3 texCoord: TEXCOORD0)
{
    VertexShaderOutput output;
    
     float4 worldPosition = mul(Position, World);
   
     output.Pos = mul(mul(worldPosition,View), Projection);
   
     output.normal=normalize(mul(normal,World));
    
     //float3 EyePosition=mul(-View._m30_m31_m32,transpose(View));
     
     //output.vVec =worldPosition - EyePosition;
     
     output.vVec =VI[3].xyz - worldPosition;
   
     output.pos=texCoord*10;
     
     return output;
}

float4 PixelShaderFunction( float4 Pos : POSITION,float3 pos :TEXCOORD0,float3 normal: TEXCOORD1,float3 vVec: TEXCOORD2) : COLOR
{
    
    pos.x += waveSpeed  * time_0_X;
    pos.z += (noiseSpeed * time_0_X);

    float4 noisy = tex3D(Noise,pos);
    
    float3 bump =2 * noisy - 1;
    bump.xy *= 0.15;
    bump.z = 0.8 * abs(bump.z) + 0.2;
    bump =normalize(bump+normal);
    
   
   float3 reflVec =reflect(vVec,bump);
   float4 refl = texCUBE(skyBox,reflVec);

   return lerp(waterColor, refl,fadeBias);
  
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
