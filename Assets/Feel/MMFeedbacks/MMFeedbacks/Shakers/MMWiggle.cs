using UnityEngine;
using System.Collections;
using System;
using MoreMountains.Tools;

namespace MoreMountains.Feedbacks
{
	/// the possible types of wiggle
	public enum WiggleTypes { None, Random, PingPong, Noise, Curve }

	/// <summary>
	/// A class to store public wiggle properties
	/// </summary>
	[Serializable]
	public class WiggleProperties
	{
		[Header("Status")]
		public bool WigglePermitted = true;

		[Header("Type")]
		/// the position mode : none, random or ping pong - none won't do anything, random will randomize min and max bounds, ping pong will oscillate between min and max bounds
		[Tooltip("位置模式：`无`、`随机值` 或 `乒乓球`。`无`不会产生任何效果；`随机值`会在最小/最大边界之间随机；`乒乓球`会在最小/最大边界之间来回振荡。")]
		public WiggleTypes WiggleType = WiggleTypes.Random;
		/// if this is true, unscaled delta time, otherwise regular delta time
		[Tooltip("若启用，则使用不受时间缩放影响的时间对应的 deltaTime；否则使用常规 deltaTime。")]
		public bool UseUnscaledTime = false;
		/// a multiplier to apply to all time related operations, allowing you to speed up or slow down the wiggle
		[Tooltip("应用到所有时间相关运算上的倍率，可用于加快或减慢 wiggle 效果。")]
		public float TimeMultiplier = 1f;
		
		/// whether or not this object should start wiggling automatically on Start()
		[Tooltip("此对象是否在 `Start()` 时自动开始 wiggle。")]
		public bool StartWigglingAutomatically = true;
		/// if this is true, position will be ping ponged with an ease in/out curve
		[Tooltip("若启用，位置在 PingPong 往返时会套用 ease in/out 曲线。")]
		public bool SmoothPingPong = true;

		[Header("Speed")]
		/// Whether or not the position's speed curve will be used
		[Tooltip("是否使用位置的速度曲线。")]
		public bool UseSpeedCurve = false;
		/// an animation curve to define the speed over time from one position to the other (x), and the actual position (y), allowing for overshoot
		[Tooltip("用于定义从一个位置移动到另一个位置时速度变化的曲线。横轴（x）表示时间进度，纵轴（y）表示实际位置值，因此也支持超调。")]
		public AnimationCurve SpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		[Header("Frequency")]
		/// the minimum time (in seconds) between two position changes
		[Tooltip("两次位置变化之间的最短时间，单位为秒。")]
		public float FrequencyMin = 0f;
		/// the maximum time (in seconds) between two position changes
		[Tooltip("两次位置变化之间的最长时间，单位为秒。")]
		public float FrequencyMax = 1f;

		[Header("Amplitude")]
		/// the minimum position the object can have
		[Tooltip("该对象允许达到的最小位置。")]
		public Vector3 AmplitudeMin = Vector3.zero;
		/// the maximum position the object can have
		[Tooltip("该对象允许达到的最大位置。")]
		public Vector3 AmplitudeMax = Vector3.one;
		/// if this is true, amplitude will be relative, otherwise world space
		[Tooltip("若启用，振幅按相对值应用；若禁用，则按世界空间数值应用。")]
		public bool RelativeAmplitude = true;
		/// if this is true, all amplitude values will match the x amplitude value
		[Tooltip("若启用，所有轴的振幅都会与 X 轴振幅保持一致。")]
		public bool UniformValues = false;
		/// if this is true, when randomizing amplitude, the resulting vector's length will be forced to match ForcedVectorLength
		[Tooltip("若启用，在随机化振幅时，结果向量的长度会被强制设为 `ForcedVectorLength`。")]
		public bool ForceVectorLength = false;
		/// the length of the randomized amplitude if ForceVectorLength is true
		[Tooltip("当 `ForceVectorLength` 为 true 时，随机振幅向量会被强制使用的长度。")]
		[MMCondition("ForceVectorLength", true)]
		public float ForcedVectorLength = 1f;

