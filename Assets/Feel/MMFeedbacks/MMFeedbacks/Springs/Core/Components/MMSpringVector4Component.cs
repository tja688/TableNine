using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// An event used to pilot a MMSpringVector4 component
	/// </summary>
	public struct MMSpringVector4Event
	{
		static MMSpringVector4Event e;
		
		public MMChannelData ChannelData;
		public MMSpringComponentBase TargetSpring;
		public SpringCommands Command;
		public Vector4 MoveToValue;
		public Vector4 BumpAmount;
		public Vector4 MoveToRandomValueMin;
		public Vector4 MoveToRandomValueMax;
		public Vector4 BumpAmountRandomValueMin;
		public Vector4 BumpAmountRandomValueMax;
		public bool OverrideDamping;
		public Vector4 NewDamping;
		public bool OverrideFrequency;
		public Vector4 NewFrequency;
		
		public static void Trigger(SpringCommands command, MMSpringComponentBase targetSpring, MMChannelData channelData, 
			Vector4 moveToValue = default, Vector4 bumpAmount = default,
			Vector4 moveToRandomValueMin = default, Vector4 moveToRandomValueMax = default,
			Vector4 bumpAmountRandomValueMin = default, Vector4 bumpAmountRandomValueMax = default,
			bool overrideDamping = false, Vector4 newDamping = default, bool overrideFrequency = false, Vector4 newFrequency = default)
		{
			e.ChannelData = channelData;
			e.TargetSpring = targetSpring;
			e.Command = command;
			e.MoveToValue = moveToValue;
			e.BumpAmount = bumpAmount;
			e.MoveToRandomValueMin = moveToRandomValueMin;
			e.MoveToRandomValueMax = moveToRandomValueMax;
			e.BumpAmountRandomValueMin = bumpAmountRandomValueMin;
			e.BumpAmountRandomValueMax = bumpAmountRandomValueMax;
			e.OverrideDamping = overrideDamping;
			e.NewDamping = newDamping;
			e.OverrideFrequency = overrideFrequency;
			e.NewFrequency = newFrequency;
			MMEventManager.TriggerEvent(e);
		}
	}	
	
	/// <summary>
	/// A 弹簧 component used to pilot Vector4 values on a target
	/// </summary>
	public abstract class MMSpringVector4<T> : MMSpringComponentBase, MMEventListener<MMSpringVector4Event> where T:Component
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
		[Header("SpringVector4")]
		public MMSpringVector4 SpringVector4 = new MMSpringVector4();
		
		[MMInspectorGroup("Randomness", true, 12, true)]
		
		[Header("Move To Random")]
		/// 调用 `MoveToRandom()` 时，随机值取样范围的最小向量。
		[Tooltip("调用 `MoveToRandom()` 时，随机值取样范围的最小向量。")]
		public Vector4 MoveToRandomValueMin = new Vector4(-2f, -2f, -2f, -2f);
		/// 调用 `MoveToRandom()` 时，随机值取样范围的最大向量。
		[Tooltip("调用 `MoveToRandom()` 时，随机值取样范围的最大向量。")]
		public Vector4 MoveToRandomValueMax = new Vector4(2f, 2f, 2f, 2f);
		
		[Header("Bump Random")]
		/// 调用 `BumpRandom()` 时，随机扰动值取样范围的最小向量。
		[Tooltip("调用 `BumpRandom()` 时，随机扰动值取样范围的最小向量。")]
		public Vector2 BumpAmountRandomValueMin = new Vector4(-20f, -20f, -20f, -20f);
		/// 调用 `BumpRandom()` 时，随机扰动值取样范围的最大向量。
		[Tooltip("调用 `BumpRandom()` 时，随机扰动值取样范围的最大向量。")]
		public Vector2 BumpAmountRandomValueMax = new Vector4(20f, 20f, 20f, 20f);

		[MMInspectorGroup("Test", true, 20, true)]
		/// 在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。
		[Tooltip("在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。")]
		public Vector4 TestMoveToValue = new Vector4(2f, 2f, 2f, 2f);
		[MMInspectorButtonBar(new string[] { "MoveTo", "MoveToAdditive", "MoveToSubtractive", "MoveToRandom", "MoveToInstant" }, 
			new string[] { "TestMoveTo", "TestMoveToAdditive", "TestMoveToSubtractive", "TestMoveToRandom", "TestMoveToInstant" }, 
			new bool[] { true, true, true, true, true },
			new string[] { "main-call-to-action", "", "", "", "" })]
		public bool MoveToToolbar;
		
		/// 在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。
		[Tooltip("在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。")]
		public Vector4 TestBumpAmount = new Vector4(75f, 100f, 50f, 25f);
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
		
		public override bool LowVelocity => (Mathf.Abs(SpringVector4.SpringX.Velocity) + Mathf.Abs(SpringVector4.SpringY.Velocity) + Mathf.Abs(SpringVector4.SpringZ.Velocity) + Mathf.Abs(SpringVector4.SpringW.Velocity)) < _velocityLowThreshold;
		public float DeltaTime => (TimeScaleMode == TimeScaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime;
		public virtual Vector4 TargetVector4 { get; set; }
		
		#region PUBLIC_API
		
		public virtual void MoveTo(Vector4 newValue)
		{
			Activate();
			SpringVector4.MoveTo(newValue);
		}
		
		public virtual void MoveToAdditive(Vector4 newValue)
		{
			Activate();
			SpringVector4.MoveToAdditive(newValue);
		}
		
		public virtual void MoveToSubtractive(Vector4 newValue)
		{
			Activate();
			SpringVector4.MoveToSubtractive(newValue);
		}

		public virtual void MoveToRandom()
		{
			Activate();
			SpringVector4.MoveToRandom(MoveToRandomValueMin, MoveToRandomValueMax);
		}

		public virtual void MoveToInstant(Vector4 newValue)
		{
			Activate();
			SpringVector4.MoveToInstant(newValue);
		}

		public virtual void MoveToRandom(Vector4 min, Vector4 max)
		{
			Activate();
			SpringVector4.MoveToRandom(min, max);
		}

		public virtual void Bump(Vector4 bumpAmount)
		{
			Activate();
			SpringVector4.Bump(bumpAmount);
		}

		public virtual void BumpRandom()
		{
			Activate();
			SpringVector4.BumpRandom(BumpAmountRandomValueMin, BumpAmountRandomValueMax);
		}

		public virtual void BumpRandom(Vector4 min, Vector4 max)
		{
			Activate();
			SpringVector4.BumpRandom(min, max);
		}
		
		public override void Stop()
		{
			base.Stop();
			this.enabled = false;
			GrabCurrentValue();
			SpringVector4.Stop();
		}
		
		public override void RestoreInitialValue()
		{
			SpringVector4.RestoreInitialValue();
			ApplyValue(SpringVector4.CurrentValue);
		}
		
		public override void ResetInitialValue()
		{
			SpringVector4.RestoreInitialValue();
		}
		
		protected override void UpdateSpringValue()
		{
			SpringVector4.UpdateSpringValue(DeltaTime);
			ApplyValue(SpringVector4.CurrentValue);
		}
		
		public override void Finish()
		{
			SpringVector4.Finish();
			ApplyValue(SpringVector4.CurrentValue);
		}
		
		#endregion

		#region INTERNAL
		
		protected override void Initialization()
		{
			base.Initialization();
			GrabCurrentValue();
			SpringVector4.SetInitialValue(SpringVector4.CurrentValue);
			SpringVector4.TargetValue = SpringVector4.CurrentValue;
		}

		protected virtual void ApplyValue(Vector4 newValue)
		{
			TargetVector4 = newValue;
		}
		
		protected override void GrabCurrentValue()
		{
			base.GrabCurrentValue();
			SpringVector4.CurrentValue = TargetVector4;
		}
		
		#endregion
		
		#region EVENTS
		
		public void OnMMEvent(MMSpringVector4Event 弹簧Event)
		{
			bool eventMatch = 弹簧Event.ChannelData != null && MMChannel.Match(弹簧Event.ChannelData, ChannelMode, Channel, MMChannelDefinition);
			bool targetMatch = 弹簧Event.TargetSpring != null && 弹簧Event.TargetSpring.Equals(this);
			if (!eventMatch && !targetMatch)
			{
				return;
			}
			
			if (弹簧Event.OverrideDamping)
			{
				SpringVector4.SetDamping(弹簧Event.NewDamping);
			}
			if (弹簧Event.OverrideFrequency)
			{
				SpringVector4.SetFrequency(弹簧Event.NewFrequency);
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
					MoveToRandom(弹簧Event.MoveToRandomValueMin, 弹簧Event.MoveToRandomValueMax);
					break;
				case SpringCommands.MoveToInstant:
					MoveToInstant(弹簧Event.MoveToValue);
					break;
				case SpringCommands.Bump:
					Bump(弹簧Event.BumpAmount);
					break;
				case SpringCommands.BumpRandom:
					BumpRandom(弹簧Event.BumpAmountRandomValueMin, 弹簧Event.BumpAmountRandomValueMax);
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
			this.MMEventStartListening<MMSpringVector4Event>();
		}

		protected void OnDestroy()
		{
			this.MMEventStopListening<MMSpringVector4Event>();
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
