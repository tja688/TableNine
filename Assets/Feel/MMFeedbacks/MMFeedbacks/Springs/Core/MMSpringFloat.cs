using System;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[Serializable]
	public class MMSpringFloat : MMSpringDefinition<float>
	{
		/// 阻尼比决定 spring 在受到扰动后恢复的速度。数值较低时会振荡较久；越接近 `1`，停止振荡就越快。
		[Tooltip("阻尼比决定 spring 在受到扰动后恢复的速度。数值较低时会振荡较久；越接近 `1`，停止振荡就越快。")]
		[Range(0.01f, 1f)]
		public float Damping = 0.4f;
		/// 频率决定 spring 在受到扰动后振荡的快慢。频率越低，每秒振荡次数越少；频率越高，每秒振荡次数越多。
		[Tooltip("频率决定 spring 在受到扰动后振荡的快慢。频率越低，每秒振荡次数越少；频率越高，每秒振荡次数越多。")]
		public float Frequency = 6f;

		[MMInspectorGroup("Debug", true, 19, true)]
		/// 当前 spring 的数值。
		[Tooltip("当前弹簧的数值。")]
		public override float CurrentValue
		{
			get
			{
				return _returnCurrentValue;
			}
			set
			{
				_actualCurrentValue = value;
				_returnCurrentValue = value;
				UpdateSpringDebug();
			}
		}

		public MMSpringClampSettings ClampSettings = new MMSpringClampSettings();
		
		/// spring 当前趋近的目标值；当振荡停止后，它最终会到达这里。
		[Tooltip("spring 当前趋近的目标值；当振荡停止后，它最终会到达这里。")]
		public override float TargetValue
		{
			get
			{
				return _targetValue;
			}
			set
			{
				_targetValue = ClampSettings.GetTargetValue(value, InitialValue);
				UpdateSpringDebug();
			}
		}

		/// spring 当前的速度值。
		[Tooltip("春天 当前的速度值。")]
		public override float Velocity
		{
			get
			{
				return _velocity;
			}
			set
			{
				_velocity = value;
				UpdateSpringDebug();
			}
		}
		
		public float InitialValue { get; protected set; }
		
		public MMSpringDebug SpringDebug = new MMSpringDebug();

		[MMHidden]
		public bool UnifiedSpring = false;
		[MMHidden]
		public float CurrentValueDisplay;
		[MMHidden]
		public float TargetValueDisplay;
		[MMHidden]
		public float VelocityDisplay;
		
		protected float _actualCurrentValue;
		protected float _returnCurrentValue;
		protected float _targetValue;
		protected float _velocity;

		public override void UpdateSpringValue(float deltaTime)
		{
			MMMaths.Spring(ref _actualCurrentValue, TargetValue, ref _velocity, Damping, Frequency, deltaTime);
			_returnCurrentValue = _actualCurrentValue;
			if (ClampSettings.ClampNeeded)
			{
				HandleClampMode();
			}
			UpdateSpringDebug();
		}

		protected virtual void HandleClampMode()
		{
			float minValue = ClampSettings.ClampMinInitial ? InitialValue : ClampSettings.ClampMinValue;
			float maxValue = ClampSettings.ClampMaxInitial ? InitialValue : ClampSettings.ClampMaxValue;
			
			if (ClampSettings.ClampMin && (_actualCurrentValue < minValue))
			{
				
				if (ClampSettings.ClampMinBounce)
				{
					_returnCurrentValue = Mathf.Abs(_actualCurrentValue - minValue) + minValue;
				}
				else
				{
					_returnCurrentValue = Mathf.Max(_actualCurrentValue, minValue);	
				}
			}
			
			if (ClampSettings.ClampMax && (_actualCurrentValue > maxValue))
			{
				if (ClampSettings.ClampMaxBounce)
				{
					_returnCurrentValue = maxValue - (_actualCurrentValue - maxValue);
				}
				else
				{
					_returnCurrentValue = Mathf.Min(_actualCurrentValue, maxValue);	
				}
			}
		}

		protected virtual void UpdateSpringDebug() 
		{
			#if UNITY_EDITOR
			CurrentValueDisplay = (float)Math.Round(CurrentValue,3);
			TargetValueDisplay = (float)Math.Round(TargetValue,3);
			VelocityDisplay = (float)Math.Round(Velocity,3);
			SpringDebug.Update(_returnCurrentValue, TargetValue);
			#endif
		}
		
		public override void MoveToInstant(float newValue)
		{
			_actualCurrentValue = newValue;
			_returnCurrentValue = newValue;
			TargetValue = newValue;
			Velocity = 0;
		}

		public override void Stop()
		{
			Velocity = 0f;
			TargetValue = _actualCurrentValue;
		}

		public override void SetInitialValue(float newInitialValue)
		{
			InitialValue = newInitialValue;
		}

		public override void RestoreInitialValue()
		{
			_actualCurrentValue = InitialValue;
			_returnCurrentValue = InitialValue;
			TargetValue = _actualCurrentValue;
			UpdateSpringDebug();
		}

		public override void SetCurrentValueAsInitialValue()
		{
			InitialValue = _actualCurrentValue;
		}
		
		public override void MoveTo(float newValue)
		{
			TargetValue = newValue;
		}
		
		public override void MoveToAdditive(float newValue)
		{
			TargetValue += newValue;
		}
		
		public override void MoveToSubtractive(float newValue)
		{
			TargetValue -= newValue;
		}

		public override void MoveToRandom(float min, float max)
		{
			TargetValue = UnityEngine.Random.Range(min, max);
		}

		public override void Bump(float bumpAmount)
		{
			Velocity += bumpAmount;
		}

		public override void BumpRandom(float min, float max)
		{
			Velocity += UnityEngine.Random.Range(min, max);
		}
		
		public override void Finish()
		{
			Velocity = 0f;
			_actualCurrentValue = TargetValue;
			_returnCurrentValue = TargetValue;
			UpdateSpringDebug();
		}
	}
}