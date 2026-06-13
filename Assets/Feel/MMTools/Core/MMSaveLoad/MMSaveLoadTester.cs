using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if MM_UI
using UnityEngine.UI;
#endif

namespace MoreMountains.Tools
{
	/// <summary>
	/// A test object to store data to test the MMSaveLoadManager class
	/// </summary>
	[System.Serializable]
	public class MMSaveLoadTestObject
	{
		public string SavedText;
	}

	/// <summary>
	/// A simple class used in the MMSaveLoadTestScene to test the MMSaveLoadManager class
	/// </summary>
	public class MMSaveLoadTester : MonoBehaviour
	{
		[Header("Bindings")]
		#if MM_UI
		/// the text to save
		[Tooltip("要保存的文本")]
		public InputField TargetInputField;
		#endif

		[Header("Save settings")]
		/// the chosen save method (json, encrypted json, binary, encrypted binary)
		[Tooltip("选择的保存方式（json、加密json、二进制、加密二进制）")]
		public MMSaveLoadManagerMethods SaveLoadMethod = MMSaveLoadManagerMethods.Binary;
		/// the name of the file to save
		[Tooltip("要保存的文件名")]
		public string FileName = "TestObject";
		/// the name of the destination folder
		[Tooltip("目标文件夹名称")]
		public string FolderName = "MMTest/";
		/// the extension to use
		[Tooltip("使用的文件扩展名")]
		public string SaveFileExtension = ".testObject";
		/// the key to use to encrypt the file (if needed)
		[Tooltip("用于文件加密的密钥（启用加密时生效）")]
		public string EncryptionKey = "ThisIsTheKey";

		/// Test button
		[MMInspectorButton("Save")]
		public bool TestSaveButton;
		/// Test button
		[MMInspectorButton("Load")]
		public bool TestLoadButton;
		/// Test button
		[MMInspectorButton("Reset")]
		public bool TestResetButton;

		protected IMMSaveLoadManagerMethod _saveLoadManagerMethod;

		/// <summary>
		/// Saves the contents of the TestObject into a file
		/// </summary>
		public virtual void Save()
		{
			InitializeSaveLoadMethod();
			MMSaveLoadTestObject testObject = new MMSaveLoadTestObject();
			#if MM_UI
			testObject.SavedText = TargetInputField.text;
			#endif
			MMSaveLoadManager.Save(testObject, FileName+SaveFileExtension, FolderName);
		}

		/// <summary>
		/// Loads the saved data
		/// </summary>
		public virtual void Load()
		{
			InitializeSaveLoadMethod();
			MMSaveLoadTestObject testObject = (MMSaveLoadTestObject)MMSaveLoadManager.Load(typeof(MMSaveLoadTestObject), FileName + SaveFileExtension, FolderName);
			#if MM_UI
			TargetInputField.text = testObject.SavedText;
			#endif
		}

		/// <summary>
		/// Resets all saves by deleting the whole folder
		/// </summary>
		protected virtual void Reset()
		{
			MMSaveLoadManager.DeleteSaveFolder(FolderName);
		}

		/// <summary>
		/// Creates a new MMSaveLoadManagerMethod and passes it to the MMSaveLoadManager
		/// </summary>
		protected virtual void InitializeSaveLoadMethod()
		{
			switch(SaveLoadMethod)
			{
				case MMSaveLoadManagerMethods.Binary:
					_saveLoadManagerMethod = new MMSaveLoadManagerMethodBinary();
					break;
				case MMSaveLoadManagerMethods.BinaryEncrypted:
					_saveLoadManagerMethod = new MMSaveLoadManagerMethodBinaryEncrypted();
					(_saveLoadManagerMethod as MMSaveLoadManagerEncrypter).Key = EncryptionKey;
					break;
				case MMSaveLoadManagerMethods.Json:
					_saveLoadManagerMethod = new MMSaveLoadManagerMethodJson();
					break;
				case MMSaveLoadManagerMethods.JsonEncrypted:
					_saveLoadManagerMethod = new MMSaveLoadManagerMethodJsonEncrypted();
					(_saveLoadManagerMethod as MMSaveLoadManagerEncrypter).Key = EncryptionKey;
					break;
			}
			MMSaveLoadManager.SaveLoadMethod = _saveLoadManagerMethod;
		}
	}
}