		[Header("Curve")]
		/// a curve to animate this property on
		[Tooltip("用于驱动该属性变化的曲线。")]
		public AnimationCurve Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		/// the minimum value to randomize the curve's zero remap to
		[Tooltip("将曲线 `0` 端重映射值随机化时允许的最小值。")]
		public Vector3 RemapCurveZeroMin = Vector3.zero;
		/// the maximum value to randomize the curve's zero remap to
		[Tooltip("将曲线 `0` 端重映射值随机化时允许的最大值。")]
		public Vector3 RemapCurveZeroMax = Vector3.zero;
		/// the minimum value to randomize the curve's one remap to
		[Tooltip("将曲线 `1` 端重映射值随机化时允许的最小值。")]
		public Vector3 RemapCurveOneMin = Vector3.one;
		/// the maximum value to randomize the curve's one remap to
		[Tooltip("将曲线 `1` 端重映射值随机化时允许的最大值。")]
		public Vector3 RemapCurveOneMax = Vector3.one;
		/// whether or not to add the initial value of this property to the curve's outcome
		[Tooltip("是否把该属性的初始值叠加到曲线结果上。若启用，曲线结果会在初始值基础上偏移。")]
		public bool RelativeCurveAmplitude = true;
		/// whether or not the curve should be read from left to right, then right to left
		[Tooltip("曲线是否先从左到右读取，再从右到左读取。若启用，会形成往返播放效果。")]
		public bool CurvePingPong = false;

		[Header("Pause")]
		/// the minimum time to spend between two random positions
		[Tooltip("两个随机位置之间停留的最短时间。")]
		public float PauseMin = 0f;
		/// the maximum time to spend between two random positions
		[Tooltip("两个随机位置之间停留的最长时间。")]
		public float PauseMax = 0f;

		[Header("Limited Time")]
		/// if this is true, this property will only animate for the specified time
		[Tooltip("若启用，此属性只会在指定时间内执行动画；时间结束后将停止。")]
		public bool LimitedTime = false;
		/// the maximum time left
		[Tooltip("可用的最大剩余时间。")]
		public float LimitedTimeTotal;
		/// the animation curve to use to decrease the effect of the wiggle as time goes
		[Tooltip("用于随着时间推移逐渐减弱 wiggle 效果的曲线。")]
		public AnimationCurve LimitedTimeFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
		/// if this is true, original position will be restored when time left reaches zero
		[Tooltip("若启用，当剩余时间归零时会恢复原始位置。")]
		public bool LimitedTimeResetValue = true;
		/// the actual time left
		[Tooltip("当前实际剩余时间。")]
		[MMFReadOnly]
		public float LimitedTimeLeft;        

		[Header("Noise Frequency")]
		/// the minimum time between two changes of noise frequency
		[Tooltip("两次噪声频率变化之间的最短时间。")]
		public Vector3 NoiseFrequencyMin = Vector3.zero;
		/// the maximum time between two changes of noise frequency
		[Tooltip("两次噪声频率变化之间的最长时间。")]
		public Vector3 NoiseFrequencyMax = Vector3.one;

		[Header("Noise Shift")]
		/// how much the noise should be shifted at minimum
		[Tooltip("噪声最小偏移量。")]
		public Vector3 NoiseShiftMin = Vector3.zero;
		/// how much the noise should be shifted at maximum
		[Tooltip("噪声最大偏移量。")]
		public Vector3 NoiseShiftMax = Vector3.zero;


		/// <summary>
		/// Returns the delta time, either regular or unscaled
		/// </summary>
		/// <returns></returns>
		public float GetDeltaTime()
		{
			float deltaTime = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
			deltaTime *= TimeMultiplier;
			return deltaTime;
		}

		/// <summary>
		/// Returns the time, either regular or unscaled
		/// </summary>
		/// <returns></returns>
		public float GetTime()
		{
			float time = UseUnscaledTime ? Time.unscaledTime : Time.time;
			time *= TimeMultiplier;
			return time;
		}
	}

	/// <summary>
	/// A struct used to store internal wiggle properties
	/// </summary>
	public struct InternalWiggleProperties
	{
		public Vector3 returnVector;
		public Vector3 newValue;
		public Vector3 initialValue;
		public Vector3 startValue;
		public float timeSinceLastChange ;
		public float randomFrequency;
		public Vector3 randomNoiseFrequency;
		public Vector3 randomAmplitude;
		public Vector3 randomNoiseShift;
		public float timeSinceLastPause;
		public float pauseDuration;
		public float noiseElapsedTime;
		public Vector3 limitedTimeValueSave;
		public Vector3 remapZero;
		public Vector3 remapOne;
		public float curveDirection;
		public bool ping;
	}

