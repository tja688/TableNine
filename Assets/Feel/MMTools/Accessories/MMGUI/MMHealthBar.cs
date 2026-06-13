using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
#if MM_UI
using UnityEngine.UI;
#endif

namespace MoreMountains.Tools
{
	/// <summary>
	/// Add this component to an object and it will show a healthbar above it
	/// You can either use a prefab for it, or have the component draw one at the start
	/// </summary>
	[AddComponentMenu("More Mountains/Tools/GUI/MM Health Bar")]
	public class MMHealthBar : MonoBehaviour 
	{
		#if MM_UI
		/// the possible health bar types
		public enum HealthBarTypes { Prefab, Drawn, Existing }
		/// the possible timescales the bar can work on
		public enum TimeScales { UnscaledTime, Time }

		[MMInformation("把这个组件挂到对象上后，它会在对象旁边添加一个血条，用于实时反映生命值。你可以在这里决定血条是自动绘制，还是使用预制体。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// whether the healthbar uses a prefab or is drawn automatically
		[Tooltip("决定血条是使用预制体、自动绘制，还是直接复用现有 MMProgressBar")]
		public HealthBarTypes HealthBarType = HealthBarTypes.Drawn;
		/// defines whether the bar will work on scaled or unscaled time (whether or not it'll keep moving if time is slowed down for example)
		[Tooltip("定义血条使用受时间缩放影响的时间，还是不受时间缩放影响的时间；例如时间变慢时，血条动画是否仍继续播放。")]
		public TimeScales TimeScale = TimeScales.UnscaledTime;

		[Header("Select a Prefab")]
		[MMInformation("选择一个挂有进度条脚本的预制体。`Common/Prefabs/GUI` 中提供了一个示例。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// the prefab to use as the health bar
		[Tooltip("作为血条使用的预制体")]
		public MMProgressBar HealthBarPrefab;

		[Header("Existing MMProgressBar")]
		/// the MMProgressBar this health bar should update 
		[Tooltip("该血条要更新的MM进度条")]
		public MMProgressBar TargetProgressBar;

		[Header("Drawn Healthbar Settings ")]
		[MMInformation("设置血条的尺寸（世界单位）、内边距，以及前景、背景等颜色。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// if the healthbar is drawn, its size in world units
		[Tooltip("当血条为自动绘制时，它在世界空间中的尺寸")]
		public Vector2 Size = new Vector2(1f,0.2f);
		/// if the healthbar is drawn, the padding to apply to the foreground, in world units
		[Tooltip("当血条为自动绘制时，前景条要应用的内边距（世界单位）")]
		public Vector2 BackgroundPadding = new Vector2(0.01f,0.01f);
		/// the rotation to apply to the MMHealthBarContainer when drawing it
		[Tooltip("自动绘制时，要应用到 MMHealthBarContainer 的旋转")]
		public Vector3 InitialRotationAngles;
		/// if the healthbar is drawn, the color of its foreground
		[Tooltip("自动绘制时，前景条使用的颜色")]
		public Gradient ForegroundColor = new Gradient()
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(MMColors.BestRed, 0),
				new GradientColorKey(MMColors.BestRed, 1f)
			},
			alphaKeys = new GradientAlphaKey[2] {new GradientAlphaKey(1, 0),new GradientAlphaKey(1, 1)}};
		/// if the healthbar is drawn, the color of its delayed bar
		[Tooltip("自动绘制时，延迟条使用的颜色")]
		public Gradient DelayedColor = new Gradient()
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(MMColors.Orange, 0),
				new GradientColorKey(MMColors.Orange, 1f)
			},
			alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
		};
		/// if the healthbar is drawn, the color of its border
		[Tooltip("自动绘制时，边框使用的颜色")]
		public Gradient BorderColor = new Gradient()
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(MMColors.AntiqueWhite, 0),
				new GradientColorKey(MMColors.AntiqueWhite, 1f)
			},
			alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
		};
		/// if the healthbar is drawn, the color of its background
		[Tooltip("自动绘制时，背景使用的颜色")]
		public Gradient BackgroundColor = new Gradient()
		{
			colorKeys = new GradientColorKey[2] {
				new GradientColorKey(MMColors.Black, 0),
				new GradientColorKey(MMColors.Black, 1f)
			},
			alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
		};
		/// the name of the sorting layer to put this health bar on
		[Tooltip("该血条要放置到的 Sorting Layer 名称")]
		public string SortingLayerName = "UI";
		/// the delay to apply to the delayed bar if drawn
		[Tooltip("自动绘制时，延迟条要使用的延迟")]
		public float Delay = 0.5f;
		/// whether or not the front bar should lerp
		[Tooltip("前景条是否进行插值过渡")]
		public bool LerpFrontBar = true;
		/// the speed at which the front bar lerps
		[Tooltip("前景条的插值速度")]
		public float LerpFrontBarSpeed = 15f;
		/// whether or not the delayed bar should lerp
		[Tooltip("延迟条是否进行插值过渡")]
		public bool LerpDelayedBar = true;
		/// the speed at which the delayed bar lerps
		[Tooltip("延迟条的插值速度")]
		public float LerpDelayedBarSpeed = 15f;
		/// if this is true, bumps the scale of the healthbar when its value changes
		[Tooltip("若开启，血条数值变化时会触发缩放弹跳效果")]
		public bool BumpScaleOnChange = true;
		/// the duration of the bump animation
		[Tooltip("弹跳动画的持续时间")]
		public float BumpDuration = 0.2f;
		/// the animation curve to map the bump animation on
		[Tooltip("控制弹跳动画的曲线")]
		public AnimationCurve BumpAnimationCurve = AnimationCurve.Constant(0,1,1);
		
		
		/// the mode the bar should follow the target in
		[Tooltip("血条跟随目标的更新模式")]
		public MMFollowTarget.UpdateModes FollowTargetMode = MMFollowTarget.UpdateModes.LateUpdate;
		/// if this is true, the drawn health bar will adapt its rotation to match the one of its target
		[Tooltip("若开启，自动绘制的血条会同步目标的旋转")]
		public bool FollowRotation = false;
		/// if this is true, the drawn health bar will adapt its scale to match the one of its target
		[Tooltip("若开启，自动绘制的血条会同步目标的缩放")]
		public bool FollowScale = true;
		/// if this is true, the drawn health bar will be nested below the MMHealthBar
		[Tooltip("若开启，自动绘制的血条会作为 MMHealthBar 的子对象挂接在其下")]
		public bool NestDrawnHealthBar = false;
		/// if this is true, a MMBillboard component will be added to the progress bar to make sure it always looks towards the camera
		[Tooltip("若开启，会给进度条添加 MMBillboard 组件，以确保它始终朝向摄像机")]
		public bool Billboard = false;

