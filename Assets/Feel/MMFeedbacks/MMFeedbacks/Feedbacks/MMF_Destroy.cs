using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback allows you to destroy a target gameobject, either via Destroy, DestroyImmediate, or SetActive:False
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你销毁目标 GameObject，可选方式为 Destroy、DestroyImmediate，或 SetActive:false。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("GameObject/Destroy")]
	public class MMF_Destroy : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetGameObject == null); }
		public override string RequiredTargetText { get { return TargetGameObject != null ? TargetGameObject.name : "";  } }
		public override string RequiresSetupText { get { return "此反馈必须先设置a TargetGameObject才能正常工作。你可以在下方进行设置。"; } }
		#endif
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetGameObject = FindAutomatedTargetGameObject();

		/// the possible ways to destroy an object
		public enum Modes { Destroy, DestroyImmediate, Disable }

		[MMFInspectorGroup("Destruction", true, 18, true)]
		/// the gameobject we want to change the active state of
		[Tooltip("要引入的目标 游戏对象")]
		public GameObject TargetGameObject;
		/// the optional list of extra gameobjects we want to change the active state of
		[Tooltip("可选的额外 GameObject 列表（会一起执行相同操作）")]
		public List<GameObject> ExtraTargetGameObjects;
		
		/// the selected destruction mode 
		[Tooltip("当前销毁模式")]
		public Modes Mode;

		protected bool _initialActiveState;

		/// <summary>
		/// On Play we change the state of our Behaviour if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (TargetGameObject == null))
			{
				return;
			}
			ProceedWithDestruction(TargetGameObject);
			foreach (GameObject go in ExtraTargetGameObjects)
			{
				ProceedWithDestruction(go);
			}
		}
        
		/// <summary>
		/// Changes the status of the Behaviour
		/// </summary>
		/// <param name="state"></param>
		protected virtual void ProceedWithDestruction(GameObject go)
		{
			switch (Mode)
			{
				case Modes.Destroy:
					Owner.ProxyDestroy(go);
					break;
				case Modes.DestroyImmediate:
					Owner.ProxyDestroyImmediate(go);
					break;
				case Modes.Disable:
					_initialActiveState = go.activeInHierarchy;
					go.SetActive(false);
					break;
			}
		}
		
		/// <summary>
		/// On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (Mode == Modes.Disable)
			{
				TargetGameObject.SetActive(_initialActiveState);
			}
		}
	}
}


