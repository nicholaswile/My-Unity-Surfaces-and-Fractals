Shader "Graph/Point Surface 3D" {

	// Configurable in inspector
	Properties {
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	
	SubShader {
		CGPROGRAM
		// Tells shader compiler to generate surface shader with standard lighting and full support for shadows
		// We create the ConfigureSurface function
		#pragma surface ConfigureSurface Standard fullforwardshadows
		// Tells shader compiler to set the minimum target quality and level to 3.0
		#pragma target 3.0

		// We want to color points based on world position
		// These are the inputs we need
		struct Input {
			float3 worldPos;
		};

		// Fields 
		float _Smoothness;

		// Input: the data passed to the Shader
		// SurfaceOutputStandard: the surface congid data, both input and output (inout)
		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo = saturate(input.worldPos * 0.5 + 0.5);
			surface.Smoothness = _Smoothness;
		}
		ENDCG
	}

	Fallback "Diffuse"

}