	/// <summary>
	/// Add this class to a GameObject to be able to control its position/rotation/scale individually and periodically, allowing it to "wiggle" (or just move however you want on a periodic basis)
	/// </summary>
	[AddComponentMenu("More Mountains/Feedbacks/Shakers/Various/MM Wiggle")]
	public class MMWiggle : MonoBehaviour 
	{
		/// the possible update modes
		public enum UpdateModes { Update, FixedUpdate, LateUpdate }

		/// 当前选择的更新模式。
		[Tooltip("当前选择的更新模式。")]
		public UpdateModes UpdateMode = UpdateModes.Update;
		/// 是否启用位置 wiggle。
		[Tooltip("是否启用位置摆动。")]
		public bool PositionActive = false;
		/// 是否启用旋转 wiggle。
		[Tooltip("是否允许旋转摆动。")]
		public bool RotationActive = false;
		/// 是否启用缩放 wiggle。
		[Tooltip("是否允许缩放摆动。")]
		public bool ScaleActive = false;
		/// 与位置 wiggle 相关的所有公开设置。
		[Tooltip("与位置 wiggle 相关的所有公开设置。")]
		public WiggleProperties PositionWiggleProperties;
		/// 与旋转 wiggle 相关的所有公开设置。
		[Tooltip("与旋转 wiggle 相关的所有公开设置。")]
		public WiggleProperties RotationWiggleProperties;
		/// 与缩放 wiggle 相关的所有公开设置。
		[Tooltip("与缩放 wiggle 相关的所有公开设置。")]
		public WiggleProperties ScaleWiggleProperties;
		/// 与调试按钮配合使用的调试时长。
		[Tooltip("与调试按钮配合使用的调试时长。")]
		public float DebugWiggleDuration = 2f;

		protected InternalWiggleProperties _positionInternalProperties;
		protected InternalWiggleProperties _rotationInternalProperties;
		protected InternalWiggleProperties _scaleInternalProperties;

		public virtual void WigglePosition(float duration)
		{
			WiggleValue(ref PositionWiggleProperties, ref _positionInternalProperties, duration);
		}

		public virtual void WiggleRotation(float duration)
		{
			WiggleValue(ref RotationWiggleProperties, ref _rotationInternalProperties, duration);
		}

		public virtual void WiggleScale(float duration)
		{
			WiggleValue(ref ScaleWiggleProperties, ref _scaleInternalProperties, duration);
		}

		protected virtual void WiggleValue(ref WiggleProperties property, ref InternalWiggleProperties internalProperties, float duration)
		{
			InitializeRandomValues(ref property, ref internalProperties);
			internalProperties.limitedTimeValueSave = internalProperties.initialValue;
			property.LimitedTime = true;
			property.LimitedTimeLeft = duration;
			property.LimitedTimeTotal = duration;
			property.WigglePermitted = true;
		}

		/// <summary>
		/// On Start() we trigger the initialization
		/// </summary>
		protected virtual void Start()
		{
			Initialization();
		}

		/// <summary>
		/// On init we get the start values and trigger our coroutines for each property
		/// </summary>
		public virtual void Initialization()
		{
			if (PositionWiggleProperties == null) { PositionWiggleProperties = new WiggleProperties(); }
			if (RotationWiggleProperties == null) { RotationWiggleProperties = new WiggleProperties(); }
			if (ScaleWiggleProperties == null) { ScaleWiggleProperties = new WiggleProperties(); }
			
			_positionInternalProperties.initialValue = transform.localPosition;
			_positionInternalProperties.startValue = this.transform.localPosition;

			_rotationInternalProperties.initialValue = transform.localEulerAngles;
			_rotationInternalProperties.startValue = this.transform.localEulerAngles;

			_scaleInternalProperties.initialValue = transform.localScale;
			_scaleInternalProperties.startValue = this.transform.localScale;

			InitializeRandomValues(ref PositionWiggleProperties, ref _positionInternalProperties);
			InitializeRandomValues(ref RotationWiggleProperties, ref _rotationInternalProperties);
			InitializeRandomValues(ref ScaleWiggleProperties, ref _scaleInternalProperties);
		}

