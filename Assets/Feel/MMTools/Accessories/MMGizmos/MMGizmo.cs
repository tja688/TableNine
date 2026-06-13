using UnityEngine;

namespace MoreMountains.Tools
{
	/// <summary>
	/// Add this component to an object and it'll let you display a gizmo for its position or collider, and an optional text
	/// </summary>
	public class MMGizmo : MonoBehaviour 
	{
		/// <summary>
		/// the possible types of gizmos to display
		/// </summary>
		public enum GizmoTypes { None, Collider, Position }
		/// <summary>
		/// whether to display gizmos always or only when the object is selected
		/// </summary>
		public enum DisplayModes { Always, OnlyWhenSelected }

		/// <summary>
		/// the shape of the gizmo to display the position of the object
		/// </summary>
		public enum PositionModes
		{
			Point, Cube, WireCube, Sphere, WireSphere, Texture, Arrows, RightArrow, UpArrow, ForwardArrow,
			Lines, RightLine, UpLine, ForwardLine
		}
		/// <summary>
		/// what to display as text for that gizmo
		/// </summary>
		public enum TextModes { GameObjectName, CustomText, Position, Rotation, Scale, Property }
		/// <summary>
		/// when displaying a collider, whether to display a full or wire gizmo
		/// </summary>
		public enum ColliderRenderTypes { Full, Wire }

		[Header("Modes")] 
		/// if this is true, gizmos will be displayed, if this is false, gizmos won't be displayed
		[Tooltip("是否显示 Gizmo；关闭后本组件不会绘制任何 Gizmo")]
		public bool DisplayGizmo = true; 
		/// what the gizmos should represent. Collider will show the bounds of the associated collider, Position will show the position of the object 
		[Tooltip("Gizmo 表达的目标。Collider 显示关联碰撞体范围；Position 显示对象位置。两种模式下只会使用各自相关参数")]
		public GizmoTypes GizmoType = GizmoTypes.Position; 
		/// whether gizmos should always be displayed, or only when selected
		[Tooltip("Gizmo 始终显示，或仅在对象被选中时显示")]
		public DisplayModes DisplayMode = DisplayModes.Always;
		
		[Header("Settings")] 
		/// the color of the collider or position gizmo 
		[Tooltip("碰撞体或位置 小发明 的颜色")]
		public Color GizmoColor = MMColors.ReunoYellow; 
		/// the shape of the gizmo when in position mode
		[Tooltip("位置模式下 小发明 的形状")]
		[MMEnumCondition("GizmoType", (int)GizmoTypes.Position)]
		public PositionModes PositionMode = PositionModes.Point; 
		/// the texture to display as a gizmo when in position & texture mode
		[Tooltip("位置模式 为质地时要显示的纹理")]
		[MMEnumCondition("PositionMode", (int)PositionModes.Texture)]
		public Texture PositionTexture; 
		/// the size of the texture to display as a gizmo
		[Tooltip("位置模式 为质地纹理时小发明的尺寸")]
		[MMEnumCondition("PositionMode", (int)PositionModes.Texture)]
		public Vector2 TextureSize = new Vector2(50f,50f); 
		/// the size of the gizmo when in position mode
		[Tooltip("位置模式下 小发明 的尺寸")]
		[MMEnumCondition("GizmoType", (int)GizmoTypes.Position)]
		public float PositionSize = 0.2f; 
		/// whether to display the collider gizmo as a wire or a full mesh
		[Tooltip("Collider 模式下以线框（Wire）还是实体（Full）绘制")]
		[MMEnumCondition("GizmoType", (int)GizmoTypes.Collider)]
		public ColliderRenderTypes ColliderRenderType = ColliderRenderTypes.Full;
		/// the distance from the scene view camera beyond which the gizmo won't be displayed
		[Tooltip("与场景视图相机距离超过该值时不再绘制 Gizmo")]
		public float ViewDistance = 20f; 
		
		[Header("Offsets")]
		/// an offset to apply when drawing a collider or position gizmo
		[Tooltip("绘制 Collider/Position Gizmo 时应用的位置偏移")]
		public Vector3 GizmoOffset = Vector3.zero;

		/// whether or not to lock the position of the gizmo on the x axis, regardless of the position of the object
		[Tooltip("是否锁定 Gizmo 的 X 坐标；开启后将忽略对象在 X 轴上的实际位置")]
		public bool LockX = false;
		/// the position at which to put the gizmo when locked on the x axis
		[Tooltip("锁X 开启时 小发明 的 X 坐标")]
		[MMCondition("LockX", true)]
		public float LockedX = 0f;
		
