Shader "Graph/Point Surface GPU" {

	Properties {
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	
	SubShader {
		CGPROGRAM
		// Function only invoked on drawing pass by default, 'addshadow' gives shadow pass.
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		// Enables procedural rendering. 
		// 'assumeuniformscaling': can ignore inverse transformations (unity_WorldToObject) since no nonuniform transformations are applied
		#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
		// Force Unity to compile shader when it is ready to be used
		// Because the dummy shader does not work with procedural drawing and may crash the program
		#pragma editor_sync_compilation
		// Target level 4.5 indicates we need compute shader support, OpenGL ES 3.1 or better.
		// Does not work on pre-DX11 GPUs, OpenGL ES 2.0 or 3.0, or WebGL.
		#pragma target 4.5 

		// Ours
		// Ensures the shader is compiled for procedural drawing
		#include "GraphColorGPU.hlsl"

		struct Input {
			float3 worldPos;
		};

		float _Smoothness;

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo = saturate(input.worldPos * 0.5 + 0.5);
			surface.Smoothness = _Smoothness;
		}
		ENDCG
	}

	Fallback "Diffuse"

}