		/// <summary>
		/// Initializes internal properties of the specified wiggle value
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="internalProperties"></param>
		protected virtual void InitializeRandomValues(ref WiggleProperties properties, ref InternalWiggleProperties internalProperties)
		{
			internalProperties.newValue = internalProperties.initialValue;
			internalProperties.timeSinceLastChange = 0;
			internalProperties.returnVector = Vector3.zero;
			internalProperties.randomFrequency = UnityEngine.Random.Range(properties.FrequencyMin, properties.FrequencyMax);
			internalProperties.randomNoiseFrequency = Vector3.zero;
			internalProperties.randomAmplitude = Vector3.zero;
			internalProperties.timeSinceLastPause = 0;
			internalProperties.pauseDuration = 0;
			internalProperties.noiseElapsedTime = 0;
			internalProperties.curveDirection = 1f;
			properties.LimitedTimeLeft = properties.LimitedTimeTotal;

			RandomizeVector3(ref internalProperties.randomAmplitude, properties.AmplitudeMin, properties.AmplitudeMax);
			RandomizeVector3(ref internalProperties.randomNoiseFrequency, properties.NoiseFrequencyMin, properties.NoiseFrequencyMax);
			RandomizeVector3(ref internalProperties.randomNoiseShift, properties.NoiseShiftMin, properties.NoiseShiftMax);
			RandomizeVector3(ref internalProperties.remapZero, properties.RemapCurveZeroMin, properties.RemapCurveZeroMax);
			RandomizeVector3(ref internalProperties.remapOne, properties.RemapCurveOneMin, properties.RemapCurveOneMax);

			if (properties.ForceVectorLength)
			{
				internalProperties.randomAmplitude = internalProperties.randomAmplitude.normalized * properties.ForcedVectorLength; 
			}

			internalProperties.newValue = DetermineNewValue(properties, internalProperties.newValue, internalProperties.initialValue, ref internalProperties.startValue, 
				ref internalProperties.randomAmplitude, ref internalProperties.randomFrequency, ref internalProperties.pauseDuration, true);
		}

		/// <summary>
		/// Every frame we update our object's position, rotation and scale
		/// </summary>
		protected virtual void Update()
		{
			if (UpdateMode == UpdateModes.Update)
			{
				ProcessUpdate();
			}
		}

		/// <summary>
		/// Every frame we update our object's position, rotation and scale
		/// </summary>
		protected virtual void LateUpdate()
		{
			if (UpdateMode == UpdateModes.LateUpdate)
			{
				ProcessUpdate();
			}
		}

		/// <summary>
		/// Every frame we update our object's position, rotation and scale
		/// </summary>
		protected virtual void FixedUpdate()
		{
			if (UpdateMode == UpdateModes.FixedUpdate)
			{
				ProcessUpdate();
			}
		}

		/// <summary>
		/// Meant to be executed at the selected UpdateMode
		/// </summary>
		protected virtual void ProcessUpdate()
		{
			_positionInternalProperties.returnVector = transform.localPosition;
			if (UpdateValue(PositionActive, PositionWiggleProperties, ref _positionInternalProperties))
			{
				transform.localPosition = _positionInternalProperties.returnVector;
			}

			_rotationInternalProperties.returnVector = transform.localEulerAngles;
			if (UpdateValue(RotationActive, RotationWiggleProperties, ref _rotationInternalProperties))
			{
				transform.localEulerAngles = _rotationInternalProperties.returnVector;
			}

			_scaleInternalProperties.returnVector = transform.localScale;
			if (UpdateValue(ScaleActive, ScaleWiggleProperties, ref _scaleInternalProperties))
			{
				transform.localScale = _scaleInternalProperties.returnVector;
			}
		}

