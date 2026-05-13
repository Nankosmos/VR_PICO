Shader "Custom/Particle/Particle_Stander2"
{
	Properties
	{
		//	主贴图
		//[Header(MainTex Setting)]
		_MainTex("MainTex (主贴图)", 2D) = "white" {}
		[Gamma][HDR]_Color("Color (主颜色)", Color) = (1, 1, 1, 1)
		_Luminance("Luminance (伪HDR，HDR开启时不设置)", Range(-10.0, 10.0)) = 0
		//	主贴图旋转
		_MainTexRotator("MainTex Rotator (旋转角度)", Range(0, 360)) = 0
		//	UV流动
		[Toggle(_PARTICLE_UV_FLOW_ON)] _ParticleUVAnimEnable("Enable Particle UV Animation (启用粒子Custom1.xy)", Float) = 0
		_UVFlowSpeedU("UV Animation Speed U (主贴图水平速度)", Range(-100.0, 100.0)) = 0
		_UVFlowSpeedV("UV Animation Speed V (主贴图垂直速度)", Range(-100.0, 100.0)) = 0
		//	双面
		[Toggle(_DOUBLE_SIDE_ON)] _DoubleSideEnable("Enable Double Side (启用双面)", Float) = 0
		[Gamma][HDR]_BackColor("Back Color (背面颜色)", Color) = (1, 1, 1, 1)

		//	溶解
		//[Header(Dissolution)]
		[Toggle(_DISSOLUTION_EDGE_ON)] _DissolutionEdgeEnable("Enable Dissolution (启用溶解)", Float) = 0
		_DissolutionTex("Dissolution Texture (R,A 溶解贴图)", 2D) = "white" {}
		[HDR]_DissolutionEdgeColor("Dissolution Edge Color (溶解边缘颜色)", Color) = (1,1,1,1)
		_Luminance2("Luminance2 (边缘颜色伪HDR，HDR开启时不设置)", Range(-10.0, 10.0)) = 0
		[Toggle(_PARTICLE_DISSOLUTION_RANGE_ON)] _ParticleCustomDissolutionRange("Enable Custom Range (启用粒子Custom1.z)", Float) = 0
		[Toggle] _DisturbEffectDissolutionEnable("Disturb Effect Dissolution (扰动影响溶解)", Float) = 0
		_DissolutionRange("Dissolution Range (溶解范围)", Range(0.0, 1.1)) = 0.5
		_DissolutionEdgeWidth("Dissolution Edge Width (溶解宽度)", Range(0.0, 1.0)) = 0.1
		_DissolutionEdgeSoftRange("Dissolution Edge Soft Range (溶解边缘软化度)", Range(0.0, 10.0)) = 0
		//	UV流动
		[Toggle(_PARTICLE_DISSOLUTION_UV_ON)] _ParticleCustomDissolutionUV("Enable Custom Data (启用粒子Custom1.xy)", Float) = 0
		_DissolutionAnimSpeedU("Dissolution Animation Speed U (溶解贴图水平速度)", Range(-100.0, 100.0)) = 0
		_DissolutionAnimSpeedV("Dissolution Animation Speed V (溶解贴图垂直速度)", Range(-100.0, 100.0)) = 0
		//	定向溶解
		[Toggle(_DISSOLUTION_SCALE_ON)] _DissolutionScaleEnable("Enable Scale (启用定向溶解)", Float) = 0
		_WorldSpaceScale("WorldSpaceScale（方向xyz + 程度w）", vector) = (0, 0, 0, 0)
		//	反向溶解
		[Toggle] _ReverseDissolution("Reverse Dissolution (启用反向溶解)", Float) = 0

		//	扭曲
		//[Header(Interference)]
		[Toggle(_INTERFERENCE_ON)] _InterferenceEnable("Enable Interference (启用扭曲)", Float) = 0
		_InterferenceTex("InterferenceTex (R,G 扭曲贴图)", 2D) = "gray" {}
		_InterferenceIntensity("Interference Intensity (扭曲强度)", Range(0.0, 3.0)) = 1
		_InterferenceAnimSpeedU("Interference Animation Speed U (扭曲贴图水平速度)", Range(-100.0, 100.0)) = 0
		_InterferenceAnimSpeedV("Interference Animation Speed V (扭曲贴图垂直速度)", Range(-100.0, 100.0)) = 0

		//	极坐标
		//[Header(Polar)]
		[Toggle(_POLAR_ON) ]_OpenPolar("Open Polar (开启极坐标)", Float) = 0
		_Opacity("Opacity (w必须为整数)", vector) = (0, 0, 1, 1)
		
		//	雾效
		//[Header(Fog)]
		[Toggle] _SetFog("Fog (开启雾效)", Float) = 0

		//	菲涅尔
		//[Header(Fresnel)]
		[Toggle(_FRESNEL_ON)] _FresnelEnable("Enable Fresnel (启用菲涅耳)", Float) = 0
		[Gamma][HDR] _FresnelColor("FresnelColor (菲涅耳颜色)", Color) = (1,1,1,1)
		_FresnelIntensity("FresnelIntensity (菲涅耳强度)", Range(0.0001, 10.0)) = 1
		[Toggle] _DFresnelEnable("DFresnel (反向菲涅耳)", Float) = 0
		[Toggle] _DFresnelAlphaEnable("FresnelAlpha (弃用菲涅耳控制透明度)", Float) = 0

		//软粒子
		[Toggle(_DepthRenderEnable_ON)] _DepthRenderEnable("软粒子接受开启/关闭",Float) = 0
		_SoftNearFade("Soft Particles 起始值", Float) = 0.0
        _SoftFarFade("Soft Particles 结束值", Float) = 1.0    

		//	副贴图
		//[Header(SecondTex Setting)]
		[Toggle(_SECONDTEX_ON)] _OpenSecondTex("_Open SecondTex (开启副贴图)", Float) = 0
		_SecondTex("SecondTex (副贴图)", 2D) = "white" {}
		//	副贴图旋转
		_SecondTexRotator("SecondTex Rotator (旋转角度)", Range(0, 360)) = 0
		//	副贴图UV流动
		_UV2FlowSpeedU("UV2 Animation Speed U (副贴图水平速度)", Range(-100.0, 100.0)) = 0
		_UV2FlowSpeedV("UV2 Animation Speed V (副贴图垂直速度)", Range(-100.0, 100.0)) = 0
		//	副扭曲贴图
		[Toggle(_INTERFERENCE2_ON)] _Interference2Enable("Enable Interferenc2e (启用副贴图扭曲)", Float) = 0
		_Interference2Tex("Interference2Tex (R,G 副扭曲贴图)", 2D) = "gray" {}
		_Interference2Intensity("Interference2 Intensity (扭曲2强度)", Range(0.0, 3.0)) = 1
		_Interference2AnimSpeedU("Interference2 Animation Speed U (副扭曲贴图水平速度)", Range(-100.0, 100.0)) = 0
		_Interference2AnimSpeedV("Interference2 Animation Speed V (副扭曲贴图垂直速度)", Range(-100.0, 100.0)) = 0

		//	序列帧
		//[Header(Sheet Animation)]
		[Toggle(_TEXTURE_SHEET_ANIM_ON)] _TextureSheetAnimEnable("Enable Texture Sheet Animation (启用序列帧)", Float) = 0
		_AnimTilingX("Animation Tiling X (水平数量)", float) = 1
		_AnimTilingY("Animation Tiling Y (垂直数量)", float) = 1
		_AnimFrameCount("Animation Frame Count (总帧数 小于 X * Y)", float) = 1
		_AnimStartFrame("Animation Start Frame (开始帧 0开始)", float) = 0
		_AnimFrameTime("Animation Frame Time (速度)", float) = 2	

		//	遮罩裁剪
		//[Header(Mask Clip)]
		[Toggle(_MASK_TEXTURE_ON)] _MaskTextureEnable("Enable Mask Texture (启用遮罩裁剪)", Float) = 0
		_MaskTex("Mask Texture (遮罩图)", 2D) = "white" {}
		[Toggle] _MaskChannel("Mask Channel (通道)", Float) = 0
		_MaskTexAnimSpeedU("MaskTex Animation Speed U (遮罩图水平速度)", Float) = 0
		_MaskTexAnimSpeedV("MaskTex Animation Speed V (遮罩图垂直速度)", Float) = 0
		//	遮罩图旋转
		_MaskTexRotator("Mask Rotator (旋转角度)", Range(0, 360)) = 0
		//	透明度裁剪
		//[Header(Alpha Clip)]
		[Toggle] _UseUIAlphaClip("Use Alpha Clip (开启透明度裁剪)", Float) = 0
		_AlphaCullingBias("Alpha Culling Bias (透明度阈值)", Range(0.0, 1.0)) = 0.001

		//	以下为shader内置设置项
		//[Header(Blend Mode)]
		[Enum(UnityEngine.Rendering.BlendOp)]  _BlendOp  ("BlendOp (混合操作)", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend (混合模式src)", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend (混合模式dst)", Float) = 10

		//[Header(Cull Mode)]
		[Enum(UnityEngine.Rendering.CullMode)] _FaceCull("FaceCull (剔除模式)", Float) = 2

		//[Header(Z Mode)]
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest (深度测试)", Float) = 2
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite (深度写入)", Float) = 0

		//[Header(Offset)]
		_OffsetFactor("OffsetFactor (缩放最大Z斜率)", Range(-1, 1)) = 0
		_OffsetUnits("OffsetUnits (最小可分辨深度缓冲区值)", Range(-1, 1)) = 0

		//[Header(Color Mask)]
		_ColorMask("Color Mask (颜色输出蒙版)", Float) = 15

		//[Header(Stencil)]
        [IntRange]_StencilID ("Stencil ID (模板ID)", Range(0, 255)) = 0
        [IntRange]_StencilReadMask ("Stencil Read Mask (读mask)", Range(0, 255)) = 255
        [IntRange]_StencilWriteMask ("Stencil Write Mask (写mask)", Range(0, 255)) = 255
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison (模板测试函数)", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass (测试成功的操作)", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail (测试失败的操作)", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail (测试深度失败的操作)", Float) = 0
	}
	
	Category	//Category 代码块可对设置渲染状态的命令进行分组，这样您可以“继承”该代码块内的分组渲染状态。
	{
		Tags
		{ 
			"RenderPipline" = "UniversalPipeline"
			"RenderType" = "Transparent" 
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True"		//我们不希望任何投影类型材质或者贴图，影响我们的物体或者着色器。
			"PreviewType" = "Plane"			//指示材质检视面板预览应如何显示材质
		}
		
		Lighting Off 
		ZWrite [_ZWrite]
		ZTest[_ZTest]
		BlendOp [_BlendOp]
		Blend [_SrcBlend] [_DstBlend]
		Cull [_FaceCull]
		ColorMask [_ColorMask]
		Offset [_OffsetFactor], [_OffsetUnits]

		SubShader
		{
			Stencil
            {
                Ref [_StencilID]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }

			HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			ENDHLSL

			Pass
			{
				Tags 
				{
					"LightMode" = "UniversalForward"
				}

				HLSLPROGRAM
				#include "../Fog/Fog.hlsl"
				
				#pragma vertex vert
				#pragma fragment frag

				#pragma shader_feature_local_fragment _POLAR_ON
				#pragma shader_feature_local_fragment _FRESNEL_ON
				#pragma shader_feature_local_fragment _DOUBLE_SIDE_ON
				#pragma shader_feature_local _SECONDTEX_ON
				#pragma shader_feature_local _MASK_TEXTURE_ON
				#pragma shader_feature_local _INTERFERENCE_ON
				#pragma shader_feature_local _INTERFERENCE2_ON
				#pragma shader_feature_local _DISSOLUTION_EDGE_ON
				#pragma shader_feature_local _DISSOLUTION_SCALE_ON
				#pragma shader_feature_local _TEXTURE_SHEET_ANIM_ON
				#pragma shader_feature_local_vertex  _PARTICLE_UV_FLOW_ON
				#pragma shader_feature_local_vertex _PARTICLE_DISSOLUTION_UV_ON
				#pragma shader_feature_local _PARTICLE_DISSOLUTION_RANGE_ON
				#pragma shader_feature_local_fragment _DepthRenderEnable_ON
				
				TEXTURE2D (_MainTex);			SAMPLER(sampler_MainTex);
				TEXTURE2D (_SecondTex);			SAMPLER(sampler_SecondTex);
				TEXTURE2D (_MaskTex);			SAMPLER(sampler_MaskTex);
				TEXTURE2D (_DissolutionTex);	SAMPLER(sampler_DissolutionTex);
				TEXTURE2D (_InterferenceTex);	SAMPLER(sampler_InterferenceTex);
				TEXTURE2D (_Interference2Tex);	SAMPLER(sampler_Interference2Tex);
				TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
				
				CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _SecondTex_ST;
				float4 _MaskTex_ST;
				float4 _DissolutionTex_ST;
				float4 _InterferenceTex_ST;
				float4 _Interference2Tex_ST;

				half4 _BackColor;

				half4 _Color;
				half _MainTexRotator;
				half _Luminance;

				float _UVFlowSpeedU;
				float _UVFlowSpeedV;

				half _MaskChannel;
				half _MaskTexAnimSpeedU;
				half _MaskTexAnimSpeedV;
				half _MaskTexRotator;

				half _InterferenceIntensity;
				float _InterferenceAnimSpeedU;
				float _InterferenceAnimSpeedV;

				float _UV2FlowSpeedU;
				float _UV2FlowSpeedV;
				half _Interference2Intensity;
				float _Interference2AnimSpeedU;
				float _Interference2AnimSpeedV;
				half _SecondTexRotator;

				half _FresnelIntensity;
				half _DFresnelEnable;
				half _DFresnelAlphaEnable;
				half4 _FresnelColor;

				half4 _DissolutionEdgeColor;
				half _Luminance2;
				half _DisturbEffectDissolutionEnable;
				half _DissolutionRange;
				half _DissolutionEdgeWidth;
				half _DissolutionEdgeSoftRange;
				float _DissolutionAnimSpeedU;
				float _DissolutionAnimSpeedV;
				half _ReverseDissolution;
				half4 _WorldSpaceScale;

				half _AnimTilingX;
				half _AnimTilingY;
				half _AnimFrameCount;
				half _AnimStartFrame;
				half _AnimFrameTime;

				half _SetFog;

				half _UseUIAlphaClip;
				half _AlphaCullingBias;

				half4 _Opacity;

				half _SoftNearFade;
				half _SoftFarFade;
				CBUFFER_END

				// 顶点数据
				struct Attributes 
				{
					float4 positionOS		: POSITION;
					float3 normalOS			: NORMAL;
					half4 color				: COLOR;
					float2 uv				: TEXCOORD0;

				#if defined(_PARTICLE_UV_FLOW_ON) || defined(_DISSOLUTION_EDGE_ON)
					float4 particleInfo		: TEXCOORD1;	//粒子组件Custom Data，必须固定在TEXCOORD1内。
				#endif
					//float4 particle2Info	: TEXCOORD2;	//如果有后续需要粒子自定义时可开放
				};

				// 片元数据
				struct Varings 
				{
					float4 positionCS					: SV_POSITION;
					half4 color							: COLOR;
					float4 MainUV_tiledAnimCoord		: TEXCOORD0;
					float3 positionWS					: TEXCOORD1;
					float3 normalWS						: TEXCOORD2;
					float3 viewDirWS					: TEXCOORD3;
					float4 screenPos					: TEXCOORD4;

				#if defined(_DISSOLUTION_EDGE_ON) || defined(_PARTICLE_DISSOLUTION_RANGE_ON)
					float4 dissolutionTexcoord			: TEXCOORD5;
				#endif

				#if defined(_MASK_TEXTURE_ON) || defined(_INTERFERENCE_ON)
					float4 maskUV_InterferenceUV		: TEXCOORD6;
				#endif

				#if defined(_SECONDTEX_ON)
					float4 SecondTexUV_InterferenceUV	: TEXCOORD7;
				#endif
					
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
				{
				    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
				    return inside.x * inside.y;
				}

				inline float SoftUnityGet2DClipping (in float2 position, in float4 clipRect)
				{
					float _ClipSoftX = 0.001;
					float _ClipSoftY = 0.001;
					float2 xy = (position.xy - clipRect.xy) / float2(_ClipSoftX, _ClipSoftY) * step(clipRect.xy, position.xy);
					float2 zw = (clipRect.zw - position.xy) / float2(_ClipSoftX, _ClipSoftY) * step(position.xy, clipRect.zw);
					float2 factor = clamp(0, zw, xy);
					return saturate(min(factor.x, factor.y));
				}

				//	直角坐标系 转 极坐标
				inline float2 Polar(in float2 UV, in float2 tex_ST)
				{
					//	0~1的1象限转-0.5~0.5的四象限
					float2 uv = UV - float2(0.5 * tex_ST.x, 0.5 * tex_ST.y);
					//	各个象限坐标到0点距离，数值为0~0.5；
					float distance = length(uv);
					//	0~0.5放大到0~1；
					distance = distance * _Opacity.z + frac(_Opacity.x * _Time.x);
					//	4象限坐标求弧度范围是[-pi, pi]
					float angle = atan2(uv.x, uv.y) * _Opacity.w + _Opacity.y * _Time.x;
					//	把[-pi, pi]转换为0~1
					float angle01 = angle / 3.1415926 / 2 + 0.5;
					//	输出角度与距离
					return float2(angle01, distance);
				}
				
				//	旋转uv
				inline float2 GetRotatorUV(in float2 uv, in float4 tex_ST, in half rotator)
				{
					float MtRotator = rotator * 6.284 / 360.0;
					float MtRotator_cos = cos(MtRotator);
					float MtRotator_sin = sin(MtRotator);
					return mul(uv - float2(tex_ST.x / 2, tex_ST.y / 2), 
						float2x2(MtRotator_cos, -MtRotator_sin, MtRotator_sin, MtRotator_cos)) +
						float2(tex_ST.x / 2, tex_ST.y / 2);
				}

				//	扭曲uv
				inline float2 InterferenceUV(float2 uv, float2 flowUv, TEXTURE2D_PARAM(tex, sampler_tex), float Intensity)
				{
					float2 interferenceUV = uv;
					interferenceUV -= flowUv * _Time.x;
					interferenceUV = (SAMPLE_TEXTURE2D(tex, sampler_tex, interferenceUV).rg - 0.5) * 2 * Intensity;
					return interferenceUV;
				}

				//计算深度(软粒子)
				#define COMPUTE_EYEDEPTH(o) o = -TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz)).z

				//	顶点着色器
				Varings vert(Attributes input)
				{
					Varings output = (Varings)0;
					UNITY_SETUP_INSTANCE_ID(input);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

					output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
					output.color = input.color;
					
					output.MainUV_tiledAnimCoord.xy = TRANSFORM_TEX(input.uv, _MainTex);
					half3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
					output.positionWS = positionWS;

					output.screenPos = ComputeScreenPos(output.positionCS);
					COMPUTE_EYEDEPTH(output.screenPos.z);

					//	副贴图
				#if defined(_SECONDTEX_ON)
					output.SecondTexUV_InterferenceUV.xy = TRANSFORM_TEX(input.uv, _SecondTex);

					//	副贴图流动
					output.SecondTexUV_InterferenceUV.xy -= float2(_UV2FlowSpeedU, _UV2FlowSpeedV) * _Time.x;

					//	副扭曲
					#if defined(_INTERFERENCE2_ON)
						output.SecondTexUV_InterferenceUV.zw = TRANSFORM_TEX(input.uv, _Interference2Tex);
					#endif
				#endif

					//	遮罩
				#if defined(_MASK_TEXTURE_ON)
					output.maskUV_InterferenceUV.xy = TRANSFORM_TEX(input.uv, _MaskTex);
				#endif

					//	扭曲
				#if defined(_INTERFERENCE_ON)
					output.maskUV_InterferenceUV.zw = TRANSFORM_TEX(input.uv, _InterferenceTex);
				#endif
					
					output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
					output.normalWS = normalize(TransformObjectToWorldDir(input.normalOS));

					//	溶解
				#if defined(_DISSOLUTION_EDGE_ON)
					float2 finaldissUV = float2(0, 0);
					output.dissolutionTexcoord.xy = float2(TRANSFORM_TEX(input.uv, _DissolutionTex));

					//	溶解UV流动
					finaldissUV = -float2(_DissolutionAnimSpeedU, _DissolutionAnimSpeedV) * _Time.x;// * sign(_DissolutionAnimSpeedU + _DissolutionAnimSpeedV);
					
					//	粒子CustomData1.xy控制溶解UV流动
					#if defined(_PARTICLE_DISSOLUTION_UV_ON)
						finaldissUV = float2(input.particleInfo.x, input.particleInfo.y);
					#endif

					//	粒子CustomData1.z控制溶解范围
					#if defined(_PARTICLE_DISSOLUTION_RANGE_ON)
						output.dissolutionTexcoord.z = input.particleInfo.z;
					#endif

					output.dissolutionTexcoord.xy += finaldissUV;

					//	定向溶解
					#if defined(_DISSOLUTION_SCALE_ON)
						float3 rootPos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
						float posOffset = dot(normalize(_WorldSpaceScale.xyz), (positionWS - rootPos)) / 2;
						output.dissolutionTexcoord.w = posOffset;
					#endif
				#endif

					//	序列帧uv
				#if defined(_TEXTURE_SHEET_ANIM_ON)
					float texIndex = fmod(floor(_Time.y / _AnimFrameTime + _AnimStartFrame), _AnimFrameCount);
					output.MainUV_tiledAnimCoord.z = (output.MainUV_tiledAnimCoord.x + fmod(texIndex, _AnimTilingX)) / _AnimTilingX;
					output.MainUV_tiledAnimCoord.w = (output.MainUV_tiledAnimCoord.y + floor(_AnimTilingY - (texIndex + 0.01) / _AnimTilingX)) / _AnimTilingY;
				#endif

					//	uv流动
					float2 finalFlowUV;
					float2 uvAnimOffset = float2(_UVFlowSpeedU, _UVFlowSpeedV) * _Time.x;
					finalFlowUV = uvAnimOffset;

					//	序列帧uv	
				#if defined(_TEXTURE_SHEET_ANIM_ON)
					output.MainUV_tiledAnimCoord.zw -= uvAnimOffset;
				#endif

					//	粒子自定义数据uv流动
				#if defined(_PARTICLE_UV_FLOW_ON)
					float2 particleUVAnimOffset = input.particleInfo.xy;
					finalFlowUV = particleUVAnimOffset;

					//	序列帧uv
					#if defined(_TEXTURE_SHEET_ANIM_ON)
						output.MainUV_tiledAnimCoord.zw -= particleUVAnimOffset;
					#endif
				#endif

					output.MainUV_tiledAnimCoord.xy -= finalFlowUV;

					return output;
				}
				
				//	片元着色器
				half4 frag(Varings input, half facing : VFACE) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(input);

					float2 MainUv = input.MainUV_tiledAnimCoord.xy;

					//	序列帧
				#if defined(_TEXTURE_SHEET_ANIM_ON)
					MainUv = input.MainUV_tiledAnimCoord.zw;
				#endif

					//	扭曲
				#if defined(_INTERFERENCE_ON)
					float2 interferenceUV = InterferenceUV(input.maskUV_InterferenceUV.zw, float2(_InterferenceAnimSpeedU, _InterferenceAnimSpeedV), TEXTURE2D_ARGS(_InterferenceTex, sampler_InterferenceTex), _InterferenceIntensity);
					MainUv += interferenceUV;
					
					#if defined (_DISSOLUTION_EDGE_ON)
					input.dissolutionTexcoord.xy += interferenceUV * _DisturbEffectDissolutionEnable;
					#endif
				#endif

					//	极坐标
				#if defined(_POLAR_ON)
					MainUv = Polar(MainUv, _MainTex_ST.xy);
				#endif

					//	旋转主贴图uv
					MainUv = GetRotatorUV(MainUv, _MainTex_ST, _MainTexRotator);

					//	主贴图
					half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, MainUv);
					col *=  input.color;							//	顶点色+染色
					col.rgb = col.rgb * pow(2, _Luminance);	//	伪HDR

					//	副贴图
				#if defined(_SECONDTEX_ON)
					float2 SecondUv = input.SecondTexUV_InterferenceUV.xy;

					//	副贴图旋转
					SecondUv = GetRotatorUV(SecondUv, _SecondTex_ST, _SecondTexRotator);

					//	副贴图扭曲
					#if defined(_INTERFERENCE2_ON)
						float2 interference2UV = InterferenceUV(input.SecondTexUV_InterferenceUV.zw, float2(_Interference2AnimSpeedU, _Interference2AnimSpeedV), TEXTURE2D_ARGS(_Interference2Tex, sampler_Interference2Tex),  _Interference2Intensity);
						SecondUv += interference2UV;
					#endif

					half4 col2 = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, SecondUv);
					col *= col2;
				#endif

					// 双面
				#if defined(_DOUBLE_SIDE_ON)
					col *= facing < 0.0 ? _BackColor : _Color;
				#else
					col *= _Color;
				#endif
					
					//	菲涅尔
				#if defined(_FRESNEL_ON)
					float3 normalDir = normalize(input.normalWS);
					float3 viewDir = input.viewDirWS;
					float dotValue = pow(1 - saturate(dot(normalDir, viewDir)), _FresnelIntensity);
					
					//	反向菲涅尔
					dotValue = lerp(dotValue, 1 - dotValue, _DFresnelEnable);

					col.rgb = lerp(col.rgb, _FresnelColor.rgb, 1 - dotValue) * _FresnelColor.a;

					half FAlpha = pow(abs(1 - dotValue), _FresnelIntensity);
					col.a *= lerp(1, FAlpha, _DFresnelAlphaEnable);
				#endif

					//	溶解
				#if defined(_DISSOLUTION_EDGE_ON)
					
					half2 dissolutionTex = SAMPLE_TEXTURE2D(_DissolutionTex, sampler_DissolutionTex, input.dissolutionTexcoord.xy).ra;
					half dissolution = dissolutionTex.x * dissolutionTex.y;

					//	反向溶解
					dissolution = lerp(dissolution, 1 - dissolution, _ReverseDissolution);
	
					half dissolutionRange = _DissolutionRange;
	
						//	粒子自定义范围
					#if defined(_PARTICLE_DISSOLUTION_RANGE_ON)
						dissolutionRange = clamp(input.dissolutionTexcoord.z, 0, 1.1);
					#endif
	
						half dissove = dissolution - dissolutionRange;
	
						//	定向溶解
					#if defined(_DISSOLUTION_SCALE_ON)
						dissove = dissolutionTex.x * dissolutionTex.y - _WorldSpaceScale.w - input.dissolutionTexcoord.w;
						dissolution *= dissove;
					#endif

					clip(dissove);
					float smooth = smoothstep(dissolutionRange, dissolutionRange + dissolutionRange * _DissolutionEdgeSoftRange * 0.5, dissolution);
					smooth = smooth * sign(_DissolutionEdgeSoftRange) + (1 - sign(_DissolutionEdgeSoftRange));
					smooth = smooth * sign(dissolutionRange) + (1 - sign(dissolutionRange));
					col.a *= smooth;
					float s1 = step(dissolution, dissolutionRange);
					float s2 = step(dissolution, (dissolutionRange + _DissolutionEdgeWidth));
					half3 DissolutionColor = _DissolutionEdgeColor.rgb;
					DissolutionColor = DissolutionColor * pow(2, _Luminance2);
					float3 dissolutionEdge = (s2 - s1) * DissolutionColor;
					col.rgb = lerp(DissolutionColor, col.rgb, step(abs(s1 - s2), 0));
				#endif
					
					//	Mask裁剪
				#if defined(_MASK_TEXTURE_ON)
					float2 maskUv = input.maskUV_InterferenceUV.xy - float2(_MaskTexAnimSpeedU, _MaskTexAnimSpeedV) * _Time.x;
					//	遮罩图旋转
					maskUv = GetRotatorUV(maskUv, _MaskTex_ST, _MaskTexRotator);

					half2 maskRA = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, maskUv).ra;
					col.a *= lerp(maskRA.y, maskRA.x, _MaskChannel);
				#endif

					//	软粒子
                #if defined(_DepthRenderEnable_ON)
					float sceneZ = LinearEyeDepth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.screenPos.xy / input.screenPos.w).r, _ZBufferParams);
					float DepthRender = saturate(((sceneZ - _SoftNearFade) - input.screenPos.z) / (_SoftFarFade - _SoftNearFade));
                    col.a *= DepthRender; 
                #endif		
					
					//	透明度裁剪
					clip(col.a - _AlphaCullingBias * _UseUIAlphaClip);		//	透明度舍弃片元

					
					if (_SetFog > 0)
					{
						col.a = ExponentialHeightFogAlpha(col.a, input.positionWS, input.screenPos);
					}

					return col;
				}
				ENDHLSL
			}
		}
	}
	CustomEditor "Particle_Stander_GUI"
}