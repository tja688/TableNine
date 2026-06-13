using System.Collections;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// An event used to pilot a MMSpringColor component
	/// </summary>
	public struct MMSpringColorEvent
	{
		static MMSpringColorEvent e;
		
		public MMChannelData ChannelData;
		public MMSpringComponentBase TargetSpring;
		public SpringCommands Command;
		public Color MoveToValue;
		public Color BumpAmount;
		public Color MoveToRandomValueMin;
		public Color MoveToRandomValueMax;
		public Color BumpAmountRandomValueMin;
		public Color BumpAmountRandomValueMax;
		public bool OverrideDamping;
		public float NewDamping;
		public bool OverrideFrequency;
		public float NewFrequency;
		
		public static void Trigger(SpringCommands command, MMSpringComponentBase targetSpring, MMChannelData channelData, 
			Color moveToValue = default, Color bumpAmount = default,
			Color moveToRandomValueMin = default, Color moveToRandomValueMax = default,
			Color bumpAmountRandomValueMin = default, Color bumpAmountRandomValueMax = default,
			bool overrideDamping = false, float newDamping = default, bool overrideFrequency = false, float newFrequency = default)
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
	/// A spring component used to pilot color values on a target
	/// </summary>
	public abstract class MMSpringColorComponent<T> : MMSpringComponentBase, MMEventListener<MMSpringColorEvent> where T:Component
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
		/// 要监听的通道，必须与触发它的反馈上配置的通道一致。
		[Tooltip("要监听的通道，必须与触发它的反馈上配置的通道一致。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.Int)]
		public int Channel = 0;
		/// the MMChannel definition asset to use to listen for events. The 反馈s targeting this shaker will have to reference that same MMChannel definition to receive events - to create a MMChannel,
		/// right click anywhere in your project (usually in a Data folder) and go MoreMountains > MMChannel, then name it with some unique name
		[Tooltip("用于监听事件的`通道资源`定义资源。只有引用同一个`通道资源`定义的反馈，才能触发这个弹簧组件。若要创建`通道资源`，可以在项目视图中右键（通常放在数据文件夹中），选择更多山脉 >通道资源，并说明说明。")]
		[MMEnumCondition("ChannelMode", (int)MMChannelModes.MMChannel)]
		public MMChannel MMChannelDefinition = null;
		
		[MMInspectorGroup("Spring Settings", true, 18)]
		[Header("Spring")]
		/// 驱动这个颜色弹簧中所有子弹簧组件的弹簧定义。
		[Tooltip("驱动这个颜色弹簧中所有子弹簧组件的弹簧定义。")]
		public MMSpringColor ColorSpring = new MMSpringColor();
		/// 对这个颜色弹簧执行弹跳时使用的倍率（如果弹跳时颜色变化不够明显，可以提高这个值）。
		[Tooltip("对这个颜色弹簧执行弹跳时使用的倍率（如果弹跳时颜色变化不够明显，可以提高这个值）。")]
		public float BumpMultiplier = 20f;
		
		[MMInspectorGroup("Randomness", true, 12, true)]
		
		[Header("Move To Random")]
		
		/// `MoveToRandom` 模式下用于随机取色的最小颜色。
		[Tooltip("`MoveToRandom` 模式下用于随机取色的最小颜色。")]
		public Color MoveToRandomColorMin = MMColors.LawnGreen;
		/// `MoveToRandom` 模式下用于随机取色的最大颜色。
		[Tooltip("`MoveToRandom` 模式下用于随机取色的最大颜色。")]
		public Color MoveToRandomColorMax = MMColors.MediumSeaGreen;
		
		/// `BumpRandom` 模式下用于随机取色的最小颜色。
		[Tooltip("`BumpRandom` 模式下用于随机取色的最小颜色。")]
		public Color BumpRandomColorMin = MMColors.HotPink;
		/// `BumpRandom` 模式下用于随机取色的最大颜色。
		[Tooltip("`BumpRandom` 模式下用于随机取色的最大颜色。")]
		public Color BumpRandomColorMax = MMColors.Plum;
		
		[MMInspectorGroup("Test", true, 20, true)]
		/// 在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。
		[Tooltip("在 Inspector 中点击任意 `MoveTo` 调试按钮时，此弹簧会移动到的目标值。")]
		public Color TestMoveToColor = MMColors.Aquamarine;
		[MMInspectorButtonBar(new string[] { "MoveTo", "MoveToAdditive", "MoveToSubtractive", "MoveToRandom", "MoveToInstant" }, 
			new string[] { "TestMoveTo", "TestMoveToAdditive", "TestMoveToSubtractive", "TestMoveToRandom", "TestMoveToInstant" }, 
			new bool[] { true, true, true, true, true },
			new string[] { "main-call-to-action", "", "", "", "" })]
		public bool MoveToToolbar;
		
		/// 在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。
		[Tooltip("在 Inspector 中点击 `Bump` 调试按钮时，施加到此弹簧的扰动量。")]
		public Color TestBumpColor = MMColors.Orange;
		
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
		
		public override bool LowVelocity => (Mathf.Abs(ColorSpring.Velocity.r) + Mathf.Abs(ColorSpring.Velocity.g) + Mathf.Abs(ColorSpring.Velocity.b) + Mathf.Abs(ColorSpring.Velocity.a) + Mathf.Abs(ColorSpring.ColorSpring.Velocity)) < _velocityLowThreshold;
		public float DeltaTime => (TimeScaleMode == TimeScaleModes.Scaled) ? Time.deltaTime : Time.unscaledDeltaTime;
		
		public virtual Color TargetColor { get; set; }
		
		protected bool _bumping = false;
		protected Color _newBumpColor;
		protected Color _bumpTargetColor;
		protected Color _initialBumpColor;
		protected Coroutine _coroutine;

		#region PUBLIC_API
		
		public virtual void MoveTo(Color newColor)
		{
			Activate();
			ColorSpring.MoveTo(newColor);
		}
		
		public virtual void MoveToAdditive(Color newValue)
		{
			Activate();
			ColorSpring.MoveToAdditive(newValue);
		}
		
		public virtual void MoveToSubtractive(Color newValue)
		{
			Activate();
			ColorSpring.MoveToSubtractive(newValue);
		}

		public virtual void MoveToRandom()
		{
			Activate();
			ColorSpring.MoveToRandom(MoveToRandomColorMin, MoveToRandomColorMax);
		}

		public virtual void MoveToInstant(Vector4 newValue)
		{
			Activate();
			ColorSpring.MoveToInstant(newValue);
		}

		public virtual void MoveToRandom(Color min, Color max)
		{
			Activate();
			ColorSpring.MoveToRandom(min, max);
		}
		
		public virtual void Bump(Color bumpColor)
		{
			Activate();
			_bumping = true;
			_bumpTargetColor = bumpColor;
			_initialBumpColor = ColorSpring.CurrentValue;
			ColorSpring.Bump(bumpColor);
		}

		public virtual void BumpRandom()
		{
			Activate();
			_bumpTargetColor = _bumpTargetColor.MMRandomColor(BumpRandomColorMin, BumpRandomColorMax);
			Bump(_bumpTargetColor);
		}

		public virtual void BumpRandom(Color min, Color max)
		{
			Activate();
			_bumpTargetColor = _bumpTargetColor.MMRandomColor(min, max);
			Bump(_bumpTargetColor);
		}
		
		public override void Stop()
		{
			base.Stop();
			this.enabled = false;
			GrabCurrentValue();
			ColorSpring.Stop();
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}
		
		public override void RestoreInitialValue()
		{
			ColorSpring.RestoreInitialValue();
			ApplyValue(ColorSpring.CurrentValue);
		}

		public override void ResetInitialValue()
		{
			ColorSpring.SetCurrentValueAsInitialValue();
		}
		
		protected override void UpdateSpringValue()
		{
			if (_bumping)
			{
				float t = ColorSpring.ColorSpring.CurrentValue * BumpMultiplier;
				ColorSpring.UpdateSpringValue(DeltaTime);
				_newBumpColor = Color.Lerp(_initialBumpColor, _bumpTargetColor, t);
				ApplyValue(_newBumpColor);
			}
			else
			{
				ColorSpring.UpdateSpringValue(DeltaTime);
				ApplyValue(ColorSpring.CurrentValue);	
			}
		}
		
		public override void Finish()
		{
			_bumping = false;
			ColorSpring.Finish();
			ApplyValue(ColorSpring.CurrentValue);
		}
		
		#endregion

		#region INTERNAL
		
		protected override void Initialization()
		{
			base.Initialization();
			GrabCurrentValue();
			ColorSpring.SetInitialValue(ColorSpring.CurrentValue);
			ColorSpring.TargetValue = ColorSpring.CurrentValue;
		}
		
		protected override void GrabCurrentValue()
		{
			base.GrabCurrentValue();
			ColorSpring.CurrentValue = TargetColor;
			
		}

		protected virtual void ApplyValue(Color newColor)
		{
			TargetColor = newColor;
		}

		/*protected virtual void ReplicateDriverSpring()
		{
			_弹簧X.Damping = ColorSpring.Damping;
			_弹簧Y.Damping = ColorSpring.Damping;
			_弹簧Z.Damping = ColorSpring.Damping;
			_弹簧W.Damping = ColorSpring.Damping;
			_弹簧X.Frequency = ColorSpring.Frequency;
			_弹簧Y.Frequency = ColorSpring.Frequency;
			_弹簧Z.Frequency = ColorSpring.Frequency;
			_弹簧W.Frequency = ColorSpring.Frequency;
		}*/

		#endregion
		
		#region EVENTS
		
		public void OnMMEvent(MMSpringColorEvent 弹簧Event)
		{
			bool eventMatch = 弹簧Event.ChannelData != null && MMChannel.Match(弹簧Event.ChannelData, ChannelMode, Channel, MMChannelDefinition);
			bool targetMatch = 弹簧Event.TargetSpring != null && 弹簧Event.TargetSpring.Equals(this);
			if (!eventMatch && !targetMatch)
			{
				return;
			}
			
			if (弹簧Event.OverrideDamping)
			{
				ColorSpring.SetDamping(弹簧Event.NewDamping);
			}
			if (弹簧Event.OverrideFrequency)
			{
				ColorSpring.SetFrequency(弹簧Event.NewFrequency);
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
			this.MMEventStartListening<MMSpringColorEvent>();
		}

		protected void OnDestroy()
		{
			this.MMEventStopListening<MMSpringColorEvent>();
		}
		
		#endregion

		#region TEST_METHODS

		protected override void TestMoveTo()
		{
			MoveTo(TestMoveToColor);
		}
		
		protected override void TestMoveToAdditive()
		{
			MoveToAdditive(TestMoveToColor);
		}
		
		protected override void TestMoveToSubtractive()
		{
			MoveToSubtractive(TestMoveToColor);
		}
		
		protected override void TestMoveToRandom()
		{
			MoveToRandom();
		}

		protected override void TestMoveToInstant()
		{
			MoveToInstant(TestMoveToColor);
		}

		protected override void TestBump()
		{
			Bump(TestBumpColor);
		}
		
		protected override void TestBumpRandom()
		{
			BumpRandom();
		}

		#endregion
	}
}