		/// <summary>
		/// Computes the next Vector3 value for the specified property
		/// </summary>
		/// <param name="valueActive"></param>
		/// <param name="properties"></param>
		/// <param name="internalProperties"></param>
		/// <returns></returns>
		protected virtual bool UpdateValue(bool valueActive, WiggleProperties properties, ref InternalWiggleProperties internalProperties)
		{
			if (!valueActive) { return false; }
			if (!properties.WigglePermitted) { return false;  }

			// handle limited time
			if ((properties.LimitedTime) && (properties.LimitedTimeTotal > 0f))
			{
				float timeSave = properties.LimitedTimeLeft;
				properties.LimitedTimeLeft -= properties.GetDeltaTime();
				if (properties.LimitedTimeLeft <= 0)
				{
					if (timeSave > 0f)
					{
						if (properties.LimitedTimeResetValue)
						{
							internalProperties.returnVector = internalProperties.limitedTimeValueSave;
							properties.LimitedTimeLeft = 0;
							properties.WigglePermitted = false;
							return true;
						}
					}                    
					return false;
				}
			}

			switch (properties.WiggleType)
			{
				case WiggleTypes.PingPong:
					return MoveVector3TowardsTarget(ref internalProperties.returnVector, properties, ref internalProperties.startValue, internalProperties.initialValue, 
						ref internalProperties.newValue, ref internalProperties.timeSinceLastPause, 
						ref internalProperties.timeSinceLastChange, ref internalProperties.randomAmplitude, 
						ref internalProperties.randomFrequency, 
						ref internalProperties.pauseDuration, internalProperties.randomFrequency);
                    

				case WiggleTypes.Random:
					return MoveVector3TowardsTarget(ref internalProperties.returnVector, properties, ref internalProperties.startValue, internalProperties.initialValue, 
						ref internalProperties.newValue, ref internalProperties.timeSinceLastPause, 
						ref internalProperties.timeSinceLastChange, ref internalProperties.randomAmplitude, 
						ref internalProperties.randomFrequency, 
						ref internalProperties.pauseDuration, internalProperties.randomFrequency);

				case WiggleTypes.Noise:
					internalProperties.returnVector = AnimateNoiseValue(ref internalProperties, properties);                    
					return true;

				case WiggleTypes.Curve:
					internalProperties.returnVector = AnimateCurveValue(ref internalProperties, properties);
					return true;
			}
			return false;
		}

		/// <summary>
		/// Applies a falloff to the computed value based on time spent and a falloff animation curve
		/// </summary>
		/// <param name="newValue"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected float ApplyFalloff(WiggleProperties properties)
		{
			float newValue = 1f;
			if ((properties.LimitedTime) && (properties.LimitedTimeTotal > 0f))
			{
				float curveProgress = (properties.LimitedTimeTotal - properties.LimitedTimeLeft) / properties.LimitedTimeTotal;
				newValue = properties.LimitedTimeFalloff.Evaluate(curveProgress);
			}
			return newValue;
		}

		/// <summary>
		/// Animates a Vector3 value along a perlin noise
		/// </summary>
		/// <param name="internalProperties"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected virtual Vector3 AnimateNoiseValue(ref InternalWiggleProperties internalProperties, WiggleProperties properties)
		{
			internalProperties.noiseElapsedTime += properties.GetDeltaTime();

			internalProperties.newValue.x = (Mathf.PerlinNoise(internalProperties.randomNoiseFrequency.x * internalProperties.noiseElapsedTime, internalProperties.randomNoiseShift.x) * 2.0f - 1.0f) * internalProperties.randomAmplitude.x;
			internalProperties.newValue.y = (Mathf.PerlinNoise(internalProperties.randomNoiseFrequency.y * internalProperties.noiseElapsedTime, internalProperties.randomNoiseShift.y) * 2.0f - 1.0f) * internalProperties.randomAmplitude.y;
			internalProperties.newValue.z = (Mathf.PerlinNoise(internalProperties.randomNoiseFrequency.z * internalProperties.noiseElapsedTime, internalProperties.randomNoiseShift.z) * 2.0f - 1.0f) * internalProperties.randomAmplitude.z;

			internalProperties.newValue *= ApplyFalloff(properties);
            
			if (properties.RelativeAmplitude)
			{
				internalProperties.newValue += internalProperties.initialValue;
			}

			if (properties.UniformValues)
			{
				internalProperties.newValue.y = internalProperties.newValue.x;
				internalProperties.newValue.z = internalProperties.newValue.x;
			}

			return internalProperties.newValue;
		}

