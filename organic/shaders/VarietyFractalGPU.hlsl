#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3x4> _Matrices;
#endif
 
void ConfigureProcedural()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        float3x4 m = _Matrices[unity_InstanceID];
        unity_ObjectToWorld._m00_m01_m02_m03 = m._m00_m01_m02_m03;
        unity_ObjectToWorld._m10_m11_m12_m13 = m._m10_m11_m12_m13;
        unity_ObjectToWorld._m20_m21_m22_m23 = m._m20_m21_m22_m23;
        unity_ObjectToWorld._m30_m31_m32_m33 = float4(0.0, 0.0, 0.0, 1.0);
    #endif
}

float4 _BaseColor;
float4 _SecondaryColor;
float4 _SequenceNumbers;

float4 GetColor()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // return (unity_InstanceID % 20.0) / 19.0;
    // return frac(unity_InstanceID * 0.381); // Weyl Sequence
    float4 color;
    color.rgb = lerp(_BaseColor.rgb, _SecondaryColor.rgb, frac(unity_InstanceID * _SequenceNumbers.x + _SequenceNumbers.y));
    color.a = lerp(_BaseColor.a, _SecondaryColor.a, frac(unity_InstanceID * _SequenceNumbers.z + _SequenceNumbers.w));
    return color; // Random factor and offset
#else
    return _BaseColor;
#endif
}

void ShaderGraphFunction_float(float3 In, out float3 Out, out float4 Color)
{
    Out = In;
    Color = GetColor();
}

void ShaderGraphFunction_half(half3 In, out half3 Out, out float4 Color)
{
    Out = In;
    Color = GetColor();
}