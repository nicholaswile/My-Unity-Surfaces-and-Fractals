Shader "Fractal/Variety Fractal Surface GPU" {

	Properties {
		// _Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	
	SubShader {
		CGPROGRAM
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
		#pragma editor_sync_compilation
		#pragma target 4.5 

		// Compute shader
		#include "VarietyFractalGPU.hlsl"

		struct Input {
			float3 worldPos;
		};

		// float _Smoothness;

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo = GetColor().rgb;
			surface.Smoothness = GetColor().a;
		}
		ENDCG
	}

	Fallback "Diffuse"

}