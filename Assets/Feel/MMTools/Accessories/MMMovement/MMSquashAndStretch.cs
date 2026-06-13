using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.Tools
{
	/// <summary>
	/// This component will automatically update scale and rotation 
	/// Put it one level below the top, and have the model one level below that
	/// Hierarchy should be as follows :
	/// 
	/// Parent (where the logic (and optionnally rigidbody lies)
	/// - MMSquashAndStretch
	/// - - Model / sprite
	/// 
	/// Make sure this intermediary layer only has one child
	/// If movement feels glitchy make sure your rigidbody is on Interpolate
	/// </summary>
	[AddComponentMenu("More Mountains/Tools/Movement/MM Squash And Stretch")]
	public class MMSquashAndStretch : MonoBehaviour
	{
		public enum Timescales { Regular, Unscaled }
		public enum Modes { Rigidbody, Rigidbody2D, Position }

		[MMInformation("该组件会基于速度应用 Squash & Stretch（速度可来自位置差分、Rigidbody 或 Rigidbody2D）。它应挂在层级中间层：上层为逻辑对象，下层为模型对象。建议该中间层仅保留一个子物体以避免变形异常。", MMInformationAttribute.InformationType.Info, false)]
		[Header("Velocity Detection")]
		/// the possible ways to get velocity from
		[Tooltip("速度来源模式：Rigidbody、Rigidbody2D 或 Position（由位移差分计算）")]
		public Modes Mode = Modes.Position;
		/// whether we should use deltaTime or unscaledDeltaTime
		[Tooltip("时间基准：常规使用 增量时间，非缩放使用 未缩放的DeltaTime")]
		public Timescales Timescale = Timescales.Regular;

		[Header("Settings")]
		/// the intensity of the squash and stretch
		[Tooltip("形变强度系数")]
		public float Intensity = 0.02f;
		/// the maximum velocity of your parent object, used to remap the computed one
		[Tooltip("父对象的最大参考速度，用于将当前速度重映射到 0~1 区间")]
		public float MaximumVelocity = 1f;

		[Header("Rescale")]
		/// the minimum scale to apply to this object
		[Tooltip("允许应用到该对象的最小缩放")]
		public Vector3 MinimumScale = new Vector3(0.5f, 0.5f, 0.5f);
		/// the maximum scale to apply to this object
		[Tooltip("允许应用到该对象的最大缩放")]
		public Vector3 MaximumScale = new Vector3(2f, 2f, 2f);
		/// whether or not to rescale on the x axis
		[Tooltip("是否允许在 X 轴上缩放")]
		public bool RescaleX = true;
		/// whether or not to rescale on the y axis
		[Tooltip("是否允许在 Y 轴上缩放")]
		public bool RescaleY = true;
		/// whether or not to rescale on the z axis
		[Tooltip("是否允许在 Z 轴上缩放")]
		public bool RescaleZ = true;
		/// whether or not to rotate the transform to align with the current direction
		[Tooltip("是否自动旋转，使对象朝向与当前运动方向对齐")]
		public bool RotateToMatchDirection = true;

		[Header("Squash")]
		/// if this is true, the object will squash once velocity goes below the specified threshold
		[Tooltip("开启后，当速度从阈值以上降到阈值以下时会触发一次 Squash（停止挤压）")]
		public bool AutoSquashOnStop = false;
		/// the curve to apply when squashing the object (this describes scale on x and z, will be inverted for y to maintain mass)
		[Tooltip("Squash 曲线（定义 X/Z 轴变化；Y 轴会反向应用以保持体积观感）")]
		public AnimationCurve SquashCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1f), new Keyframe(1, 0f));
		/// the velocity threshold after which a squash can be triggered if the object stops
		[Tooltip("停止触发 Squash 的速度阈值；只有先超过该阈值再降到其下才会触发")]
		public float SquashVelocityThreshold = 0.1f;
		/// the maximum duration of the squash (will be reduced if velocity is low)
		[Tooltip("Squash 持续时间范围（速度越低，实际持续时间会越短）")]
		[MMVector("Min","Max")]
		public Vector2 SquashDuration = new Vector2(0.25f, 0.5f);
		/// the maximum intensity of the squash
		[Tooltip("Squash 强度范围（会按触发时速度映射）")]
		[MMVector("Min", "Max")]
		public Vector2 SquashIntensity = new Vector2(0f, 1f);

		[Header("Spring")] 
		/// whether or not to add extra spring to the squash and stretch
		[Tooltip("是否叠加弹簧平滑效果；开启后 SpringDamping 与 SpringFrequency 才生效")]
		public bool Spring = false;
		/// the damping to apply to the spring
		[Tooltip("弹簧阻尼系数（仅 Spring 开启时生效）")]
		[MMCondition("Spring", true)]
		public float SpringDamping = 0.3f;
		/// the spring's frequency
		[Tooltip("弹簧频率（仅 Spring 开启时生效）")]
		[MMCondition("Spring", true)] 
		public float SpringFrequency = 3f;
        
		[Header("Debug")]
		[MMReadOnly]
		/// the current velocity of the parent object
		[Tooltip("父对象当前速度（调试只读）")]
		public Vector3 Velocity;
		[MMReadOnly]
		/// the remapped velocity
		[Tooltip("重映射后的速度值（调试只读）")]
		public float RemappedVelocity;
		[MMReadOnly]
		/// the current velocity magnitude
		[Tooltip("当前速度标量（调试只读）")]
		public float VelocityMagnitude;

		public virtual float TimescaleTime { get { return (Timescale == Timescales.Regular) ? Time.time : Time.unscaledTime; } }
		public virtual float TimescaleDeltaTime { get { return (Timescale == Timescales.Regular) ? Time.deltaTime : Time.unscaledDeltaTime; } }

		#if MM_PHYSICS2D
		protected Rigidbody2D _rigidbody2D;
		#endif
		protected Rigidbody _rigidbody;
		protected Transform _childTransform;
		protected Transform _parentTransform;
		protected Vector3 _direction;
		protected Vector3 _previousPosition;
		protected Vector3 _newLocalScale;
		protected Vector3 _initialScale;
		protected Quaternion _newRotation = Quaternion.identity;
		protected Quaternion _deltaRotation;
		protected float _squashStartedAt = 0f;
		protected bool _squashing = false;
		protected float _squashIntensity;
		protected float _squashDuration;
		protected bool _movementStarted = false;
		protected float _lastVelocity = 0f;
		protected Vector3 _springScale;
		protected Vector3 _springVelocity = Vector3.zero;

		/// <summary>
		/// On start, we initialize our component
		/// </summary>
		protected virtual void Start()
		{
			Initialization();
		}

		/// <summary>
		/// Stores the initial scale, grabs the rigidbodies (or tries to), as well as the parent and child
		/// </summary>
		protected virtual void Initialization()
		{
			_initialScale = this.transform.localScale;
			_springScale = _initialScale;

			_rigidbody = this.transform.parent.GetComponent<Rigidbody>();
			#if MM_PHYSICS2D
			_rigidbody2D = this.transform.parent.GetComponent<Rigidbody2D>();
			#endif

			_childTransform = this.transform.GetChild(0).transform;
			_parentTransform = this.transform.parent.GetComponent<Transform>();

			_previousPosition = _parentTransform.position;
		}
        
		/// <summary>
		/// On late update, we apply our squash and stretch effect
		/// </summary>
		protected virtual void LateUpdate()
		{
			SquashAndStretch();
		}

		/// <summary>
		/// Computes velocity and applies the effect
		/// </summary>
		protected virtual void SquashAndStretch()
		{
			if (TimescaleDeltaTime <= 0f)
			{
				return;
			}

			ComputeVelocityAndDirection();
			ComputeNewRotation();
			ComputeNewLocalScale();
			StorePreviousPosition();
		}

		/// <summary>
		/// Determines the current velocity and direction of the parent object
		/// </summary>
		protected virtual void ComputeVelocityAndDirection()
		{
			Velocity = Vector3.zero;

			switch (Mode)
			{
				case Modes.Rigidbody:
					Velocity = _rigidbody.velocity;
					break;

				case Modes.Rigidbody2D:
					#if MM_PHYSICS2D
					Velocity = _rigidbody2D.velocity;
					#endif
					break;

				case Modes.Position:
					Velocity = (_previousPosition - _parentTransform.position) / TimescaleDeltaTime;
					break;
			}

			VelocityMagnitude = Velocity.magnitude;
			RemappedVelocity = MMMaths.Remap(VelocityMagnitude, 0f, MaximumVelocity, 0f, 1f);
			_direction = Vector3.Normalize(Velocity);

			if (AutoSquashOnStop)
			{
				// if we've moved fast enough and have now stopped, we trigger a squash
				if (VelocityMagnitude > SquashVelocityThreshold)
				{
					_movementStarted = true;
					_lastVelocity = Mathf.Clamp(VelocityMagnitude, 0f, MaximumVelocity);
				}
				else if (_movementStarted)
				{
					_movementStarted = false;
					_squashing = true;
					float duration = MMMaths.Remap(_lastVelocity, 0f, MaximumVelocity, SquashDuration.x, SquashDuration.y);
					float intensity = MMMaths.Remap(_lastVelocity, 0f, MaximumVelocity, SquashIntensity.x, SquashIntensity.y);
					Squash(duration, intensity);
				}
			}            
		}

		/// <summary>
		/// Computes a new rotation for both this object and the child
		/// </summary>
		protected virtual void ComputeNewRotation()
		{
			if (!RotateToMatchDirection)
			{
				return;
			}
			if (VelocityMagnitude > 0.01f)
			{
				_newRotation = Quaternion.FromToRotation(Vector3.up, _direction);
			}
			_deltaRotation = _parentTransform.rotation;
			this.transform.rotation = _newRotation;
			_childTransform.rotation = _deltaRotation;
		}
        
		/// <summary>
		/// Computes a new local scale for this object
		/// </summary>
		protected virtual void ComputeNewLocalScale()
		{
			if (_squashing)
			{
				float elapsed = MMMaths.Remap(TimescaleTime - _squashStartedAt, 0f, _squashDuration, 0f, 1f);
				float curveValue = SquashCurve.Evaluate(elapsed);
				_newLocalScale.x = _initialScale.x + curveValue * _squashIntensity;
				_newLocalScale.y = _initialScale.y - curveValue * _squashIntensity;
				_newLocalScale.z = _initialScale.z + curveValue * _squashIntensity;

				if (elapsed >= 1f)
				{
					_squashing = false;
				}
			}
			else
			{
				_newLocalScale.x = Mathf.Clamp01(1f / (RemappedVelocity + 0.001f));
				_newLocalScale.y = RemappedVelocity;
				_newLocalScale.z = Mathf.Clamp01(1f / (RemappedVelocity + 0.001f));
				_newLocalScale = Vector3.Lerp(Vector3.one, _newLocalScale, VelocityMagnitude * Intensity);
			}            

			_newLocalScale.x = Mathf.Clamp(_newLocalScale.x, MinimumScale.x, MaximumScale.x);
			_newLocalScale.y = Mathf.Clamp(_newLocalScale.y, MinimumScale.y, MaximumScale.y);
			_newLocalScale.z = Mathf.Clamp(_newLocalScale.z, MinimumScale.z, MaximumScale.z);

			if (Spring)
			{
				MMMaths.Spring(ref _springScale, _newLocalScale, ref _springVelocity, SpringDamping, SpringFrequency, Time.deltaTime);
				_newLocalScale = _springScale;
			}
			
			if (!RescaleX)
			{
				_newLocalScale.x = _initialScale.x;
			}
			if (!RescaleY)
			{
				_newLocalScale.y = _initialScale.y;
			}
			if (!RescaleZ)
			{
				_newLocalScale.z = _initialScale.z;
			}

			this.transform.localScale = _newLocalScale;
		}

		/// <summary>
		/// Stores the previous position of the parent to compute velocity
		/// </summary>
		protected virtual void StorePreviousPosition()
		{
			_previousPosition = _parentTransform.position;
		}
        
		/// <summary>
		/// Triggered either directly or via the AutoSquash setting, this squashes the object (usually after a contact / stop)
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="intensity"></param>
		public virtual void Squash(float duration, float intensity)
		{
			_squashStartedAt = TimescaleTime;
			_squashing = true;
			_squashIntensity = intensity;
			_squashDuration = duration;
		}
	}
}