		/// <summary>
		/// Animates a Vector3 value along a specified curve
		/// </summary>
		/// <param name="internalProperties"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		protected virtual Vector3 AnimateCurveValue(ref InternalWiggleProperties internalProperties, WiggleProperties properties)
		{
			internalProperties.timeSinceLastPause += properties.GetDeltaTime();
			internalProperties.timeSinceLastChange += properties.GetDeltaTime();

			// handle pause
			if (internalProperties.timeSinceLastPause < internalProperties.pauseDuration)
			{
				float curveProgress = (internalProperties.curveDirection == 1f) ? 1f : 0f;

				EvaluateCurve(properties.Curve, curveProgress, internalProperties.remapZero, internalProperties.remapOne, ref internalProperties.newValue, properties);
				if (properties.RelativeCurveAmplitude)
				{
					internalProperties.newValue += internalProperties.initialValue;
				}
			}

			// if we're just out of a pause
			if (internalProperties.timeSinceLastPause == internalProperties.timeSinceLastChange)
			{
				internalProperties.timeSinceLastChange = 0f;
			}

			// if we've reached the end
			if (internalProperties.randomFrequency > 0)
			{
				float curveProgress = (internalProperties.timeSinceLastChange) / internalProperties.randomFrequency;
				if (internalProperties.curveDirection < 0f)
				{
					curveProgress = 1 - curveProgress;
				}

				EvaluateCurve(properties.Curve, curveProgress, internalProperties.remapZero, internalProperties.remapOne, ref internalProperties.newValue, properties);
                
				if (internalProperties.timeSinceLastChange > internalProperties.randomFrequency)
				{
					internalProperties.timeSinceLastChange = 0f;
					internalProperties.timeSinceLastPause = 0f;
					if (properties.CurvePingPong)
					{
						internalProperties.curveDirection = -internalProperties.curveDirection;
					}                    

					RandomizeFloat(ref internalProperties.randomFrequency, properties.FrequencyMin, properties.FrequencyMax);
				}
			}
            
			if (properties.RelativeCurveAmplitude)
			{
				internalProperties.newValue = internalProperties.initialValue + internalProperties.newValue;
			}
			
			return internalProperties.newValue;
		}

		protected virtual void EvaluateCurve(AnimationCurve curve, float percent, Vector3 remapMin, Vector3 remapMax, ref Vector3 returnValue, WiggleProperties properties)
		{
			returnValue.x = MMFeedbacksHelpers.Remap(curve.Evaluate(percent), 0f, 1f, remapMin.x, remapMax.x);
			returnValue.y = MMFeedbacksHelpers.Remap(curve.Evaluate(percent), 0f, 1f, remapMin.y, remapMax.y);
			returnValue.z = MMFeedbacksHelpers.Remap(curve.Evaluate(percent), 0f, 1f, remapMin.z, remapMax.z);
			returnValue *= ApplyFalloff(properties);
		}

		/// <summary>
		/// Moves a vector3's values towards a target
		/// </summary>
		/// <param name="movedValue"></param>
		/// <param name="properties"></param>
		/// <param name="startValue"></param>
		/// <param name="initialValue"></param>
		/// <param name="destinationValue"></param>
		/// <param name="timeSinceLastPause"></param>
		/// <param name="timeSinceLastValueChange"></param>
		/// <param name="randomAmplitude"></param>
		/// <param name="randomFrequency"></param>
		/// <param name="pauseDuration"></param>
		/// <param name="frequency"></param>
		/// <returns></returns>
		protected virtual bool MoveVector3TowardsTarget(ref Vector3 movedValue, WiggleProperties properties, ref Vector3 startValue, Vector3 initialValue, 
			ref Vector3 destinationValue, ref float timeSinceLastPause, ref float timeSinceLastValueChange, 
			ref Vector3 randomAmplitude, ref float randomFrequency,
			ref float pauseDuration, float frequency)
		{
			timeSinceLastPause += properties.GetDeltaTime();
			timeSinceLastValueChange += properties.GetDeltaTime();

			// handle pause
			if (timeSinceLastPause < pauseDuration)
			{
				return false;
			}
            
			// if we're just out of a pause
			if (timeSinceLastPause == timeSinceLastValueChange)
			{
				timeSinceLastValueChange = 0f;
			}

			// if we've reached the end
			if (frequency > 0)
			{
				float curveProgress = (timeSinceLastValueChange) / frequency;

				if (!properties.UseSpeedCurve)
				{
					movedValue = Vector3.Lerp(startValue, destinationValue, curveProgress);
				}
				else
				{
					float curvePercent = properties.SpeedCurve.Evaluate(curveProgress);
					movedValue = Vector3.LerpUnclamped(startValue, destinationValue, curvePercent);
				}

				if (timeSinceLastValueChange > frequency)
				{
					timeSinceLastValueChange = 0f;
					timeSinceLastPause = 0f;
					movedValue = destinationValue;
					destinationValue = DetermineNewValue(properties, movedValue, initialValue, ref startValue, 
						ref randomAmplitude, ref randomFrequency, ref pauseDuration);
				}
			}
			return true;
		}

