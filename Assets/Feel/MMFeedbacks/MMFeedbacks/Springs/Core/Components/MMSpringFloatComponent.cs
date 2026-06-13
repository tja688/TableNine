using System;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// The possible commands used to pilot a 弹簧
	/// MoveTo : move the current value of the 弹簧 to the MoveToValue specified in the event
	/// MoveToAdditive : adds the MoveToValue specified in the event to the current target value of the 弹簧
	/// MoveToSubtractive : subtracts the MoveToValue specified in the event to the current target value of the 弹簧
	/// MoveToRandom : moves the current value of the 弹簧 to a random value using MoveToRandomValue
	/// MoveToInstant : instantly moves the current value of the 弹簧 to the MoveToValue specified in the event
	/// Bump : 弹跳s the 弹簧 by the BumpAmount specified in the event
	/// BumpRandom : 弹跳s the 弹簧 by a random amount specified in the event
	/// Stop : stops the 弹簧 instantly
	/// Finish : instantly moves the 弹簧 to its final target value
	/// RestoreInitialValue : restores the 弹簧's initial value
	/// ResetInitialValue : resets the 弹簧's initial value to its current value
	/// </summary>
	public enum SpringCommands { MoveTo, MoveToAdditive, MoveToSubtractive, MoveToRandom, MoveToInstant, Bump, BumpRandom, Stop, Finish, RestoreInitialValue, ResetInitialValue }
	
	/// <summary>
	/// An event used to pilot a MMSpringColor component
	/// </summary>
	public struct MMSpringFloatEvent
	{
		static MMSpringFloatEvent e;
		
		public MMChannelData ChannelData;
		public MMSpringComponentBase TargetSpring;
		public SpringCommands Command;
		public float MoveToValue;
		public float BumpAmount;
		public Vector2 MoveToRandomValue;
		public Vector2 BumpAmountRandomValue;
		public bool OverrideDamping;
		public float NewDamping;
		public bool OverrideFrequency;
		public float NewFrequency;
		
		public static void Trigger(SpringCommands command, MMSpringComponentBase targetSpring, MMChannelData channelData, 
			float moveToValue = 1f, float bumpAmount = 1f, Vector2 moveToRandomValue = default, Vector2 bumpAmountRandomValue = default, 
			bool overrideDamping = false, float newDamping = 0.8f, bool overrideFrequency = false, float newFrequency = 5f)
		{
			e.ChannelData = channelData;
			e.TargetSpring = targetSpring;
			e.Command = command;
			e.MoveToValue = moveToValue;
			e.BumpAmount = bumpAmount;
			e.MoveToRandomValue = moveToRandomValue;
			e.BumpAmountRandomValue = bumpAmountRandomValue;
			e.OverrideDamping = overrideDamping;
			e.NewDamping = newDamping;
			e.OverrideFrequency = overrideFrequency;
			e.NewFrequency = newFrequency;
			MMEventManager.TriggerEvent(e);
		}
	}	
	
	/// <summary>
	/// A 弹簧 component used to pilot float values on a target
	/// </summary>
	public abstract class MMSpringFloatComponent<T> : MMSpringComponentBase, MMEventListener<MMSpringFloatEvent> where T:Component
	{
		[MMInspectorGroup("Target", true, 17)] 
		public T Target;
		
		[MMInspectorGroup("Channel & TimeScale", true, 16, true)] 
		/// 此 弹簧 使用 `scaled time` 还是 `unscaled time`。前者会受到时间缩放影响，后者不会。
		[Tooltip("此弹簧使用 `受时间缩放影响的时间` 还是 `不受时间缩放影响的时间`。前者会受到时间缩放影响，后者不会。")]
		public TimeScaleModes TimeScaleMode = TimeScaleModes.Scaled;
		/// whether to listen on a channel defined by an int or by a MMChannel scriptable object. Ints are simple to setup but can get messy and make it harder to remember what int corresponds to what.
		/// MMChannel scriptable objects require you to create them in advance, but come with a readable name and are more scalable
		[Tooltip("决定此组件是监听 `int` 定义的通道，还是监听 `MMChannel` ScriptableObject 定义的通道。`int` 配置简单，但项目一大就容易混乱，也不便记忆每个数字代表什么；`MMChannel` 需要预先创建资源，但名称更直观，也更适合扩展。")]
		public MMChannelModes ChannelMode = MMChannelModes.Int;
		/// 要监听的通道，必须与对应 反馈 上配置的通道一致。
		[Tooltip("要监听的通道，必须与触发它的反馈上配置的通道一致。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The 反馈s targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("用于监听事件的`通道资源`定义资源。只有引用同一个`通道资源`定义的反馈，才能触发这个弹簧组件。若要创建`通道资源`，可以在项目视图中右键（通常放在数据文件夹中），选择更多山脉 >通道资源，并说明说明。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;
		
		[MMInspectorGroup("Spring Settings", true, 18)]
		public MMSpringFloat FloatSpring = new MMSpringFloat();
		
		[MMInspectorGroup("Randomness", true, 12, true)]
		/// 调用 `MoveToRandom` 时，随机目标值的最小值（`x`）与最大值（`y`）。
		[Tooltip("调用 `MoveToRandom` 时，随机目标值的最小值（`x`）与最大值（`y`）。")]
		[MMVector("Min", "Max")]
		public Vector2 MoveToRandomValue = new Vector2(-2f, 2f);
		/// 调用 `BumpRandom` 时，随机弹跳值的最小值（`x`）与最大值（`y`）。
		[Tooltip("调用 `BumpRandom` 时，随机弹跳值的最小值（`x`）与最大值（`y`）。")]
		[MMVector("Min", "Max")]
		public Vector2 BumpAmountRandomValue = new Vector2(20f, 100f);
		
		[MMInspectorGroup("Test", true, 20, true)]
		/// 在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。
		[Tooltip("在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。")]
		public float TestMoveToValue = 2f;
		[MMInspectorButtonBar(new string[] { "MoveTo", "MoveToAdditive", "MoveToSubtractive", "MoveToRandom", "MoveToInstant" }, 
			new string[] { "TestMoveTo", "TestMoveToAdditive", "TestMoveToSubtractive", "TestMoveToRandom", "TestMoveToInstant" }, 
			new bool[] { true, true, true, true, true },
		new string[] { "main-call-to-action", "", "", "", "" })]
		public bool MoveToToolbar;
		
		/// 在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。
		[Tooltip("在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。")]
		public float TestBumpAmount = 75f;
		[MMInspectorButtonBar(new string[] { "Bump", "BumpRandom" }, 
			new string[] { "TestBump", "TestBumpRandom" }, 
			new bool[] { true, true },
			new string[] { "main-call-to-action", "" })]
		public bool BumpToToolbar;
		
		[MMInspectorButtonBar(new string[] { "Stop", "Finish", "RestoreInitialValue", "ResetInitialValue" }, 
			new string[] { "Stop", "Finish", "RestoreInitialValue", "ResetInitialValue" }, 
			new bool[] { true, true, true, true },
			new string[] { "", "", "", "" })]
		public bool OtherControlsToToolbar;
		
		public override bool LowVelocity => Mathf.Abs(FloatSpring.Velocity) < _velocityLowThreshold;
		public float DeltaTime => (TimeScaleMode == TimeScaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime;

		public virtual float TargetFloat { get; set; }
		
		#region PUBLIC_API
		
		public virtual void MoveTo(float newValue)
		{
			Activate();
			FloatSpring.MoveTo(newValue);
		}
		
		public virtual void MoveToAdditive(float newValue)
		{
			Activate();
			FloatSpring.MoveToAdditive(newValue);
		}
		
		public virtual void MoveToSubtractive(float newValue)
		{
			Activate();
			FloatSpring.MoveToSubtractive(newValue);
		}

		public virtual void MoveToRandom()
		{
			Activate();
			FloatSpring.MoveToRandom(MoveToRandomValue.x, MoveToRandomValue.y);
		}

		public virtual void MoveToInstant(float newValue)
		{
			Activate();
			FloatSpring.MoveToInstant(newValue);
		}

		public virtual void MoveToRandom(float min, float max)
		{
			Activate();
			FloatSpring.MoveToRandom(min, max);
		}

		public virtual void Bump(float bumpAmount)
		{
			Activate();
			FloatSpring.Bump(bumpAmount);
		}

		public virtual void BumpRandom()
		{
			Activate();
			FloatSpring.BumpRandom(BumpAmountRandomValue.x, BumpAmountRandomValue.y);
		}

		public virtual void BumpRandom(float min, float max)
		{
			Activate();
			FloatSpring.BumpRandom(min, max);
		}
		
		public override void Stop()
		{
			base.Stop();
			this.enabled = false;
			GrabCurrentValue();
			FloatSpring.Stop();
		}
		
		public override void RestoreInitialValue()
		{
			FloatSpring.RestoreInitialValue();
			ApplyValue(FloatSpring.CurrentValue);
		}
		
		public override void ResetInitialValue()
		{
			FloatSpring.SetCurrentValueAsInitialValue();
		}
		
		protected override void UpdateSpringValue()
		{
			FloatSpring.UpdateSpringValue(DeltaTime);
			ApplyValue(FloatSpring.CurrentValue);
		}
		
		public override void Finish()
		{
			FloatSpring.Finish();
			ApplyValue(FloatSpring.CurrentValue);
		}
		
		#endregion

		#region INTERNAL
		
		protected override void Initialization()
		{
			base.Initialization();
			GrabCurrentValue();
			FloatSpring.SetInitialValue(FloatSpring.CurrentValue);
			FloatSpring.TargetValue = FloatSpring.CurrentValue;
		}

		protected virtual void ApplyValue(float newValue)
		{
			TargetFloat = newValue;
		}
		
		protected override void GrabCurrentValue()
		{
			base.GrabCurrentValue();
			FloatSpring.CurrentValue = TargetFloat;
		}

		#endregion

		#region EVENTS
		
		public void OnMMEvent(MMSpringFloatEvent 弹簧Event)
		{
			bool eventMatch = 弹簧Event.ChannelData != null && MMChannel.Match(弹簧Event.ChannelData, ChannelMode, Channel, MMChannelDefinition);
			bool targetMatch = 弹簧Event.TargetSpring != null && 弹簧Event.TargetSpring.Equals(this);
			if (!eventMatch && !targetMatch)
			{
				return;
			}
			
			if (弹簧Event.OverrideDamping)
			{
				FloatSpring.Damping = 弹簧Event.NewDamping;
			}
			if (弹簧Event.OverrideFrequency)
			{
				FloatSpring.Frequency = 弹簧Event.NewFrequency;
			}

			switch (弹簧Event.Command)
			{
				case SpringCommands.MoveTo:
					MoveTo(弹簧Event.MoveToValue);
					break;
				case SpringCommands.MoveToAdditive:
					MoveToAdditive(弹簧Event.MoveToValue);
					break;
				case SpringCommands.MoveToSubtractive:
					MoveToSubtractive(弹簧Event.MoveToValue);
					break;
				case SpringCommands.MoveToRandom:
					MoveToRandom(弹簧Event.MoveToRandomValue.x, 弹簧Event.MoveToRandomValue.y);
					break;
				case SpringCommands.MoveToInstant:
					MoveToInstant(弹簧Event.MoveToValue);
					break;
				case SpringCommands.Bump:
					Bump(弹簧Event.BumpAmount);
					break;
				case SpringCommands.BumpRandom:
					BumpRandom(弹簧Event.BumpAmountRandomValue.x, 弹簧Event.BumpAmountRandomValue.y);
					break;
				case SpringCommands.Stop:
					Stop();
					break;
				case SpringCommands.Finish:
					Finish();
					break;
				case SpringCommands.RestoreInitialValue:
					RestoreInitialValue();
					break;
				case SpringCommands.ResetInitialValue:
					ResetInitialValue();
					break;
			}
		}
		
		protected override void Awake()
		{
			if (Target == null)
			{
				Target = GetComponent<T>();
			}
			base.Awake();
			this.MMEventStartListening<MMSpringFloatEvent>();
		}

		protected void OnDestroy()
		{
			this.MMEventStopListening<MMSpringFloatEvent>();
		}

		#endregion

		#region TEST_METHODS

		protected override void TestMoveTo()
		{
			MoveTo(TestMoveToValue);
		}
		
		protected override void TestMoveToAdditive()
		{
			MoveToAdditive(TestMoveToValue);
		}
		
		protected override void TestMoveToSubtractive()
		{
			MoveToSubtractive(TestMoveToValue);
		}
		
		protected override void TestMoveToRandom()
		{
			MoveToRandom();
		}

		protected override void TestMoveToInstant()
		{
			MoveToInstant(TestMoveToValue);
		}

		protected override void TestBump()
		{
			Bump(TestBumpAmount);
		}
		
		protected override void TestBumpRandom()
		{
			BumpRandom();
		}

		#endregion
	}
}
