using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Baseclass for all UserData SO, the T is the SaveForm
/// </summary>
public abstract class BaseUserDataSO<T> : ScriptableObject, IUserInventory where T : class, new() {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string saveNameEditor = "YourEditorNameOnly.data";
	[SerializeField] string saveNameDevice = "FileNameOnDevice.data";
	[SerializeField] string encriptKey = "YourKey";

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		LoadFromDiskOrCreateDefault();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SaveToDisk();
	}

	void Awake() {
		if(isFailedConfig)
			return;
		LoadFromDiskOrCreateDefault();
	}
	#endregion// Init config


	#region IUserInventory
	public void ResetDataToDefault() {
		CreateDefaultData();
		SaveToDisk();
	}
	#endregion//PubIUserInventorylic


	#region UI callbacks
	///<summary> Use for call from UI or other UnityEvent </summary>
	public void In_LoadFromDisk() {
		if(isFailedConfig)
			return;
		LoadFromDiskOrCreateDefault();
	}

	///<summary> Use for call from UI or other UnityEvent </summary>
	public void In_SaveToDisk() {
		if(isFailedConfig)
			return;
		SaveToDisk();
	}
	#endregion//UI callbacks


	#region Virtual
	///<summary> Override this when we want to custom the default data (more than the New(T) function already define at inherit classes) </summary>
	protected virtual void CreateDefaultData() {
		// NOTES: must call new T(), not default(T) cause it will return null if the class T not define thew construction() function.
		// cause null pointer on device!!!
		_saveData = new T();
	}
	#endregion//Virtual


	#region Private
	[Header("Private")]
	[SerializeField] protected T _saveData;

	///<summary> Use this at main method to get data, already check null to load from disk, or create default data </summary>
	public T Data {
		get {
			if(_saveData == null) {
				LoadFromDiskOrCreateDefault();
			}
			return _saveData;
		}
	}

	protected void LoadFromDiskOrCreateDefault() {
		// First time load game ? then create the default data
		if(!JsonFileWriter.Exist(SavePath)) {
			CreateDefaultData();
			Log.Info($"Create default [{typeof(T)}]");
		}
		//ok. load. Cannot load ? then create default file
		else {
			_saveData = JsonFileWriter.Load<T>(SavePath, encriptKey);
			Log.Info($"Load [{SavePath} successful]");
		}
	}

	/// <summary> Save the current userData to file on LocalDisk </summary>
	protected void SaveToDisk() {
		JsonFileWriter.Save<T>(_saveData, SavePath, encriptKey);
	}

	///<summary> In unity, this var get serialized, so the cachePath never null and not get update (default value = "YourEditorNameOnly.data"), cause error when load file </summary>
	[System.NonSerialized] string _cachedSavePath = null;
	string SavePath {
		get {
			if(string.IsNullOrEmpty(_cachedSavePath)) {
#if UNITY_EDITOR
				_cachedSavePath = Application.dataPath + "/UserData/" + saveNameEditor;
#else
				_cachedSavePath = Application.persistentDataPath + "/" + saveNameDevice;
#endif
			}
			return _cachedSavePath;
		}
	}

	//void DeleteOldExistFile() {
	//	BinaryFileWriter.DeleteFile(SavePath);
	//}
	#endregion//Private
}