		/// <summary>
		/// Picks a new target value
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="newValue"></param>
		/// <param name="initialValue"></param>
		/// <param name="startValue"></param>
		/// <param name="randomAmplitude"></param>
		/// <param name="randomFrequency"></param>
		/// <param name="pauseDuration"></param>
		/// <returns></returns>
		protected virtual Vector3 DetermineNewValue(WiggleProperties properties, Vector3 newValue, Vector3 initialValue, ref Vector3 startValue, 
			ref Vector3 randomAmplitude, ref float randomFrequency, ref float pauseDuration, bool firstPlay = false)
		{
			switch (properties.WiggleType)
			{
				case WiggleTypes.PingPong:
					if (properties.RelativeAmplitude)
					{
						if (firstPlay)
						{
							startValue = properties.AmplitudeMin * ApplyFalloff(properties) + initialValue;
							newValue = properties.AmplitudeMax * ApplyFalloff(properties) + initialValue;
						}
						else
						{
							if (newValue == properties.AmplitudeMin + initialValue)
							{
								startValue = newValue;
								newValue = properties.AmplitudeMax * ApplyFalloff(properties) + initialValue;
							}
							else
							{
								startValue = newValue;
								newValue = properties.AmplitudeMin  * ApplyFalloff(properties) + initialValue;
							}
						}
					}
					else
					{
						if (firstPlay)
						{
							startValue = properties.AmplitudeMin * ApplyFalloff(properties);
							newValue = properties.AmplitudeMax * ApplyFalloff(properties);
						}
						else
						{
							startValue = newValue;
							newValue = (newValue == properties.AmplitudeMin) ? properties.AmplitudeMax * ApplyFalloff(properties) : properties.AmplitudeMin;	
						}
					}                    
					RandomizeFloat(ref randomFrequency, properties.FrequencyMin, properties.FrequencyMax);
					RandomizeFloat(ref pauseDuration, properties.PauseMin, properties.PauseMax);

					if (properties.UniformValues)
					{
						newValue.y = newValue.x;
						newValue.z = newValue.x;
					}
					
					return newValue;

				case WiggleTypes.Random:
					startValue = newValue;
					RandomizeFloat(ref randomFrequency, properties.FrequencyMin, properties.FrequencyMax);
					RandomizeVector3(ref randomAmplitude, properties.AmplitudeMin, properties.AmplitudeMax);
					RandomizeFloat(ref pauseDuration, properties.PauseMin, properties.PauseMax);
					newValue = randomAmplitude;
                    
					if (properties.UniformValues)
					{
						newValue.y = newValue.x;
						newValue.z = newValue.x;
					}
                    
					newValue *= ApplyFalloff(properties);
					if (properties.RelativeAmplitude)
					{
						newValue += initialValue;
					}
                    
					return newValue;
			}
			return Vector3.zero;            
		}
        
		/// <summary>
		/// Randomizes a float between bounds
		/// </summary>
		/// <param name="randomizedFloat"></param>
		/// <param name="floatMin"></param>
		/// <param name="floatMax"></param>
		/// <returns></returns>
		protected virtual float RandomizeFloat(ref float randomizedFloat, float floatMin, float floatMax)
		{
			randomizedFloat = UnityEngine.Random.Range(floatMin, floatMax);
			return randomizedFloat;
		}

		/// <summary>
		/// Randomizes a vector3 within bounds
		/// </summary>
		/// <param name="randomizedVector"></param>
		/// <param name="vectorMin"></param>
		/// <param name="vectorMax"></param>
		/// <returns></returns>
		protected virtual Vector3 RandomizeVector3(ref Vector3 randomizedVector, Vector3 vectorMin, Vector3 vectorMax)
		{
			randomizedVector.x = UnityEngine.Random.Range(vectorMin.x, vectorMax.x);
			randomizedVector.y = UnityEngine.Random.Range(vectorMin.y, vectorMax.y);
			randomizedVector.z = UnityEngine.Random.Range(vectorMin.z, vectorMax.z);
			return randomizedVector;
		}
		
		public virtual void RestoreInitialValues()
		{
			transform.localPosition = _positionInternalProperties.initialValue;
			transform.localEulerAngles = _rotationInternalProperties.initialValue;
			transform.localScale = _scaleInternalProperties.initialValue;
		}

		/// <summary>
		/// On Validate, if the app is running, we reinitialize to allow for faster iteration times
		/// </summary>
		protected virtual void OnValidate()
		{
			if (!Application.isPlaying)
			{
				return;
			}
			Initialization();
		}
	}
}