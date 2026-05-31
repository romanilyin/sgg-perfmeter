Shader "Hidden/SGG/PerfMeter/OverdrawCounter"
{
	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "17.4.0"
		}

		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			Name "OverdrawCounter"
			Tags
			{
				"LightMode" = "SRPDefaultUnlit"
			}

			ZWrite Off
			ZTest Always
			Cull Back
			ColorMask 0

			HLSLPROGRAM
			#pragma target 5.0
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			RWStructuredBuffer<uint> _SGGPerfMeterOverdrawCounter : register(u1);

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				InterlockedAdd(_SGGPerfMeterOverdrawCounter[0], 1u);
				return half4(0.0h, 0.0h, 0.0h, 0.0h);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