		/// whether or not to lock the position of the gizmo on the y axis, regardless of the position of the object
		[Tooltip("是否锁定 Gizmo 的 Y 坐标；开启后将忽略对象在 Y 轴上的实际位置")]
		public bool LockY = false;
		/// the position at which to put the gizmo when locked on the y axis
		[Tooltip("锁Y 开启时 小发明 的 Y 坐标")]
		[MMCondition("LockY", true)]
		public float LockedY = 0f;
		
		/// whether or not to lock the position of the gizmo on the z axis, regardless of the position of the object
		[Tooltip("是否锁定 Gizmo 的 Z 坐标；开启后将忽略对象在 Z 轴上的实际位置")]
		public bool LockZ = false;
		/// the position at which to put the gizmo when locked on the z axis
		[Tooltip("锁Z 开启时 小发明 的 Z 坐标")]
		[MMCondition("LockZ", true)]
		public float LockedZ = 0f;

		[Header("Text")]  
		/// whether or not to display text on that gizmo
		[Tooltip("是否在 Gizmo 旁显示文字信息；关闭后下面所有文字相关设置都不生效")]
		public bool DisplayText = false; 
		/// what to display as text for that gizmo (some custom text, the object's name, position, rotation, scale, or a target property)
		[Tooltip("文字显示内容来源：自定义文本、对象名称、位置、旋转、缩放或目标属性值")]
		[MMCondition("DisplayText", true)]
		public TextModes TextMode; 
		/// when in CustomText mode, the text to display on that gizmo
		[Tooltip("文本模式 为 自定义文本 时要显示的文本")]
		[MMEnumCondition("TextMode", (int)TextModes.CustomText)]
		public string TextToDisplay = "Some Text"; 
		/// the offset to apply to the text
		[Tooltip("文字显示位置的偏移量")]
		[MMCondition("DisplayText", true)]
		public Vector3 TextOffset = new Vector3(0f, 0.5f, 0f);
		/// what style to use for the text's font
		[Tooltip("文字字体样式")]
		[MMCondition("DisplayText", true)]
		public FontStyle TextFontStyle = FontStyle.Normal; 
		/// the size of the text's font
		[Tooltip("文字字号")]
		[MMCondition("DisplayText", true)]
		public int TextSize = 12; 
		/// the color in which to display the gizmo's text
		[Tooltip("文字颜色")]
		[MMCondition("DisplayText", true)]
		public Color TextColor = MMColors.ReunoYellow; 
		/// the color of the background behind the text
		[Tooltip("文字背景颜色")]
		[MMCondition("DisplayText", true)]
		public Color TextBackgroundColor = new Color(0,0,0,0.3f); 
		/// the padding to apply to the text's background
		[Tooltip("文字背景内边距（左、上、右、下）")]
		[MMCondition("DisplayText", true)]
		public Vector4 TextPadding = new Vector4(5,0,5,0); 
		/// the distance from the scene view camera beyond which the gizmo text won't be displayed
		[Tooltip("与场景视图相机距离超过该值时不再显示文字")]
		[MMCondition("DisplayText", true)]
		public float TextMaxDistance = 14f;
		/// when in Property mode, the property whose value to display on the gizmo
		[Tooltip("TextMode 为 Property 时，要读取并显示数值的目标属性")]
		public MMPropertyPicker TargetProperty;
		
		public virtual bool Initialized { get; set; }
		public virtual SphereCollider _sphereCollider { get; set; }
		public virtual BoxCollider _boxCollider { get; set; }
		public virtual MeshCollider _meshCollider { get; set; }
		#if MM_PHYSICS2D
		public virtual CircleCollider2D _circleCollider2D { get; set; }
		public virtual BoxCollider2D _boxCollider2D { get; set; }
		#endif
		public virtual Vector3 _vector3Zero { get; set; }
		public virtual Vector3 _newPosition { get; set; }
		public virtual Vector2 _worldToGUIPosition { get; set; }
		public virtual Rect _textureRect { get; set; }
		public virtual GUIStyle _textGUIStyle { get; set; }
		public virtual string _textToDisplay { get; set; }
		public virtual bool _sphereColliderNotNull { get; set; }
		public virtual bool _boxColliderNotNull { get; set; }
		public virtual bool _meshColliderNotNull { get; set; }
		public virtual bool _circleCollider2DNotNull { get; set; }
		public virtual bool _boxCollider2DNotNull { get; set; }
		public virtual bool _positionTextureNotNull { get; set; }
		
		#if UNITY_EDITOR
		
		/// <summary>
		/// On awake we initialize our property
		/// </summary>
		protected virtual void Awake()
		{
			TargetProperty.Initialization(this.gameObject);
		}
		
		#else 
		
		/// <summary>
		/// If we're not in editor, we disable ourselves
		/// </summary>
		protected virtual void Awake()
		{
			this.enabled = false;
		}
		
		#endif 
		
		
	}	
}
