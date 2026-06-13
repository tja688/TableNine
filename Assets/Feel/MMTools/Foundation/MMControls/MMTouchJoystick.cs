#if MM_UI
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using Unity.Collections;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoreMountains.Tools 
{
	[System.Serializable]
	public class JoystickEvent : UnityEvent<Vector2> {}
	[System.Serializable]
	public class JoystickFloatEvent : UnityEvent<float> {}

	/// <summary>
	/// Joystick input class.
	/// In charge of the behaviour of the joystick mobile touch input.
	/// Bind its actions from the inspector
	/// Handles mouse and multi touch
	/// </summary>
	[RequireComponent(typeof(Rect))]
	[RequireComponent(typeof(CanvasGroup))]
	[AddComponentMenu("More Mountains/Tools/Controls/MM Touch Joystick")]
	public class MMTouchJoystick : MMMonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
	{
		public enum MaxRangeModes { Distance, DistanceToTransform }
		
		[MMInspectorGroup("Camera", true, 16)]
		/// The camera to use as the reference for any ScreenToWorldPoint computations
		[Tooltip("用于一切 ScreenToWorldPoint 计算的参考摄像机")]
		public Camera TargetCamera;

		[MMInspectorGroup("Joystick Behaviour", true, 18)]
		[Tooltip("决定是否启用该摇杆的水平轴；若关闭，摇杆将只能垂直移动。")]
		/// Is horizontal axis allowed
		public bool HorizontalAxisEnabled = true;
		/// Is vertical axis allowed
		[Tooltip("决定是否启用该摇杆的垂直轴；若关闭，摇杆将只能水平移动。")]
		public bool VerticalAxisEnabled = true;
		/// the mode in which to compute the range. Distance will be a flat value, DistanceToTransform will be a distance to a transform you can move around and potentially resize as you wish for various resolutions
		[Tooltip("用于计算范围的模式。Distance 使用固定值；DistanceToTransform 使用到某个 Transform 的距离，便于你针对不同分辨率移动或缩放该参照物。")]
		public MaxRangeModes MaxRangeMode = MaxRangeModes.Distance;
		/// The MaxRange is the maximum distance from its initial center position you can drag the joystick to
		[Tooltip("MaxRange 表示从初始中心点出发，摇杆可被拖动到的最大距离。")]
		[MMEnumCondition("MaxRangeMode", (int)MaxRangeModes.Distance)]
		public float MaxRange = 1.5f;
		/// in DistanceToTransform mode, the object whose distance to the center will be used to compute the max range. Note that this is computed once, at init. Call RefreshMaxRangeDistance() to recompute it.
		[Tooltip("在 DistanceToTransform 模式下，会使用该对象到中心点的距离来计算最大范围。注意：此值只会在初始化时计算一次；若之后发生变化，请手动调用 RefreshMaxRangeDistance() 重新计算。")]
		[MMEnumCondition("MaxRangeMode", (int)MaxRangeModes.DistanceToTransform)]
		public Transform MaxRangeTransform;

		public float ComputedMaxRange
		{
			get
			{
				if (Application.isPlaying)
				{
					return MaxRangeMode == MaxRangeModes.Distance ? MaxRange : _maxRangeTransformDistance;
				}
				else
				{
					if (MaxRangeMode == MaxRangeModes.Distance)
					{
						return MaxRange;
					}
					else
					{
						if (MaxRangeTransform == null)
						{
							return -1f;
						}
						RefreshMaxRangeDistance();
						return _maxRangeTransformDistance;
					}
				}
			}
		} 

		[MMInspectorGroup("Value Events", true, 19)]
		/// An event to use the raw value of the joystick
		[Tooltip("用于接收摇杆原始值的事件")]
		public JoystickEvent JoystickValue;
		/// An event to use the normalized value of the joystick
		[Tooltip("用于接收摇杆归一化值的事件")]
		public JoystickEvent JoystickNormalizedValue;
		// An event to use the joystick's amplitude (the magnitude of its Vector2 output)
		[Tooltip("用于接收摇杆幅值（其 Vector2 输出的模长）的事件")]
		public JoystickFloatEvent JoystickMagnitudeValue;
		
		[MMInspectorGroup("Touch Events", true, 8)]
		/// An event triggered when tapping the joystick for the first time
		[Tooltip("首次按下摇杆时触发的事件")]
		public UnityEvent OnPointerDownEvent;
		/// An event triggered when dragging the stick
		[Tooltip("拖动摇杆时触发的事件")]
		public UnityEvent OnDragEvent;
		/// An event triggered when releasing the stick
		[Tooltip("释放摇杆时触发的事件")]
		public UnityEvent OnPointerUpEvent;
		
		[MMInspectorGroup("Rotating Direction Indicator", true, 20)]
		/// an object you can rotate to show the direction of the joystick. Will only be visible if the movement is above a threshold
		[Tooltip("用于显示摇杆方向的可旋转对象；只有当移动幅度超过阈值时才会显示。")]
		public Transform RotatingIndicator;
		/// the threshold above which the rotating indicator will appear
		[Tooltip("旋转方向指示器开始显示所需超过的阈值")]
		public float RotatingIndicatorThreshold = 0.1f;
		
		[MMInspectorGroup("Knob Opacity", true, 17)]
		/// the new opacity to apply to the canvas group when the button is pressed
		[Tooltip("摇杆按下时要应用到 CanvasGroup 的透明度")]
		public float PressedOpacity = 0.5f;
		/// whether or not to interpolate opacity changes on the knob's canvas group
		[Tooltip("是否对摇杆圆帽 CanvasGroup 的透明度变化进行插值")]
		public bool InterpolateOpacity = true;
		/// the speed at which to interpolate opacity
		[Tooltip("透明度插值速度")]
		[MMCondition("InterpolateOpacity", true)]
		public float InterpolateOpacitySpeed = 1f;
		
		[MMInspectorGroup("Debug Output", true, 5)]
		/// the raw value of the joystick, from 0 to 1 on each axis
		[Tooltip("摇杆原始值，每个轴的范围为 0 到 1")]
		[MMReadOnly]
		public Vector2 RawValue;
		/// the normalized value of the joystick
		[Tooltip("摇杆的归一化值")]
		[MMReadOnly]
		public Vector2 NormalizedValue;
		/// the magnitude of the stick's vector
		[Tooltip("摇杆向量的模长")]
		[MMReadOnly]
		public float Magnitude;
		/// whether or not to draw gizmos associated to this stick
		[Tooltip("是否绘制与该摇杆相关的 Gizmo")] 
		public bool DrawGizmos = true;

		/// the render mode of the parent canvas this stick is on
		public virtual RenderMode ParentCanvasRenderMode { get; protected set; }

		protected Vector3 _neutralPosition;
		protected Vector3 _newTargetPosition;
		protected Vector3 _newJoystickPosition;
		protected float _initialZPosition;
		protected float _targetOpacity;
		protected CanvasGroup _canvasGroup;
		protected float _initialOpacity;
		protected Transform _knobTransform;
		protected bool _rotatingIndicatorIsNotNull = false;
		protected float _maxRangeTransformDistance;
		protected Canvas _parentCanvas;
		
		/// <summary>
		/// On Start we initialize our stick
		/// </summary>
		protected virtual void Start()
		{
			Initialize();
		}

		/// <summary>
		/// Initializes the various parts of the stick
		/// </summary>
		/// <exception cref="Exception"></exception>
		public virtual void Initialize()
		{
			if ((ParentCanvasRenderMode == RenderMode.ScreenSpaceCamera) && (TargetCamera == null))
			{
				throw new Exception("MMTouchJoystick : you have to set a target camera");
			}
			
			_canvasGroup = GetComponent<CanvasGroup>();
			_parentCanvas = GetComponentInParent<Canvas>();
			_rotatingIndicatorIsNotNull = (RotatingIndicator != null);
			RefreshMaxRangeDistance();

			SetKnobTransform(this.transform);

			SetNeutralPosition();
			
			ParentCanvasRenderMode = GetComponentInParent<Canvas>().renderMode;
			_initialZPosition = _knobTransform.position.z;
			_initialOpacity = _canvasGroup.alpha;
		}

		/// <summary>
		/// This method is used to compute the max range distance when in DistanceToTransform mode
		/// </summary>
		public virtual void RefreshMaxRangeDistance()
		{
			if (MaxRangeMode == MaxRangeModes.DistanceToTransform)
			{
				_maxRangeTransformDistance = Vector2.Distance(this.transform.position, MaxRangeTransform.position);
			}
		}

		/// <summary>
		/// Assigns a new transform as the joystick knob
		/// </summary>
		/// <param name="newTransform"></param>
		public virtual void SetKnobTransform(Transform newTransform)
		{
			_knobTransform = newTransform;
		}

		/// <summary>
		/// On Update we check for an orientation change if needed, and send our input values.
		/// </summary>
		protected virtual void Update()
		{
			NormalizedValue = RawValue.normalized;
			Magnitude = RawValue.magnitude;
			
			if (HorizontalAxisEnabled || VerticalAxisEnabled)
			{
				JoystickValue.Invoke(RawValue);
				JoystickNormalizedValue.Invoke(NormalizedValue);
				JoystickMagnitudeValue.Invoke(Magnitude);
			}
			
			RotateIndicator();
			HandleOpacity();
		}

		/// <summary>
		/// Changes or interpolates the opacity of the knob
		/// </summary>
		protected virtual void HandleOpacity()
		{
			if (InterpolateOpacity)
			{
				_canvasGroup.alpha = MMMaths.Lerp(_canvasGroup.alpha, _targetOpacity, InterpolateOpacitySpeed, Time.unscaledDeltaTime);	
			}
			else
			{
				_canvasGroup.alpha = _targetOpacity;
			}
		}

		/// <summary>
		/// Rotates an indicator to match the rotation of the stick
		/// </summary>
		protected virtual void RotateIndicator()
		{
			if (!_rotatingIndicatorIsNotNull)
			{
				return;
			}

			RotatingIndicator.gameObject.SetActive(RawValue.magnitude > RotatingIndicatorThreshold);
			float angle = Mathf.Atan2(RawValue.y, RawValue.x) * Mathf.Rad2Deg;
			RotatingIndicator.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
		}
		
		/// <summary>
		/// Sets the neutral position of the joystick
		/// </summary>
		public virtual void SetNeutralPosition()
		{
			_neutralPosition = _knobTransform.position;
		}

		public virtual void SetNeutralPosition(Vector3 newPosition)
		{
			_neutralPosition = newPosition;
		}

		/// <summary>
		/// Handles dragging of the joystick
		/// </summary>
		public virtual void OnDrag(PointerEventData eventData)
		{
			OnDragEvent.Invoke();

			_newTargetPosition = ConvertToWorld(eventData.position);

			Vector3 localDelta = TransformToLocalSpace(_newTargetPosition - _neutralPosition);
			localDelta = Vector2.ClampMagnitude(localDelta, ComputedMaxRange);

			if (!HorizontalAxisEnabled)
			{
				localDelta.x = 0;
			}
			if (!VerticalAxisEnabled)
			{
				localDelta.y = 0;
			}

			RawValue.x = EvaluateInputValue(localDelta.x);
			RawValue.y = EvaluateInputValue(localDelta.y);

			_newTargetPosition = _neutralPosition + TransformToWorldSpace(localDelta);
			_newJoystickPosition = _newTargetPosition;
			_newJoystickPosition.z = _initialZPosition;

			_knobTransform.position = _newJoystickPosition;
		}

		/// <summary>
		/// Transforms a world space vector to the canvas/camera local space
		/// </summary>
		protected virtual Vector3 TransformToLocalSpace(Vector3 worldVector)
		{
			if (ParentCanvasRenderMode == RenderMode.ScreenSpaceCamera && TargetCamera != null)
			{
				return Quaternion.Inverse(TargetCamera.transform.rotation) * worldVector;
			}
			return worldVector;
		}

		/// <summary>
		/// Transforms a local space vector back to world space
		/// </summary>
		protected virtual Vector3 TransformToWorldSpace(Vector3 localVector)
		{
			if (ParentCanvasRenderMode == RenderMode.ScreenSpaceCamera && TargetCamera != null)
			{
				return TargetCamera.transform.rotation * localVector;
			}
			return localVector;
		}
		
		protected virtual Vector3 ConvertToWorld(Vector3 position)
		{
			if (ParentCanvasRenderMode == RenderMode.ScreenSpaceCamera)
			{
				float distance = _parentCanvas != null ? _parentCanvas.planeDistance : 0f;
				position.z = distance;
				return TargetCamera.ScreenToWorldPoint(position);
			}
			else
			{
				return position;
			}
		}

		/// <summary>
		/// Clamps the position of the new target
		/// </summary>
		protected virtual void ClampToBounds()
		{
			_newTargetPosition = Vector2.ClampMagnitude(_newTargetPosition - _neutralPosition, ComputedMaxRange);
		}

		/// <summary>
		/// Resets the stick's position and values
		/// </summary>
		public virtual void ResetJoystick()
		{
			// we reset the stick's position
			_newJoystickPosition = _neutralPosition;
			_newJoystickPosition.z = _initialZPosition;
			_knobTransform.position = _newJoystickPosition;
			
			RawValue.x = 0f;
			RawValue.y = 0f;

			// we set its opacity back
			_targetOpacity = _initialOpacity;
		}

		/// <summary>
		/// We compute the axis value from the interval between neutral position, current stick position (vectorPosition) and max range
		/// </summary>
		/// <returns>The axis value, a float between -1 and 1</returns>
		/// <param name="vectorPosition">stick position.</param>
		protected virtual float EvaluateInputValue(float vectorPosition)
		{
			return Mathf.InverseLerp(0, ComputedMaxRange, Mathf.Abs(vectorPosition)) * Mathf.Sign(vectorPosition);
		}

		/// <summary>
		/// What happens when the stick stops being dragged
		/// </summary>
		public virtual void OnEndDrag(PointerEventData eventData)
		{
		}
		
		/// <summary>
		/// What happens when the stick is released (even if no drag happened)
		/// </summary>
		/// <param name="data"></param>
		public virtual void OnPointerUp(PointerEventData data)
		{
			ResetJoystick();
			OnPointerUpEvent.Invoke();
		}
		
		/// <summary>
		/// What happens when the stick is pressed for the first time
		/// </summary>
		/// <param name="data"></param>
		public virtual void OnPointerDown(PointerEventData data)
		{
			_targetOpacity = PressedOpacity;
			OnPointerDownEvent.Invoke();
		}

		/// <summary>
		/// On enable, we initialize our stick
		/// </summary>
		protected virtual void OnEnable()
		{
			Initialize();
			_targetOpacity = _initialOpacity;
		}
		
		#if UNITY_EDITOR
		/// <summary>
		/// Draws gizmos if needed
		/// </summary>
		protected virtual void OnDrawGizmos()
		{
			if (!DrawGizmos)
			{
				return;
			}

			Handles.color = MMColors.Orange;
			if (!Application.isPlaying)
			{
				Handles.DrawWireDisc(this.transform.position, this.transform.forward, ComputedMaxRange);	
			}
			else
			{
				Handles.DrawWireDisc(_neutralPosition, this.transform.forward, ComputedMaxRange);
			}
		}
		#endif
	}
}
#endif
