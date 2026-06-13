using System;
using UnityEngine;
#if MM_VISUALEFFECTGRAPH
using UnityEngine.VFX;
#endif
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.FeedbacksForThirdParty
{
	/// <summary>
	/// 这个反馈可为目标 VisualEffect 设置属性。
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	[FeedbackHelp("这个反馈可为目标 VisualEffect 设置属性。")]
	#if MM_VISUALEFFECTGRAPH
	[FeedbackPath("Particles/VisualEffectSetProperty")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.VisualEffectGraph")]
	public class MMF_VisualEffectSetProperty : MMF_Feedback 
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.ParticlesColor; } }
		#endif

		/// the duration of this feedback is the duration of the shake
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		public override bool HasChannel => true;
		public override bool HasRandomness => true;
		
		[MMFInspectorGroup("Visual Effect Property", true, 41)]
		/// 这是提供给 MMF_Player 参考的反馈持续时间，不会直接影响你的 VisualEffect。通常建议让它与实际粒子系统的持续时间一致；这样在使用 Holding Pause 时，本反馈的时序会更准确。
		[Tooltip("这是提供给 MMF_Player 参考的反馈持续时间，不会直接影响你的 VisualEffect。通常建议让它与实际粒子系统的持续时间一致；这样在使用 Holding Pause 时，本反馈的时序会更准确。")]
		public float DeclaredDuration = 0f;
		
		#if MM_VISUALEFFECTGRAPH
		
		public enum PropertyTypes { AnimationCurve, Bool, Float, Gradient, Int, Mesh, Texture, UInt, Vector2, Vector3, Vector4, }
		
		/// 要设置属性的 VisualEffect。
		[Tooltip("要设置属性的视觉效果。")]
		public VisualEffect TargetVisualEffect;
		/// 要设置的属性 ID，应与 Visual Effect Graph 中暴露的属性一致。
		[Tooltip("要设置的属性 ID，应与 Visual Effect Graph 中暴露的属性一致。")] 
		public string PropertyID;
		/// 要设置的属性类型。
		[Tooltip("要设置的属性类型。")]
		public PropertyTypes PropertyType = PropertyTypes.Float;
		/// 如果属性类型为 AnimationCurve，则这里指定新的曲线值。
		[Tooltip("如果属性类型为 AnimationCurve，则这里指定新的曲线值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.AnimationCurve)]
		public AnimationCurve NewAnimationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
		/// 如果属性类型为 bool，则这里指定新的布尔值。
		[Tooltip("如果属性类型为 bool，则这里指定新的布尔值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Bool)]
		public bool NewBool = true;
		/// 如果属性类型为 float，则这里指定新的浮点值。
		[Tooltip("如果属性类型为 float，则这里指定新的浮点值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Float)]
		public float NewFloat = 1f;
		/// 如果属性类型为 Gradient，则这里指定新的渐变。
		[Tooltip("如果属性类型为 Gradient，则这里指定新的渐变。")] 
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Gradient)]
		[GradientUsage(true)]
		public Gradient NewGradient = new Gradient();
		/// 如果属性类型为 int，则这里指定新的整数值。
		[Tooltip("如果属性类型为 int，则这里指定新的整数值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Int)]
		public int NewInt;
		/// 如果属性类型为 Mesh，则这里指定新的网格。
		[Tooltip("如果属性类型为 Mesh，则这里指定新的网格。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Mesh)]
		public Mesh NewMesh;
		/// 如果属性类型为 Texture，则这里指定新的纹理。
		[Tooltip("如果属性类型为 Texture，则这里指定新的纹理。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Texture)]
		public Texture NewTexture;
		/// 如果属性类型为 unsigned int，则这里指定新的无符号整数值。
		[Tooltip("如果属性类型为 unsigned int，则这里指定新的无符号整数值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.UInt)]
		public uint NewUInt;
		/// 如果属性类型为 Vector2，则这里指定新的 Vector2 值。
		[Tooltip("如果属性类型为 Vector2，则这里指定新的 Vector2 值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Vector2)]
		public Vector2 NewVector2;
		/// 如果属性类型为 Vector3，则这里指定新的 Vector3 值。
		[Tooltip("如果属性类型为 Vector3，则这里指定新的 Vector3 值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Vector3)]
		public Vector3 NewVector3;
		/// 如果属性类型为 Vector4，则这里指定新的 Vector4 值。
		[Tooltip("如果属性类型为 Vector4，则这里指定新的 Vector4 值。")]
		[MMFEnumCondition("PropertyType", (int)PropertyTypes.Vector4)]
		public Vector4 NewVector4;

		protected int _propertyID;

		protected AnimationCurve _initialAnimationCurve;
		protected bool _initialBool;
		protected float _initialFloat;
		protected Gradient _initialGradient;
		protected int _initialInt;
		protected Mesh _initialMesh;
		protected Texture _initialTexture;
		protected uint _initialUInt;
		protected Vector2 _initialVector2;
		protected Vector3 _initialVector3;
		protected Vector4 _initialVector4;
		
		/// <summary>
		/// On init we cache our property ID
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			_propertyID = Shader.PropertyToID(PropertyID);
			GetInitialValue();
		}

		/// <summary>
		/// Grabs and stores the initial value of the target property
		/// </summary>
		protected virtual void GetInitialValue()
		{
			if (TargetVisualEffect == null)
			{
				return;
			}
			
			switch (PropertyType)
			{
				case PropertyTypes.AnimationCurve:
					_initialAnimationCurve = TargetVisualEffect.GetAnimationCurve(_propertyID);
					break;
				case PropertyTypes.Bool:
					_initialBool = TargetVisualEffect.GetBool(_propertyID);
					break;
				case PropertyTypes.Float:
					_initialFloat = TargetVisualEffect.GetFloat(_propertyID);
					break;
				case PropertyTypes.Gradient:
					_initialGradient = TargetVisualEffect.GetGradient(_propertyID);
					break;
				case PropertyTypes.Int:
					_initialInt = TargetVisualEffect.GetInt(_propertyID);
					break;
				case PropertyTypes.Mesh:
					_initialMesh = TargetVisualEffect.GetMesh(_propertyID);
					break;
				case PropertyTypes.Texture:
					_initialTexture = TargetVisualEffect.GetTexture(_propertyID);
					break;
				case PropertyTypes.UInt:
					_initialUInt = TargetVisualEffect.GetUInt(_propertyID);
					break;
				case PropertyTypes.Vector2:
					_initialVector2 = TargetVisualEffect.GetVector2(_propertyID);
					break;
				case PropertyTypes.Vector3:
					_initialVector3 = TargetVisualEffect.GetVector3(_propertyID);
					break;
				case PropertyTypes.Vector4:
					_initialVector4 = TargetVisualEffect.GetVector4(_propertyID);
					break;
			}
		}

		/// <summary>
		/// Sets the target property on the target VisualEffect to the specified new value
		/// </summary>
		/// <param name="position"></param>
		/// <param name="attenuation"></param>
		protected override void CustomPlayFeedback(Vector3 position, float attenuation = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetVisualEffect == null))
			{
				return;
			}

			switch (PropertyType)
			{
				case PropertyTypes.AnimationCurve:
					TargetVisualEffect.SetAnimationCurve(_propertyID, NewAnimationCurve);
					break;
				case PropertyTypes.Bool:
					TargetVisualEffect.SetBool(_propertyID, NewBool);
					break;
				case PropertyTypes.Float:
					TargetVisualEffect.SetFloat(_propertyID, NewFloat);
					break;
				case PropertyTypes.Gradient:
					TargetVisualEffect.SetGradient(_propertyID, NewGradient);
					break;
				case PropertyTypes.Int:
					TargetVisualEffect.SetInt(_propertyID, NewInt);
					break;
				case PropertyTypes.Mesh:
					TargetVisualEffect.SetMesh(_propertyID, NewMesh);
					break;
				case PropertyTypes.Texture:
					TargetVisualEffect.SetTexture(_propertyID, NewTexture);
					break;
				case PropertyTypes.UInt:
					TargetVisualEffect.SetUInt(_propertyID, NewUInt);
					break;
				case PropertyTypes.Vector2:
					TargetVisualEffect.SetVector2(_propertyID, NewVector2);
					break;
				case PropertyTypes.Vector3:
					TargetVisualEffect.SetVector3(_propertyID, NewVector3);
					break;
				case PropertyTypes.Vector4:
					TargetVisualEffect.SetVector4(_propertyID, NewVector4);
					break;
			}
		}
		
		
		/// <summary>
		/// On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			
			switch (PropertyType)
			{
				case PropertyTypes.AnimationCurve:
					TargetVisualEffect.SetAnimationCurve(_propertyID, _initialAnimationCurve);
					break;
				case PropertyTypes.Bool:
					TargetVisualEffect.SetBool(_propertyID, _initialBool);
					break;
				case PropertyTypes.Float:
					TargetVisualEffect.SetFloat(_propertyID, _initialFloat);
					break;
				case PropertyTypes.Gradient:
					TargetVisualEffect.SetGradient(_propertyID, _initialGradient);
					break;
				case PropertyTypes.Int:
					TargetVisualEffect.SetInt(_propertyID, _initialInt);
					break;
				case PropertyTypes.Mesh:
					TargetVisualEffect.SetMesh(_propertyID, _initialMesh);
					break;
				case PropertyTypes.Texture:
					TargetVisualEffect.SetTexture(_propertyID, _initialTexture);
					break;
				case PropertyTypes.UInt:
					TargetVisualEffect.SetUInt(_propertyID, _initialUInt);
					break;
				case PropertyTypes.Vector2:
					TargetVisualEffect.SetVector2(_propertyID, _initialVector2);
					break;
				case PropertyTypes.Vector3:
					TargetVisualEffect.SetVector3(_propertyID, _initialVector3);
					break;
				case PropertyTypes.Vector4:
					TargetVisualEffect.SetVector4(_propertyID, _initialVector4);
					break;
			}
		}
		
		#else
		protected override void CustomPlayFeedback(Vector3 position, float attenuation = 1.0f) { }
		#endif
	}
}