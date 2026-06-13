using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// An event used to pilot a MMSpringVector3 component
	/// </summary>
	public struct MMSpringVector3Event
	{
		static MMSpringVector3Event e;
		
		public MMChannelData ChannelData;
		public MMSpringComponentBase TargetSpring;
		public SpringCommands Command;
		public Vector3 MoveToValue;
		public Vector3 BumpAmount;
		public Vector3 MoveToRandomValueMin;
		public Vector3 MoveToRandomValueMax;
		public Vector3 BumpAmountRandomValueMin;
		public Vector3 BumpAmountRandomValueMax;
		public bool OverrideDamping;
		public Vector3 NewDamping;
		public bool OverrideFrequency;
		public Vector3 NewFrequency;
		
		public static void Trigger(SpringCommands command, MMSpringComponentBase targetSpring, MMChannelData channelData, 
			Vector3 moveToValue = default, Vector3 bumpAmount = default,
			Vector3 moveToRandomValueMin = default, Vector3 moveToRandomValueMax = default,
			Vector3 bumpAmountRandomValueMin = default, Vector3 bumpAmountRandomValueMax = default,
			bool overrideDamping = false, Vector3 newDamping = default, bool overrideFrequency = false, Vector3 newFrequency = default)
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
	/// A 弹簧 component used to pilot Vector3 values on a target
	/// </summary>
	public abstract class MMSpringVector3Component<T> : MMSpringComponentBase, MMEventListener<MMSpringVector3Event> where T:Component
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
		[Header("SpringVector3")]
		public MMSpringVector3 SpringVector3 = new MMSpringVector3();
		
		[MMInspectorGroup("Randomness", true, 12, true)]
		
		[Header("Move To Random")]
		/// 调用 `MoveToRandom()` 时，随机值取样范围的最小向量。
		[Tooltip("调用 `MoveToRandom()` 时，随机值取样范围的最小向量。")]
		public Vector3 MoveToRandomValueMin = new Vector3(-2f, -2f, -2f);
		/// 调用 `MoveToRandom()` 时，随机值取样范围的最大向量。
		[Tooltip("调用 `MoveToRandom()` 时，随机值取样范围的最大向量。")]
		public Vector3 MoveToRandomValueMax = new Vector3(2f, 2f, 2f);
		
		[Header("Bump Random")]
		/// 调用 `BumpRandom()` 时，随机扰动值取样范围的最小向量。
		[Tooltip("调用 `BumpRandom()` 时，随机扰动值取样范围的最小向量。")]
		public Vector3 BumpAmountRandomValueMin = new Vector3(-20f, 20f, -20f);
		/// 调用 `BumpRandom()` 时，随机扰动值取样范围的最大向量。
		[Tooltip("调用 `BumpRandom()` 时，随机扰动值取样范围的最大向量。")]
		public Vector3 BumpAmountRandomValueMax = new Vector3(20f, 20f, 20f);
		
		[MMInspectorGroup("Test", true, 20, true)]
		/// 在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。
		[Tooltip("在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。")]
		public Vector3 TestMoveToValue = new Vector3(2f, 2f, 2f);
		[MMInspectorButtonBar(new string[] { "MoveTo", "MoveToAdditive", "MoveToSubtractive", "MoveToRandom", "MoveToInstant" }, 
			new string[] { "TestMoveTo", "TestMoveToAdditive", "TestMoveToSubtractive", "TestMoveToRandom", "TestMoveToInstant" }, 
			new bool[] { true, true, true, true, true },
			new string[] { "main-call-to-action", "", "", "", "" })]
		public bool MoveToToolbar;
		
		/// 在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。
		[Tooltip("在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。")]
		public Vector3 TestBumpAmount = new Vector3(75f, 100f, 50f);
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
		
		public override bool LowVelocity => (Mathf.Abs(SpringVector3.Velocity.x) + Mathf.Abs(SpringVector3.Velocity.y) + Mathf.Abs(SpringVector3.Velocity.z)) < _velocityLowThreshold;
		public float DeltaTime => (TimeScaleMode == TimeScaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime;
		public virtual Vector3 TargetVector3 { get; set; }

		#region PUBLIC_API
		
		public virtual void MoveTo(Vector3 newValue)
		{
			Activate();
			SpringVector3.MoveTo(newValue);
		}
		
		public virtual void MoveToAdditive(Vector3 newValue)
		{
			Activate();
			SpringVector3.MoveToAdditive(newValue);
		}
		
		public virtual void MoveToSubtractive(Vector3 newValue)
		{
			Activate();
			SpringVector3.MoveToSubtractive(newValue);
		}

		public virtual void MoveToRandom()
		{
			Activate();
			SpringVector3.MoveToRandom(MoveToRandomValueMin, MoveToRandomValueMax);
		}

		public virtual void MoveToInstant(Vector3 newValue)
		{
			Activate();
			SpringVector3.MoveToInstant(newValue);
		}

		public virtual void MoveToRandom(Vector3 min, Vector3 max)
		{
			Activate();
			SpringVector3.MoveToRandom(min, max);
		}

		public virtual void Bump(Vector3 bumpAmount)
		{
			Activate();
			SpringVector3.Bump(bumpAmount);
		}

		public virtual void BumpRandom()
		{
			Activate();
			SpringVector3.BumpRandom(BumpAmountRandomValueMin, BumpAmountRandomValueMax);
		}

		public virtual void BumpRandom(Vector3 min, Vector3 max)
		{
			Activate();
			SpringVector3.BumpRandom(min, max);
		}
		
		public override void Stop()
		{
			base.Stop();
			this.enabled = false;
			GrabCurrentValue();
			SpringVector3.Stop();
		}
		
		public override void RestoreInitialValue()
		{
			SpringVector3.RestoreInitialValue();
			ApplyValue(SpringVector3.CurrentValue);
		}
		
		public override void ResetInitialValue()
		{
			SpringVector3.SetCurrentValueAsInitialValue();
		}
		
		protected override void UpdateSpringValue()
		{
			SpringVector3.UpdateSpringValue(DeltaTime);
			ApplyValue(SpringVector3.CurrentValue);
		}
		
		public override void Finish()
		{
			SpringVector3.Finish();
			ApplyValue(SpringVector3.CurrentValue);
		}
		
		#endregion

		#region INTERNAL
		
		protected override void Initialization()
		{
			base.Initialization();
			GrabCurrentValue();
			SpringVector3.SetInitialValue(SpringVector3.CurrentValue);
			SpringVector3.TargetValue = SpringVector3.CurrentValue;
		}
		
		protected virtual void ApplyValue(Vector3 newValue)
		{
			TargetVector3 = newValue;
		}
		
		protected override void GrabCurrentValue()
		{
			base.GrabCurrentValue();
			SpringVector3.CurrentValue = TargetVector3;
		}

		#endregion
		
		#region EVENTS
		
		public void OnMMEvent(MMSpringVector3Event 弹簧Event)
		{
			bool eventMatch = 弹簧Event.ChannelData != null && MMChannel.Match(弹簧Event.ChannelData, ChannelMode, Channel, MMChannelDefinition);
			bool targetMatch = 弹簧Event.TargetSpring != null && 弹簧Event.TargetSpring.Equals(this);
			if (!eventMatch && !targetMatch)
			{
				return;
			}
			
			if (弹簧Event.OverrideDamping)
			{
				SpringVector3.SetDamping(弹簧Event.NewDamping);
			}
			if (弹簧Event.OverrideFrequency)
			{
				SpringVector3.SetFrequency(弹簧Event.NewFrequency);
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
			this.MMEventStartListening<MMSpringVector3Event>();
		}

		protected void OnDestroy()
		{
			this.MMEventStopListening<MMSpringVector3Event>();
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
