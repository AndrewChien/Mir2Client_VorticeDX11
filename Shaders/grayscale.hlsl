// 转换后的ps_4.0版本
Texture2D tex0 : register(t0);
SamplerState sampler0 : register(s0);

float4 main(float2 uv : TEXCOORD) : SV_Target
{
    // 常量向量对应原始c0寄存器
    static const float3 luminanceWeights = float3(0.3, 0.59, 0.11);
    
    // 纹理采样替代tex指令
    float4 texColor = tex0.Sample(sampler0, uv);
    
    // 点积运算替代dp3指令
    float luminance = dot(texColor.rgb, luminanceWeights);
    
    // 保留原始alpha通道
    return float4(luminance, luminance, luminance, texColor.a);
}