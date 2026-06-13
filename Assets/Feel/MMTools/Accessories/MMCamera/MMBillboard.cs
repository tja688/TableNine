using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MoreMountains.Tools
{
	/// <summary>
	/// Add this class to an object (usually a sprite) and it'll face the camera at all times
	/// </summary>
	[AddComponentMenu("More Mountains/Tools/Camera/MM Billboard")]
	public class MMBillboard : MonoBehaviour
	{
		/// the camera we're facing
		public virtual Camera MainCamera { get; set; }
		/// whether or not this object should automatically grab a camera on start
		[Tooltip("是否在 Start 时自动抓取摄像机（通常为 Camera.main）")]
		public bool GrabMainCameraOnStart = true;
		/// whether or not to nest this object below a parent container
		[Tooltip("是否将该对象先挂到一个临时父容器下，再由父容器执行朝向计算")]
		public bool NestObject = true;
		/// the Vector3 to offset the look at direction by
		[Tooltip("用于偏移 LookAt 朝向的 Vector3（会与相机旋转共同作用）")]
		public Vector3 OffsetDirection = Vector3.back;
		/// the Vector3 to consider as "world up"
		[Tooltip("看看使用“世界向上”矢量3")] 
		public Vector3 Up = Vector3.up;

		protected GameObject _parentContainer;
		private Transform _transform;

		/// <summary>
		/// On awake we grab a camera if needed, and nest our object
		/// </summary>
		protected virtual void Awake()
		{
			_transform = transform;

			if (GrabMainCameraOnStart == true)
			{
				GrabMainCamera ();
			}
		}

		private void Start()
		{
			if (NestObject)
			{
				NestThisObject();
			}                
		}

		/// <summary>
		/// Nests this object below a parent container
		/// </summary>
		protected virtual void NestThisObject()
		{
			_parentContainer = new GameObject();
			SceneManager.MoveGameObjectToScene(_parentContainer, this.gameObject.scene);
			_parentContainer.name = "Parent"+transform.gameObject.name;
			_parentContainer.transform.position = transform.position;
			transform.SetParent(_parentContainer.transform);
		}

		/// <summary>
		/// Grabs the main camera.
		/// </summary>
		protected virtual void GrabMainCamera()
		{
			MainCamera = Camera.main;
		}

		/// <summary>
		/// On update, we change our parent container's rotation to face the camera
		/// </summary>
		protected virtual void Update()
		{
			if (NestObject)
			{
				_parentContainer.transform.LookAt(_parentContainer.transform.position + MainCamera.transform.rotation * OffsetDirection, MainCamera.transform.rotation * Up);
			}                
			else
			{
				_transform.LookAt(_transform.position + MainCamera.transform.rotation * OffsetDirection, MainCamera.transform.rotation * Up);
			}
		}
	}
}
