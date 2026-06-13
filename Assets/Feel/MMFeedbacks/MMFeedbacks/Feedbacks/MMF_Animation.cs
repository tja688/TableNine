using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback used to trigger an animation (bool, int, float or trigger) on the associated animator, with or without randomness
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可向一个 Animator（在 Inspector 中绑定）发送 bool / int / float / trigger 参数，从而触发动画；你也可以启用随机模式来随机选择要写入的参数。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Animation/Animation Parameter")]
	public class MMF_Animation : MMF_Feedback 
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
        
		/// the possible modes that pilot triggers        
		public enum TriggerModes { SetTrigger, ResetTrigger }
        
		/// the possible ways to set a value
		public enum ValueModes { None, Constant, Random, Incremental }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.AnimationColor; } }
		public override bool EvaluateRequiresSetup() { return (BoundAnimator == null); }
		public override string RequiredTargetText { get { return BoundAnimator != null ? BoundAnimator.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先指定 BoundAnimator 才能正常工作。你可以在下方进行设置。"; } }
		#endif
		
		/// the duration of this feedback is the declared duration 
		public override float FeedbackDuration { get { return ApplyTimeMultiplier(DeclaredDuration); } set { DeclaredDuration = value;  } }
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => BoundAnimator = FindAutomatedTarget<Animator>();

		[MMFInspectorGroup("Animation", true, 12, true)]
		/// the animator whose parameters you want to update
		[Tooltip("要更新动画器的参数")]
		public Animator BoundAnimator;
		/// the list of extra animators whose parameters you want to update
		[Tooltip("要一并更新参数的额外 Animator 列表")]
		public List<Animator> ExtraBoundAnimators;
		/// the duration for the player to consider. This won't impact your animation, but is a way to communicate to the MMF Player the duration of this feedback. Usually you'll want it to match your actual animation, and setting it can be useful to have this feedback work with holding pauses.
		[Tooltip("供播放器参考的持续时间。它不会直接影响你的动画，而是用于向 MMF_Player 声明此反馈应持续多久。通常建议将其设置为与你的实际动画时长一致，这样在使用 Holding Pause 时才能正确协同工作。")]
		public float DeclaredDuration = 0f;
        
		[MMFInspectorGroup("Trigger", true, 16)]
		/// if this is true, will update the specified trigger parameter
		[Tooltip("若开启此项，将更新指定的 trigger 参数")]
		public bool UpdateTrigger = false;
		/// the selected mode to interact with this trigger
		[Tooltip("与此 Trigger 交互时使用的模式")]
		[MMFCondition("UpdateTrigger", true)]
		public TriggerModes TriggerMode = TriggerModes.SetTrigger;
		/// the trigger animator parameter to, well, trigger when the feedback is played
		[Tooltip("反馈播放时要触发的 Animator trigger 参数")]
		[MMFCondition("UpdateTrigger", true)]
		public string TriggerParameterName;
        
		[MMFInspectorGroup("Random Trigger", true, 20)]
		/// if this is true, will update a random trigger parameter, picked from the list below
		[Tooltip("若开启此项，将从下方列表中随机选取并更新一个 trigger 参数。注意：列表不能为空，否则会报错。")]
		public bool UpdateRandomTrigger = false;
		/// the selected mode to interact with this trigger
		[Tooltip("与此 Trigger 交互时使用的模式")]
		[MMFCondition("UpdateRandomTrigger", true)]
		public TriggerModes RandomTriggerMode = TriggerModes.SetTrigger;
		/// the trigger animator parameters to trigger at random when the feedback is played
		[Tooltip("反馈播放时要随机触发的 Animator trigger 参数列表")]
		public List<string> RandomTriggerParameterNames;
        
		[MMFInspectorGroup("Bool", true, 17)]
		/// if this is true, will update the specified bool parameter
		[Tooltip("若开启此项，将更新指定的 bool 参数")]
		public bool UpdateBool = false;
		/// the bool parameter to turn true when the feedback gets played
		[Tooltip("反馈播放时要设置的 bool 参数")]
		[MMFCondition("UpdateBool", true)]
		public string BoolParameterName;
		/// when in bool mode, whether to set the bool parameter to true or false
		[Tooltip("在 bool 模式下，决定把该参数设为 true 还是 false")]
		[MMFCondition("UpdateBool", true)]
		public bool BoolParameterValue = true;
        
		[MMFInspectorGroup("Random Bool", true, 19)]
		/// if this is true, will update a random bool parameter picked from the list below
		[Tooltip("若开启此项，将从下方列表中随机选取并更新一个 bool 参数。注意：列表不能为空，否则会报错。")]
		public bool UpdateRandomBool = false;
		/// when in bool mode, whether to set the bool parameter to true or false
		[Tooltip("在 bool 模式下，决定把随机选中的参数设为 true 还是 false")]
		[MMFCondition("UpdateRandomBool", true)]
		public bool RandomBoolParameterValue = true;
		/// the bool parameter to turn true when the feedback gets played
		[Tooltip("反馈播放时要设置的 bool 参数")]
		public List<string> RandomBoolParameterNames;
        
		[MMFInspectorGroup("Int", true, 24)]
		/// the int parameter to turn true when the feedback gets played
		[Tooltip("整数参数写入模式：无（不修改）、固定值（固定值）、随机值（随机值）或增量（增量）。")]
		public ValueModes IntValueMode = ValueModes.None;
		/// the int parameter to turn true when the feedback gets played
		[Tooltip("反馈播放时要设置的 int 参数")]
		[MMFEnumCondition("IntValueMode", (int)ValueModes.Constant, (int)ValueModes.Random, (int)ValueModes.Incremental)]
		public string IntParameterName;
		/// the value to set to that int parameter
		[Tooltip("在 Constant 模式下要写入该 int 参数的值")]
		[MMFEnumCondition("IntValueMode", (int)ValueModes.Constant)]
		public int IntValue;
		/// the min value (inclusive) to set at random to that int parameter
		[Tooltip("在 Random 模式下随机范围的最小值（包含该值）")]
		[MMFEnumCondition("IntValueMode", (int)ValueModes.Random)]
		public int IntValueMin;
		/// the max value (exclusive) to set at random to that int parameter
		[Tooltip("在 Random 模式下随机范围的最大值（不包含该值）")]
		[MMFEnumCondition("IntValueMode", (int)ValueModes.Random)]
		public int IntValueMax = 5;
		/// the value to increment that int parameter by
		[Tooltip("在 Incremental 模式下每次播放要增加的 int 数值")]
		[MMFEnumCondition("IntValueMode", (int)ValueModes.Incremental)]
		public int IntIncrement = 1;

		[MMFInspectorGroup("Float", true, 22)]
		/// the Float parameter to turn true when the feedback gets played
		[Tooltip("浮点数参数写入模式：无（不修改）、固定值（固定值）、随机值（随机值）或增量（增量）。")]
		public ValueModes FloatValueMode = ValueModes.None;
		/// the float parameter to turn true when the feedback gets played
		[Tooltip("反馈播放时要设置的 float 参数")]
		[MMFEnumCondition("FloatValueMode", (int)ValueModes.Constant, (int)ValueModes.Random, (int)ValueModes.Incremental)]
		public string FloatParameterName;
		/// the value to set to that float parameter
		[Tooltip("在 Constant 模式下要写入该 float 参数的值")]
		[MMFEnumCondition("FloatValueMode", (int)ValueModes.Constant)]
		public float FloatValue;
		/// the min value (inclusive) to set at random to that float parameter
		[Tooltip("在 Random 模式下随机范围的最小值（包含该值）")]
		[MMFEnumCondition("FloatValueMode", (int)ValueModes.Random)]
		public float FloatValueMin;
		/// the max value (exclusive) to set at random to that float parameter
		[Tooltip("在 Random 模式下随机范围的最大值（不包含该值）")]
		[MMFEnumCondition("FloatValueMode", (int)ValueModes.Random)]
		public float FloatValueMax = 5;
		/// the value to increment that float parameter by
		[Tooltip("在 Incremental 模式下每次播放要增加的 float 数值")]
		[MMFEnumCondition("FloatValueMode", (int)ValueModes.Incremental)]
		public float FloatIncrement = 1;

		[MMFInspectorGroup("Layer Weights", true, 22)]
		/// whether or not to set layer weights on the specified layer when playing this feedback
		[Tooltip("播放此反馈时是否同时设置 Animator 层权重。关闭后，下方层权重相关字段将不生效。")]
		public bool SetLayerWeight = false;
		/// the index of the layer to target when changing layer weights
		[Tooltip("修改层权重时要作用的层索引")]
		[MMFCondition("SetLayerWeight", true)]
		public int TargetLayerIndex = 1;
		/// the name of the Animator layer you want the layer weight change to occur on. This is optional. If left empty, the layer ID above will be used, if not empty, the Layer id specified above will be ignored.
		[Tooltip("要修改层权重的 Animator 层名称。此项可选；若留空，将使用上方的层 ID；若填写此项，则上方指定的层 ID 将被忽略。")]
		public string LayerName = "";
		/// the new weight to set on the target animator layer
		[Tooltip("要设置到目标 Animator 层的新权重值")]
		[MMFCondition("SetLayerWeight", true)]
		public float NewWeight = 0.5f;

		protected int _triggerParameter;
		protected int _boolParameter;
		protected int _intParameter;
		protected int _floatParameter;
		protected List<int> _randomTriggerParameters;
		protected List<int> _randomBoolParameters;
		protected int _layerID;

		/// <summary>
		/// Custom Init
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);
			
			_triggerParameter = Animator.StringToHash(TriggerParameterName);
			_boolParameter = Animator.StringToHash(BoolParameterName);
			_intParameter = Animator.StringToHash(IntParameterName);
			_floatParameter = Animator.StringToHash(FloatParameterName);
			
			if (RandomTriggerParameterNames == null)
			{
				RandomTriggerParameterNames = new List<string>();
			}
			if (RandomBoolParameterNames == null)
			{
				RandomBoolParameterNames = new List<string>();
			}

			_randomTriggerParameters = new List<int>();
			
			foreach (string name in RandomTriggerParameterNames)
			{
				_randomTriggerParameters.Add(Animator.StringToHash(name));
			}

			_randomBoolParameters = new List<int>();
			foreach (string name in RandomBoolParameterNames)
			{
				_randomBoolParameters.Add(Animator.StringToHash(name));
			}
			
			_layerID = TargetLayerIndex;
			if ((LayerName != "") && (BoundAnimator != null))
			{
				_layerID = BoundAnimator.GetLayerIndex(LayerName);
			}
		}

		/// <summary>
		/// On Play, checks if an animator is bound and triggers parameters
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (BoundAnimator == null)
			{
				Debug.LogWarning("[Animation Feedback] The animation feedback on "+Owner.name+" doesn't have a BoundAnimator, it won't work. You need to specify one in its inspector.");
				return;
			}

			float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);

			ApplyValue(BoundAnimator, intensityMultiplier);
			foreach (Animator animator in ExtraBoundAnimators)
			{
				ApplyValue(animator, intensityMultiplier);
			}
		}

		/// <summary>
		/// Applies values on the target Animator
		/// </summary>
		/// <param name="targetAnimator"></param>
		/// <param name="intensityMultiplier"></param>
		protected virtual void ApplyValue(Animator targetAnimator, float intensityMultiplier)
		{
			if (UpdateTrigger)
			{
				if (TriggerMode == TriggerModes.SetTrigger)
				{
					targetAnimator.SetTrigger(_triggerParameter);
				}
				if (TriggerMode == TriggerModes.ResetTrigger)
				{
					targetAnimator.ResetTrigger(_triggerParameter);
				}
			}
            
			if (UpdateRandomTrigger)
			{
				int randomParameter = _randomTriggerParameters[Random.Range(0, _randomTriggerParameters.Count)];
                
				if (RandomTriggerMode == TriggerModes.SetTrigger)
				{
					targetAnimator.SetTrigger(randomParameter);
				}
				if (RandomTriggerMode == TriggerModes.ResetTrigger)
				{
					targetAnimator.ResetTrigger(randomParameter);
				}
			}

			if (UpdateBool)
			{
				targetAnimator.SetBool(_boolParameter, BoolParameterValue);
			}

			if (UpdateRandomBool)
			{
				int randomParameter = _randomBoolParameters[Random.Range(0, _randomBoolParameters.Count)];
                
				targetAnimator.SetBool(randomParameter, RandomBoolParameterValue);
			}

			switch (IntValueMode)
			{
				case ValueModes.Constant:
					targetAnimator.SetInteger(_intParameter, IntValue);
					break;
				case ValueModes.Incremental:
					int newValue = targetAnimator.GetInteger(_intParameter) + IntIncrement;
					targetAnimator.SetInteger(_intParameter, newValue);
					break;
				case ValueModes.Random:
					int randomValue = Random.Range(IntValueMin, IntValueMax);
					targetAnimator.SetInteger(_intParameter, randomValue);
					break;
			}

			switch (FloatValueMode)
			{
				case ValueModes.Constant:
					targetAnimator.SetFloat(_floatParameter, FloatValue * intensityMultiplier);
					break;
				case ValueModes.Incremental:
					float newValue = targetAnimator.GetFloat(_floatParameter) + FloatIncrement * intensityMultiplier;
					targetAnimator.SetFloat(_floatParameter, newValue);
					break;
				case ValueModes.Random:
					float randomValue = Random.Range(FloatValueMin, FloatValueMax) * intensityMultiplier;
					targetAnimator.SetFloat(_floatParameter, randomValue);
					break;
			}

			if (SetLayerWeight)
			{
				targetAnimator.SetLayerWeight(_layerID, NewWeight);
			}
		}
        
		/// <summary>
		/// On stop, turns the bool parameter to false
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !UpdateBool || !FeedbackTypeAuthorized)
			{
				return;
			}
            
			BoundAnimator.SetBool(_boolParameter, false);
			foreach (Animator animator in ExtraBoundAnimators)
			{
				animator.SetBool(_boolParameter, false);
			}
		}
	}
}