		[Header("Death")]
		/// a gameobject (usually a particle system) to instantiate when the healthbar reaches zero
		[Tooltip("当血条降为 0 时要实例化的 GameObject（通常是粒子系统）")]
		public GameObject InstantiatedOnDeath;

		[Header("Offset")]
		[MMInformation("设置血条相对于对象中心的显示偏移（世界单位）。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// the offset to apply to the healthbar compared to the object's center
		[Tooltip("相对于对象中心应用到血条的偏移量")]
		public Vector3 HealthBarOffset = new Vector3(0f,1f,0f);

		[Header("Display")]
		[MMInformation("在这里定义血条是否始终可见。若关闭，可进一步设置对象受击后血条还要保持可见多久。",MoreMountains.Tools.MMInformationAttribute.InformationType.Info,false)]
		/// whether or not the bar should be permanently displayed
		[Tooltip("血条是否始终显示")]
		public bool AlwaysVisible = true;
		/// the duration (in seconds) during which to display the bar
		[Tooltip("血条显示的持续时间（秒）")]
		public float DisplayDurationOnHit = 1f;
		/// if this is set to true the bar will hide itself when it reaches zero
		[Tooltip("若开启，血条降为 0 时会自动隐藏")]
		public bool HideBarAtZero = true;
		/// the delay (in seconds) after which to hide the bar
		[Tooltip("延迟多久后隐藏血条（秒）")]
		public float HideBarAtZeroDelay = 1f;

		[Header("Test")] 
		/// a test value to use when pressing the TestUpdateHealth button
		[Tooltip("点击 TestUpdateHealth 按钮时使用的测试值")]
		public float TestMinHealth = 0f;
		/// a test value to use when pressing the TestUpdateHealth button
		[Tooltip("点击 TestUpdateHealth 按钮时使用的测试值")]
		public float TestMaxHealth = 100f;
		/// a test value to use when pressing the TestUpdateHealth button
		[Tooltip("点击 TestUpdateHealth 按钮时使用的测试值")]
		public float TestCurrentHealth = 25f;
		[MMInspectorButton("TestUpdateHealth")]
		public bool TestUpdateHealthButton;
		
		
		protected MMProgressBar _progressBar;
		protected MMFollowTarget _followTransform;
		protected float _lastShowTimestamp = 0f;
		protected bool _showBar = false;
		protected Image _backgroundImage = null;
		protected Image _borderImage = null;
		protected Image _foregroundImage = null;
		protected Image _delayedImage = null;
		protected bool _finalHideStarted = false;

		/// <summary>
		/// On Start, creates or sets the health bar up
		/// </summary>
		protected virtual void Awake()
		{
			Initialization();
		}

		/// <summary>
		/// On enable, initializes the bar again
		/// </summary>
		protected void OnEnable()
		{
			_finalHideStarted = false;

			SetInitialActiveState();
		}

		/// <summary>
		/// Forces the bar into its initial active state (hiding it if AlwaysVisible is false)
		/// </summary>
		public virtual void SetInitialActiveState()
		{
			if (!AlwaysVisible && (_progressBar != null))
			{
				ShowBar(false);
			}
		}

		/// <summary>
		/// Shows or hides the bar by changing its object's active state
		/// </summary>
		/// <param name="state"></param>
		public virtual void ShowBar(bool state)
		{
			_progressBar.gameObject.SetActive(state);
		}

		/// <summary>
		/// Whether or not the bar is currently active
		/// </summary>
		/// <returns></returns>
		public virtual bool BarIsShown()
		{
			return _progressBar.gameObject.activeInHierarchy;
		}

		/// <summary>
		/// Initializes the bar (handles visibility, parenting, initial value
		/// </summary>
		public virtual void Initialization()
		{
			_finalHideStarted = false;

			if (_progressBar != null)
			{
				ShowBar(AlwaysVisible);
				return;
			}

			switch (HealthBarType)
			{
				case HealthBarTypes.Prefab:
					if (HealthBarPrefab == null)
					{
						Debug.LogWarning(this.name + " : the HealthBar has no prefab associated to it, nothing will be displayed.");
						return;
					}
					_progressBar = Instantiate(HealthBarPrefab, transform.position + HealthBarOffset, transform.rotation) as MMProgressBar;
					SceneManager.MoveGameObjectToScene(_progressBar.gameObject, this.gameObject.scene);
					_progressBar.transform.SetParent(this.transform);
					_progressBar.gameObject.name = "HealthBar";
					break;
				case HealthBarTypes.Drawn:
					DrawHealthBar();
					UpdateDrawnColors();
					break;
				case HealthBarTypes.Existing:
					_progressBar = TargetProgressBar;
					break;
			}

			if (!AlwaysVisible)
			{
				ShowBar(false);
			}

			if (_progressBar != null)
			{
				_progressBar.SetBar(100f, 0f, 100f);
			}
		}
		

		/// <summary>
		/// Draws the health bar.
		/// </summary>
		protected virtual void DrawHealthBar()
		{
			GameObject newGameObject = new GameObject();
			SceneManager.MoveGameObjectToScene(newGameObject, this.gameObject.scene);
			newGameObject.name = "HealthBar|"+this.gameObject.name;

			if (NestDrawnHealthBar)
			{
				newGameObject.transform.SetParent(this.transform);
			}

			_progressBar = newGameObject.AddComponent<MMProgressBar>();

			_followTransform = newGameObject.AddComponent<MMFollowTarget>();
			_followTransform.Offset = HealthBarOffset;
			_followTransform.Target = this.transform;
			_followTransform.FollowRotation = FollowRotation;
			_followTransform.FollowScale = FollowScale; 
			_followTransform.InterpolatePosition = false;
			_followTransform.InterpolateRotation = false;
			_followTransform.UpdateMode = FollowTargetMode;

			Canvas newCanvas = newGameObject.AddComponent<Canvas>();
			newCanvas.renderMode = RenderMode.WorldSpace;
			newCanvas.transform.localScale = Vector3.one;
			newCanvas.GetComponent<RectTransform>().sizeDelta = Size;
			if (!string.IsNullOrEmpty(SortingLayerName))
			{
				newCanvas.sortingLayerName = SortingLayerName;
			}

			GameObject container = new GameObject();
			container.transform.SetParent(newGameObject.transform);
			container.name = "MMProgressBarContainer";
			container.transform.localScale = Vector3.one;
            
			GameObject borderImageGameObject = new GameObject();
			borderImageGameObject.transform.SetParent(container.transform);
			borderImageGameObject.name = "HealthBar Border";
			_borderImage = borderImageGameObject.AddComponent<Image>();
			_borderImage.transform.position = Vector3.zero;
			_borderImage.transform.localScale = Vector3.one;
			_borderImage.GetComponent<RectTransform>().sizeDelta = Size;
			_borderImage.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

			GameObject bgImageGameObject = new GameObject();
			bgImageGameObject.transform.SetParent(container.transform);
			bgImageGameObject.name = "HealthBar Background";
			_backgroundImage = bgImageGameObject.AddComponent<Image>();
			_backgroundImage.transform.position = Vector3.zero;
			_backgroundImage.transform.localScale = Vector3.one;
			_backgroundImage.GetComponent<RectTransform>().sizeDelta = Size - BackgroundPadding*2;
			_backgroundImage.GetComponent<RectTransform>().anchoredPosition = -_backgroundImage.GetComponent<RectTransform>().sizeDelta/2;
			_backgroundImage.GetComponent<RectTransform>().pivot = Vector2.zero;

			GameObject delayedImageGameObject = new GameObject();
			delayedImageGameObject.transform.SetParent(container.transform);
			delayedImageGameObject.name = "HealthBar Delayed Foreground";
			_delayedImage = delayedImageGameObject.AddComponent<Image>();
			_delayedImage.transform.position = Vector3.zero;
			_delayedImage.transform.localScale = Vector3.one;
			_delayedImage.GetComponent<RectTransform>().sizeDelta = Size - BackgroundPadding*2;
			_delayedImage.GetComponent<RectTransform>().anchoredPosition = -_delayedImage.GetComponent<RectTransform>().sizeDelta/2;
			_delayedImage.GetComponent<RectTransform>().pivot = Vector2.zero;

			GameObject frontImageGameObject = new GameObject();
			frontImageGameObject.transform.SetParent(container.transform);
			frontImageGameObject.name = "HealthBar Foreground";
			_foregroundImage = frontImageGameObject.AddComponent<Image>();
			_foregroundImage.transform.position = Vector3.zero;
			_foregroundImage.transform.localScale = Vector3.one;
			_foregroundImage.color = ForegroundColor.Evaluate(1);
			_foregroundImage.GetComponent<RectTransform>().sizeDelta = Size - BackgroundPadding*2;
			_foregroundImage.GetComponent<RectTransform>().anchoredPosition = -_foregroundImage.GetComponent<RectTransform>().sizeDelta/2;
			_foregroundImage.GetComponent<RectTransform>().pivot = Vector2.zero;

			if (Billboard)
			{
				MMBillboard billboard = _progressBar.gameObject.AddComponent<MMBillboard>();
				billboard.NestObject = !NestDrawnHealthBar;
			}

			_progressBar.LerpDecreasingDelayedBar = LerpDelayedBar;
			_progressBar.LerpForegroundBar = LerpFrontBar;
			_progressBar.LerpDecreasingDelayedBarSpeed = LerpDelayedBarSpeed;
			_progressBar.LerpForegroundBarSpeedIncreasing = LerpFrontBarSpeed;
			_progressBar.ForegroundBar = _foregroundImage.transform;
			_progressBar.DelayedBarDecreasing = _delayedImage.transform;
			_progressBar.DecreasingDelay = Delay;
			_progressBar.BumpScaleOnChange = BumpScaleOnChange;
			_progressBar.BumpDuration = BumpDuration;
			_progressBar.BumpScaleAnimationCurve = BumpAnimationCurve;
			_progressBar.TimeScale = (TimeScale == TimeScales.Time) ? MMProgressBar.TimeScales.Time : MMProgressBar.TimeScales.UnscaledTime;
			container.transform.localEulerAngles = InitialRotationAngles;
			_progressBar.Initialization();
		}

		/// <summary>
		/// On Update, we hide or show our healthbar based on our current status
		/// </summary>
		protected virtual void Update()
		{
			if (_progressBar == null) 
			{
				return; 
			}

			if (_finalHideStarted)
			{
				return;
			}

			UpdateDrawnColors();
            
			if (AlwaysVisible)	
			{ 
				return; 
			}

			if (_showBar)
			{
				ShowBar(true);
				float currentTime = (TimeScale == TimeScales.UnscaledTime) ? Time.unscaledTime : Time.time;
				if (currentTime - _lastShowTimestamp > DisplayDurationOnHit)
				{
					_showBar = false;
				}
			}
			else
			{
				if (BarIsShown())
				{
					ShowBar(false);	
				}
			}
		}

		/// <summary>
		/// Hides the bar when it reaches zero
		/// </summary>
		/// <returns>The hide bar.</returns>
		protected virtual IEnumerator FinalHideBar()
		{
			_finalHideStarted = true;
			if (InstantiatedOnDeath != null)
			{
				GameObject instantiatedOnDeath = Instantiate(InstantiatedOnDeath, this.transform.position + HealthBarOffset, this.transform.rotation);
				SceneManager.MoveGameObjectToScene(instantiatedOnDeath.gameObject, this.gameObject.scene);
			}
			if (HideBarAtZeroDelay == 0)
			{
				_showBar = false;
				ShowBar(false);
				yield return null;
			}
			else
			{
				_progressBar.HideBar(HideBarAtZeroDelay);
			}            
		}

		/// <summary>
		/// Updates the colors of the different bars
		/// </summary>
		protected virtual void UpdateDrawnColors()
		{
			if (HealthBarType != HealthBarTypes.Drawn)
			{
				return;
			}

			if (_progressBar.Bumping)
			{
				return;
			}

			if (_borderImage != null)
			{
				_borderImage.color = BorderColor.Evaluate(_progressBar.BarProgress);
			}

			if (_backgroundImage != null)
			{
				_backgroundImage.color = BackgroundColor.Evaluate(_progressBar.BarProgress);
			}

			if (_delayedImage != null)
			{
				_delayedImage.color = DelayedColor.Evaluate(_progressBar.BarProgress);
			}

			if (_foregroundImage != null)
			{
				_foregroundImage.color = ForegroundColor.Evaluate(_progressBar.BarProgress);
			}
		}

		/// <summary>
		/// Updates the bar
		/// </summary>
		/// <param name="currentHealth">Current health.</param>
		/// <param name="minHealth">Minimum health.</param>
		/// <param name="maxHealth">Max health.</param>
		/// <param name="show">Whether or not we should show the bar.</param>
		public virtual void UpdateBar(float currentHealth, float minHealth, float maxHealth, bool show)
		{
			// if the healthbar isn't supposed to be always displayed, we turn it on for the specified duration
			if (!AlwaysVisible && show)
			{
				_showBar = true;
				_lastShowTimestamp = (TimeScale == TimeScales.UnscaledTime) ? Time.unscaledTime : Time.time;
			}

			if (_progressBar != null)
			{
				_progressBar.UpdateBar(currentHealth, minHealth, maxHealth)	;
                
				if (HideBarAtZero && _progressBar.BarTarget <= 0)
				{
					StartCoroutine(FinalHideBar());
				}

				if (BumpScaleOnChange)
				{
					_progressBar.Bump();
				}
			}
		}

		/// <summary>
		/// A test method used to update the bar when pressing the TestUpdateHealth button in the inspector
		/// </summary>
		protected virtual void TestUpdateHealth()
		{
			UpdateBar(TestCurrentHealth, TestMinHealth, TestMaxHealth, true);
		}

		#endif
	}
}
