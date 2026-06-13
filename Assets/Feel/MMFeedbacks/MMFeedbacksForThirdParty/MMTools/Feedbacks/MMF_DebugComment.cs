using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback doesn't do anything by default, it's just meant as a comment, you can store text in it for future reference, maybe to remember how you setup a particular MMFeedbacks. Optionally it can also output that comment to the console on Play.
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈默认不会执行任何操作，它更像是一条注释，你可以在其中记录文本，方便以后回看，例如备注某个 MMFeedbacks 的配置思路。你也可以选择在 Play 时将这条注释输出到控制台。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("Debug/Comment")]
	public class MMF_DebugComment : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.DebugColor; } }
		#endif
     
		[MMFInspectorGroup("Comment", true, 61)]
		/// the comment / note associated to this feedback 
		[Tooltip("与该反馈关联的注释 / 备注")]
		[TextArea(10,30)] 
		public string Comment;

		/// if this is true, the comment will be output to the console on Play 
		[Tooltip("若启用，Play 时会将该注释输出到控制台")]
		public bool LogComment = false;
		/// the color of the message when in DebugLogTime mode
		[Tooltip("处于 DebugLogTime 模式时消息显示的颜色")]
		[MMCondition("LogComment", true)]
		public Color DebugColor = Color.gray;
        
		/// <summary>
		/// On Play we output our message to the console if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || !LogComment)
			{
				return;
			}
            
			MMDebug.DebugLogInfo(Comment);
		}
	}
}