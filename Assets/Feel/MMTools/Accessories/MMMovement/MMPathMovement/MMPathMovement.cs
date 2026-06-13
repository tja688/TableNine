using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Random = UnityEngine.Random;

namespace MoreMountains.Tools
{
	/// <summary>
	/// Add this component to an object and it'll be able to move along a path defined from its inspector.
	/// </summary>
	[AddComponentMenu("More Mountains/Tools/Movement/MM Path Movement")]
	public class MMPathMovement : MonoBehaviour 
	{
		/// the possible movement types
		public enum PossibleAccelerationType { ConstantSpeed, EaseOut, AnimationCurve }
		/// the possible cycle options
		public enum CycleOptions { BackAndForth, Loop, OnlyOnce, StopAtBounds, Random }
		/// the possible movement directions
		public enum MovementDirection { Ascending, Descending }
		/// whether progress on the pass should be made at update, fixed update or late update
		public enum UpdateModes { Update, FixedUpdate, LateUpdate }
		/// whether to align the path on nothing, this object's rotation, or this object's parent's rotation
		public enum AlignmentModes { None, ThisRotation, ParentRotation }

		[Header("Path")]
		[MMInformation("你可以在这里选择 '<b>Cycle Option</b>'：`BackAndForth` 会让对象沿路径往返移动；`Loop` 会闭合整条路径并持续循环；`OnlyOnce` 会从第一个点移动到最后一个点后停止；`StopAtBounds` 会在边界处停住；`Random` 会在路径点间随机跳转。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		public CycleOptions CycleOption;

		[MMInformation("先为 <b>Path</b> 设置大小，再添加路径点；之后你可以在 Inspector 中输入坐标，也可以在场景视图拖动句柄。每个路径点都可单独设置延迟（秒），对象会按顺序移动。\n对于循环路径，还可通过初始方向决定起步顺序：`Ascending`（0、1、2...）或 `Descending`（最后一个、倒数第二个、倒数第三个...）。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// the initial movement direction : ascending > will go from the points 0 to 1, 2, etc ; descending > will go from the last point to last-1, last-2, etc
		[Tooltip("循环路径的初始移动方向：`Ascending` 从点 0 递增前进；`Descending` 从最后一个点向前倒序移动")]
		public MovementDirection LoopInitialMovementDirection = MovementDirection.Ascending;
		/// the points that make up the path the object will follow
		[Tooltip("构成路径的点；对象会按这些点组成的路径移动")]
		public List<MMPathMovementElement> PathElements;

		[Header("Path Alignment")] 
		/// whether to align the path on nothing, this object's rotation, or this object's parent's rotation
		[Tooltip("决定路径是否不对齐，或对齐到当前对象的旋转 / 父对象的旋转")]
		public AlignmentModes AlignmentMode = AlignmentModes.None;
		
		[Header("Movement")]
		[MMInformation("设置沿路径移动的<b>速度</b>，以及运动是保持匀速还是使用缓动。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// the movement speed
		[Tooltip("移动速度")]
		public float MovementSpeed = 1;
		/// returns the current speed at which the object is traveling
		public virtual Vector3 CurrentSpeed { get; protected set; }
		/// the movement type of the object
		[Tooltip("对象的移动类型")]
		public PossibleAccelerationType AccelerationType = PossibleAccelerationType.ConstantSpeed;
		/// the acceleration to apply to an object traveling between two points of the path.
		[Tooltip("对象在两个路径点之间移动时要应用的加速度曲线。")] 
		public AnimationCurve Acceleration = new AnimationCurve(new Keyframe(0,1f),new Keyframe(1f,0f));
		/// the chosen update mode (update, fixed update, late update)
		[Tooltip("选定的更新模式（更新、固定更新 或 后期更新）")]
		public UpdateModes UpdateMode = UpdateModes.Update;

		[Header("Settings")]
		[MMInformation("<b>MinDistanceToGoal</b> 用于判断对象是否“几乎已经到达”某个路径点。这里另外两个设置仅用于调试，请不要修改。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// the minimum distance to a point at which we'll arbitrarily decide the point's been reached
		[Tooltip("当对象与某个路径点的距离小于该值时，就会视为已经到达该点")]
		public float MinDistanceToGoal = .1f;
		/// the original position of the transform, hidden and shouldn't be accessed
		[Tooltip("对象的初始 Transform 位置（内部调试字段，通常不应手动访问）")]
		protected Vector3 _originalTransformPosition;
		/// if this is true, the object can move along the path
		public virtual bool CanMove { get; set; }
        
		protected bool _originalTransformPositionStatus = false;
		protected bool _active=false;
		protected IEnumerator<Vector3> _currentPoint;
		protected int _direction = 1;
		protected Vector3 _initialPosition;
		protected Vector3 _finalPosition;
		protected Vector3 _previousPoint = Vector3.zero;
		protected float _waiting=0;
		protected int _currentIndex;
		protected float _distanceToNextPoint;
		protected bool _endReached = false;
		protected Vector3 _positionLastFrame;
		protected Vector3 _vector3Zero = Vector3.zero;

		/// <summary>
		/// Initialization
		/// </summary>
		protected virtual void Awake ()
		{
			Initialization ();
		}

		/// <summary>
		/// On Start we store our initial position
		/// </summary>
		protected virtual void Start()
		{
			_originalTransformPosition = transform.position;
		}

		/// <summary>
		/// A public method you can call to reset the path
		/// </summary>
		public virtual void ResetPath()
		{
			Initialization();
			CanMove = false;
			transform.position = _originalTransformPosition;
		}

		/// <summary>
		/// Flag inits, initial movement determination, and object positioning
		/// </summary>
		protected virtual void Initialization()
		{
			// on Start, we set our active flag to true
			_active=true;
			_endReached = false;
			CanMove = true;

			// if the path is null we exit
			if(PathElements == null || PathElements.Count < 1)
			{
				return;
			}

			// we set our initial direction based on the settings
			if (LoopInitialMovementDirection == MovementDirection.Ascending)
			{
				_direction=1;
			}
			else
			{
				_direction=-1;
			}

			// we initialize our path enumerator
			_currentPoint = GetPathEnumerator();
			_previousPoint = _currentPoint.Current;
			_currentPoint.MoveNext();

			// initial positioning
			if (!_originalTransformPositionStatus)
			{
				_originalTransformPositionStatus = true;
				_originalTransformPosition = transform.position;
			}
			transform.position = PointPosition(_currentPoint.Current);
		}

		protected virtual void FixedUpdate()
		{
			if (UpdateMode == UpdateModes.FixedUpdate)
			{
				ExecuteUpdate();
			}
		}

		protected virtual void LateUpdate()
		{
			if (UpdateMode == UpdateModes.LateUpdate)
			{
				ExecuteUpdate();
			}
		}

		protected virtual void Update()
		{
			if (UpdateMode == UpdateModes.Update)
			{
				ExecuteUpdate();
			}
		}

		/// <summary>
		/// Override this to describe what happens when a point is reached
		/// </summary>
		protected virtual void PointReached()
		{

		}

		/// <summary>
		/// Override this to describe what happens when the end of the path is reached
		/// </summary>
		protected virtual void EndReached()
		{

		}

		/// <summary>
		/// On update we keep moving along the path
		/// </summary>
		protected virtual void ExecuteUpdate () 
		{
			// if the path is null we exit, if we only go once and have reached the end we exit, if we can't move we exit
			if(PathElements == null 
			   || PathElements.Count < 1
			   || _endReached
			   || !CanMove
			  )
			{
				CurrentSpeed = _vector3Zero;
				return;
			}

			Move ();

			_positionLastFrame = this.transform.position;
		}

		/// <summary>
		/// Moves the object and determines when a point has been reached
		/// </summary>
		protected virtual void Move()
		{
			// we wait until we can proceed
			_waiting -= Time.deltaTime;
			if (_waiting > 0)
			{
				CurrentSpeed = Vector3.zero;
				return;
			}

			// we store our initial position to compute the current speed at the end of the udpate	
			_initialPosition = transform.position;

			// we move our object
			MoveAlongThePath();

			// we decide if we've reached our next destination or not, if yes, we move our destination to the next point 
			_distanceToNextPoint = (transform.position - (PointPosition(_currentPoint.Current))).magnitude;
			if(_distanceToNextPoint < MinDistanceToGoal)
			{
				//we check if we need to wait
				if (PathElements.Count > _currentIndex)
				{
					_waiting = PathElements[_currentIndex].Delay;				 
				}
				PointReached();
				_previousPoint = _currentPoint.Current;
				_currentPoint.MoveNext();
			}

			// we determine the current speed		
			_finalPosition = this.transform.position;
			if (Time.deltaTime != 0f)
			{
				CurrentSpeed = (_finalPosition - _initialPosition) / Time.deltaTime;
			}

			if (_endReached) 
			{
				EndReached();
				CurrentSpeed = Vector3.zero;
			}
		}

		/// <summary>
		/// Moves the object along the path according to the specified movement type.
		/// </summary>
		public virtual void MoveAlongThePath()
		{
			switch (AccelerationType)
			{
				case PossibleAccelerationType.ConstantSpeed:
					transform.position = Vector3.MoveTowards (transform.position, PointPosition(_currentPoint.Current), Time.deltaTime * MovementSpeed);
					break;
				
				case PossibleAccelerationType.EaseOut:
					transform.position = Vector3.Lerp (transform.position, PointPosition(_currentPoint.Current), Time.deltaTime * MovementSpeed);
					break;

				case PossibleAccelerationType.AnimationCurve:
					float distanceBetweenPoints = Vector3.Distance (_previousPoint, _currentPoint.Current);

					if (distanceBetweenPoints <= 0)
					{
						return;
					}

					float remappedDistance = 1 - MMMaths.Remap (_distanceToNextPoint, 0f, distanceBetweenPoints, 0f, 1f);
					float speedFactor = Acceleration.Evaluate (remappedDistance);

					transform.position = Vector3.MoveTowards (transform.position, PointPosition(_currentPoint.Current), Time.deltaTime * MovementSpeed * speedFactor);
					break;
			}
		}

		/// <summary>
		/// Returns the current target point in the path
		/// </summary>
		/// <returns>The path enumerator.</returns>
		public virtual IEnumerator<Vector3> GetPathEnumerator()
		{

			// if the path is null we exit
			if(PathElements == null || PathElements.Count < 1)
			{
				yield break;
			}

			int index = 0;
			_currentIndex = index;
			while (true)
			{
				_currentIndex = index;
				yield return PathElements[index].PathElementPosition;
				
				if(PathElements.Count <= 1)
				{
					continue;
				}

				// if the path is looping
				switch(CycleOption)
				{
					case CycleOptions.Loop:
						index = index + _direction;
						if (index < 0)
						{
							index = PathElements.Count - 1;
						}
						else if (index > PathElements.Count - 1)
						{
							index = 0;
						}
						break;

					case CycleOptions.BackAndForth:
						if (index <= 0)
						{
							_direction = 1;
						}
						else if (index >= PathElements.Count - 1)
						{
							_direction = -1;
						}
						index = index + _direction;
						break;

					case CycleOptions.OnlyOnce:
						if (index <= 0)
						{
							_direction = 1;
						}
						else if (index >= PathElements.Count - 1)
						{
							_direction = 0;
							CurrentSpeed = Vector3.zero;
							_endReached = true;
						}
						index = index + _direction;
						break;
                    
					case CycleOptions.Random:
						int newIndex = index;
						if (PathElements.Count > 1)
						{
							while (newIndex == index)
							{
								newIndex = Random.Range(0, PathElements.Count);
							}    
						}
						index = newIndex;
						break;

					case CycleOptions.StopAtBounds:
						if (index <= 0)
						{
							if (_direction == -1)
							{
								CurrentSpeed = Vector3.zero;
								_endReached = true;
							}
							_direction = 1;
						}
						else if (index >= PathElements.Count - 1)
						{
							if (_direction == 1)
							{
								CurrentSpeed = Vector3.zero;
								_endReached = true;
							}
							_direction = -1;
						}
						index = index + _direction;
						break;
				}
			}
		}

		/// <summary>
		/// Call this method to force a change in direction at any time
		/// </summary>
		public virtual void ChangeDirection()
		{
			_direction = -_direction;
			_currentPoint.MoveNext();
		}

		/// <summary>
		/// On DrawGizmos, we draw lines to show the path the object will follow
		/// </summary>
		protected virtual void OnDrawGizmos()
		{	
			#if UNITY_EDITOR
			if (PathElements == null)
			{
				return;
			}

			if (PathElements.Count == 0)
			{
				return;
			}
							
			// if we haven't stored the object's original position yet, we do it
			if (_originalTransformPositionStatus == false)
			{
				_originalTransformPosition = this.transform.position;
				_originalTransformPositionStatus = true;
			}
			// if we're not in runtime mode and the transform has changed, we update our position
			if (transform.hasChanged && _active==false)
			{
				_originalTransformPosition = this.transform.position;
			}
			// for each point in the path
			for (int i=0;i<PathElements.Count;i++)
			{
				// we draw a green point 
				MMDebug.DrawGizmoPoint(PointPosition(i),0.2f,Color.green);

				// we draw a line towards the next point in the path
				if ((i+1)<PathElements.Count)
				{
					Gizmos.color=Color.white;
					Gizmos.DrawLine(PointPosition(i), PointPosition(i + 1));
				}
				// we draw a line from the first to the last point if we're looping
				if ( (i == PathElements.Count-1) && (CycleOption == CycleOptions.Loop) )
				{
					Gizmos.color=Color.white;
					Gizmos.DrawLine(PointPosition(0), PointPosition(i));
				}
			}

			// if the game is playing, we add a blue point to the destination, and a red point to the last visited point
			if (Application.isPlaying)
			{
				MMDebug.DrawGizmoPoint(PointPosition(_currentPoint.Current), 0.2f, Color.blue);
				MMDebug.DrawGizmoPoint(PointPosition(_previousPoint),0.2f,Color.red);
			}
			#endif
		}

		public virtual Vector3 PointPosition(int index)
		{
			return PointPosition(PathElements[index].PathElementPosition);
		}

		public virtual Vector3 PointPosition(Vector3 relativePointPosition)
		{
			switch (AlignmentMode)
			{
				case AlignmentModes.None:
					return _originalTransformPosition + relativePointPosition;
				case AlignmentModes.ThisRotation:
					return _originalTransformPosition + this.transform.rotation *  relativePointPosition;
				case AlignmentModes.ParentRotation:
					return _originalTransformPosition + this.transform.parent.rotation *  relativePointPosition;
			}
			return Vector3.zero;
		}

		/// <summary>
		/// Updates the original transform position.
		/// </summary>
		/// <param name="newOriginalTransformPosition">New original transform position.</param>
		public virtual void UpdateOriginalTransformPosition(Vector3 newOriginalTransformPosition)
		{
			_originalTransformPosition = newOriginalTransformPosition;
		}

		/// <summary>
		/// Gets the original transform position.
		/// </summary>
		/// <returns>The original transform position.</returns>
		public virtual Vector3 GetOriginalTransformPosition()
		{
			return _originalTransformPosition;
		}

		/// <summary>
		/// Sets the original transform position status.
		/// </summary>
		/// <param name="status">If set to <c>true</c> status.</param>
		public virtual void SetOriginalTransformPositionStatus(bool status)
		{
			_originalTransformPositionStatus = status;
		}

		/// <summary>
		/// Gets the original transform position status.
		/// </summary>
		/// <returns><c>true</c>, if original transform position status was gotten, <c>false</c> otherwise.</returns>
		public virtual bool GetOriginalTransformPositionStatus()
		{
			return _originalTransformPositionStatus ;
		}
	}
}
