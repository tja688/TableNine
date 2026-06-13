using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A feedback used to change the parent of a transform
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你修改某个 Transform 的父级。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Transform/Set Parent")]
	public class MMF_SetParent : MMF_Feedback 
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
        
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TransformColor; } }
		public override bool EvaluateRequiresSetup() { return (ObjectToParent == null); }
		public override string RequiredTargetText { get { return ObjectToParent != null ? ObjectToParent.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈需要先设置 ObjectToParent；播放时它会被重新设为 NewParent 的子级。"; } } 
		#endif
		
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => ObjectToParent = FindAutomatedTarget<Transform>(); 

		[MMFInspectorGroup("Parenting", true, 12, true)]
		/// the object we want to change the parent of
		[Tooltip("要修改父级的对象")]
		public Transform ObjectToParent;
		/// the object ObjectToParent should now be parented to after playing this feedback
		[Tooltip("播放后，ObjectToParent 将被设置到该父级之下")]
		public Transform NewParent;
		/// if true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before
		[Tooltip("若开启，会调整父级相对位置/旋转/缩放，以保持对象世界空间下的位姿不变。")]
		public bool WorldPositionStays = true;

		/// <summary>
		/// On Play, changes the parent of the target transform
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			if (ObjectToParent == null)
			{
				Debug.LogWarning("[SetParent Feedback] The set parent feedback on "+Owner.name+" doesn't have an ObjectToParent, it won't work. You need to specify one in its inspector.");
				return;
			}
			ObjectToParent.SetParent(NewParent, WorldPositionStays);
		}
	}
}

