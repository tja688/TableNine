using System;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[Serializable]
	public class MMSpringClampSettings
	{
		[Header("Min")]
		/// 是否限制此 spring 的最小值，防止它低于某个阈值。
		[Tooltip("是否限制此 spring 的最小值，防止它低于某个阈值。")]
		public bool ClampMin = false;
		/// 此 spring 允许达到的最低值。
		[Tooltip("此 spring 允许达到的最低值。")]
		[MMCondition("ClampMin", true)]
		public float ClampMinValue = 0f;
		/// 当 `ClampMin` 为 true 时，是否把初始值当作最小值使用。若启用，下方手动最小值将失效。
		[Tooltip("当 `ClampMin` 为 true 时，是否把初始值当作最小值使用。若启用，下方手动最小值将失效。")]
		[MMCondition("ClampMin", true)]
		public bool ClampMinInitial = false;
		/// spring 触及最小值时是否产生反弹。
		[Tooltip("spring 触及最小值时是否产生反弹。")]
		[MMCondition("ClampMin", true)]
		public bool ClampMinBounce = false;
		
		[Header("Max")]
		/// 是否限制此 spring 的最大值，防止它超过某个阈值。
		[Tooltip("是否限制此 spring 的最大值，防止它超过某个阈值。")]
		public bool ClampMax = false;
		/// 此 spring 允许达到的最大值。
		[Tooltip("此 spring 允许达到的最大值。")]
		[MMCondition("ClampMax", true)]
		public float ClampMaxValue = 10f;
		/// 当 `ClampMax` 为 true 时，是否把初始值当作最大值使用。若启用，下方手动最大值将失效。
		[Tooltip("当 `ClampMax` 为 true 时，是否把初始值当作最大值使用。若启用，下方手动最大值将失效。")]
		[MMCondition("ClampMax", true)]
		public bool ClampMaxInitial = false;
		/// spring 触及最大值时是否产生反弹。
		[Tooltip("spring 触及最大值时是否产生反弹。")]
		[MMCondition("ClampMax", true)]
		public bool ClampMaxBounce = false;

		public bool ClampNeeded => ClampMin || ClampMax || ClampMinBounce || ClampMaxBounce;

		public virtual float GetTargetValue(float value, float initialValue)
		{
			float targetValue = value;
			float clampMinValue = ClampMinInitial ? initialValue : ClampMinValue;
			if (ClampMin && value < clampMinValue)
			{
				targetValue = clampMinValue;
			}
			float clampMaxValue = ClampMaxInitial ? initialValue : ClampMaxValue;
			if (ClampMax && value > clampMaxValue)
			{
				targetValue = clampMaxValue;
			}
			return targetValue;
		}
	}
}

