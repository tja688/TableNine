using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// Turns an object active or inactive at the various stages of the feedback
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你设置 Shader 的全局属性，或启用/禁用关键字。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Renderer/Shader Global")]
	public class MMF_ShaderGlobal : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.RendererColor; } }
		public override string RequiredTargetText { get { return Mode.ToString();  } }
		#endif

		public enum Modes { SetGlobalColor, SetGlobalFloat, SetGlobalInt, SetGlobalMatrix, SetGlobalTexture, SetGlobalVector, EnableKeyword, DisableKeyword, WarmupAllShaders }

		[MMFInspectorGroup("Shader Global", true, 24)]
		/// the selected mode for this feedback
		[Tooltip("此反馈当前选择的模式")]
		public Modes Mode = Modes.SetGlobalFloat;
		/// 全局属性名
		[Tooltip("全局属性名")]
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalColor, (int)Modes.SetGlobalFloat, (int)Modes.SetGlobalInt, (int)Modes.SetGlobalMatrix, (int)Modes.SetGlobalTexture, (int)Modes.SetGlobalVector)]
		public string PropertyName = "";
		/// 通过 Shader.PropertyToID 获取的属性 ID
		[Tooltip("通过 着色器.属性到编号 获取的属性编号")]
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalColor, (int)Modes.SetGlobalFloat, (int)Modes.SetGlobalInt, (int)Modes.SetGlobalMatrix, (int)Modes.SetGlobalTexture, (int)Modes.SetGlobalVector)]
		public int PropertyNameID = 0;
		/// 写入 Shader 全局 Color 属性
		[Tooltip("写入 着色器 全局颜色属性")]
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalColor)]
		public Color GlobalColor = Color.yellow;
		/// 写入 Shader 全局 Float 属性
		[Tooltip("写入 着色器 全局 漂浮 属性")] 
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalFloat)]
		public float GlobalFloat = 1f;
		/// 写入 Shader 全局 Int 属性
		[Tooltip("写入 着色器 全局 Int 属性")] 
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalInt)]
		public int GlobalInt = 1;
		/// 写入 Shader 全局 Matrix 属性
		[Tooltip("写入着色器全局矩阵属性")] 
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalMatrix)]
		public Matrix4x4 GlobalMatrix = Matrix4x4.identity;
		/// 写入 Shader 全局 Texture 属性
		[Tooltip("写入 着色器 全局纹理属性")] 
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalTexture)]
		public RenderTexture GlobalTexture;
		/// 写入 Shader 全局 Vector 属性
		[Tooltip("写入 着色器 全局向量属性")] 
		[MMFEnumCondition("Mode", (int)Modes.SetGlobalVector)]
		public Vector4 GlobalVector;
		/// 全局 Shader 关键字
		[Tooltip("全局着色器关键字")] 
		[MMFEnumCondition("Mode", (int)Modes.EnableKeyword, (int)Modes.DisableKeyword)]
		public string Keyword;

		protected Color _initialColor;
		protected float _initialFloat;
		protected int _initialInt;
		protected Matrix4x4 _initialMatrix;
		protected RenderTexture _initialTexture;
		protected Vector4 _initialVector;
        
		/// <summary>
		/// On Play we set our global shader property
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			switch (Mode)
			{
				case Modes.SetGlobalColor:
					if (PropertyName == "")
					{
						Shader.SetGlobalColor(PropertyNameID, GlobalColor);
					}
					else
					{
						Shader.SetGlobalColor(PropertyName, GlobalColor);
					}
					break;
				case Modes.SetGlobalFloat:
					if (PropertyName == "")
					{
						Shader.SetGlobalFloat(PropertyNameID, GlobalFloat);
					}
					else
					{
						Shader.SetGlobalFloat(PropertyName, GlobalFloat);
					}
					break;
				case Modes.SetGlobalInt:
					if (PropertyName == "")
					{
						Shader.SetGlobalInt(PropertyNameID, GlobalInt);
					}
					else
					{
						Shader.SetGlobalInt(PropertyName, GlobalInt);
					}
					break;
				case Modes.SetGlobalMatrix:
					if (PropertyName == "")
					{
						Shader.SetGlobalMatrix(PropertyNameID, GlobalMatrix);
					}
					else
					{
						Shader.SetGlobalMatrix(PropertyName, GlobalMatrix);
					}
					break;
				case Modes.SetGlobalTexture:
					if (PropertyName == "")
					{
						Shader.SetGlobalTexture(PropertyNameID, GlobalTexture);
					}
					else
					{
						Shader.SetGlobalTexture(PropertyName, GlobalTexture);
					}
					break;
				case Modes.SetGlobalVector:
					if (PropertyName == "")
					{
						Shader.SetGlobalVector(PropertyNameID, GlobalVector);
					}
					else
					{
						Shader.SetGlobalVector(PropertyName, GlobalVector);
					}
					break;
				case Modes.EnableKeyword:
					Shader.EnableKeyword(Keyword);
					break;
				case Modes.DisableKeyword:
					Shader.DisableKeyword(Keyword);
					break;
				case Modes.WarmupAllShaders:
					Shader.WarmupAllShaders();
					break;
			}
		}
		
		/// <summary>
		/// On restore, we restore our initial state
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			switch (Mode)
			{
				case Modes.SetGlobalColor:
					if (PropertyName == "")
					{
						Shader.SetGlobalColor(PropertyNameID, _initialColor);
					}
					else
					{
						Shader.SetGlobalColor(PropertyName, _initialColor);
					}
					break;
				case Modes.SetGlobalFloat:
					if (PropertyName == "")
					{
						Shader.SetGlobalFloat(PropertyNameID, _initialFloat);
					}
					else
					{
						Shader.SetGlobalFloat(PropertyName, _initialFloat);
					}
					break;
				case Modes.SetGlobalInt:
					if (PropertyName == "")
					{
						Shader.SetGlobalInt(PropertyNameID, _initialInt);
					}
					else
					{
						Shader.SetGlobalInt(PropertyName, _initialInt);
					}
					break;
				case Modes.SetGlobalMatrix:
					if (PropertyName == "")
					{
						Shader.SetGlobalMatrix(PropertyNameID, _initialMatrix);
					}
					else
					{
						Shader.SetGlobalMatrix(PropertyName, _initialMatrix);
					}
					break;
				case Modes.SetGlobalTexture:
					if (PropertyName == "")
					{
						Shader.SetGlobalTexture(PropertyNameID, _initialTexture);
					}
					else
					{
						Shader.SetGlobalTexture(PropertyName, _initialTexture);
					}
					break;
				case Modes.SetGlobalVector:
					if (PropertyName == "")
					{
						Shader.SetGlobalVector(PropertyNameID, _initialVector);
					}
					else
					{
						Shader.SetGlobalVector(PropertyName, _initialVector);
					}
					break;
			}
		}
	}
}

