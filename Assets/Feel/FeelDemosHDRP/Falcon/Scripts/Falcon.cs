using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace  MoreMountains.Feel
{
	[AddComponentMenu("")]
	public class Falcon : MonoBehaviour
	{
		[Header("Input")]
		/// a key to use to jump
		[Tooltip("用于跳跃的按键")]
		public KeyCode ActionKey = KeyCode.Space;
		/// a secondary key to use to jump
		[Tooltip("用于跳跃的备用按键")]
		public KeyCode ActionKeyAlt = KeyCode.Joystick1Button0;

		[Header("Bindings")]
		/// the various wigglers that make the car move
		[Tooltip("驾驶运动车辆的各类移动装置")]
		public List<MMWiggle> Wigglers;
		/// the wiggler associated to the camera
		[Tooltip("绑定到宇航员的移动器")]
		public MMWiggle CameraWiggler;
		/// the ground's panning texture
		[Tooltip("地面材质上用于滚动的纹理")]
		public MMPanningTexture Offsetter;
		/// the particles that are supposed to loop (rocks etc)
		[Tooltip("需要循环播放的粒子效果（如石块等）")]
		public List<ParticleSystem> ParticleLoops;
		/// the on/off emitters (wind, smoke)
		[Tooltip("可开关的发射器（风、烟等）")]
		public List<ParticleSystem> ParticleEmitters;
		/// the wheels' auto rotators
		[Tooltip("车轮自动旋转组件")]
		public List<MMAutoRotate> AutoRotaters;

		[Header("Settings")] 
		/// the speed at which the wheel should rotate
		[Tooltip("车轮旋转速度")]
		public float RotationSpeed = 20f;

		[Header("Feedbacks")]
		/// a feedback to call when the car starts driving
		[Tooltip("车辆开始行驶时要触发的反馈")]
		public MMFeedbacks DriveFeedback;
		/// a feedback to call when the car stops
		[Tooltip("车辆停止时要触发的反馈")]
		public MMFeedbacks StopFeedback;
        
		protected bool _turning;

		/// <summary>
		/// On Start, we turn the car off
		/// </summary>
		protected virtual void Start()
		{
			SetCar(false);
		}

		/// <summary>
		/// Turns the car on or off depending on the status passed in parameters
		/// </summary>
		/// <param name="status"></param>
		protected virtual void SetCar(bool status)
		{
			foreach (MMWiggle wiggler in Wigglers)
			{
				wiggler.PositionActive = status;
			}
			foreach (ParticleSystem system in ParticleEmitters)
			{
				if (status)
				{
					system.Play();
				}
				else
				{
					system.Stop();
				}
			}
			foreach (ParticleSystem system in ParticleLoops)
			{
				if (status)
				{
					system.Play();
				}
				else
				{
					system.Pause();
				}
			}
			foreach (MMAutoRotate rotater in AutoRotaters)
			{
				rotater.Rotating = status;
			}

			Offsetter.TextureShouldPan = status;

			CameraWiggler.PositionActive = status;
			CameraWiggler.RotationActive = status;
		}

		/// <summary>
		/// On Update we look for input
		/// </summary>
		protected virtual void Update()
		{
			HandleInput();
			HandleCar();
		}

		/// <summary>
		/// Detects input
		/// </summary>
		protected virtual void HandleInput()
		{
			if (FeelDemosInputHelper.CheckMainActionInputPressed())
			{
				Drive();
			}
			if (FeelDemosInputHelper.CheckMainActionInputUpThisFrame())
			{
				TurnStop();
			}
		}

		/// <summary>
		/// Every frame, rotates the wheel if needed
		/// </summary>
		protected virtual void HandleCar()
		{
			if (_turning)
			{
				//RotatingPart.transform.Rotate(this.transform.right, RotationSpeed * Time.deltaTime);
			}
		}

		/// <summary>
		/// Makes the car drive, plays a feedback if it's just starting to turn this frame
		/// </summary>
		protected virtual void Drive()
		{
			if (!_turning)
			{
				DriveFeedback?.PlayFeedbacks();
				SetCar(true);
			}
			_turning = true;
		}
        
		/// <summary>
		/// Stops the car
		/// </summary>
		protected virtual void TurnStop()
		{
			DriveFeedback?.StopFeedbacks();
			StopFeedback?.PlayFeedbacks();
			SetCar(false);
			_turning = false;
		}
	}    
}
