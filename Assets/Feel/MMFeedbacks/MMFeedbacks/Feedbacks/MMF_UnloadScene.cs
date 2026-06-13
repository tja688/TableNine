using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you unload a scene by name or build index
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈可让你通过场景名或 Build Index 卸载场景。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Scene/Unload Scene")]
	public class MMF_UnloadScene : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		public enum ColorModes { Instant, Gradient, Interpolate }

		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SceneColor; } }

		public override bool EvaluateRequiresSetup()
		{
			if (Method == Methods.BuildIndex)
			{
				return false;
                
			}
			else if (Method == Methods.SceneName)
			{
				return ((SceneName == null) || (SceneName == ""));
			}
			return false;
		}
		public override string RequiredTargetText { get { return SceneName;  } }
		public override string RequiresSetupText { get { return "此反馈需要先根据 Method 配置有效目标：SceneName 模式填写 SceneName，BuildIndex 模式填写 BuildIndex；并确保目标场景已加入 Build Settings。"; } }
		#endif
        
		public enum Methods { BuildIndex, SceneName }

		[MMFInspectorGroup("Unload Scene", true, 43, false)]
        
		/// whether to unload a scene by build index or by name
		[Tooltip("卸载方式：按构建索引或按场景名称。")]
		public Methods Method = Methods.SceneName;

		/// the build ID of the scene to unload, find it in your Build Settings
		[Tooltip("要卸载的场景建立索引（可在构建设置中查看）。仅在构建索引模式下生效。")]
		[MMFEnumCondition("Method", (int)Methods.BuildIndex)]
		public int BuildIndex = 0;

		/// the name of the scene to unload
		[Tooltip("要卸载的场景名称。仅在 SceneName 模式下生效。")]
		[MMFEnumCondition("Method", (int)Methods.SceneName)]
		public string SceneName = "";

        
		/// whether or not to output warnings if the scene doesn't exist or can't be loaded
		[Tooltip("若场景不存在、未加载或无法卸载，是否输出警告日志。")]
		public bool OutputWarningsIfNeeded = true;
        
		protected Scene _sceneToUnload;

		/// <summary>
		/// On play we change the text of our target TMPText
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			if (Method == Methods.BuildIndex)
			{
				_sceneToUnload = SceneManager.GetSceneByBuildIndex(BuildIndex);
			}
			else
			{
				_sceneToUnload = SceneManager.GetSceneByName(SceneName);
			}

			if ((_sceneToUnload != null) && (_sceneToUnload.isLoaded))
			{
				SceneManager.UnloadSceneAsync(_sceneToUnload);    
			}
			else
			{
				if (OutputWarningsIfNeeded)
				{
					Debug.LogWarning("[Unload Scene Feedback] The unload scene feedback on "+Owner.name+" is trying to unload a scene that hasn't been loaded.");   
				}
			}
		}
	}
}

