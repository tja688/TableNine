using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback will request the load of a new scene, using the method of your choice
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("此反馈会按你选择的方式请求加载一个新场景。")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks.MMTools")]
	[System.Serializable]
	[FeedbackPath("Scene/Load Scene")]
	public class MMF_LoadScene : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.SceneColor; } }
		public override bool EvaluateRequiresSetup() { return (DestinationSceneName == ""); }
		public override string RequiredTargetText { get { return DestinationSceneName;  } }
		public override string RequiresSetupText { get { return "此反馈必须先填写 DestinationSceneName，并确保目标场景已加入 Build Settings。"; } }
		#endif

		/// the possible ways to load a new scene :
		/// - direct : uses Unity's SceneManager API
		/// - direct additive : uses Unity's SceneManager API, but with additive mode (so loading the scene on top of the current one)
		/// - MMSceneLoadingManager : the simple, original MM way of loading scenes
		/// - MMAdditiveSceneLoadingManager : a more advanced way of loading scenes, with (way) more options
		public enum LoadingModes { Direct, MMSceneLoadingManager, MMAdditiveSceneLoadingManager, DirectAdditive }

		[MMFInspectorGroup("Scene Loading", true, 57, true)]
		/// the name of the loading screen scene to use
		[Tooltip("要使用的加载界面场景名称，必须已添加到 Build Settings 中")]
		public string LoadingSceneName = "MMAdditiveLoadingScreen";
		/// the name of the destination scene
		[Tooltip("目标场景名称，必须已添加到 Build Settings 中")]
		public string DestinationSceneName = "";

		[Header("Mode")] 
		/// the loading mode to use
		[Tooltip("用于加载目标场景的方式： - 直接：使用 统一 的 场景管理器应用程序编程接口 - 毫米场景加载管理器：毫米 提供的简单的原始加载方式 - 毫米 增材场景加载管理器：更高级的加载方式，拥有更多可选项")]
		public LoadingModes LoadingMode = LoadingModes.MMAdditiveSceneLoadingManager;
        
		[Header("Loading Scene Manager")]
		/// the priority to use when loading the new scenes
		[Tooltip("加载新场景时使用的优先级")]
		public ThreadPriority Priority = ThreadPriority.High;
		/// whether or not to perform extra checks to make sure the loading screen and destination scene are in the build settings
		[Tooltip("是否执行额外检查，以确认加载场景与目标场景都已加入 Build Settings")]
		public bool SecureLoad = true;
		/// the chosen way to unload scenes (none, only the active scene, all loaded scenes)
		[Tooltip("场景卸载方式（不卸载、仅卸载当前活动场景、卸载所有已加载场景）")]
		[MMFEnumCondition("LoadingMode", (int)LoadingModes.MMAdditiveSceneLoadingManager)]
		public MMAdditiveSceneLoadingManagerSettings.UnloadMethods UnloadMethod =
			MMAdditiveSceneLoadingManagerSettings.UnloadMethods.AllScenes;
		/// the name of the anti spill scene to use when loading additively.
		/// If left empty, that scene will be automatically created, but you can specify any scene to use for that. Usually you'll want your own anti spill scene to be just an empty scene, but you can customize its lighting settings for example.
		[Tooltip("以 Additive 方式加载时使用的 anti spill 场景名称。" +
		         "若留空，系统会自动创建该场景；你也可以手动指定任意场景。通常建议将 anti spill 场景设为空场景，但你也可以按需自定义其光照设置。")]
		[MMFEnumCondition("LoadingMode", (int)LoadingModes.MMAdditiveSceneLoadingManager)]
		public string AntiSpillSceneName = "";
		/// in additive mode, whether or not to display debug logs of the loading sequence
		[Tooltip("在 Additive 模式下，是否输出加载流程的调试日志")]
		[MMFEnumCondition("LoadingMode", (int)LoadingModes.MMAdditiveSceneLoadingManager)]
		public bool DebugMode = false;
		
		[MMFInspectorGroup("Loading Scene Delays", true, 58)] 
		/// a delay (in seconds) to apply before the first fade plays
		[Tooltip("第一次淡入淡出播放前要等待的延迟（秒）")]
		public float BeforeEntryFadeDelay = 0f;
		/// the duration (in seconds) of the entry fade
		[Tooltip("入场淡入淡出的持续时间（秒）")]
		public float EntryFadeDuration = 0.2f;
		/// a delay (in seconds) to apply after the first fade plays
		[Tooltip("第一次淡入淡出播放后要等待的延迟（秒）")]
		public float AfterEntryFadeDelay = 0f;
		/// a delay (in seconds) to apply before the scene gets activated
		[Tooltip("场景激活前要等待的延迟（秒）")]
		public float BeforeSceneActivationDelay = 0f;
		/// a delay applied after the scene is loaded
		[Tooltip("场景加载完成后施加的延迟")]
		public float AfterSceneActivationDelay = 0f;
		/// the duration (in seconds) of the exit fade
		[Tooltip("退场淡入淡出的持续时间（秒）")]
		public float ExitFadeDuration = 0.2f;
		
		[MMFInspectorGroup("Speed", true, 59)] 
		/// whether or not to interpolate progress (slower, but usually looks better and smoother)
		[Tooltip("是否对进度值做插值处理（速度会稍慢，但通常视觉上更平滑）")]
		public bool InterpolateProgress = true;
		/// the speed at which the progress bar should move if interpolated
		[Tooltip("若启用插值，进度条移动的速度")]
		public float ProgressInterpolationSpeed = 5f;
		/// a list of progress intervals (values should be between 0 and 1) and their associated speeds, letting you have the bar progress less linearly
		[Tooltip("进度区间列表（取值应位于 0 到 1 之间）及其对应速度，可让进度条以更非线性的方式推进")]
		public List<MMSceneLoadingSpeedInterval> SpeedIntervals;
        
		[MMFInspectorGroup("Transitions", true, 59)]
		/// the order in which to play fades (really depends on the type of fader you have in your loading screen
		[Tooltip("淡入淡出的播放顺序（实际应取决于你的加载界面中使用的淡入淡出器类型）")]
		public MMAdditiveSceneLoadingManager.FadeModes FadeMode = MMAdditiveSceneLoadingManager.FadeModes.FadeInThenOut;
		/// the tween to use on the entry fade
		[Tooltip("入场淡入淡出使用的 Tween")]
		public MMTweenType EntryFadeTween = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
		/// the tween to use on the exit fade
		[Tooltip("退场淡入淡出使用的 Tween")]
		public MMTweenType ExitFadeTween = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));

		/// <summary>
		/// On play, we request a load of the destination scene using hte specified method
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized || (DestinationSceneName == ""))
			{
				return;
			}
			switch (LoadingMode)
			{
				case LoadingModes.Direct:
					SceneManager.LoadScene(DestinationSceneName);
					break;
				case LoadingModes.DirectAdditive:
					SceneManager.LoadScene(DestinationSceneName, LoadSceneMode.Additive);
					break;
				case LoadingModes.MMSceneLoadingManager:
					MMSceneLoadingManager.LoadScene(DestinationSceneName, LoadingSceneName);
					break;
				case LoadingModes.MMAdditiveSceneLoadingManager:
					MMAdditiveSceneLoadingManager.LoadScene(DestinationSceneName, LoadingSceneName, 
						Priority, SecureLoad, InterpolateProgress, 
						BeforeEntryFadeDelay, EntryFadeDuration,
						AfterEntryFadeDelay,
						BeforeSceneActivationDelay, 
						AfterSceneActivationDelay,
						ExitFadeDuration,
						EntryFadeTween, ExitFadeTween,
						ProgressInterpolationSpeed, FadeMode, UnloadMethod, AntiSpillSceneName,
						SpeedIntervals, DebugMode);
					break;
			}
		}
	}
}
