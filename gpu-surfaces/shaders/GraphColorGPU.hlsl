// Only add positions declared in positions buffer if the shader is compiled for procedural drawing.
// This is used in the SRP GraphColorGPU.shader file as well as the URP GraphColorURPGPU shader graph.
// The URP shader graph does not support procedural drawing by default, but this code enables that.
// We do this by using the Custom Function node in the shader graph.
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3> _PositionsBuffer;
#endif

float _Step;

// unity_InstanceID is a globally accessible ID.
void ConfigureProcedural()
{
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float3 position = _PositionsBuffer[unity_InstanceID];

		// Create transformation matrix 
		// [ scale.x		0				0			translate.x	]
		// [ 0				scale.y			0			translate.y	]
		// [ 0				0				scale.z		translate.z	]
		// [ 0				0				0			1			]
		unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);	// Translate
		unity_ObjectToWorld._m00_m11_m22 = _Step;						// Scale	
	#endif
}

// This function is what we will call from the Shader Graph, it does not change the data. 
void ShaderGraphFunction_float(float3 In, out float3 Out)
{
    Out = In;
}

void ShaderGraphFunction_half(half3 In, out half3 Out)
{
    Out = In;
}