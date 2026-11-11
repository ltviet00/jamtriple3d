using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Control user coin </summary>
[CreateAssetMenu(fileName = "UserStarInventory", menuName = "Game/User/UserStarInventory")]
public class UserStarInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserStar-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "bsftasgjgb67345.data";
	[SerializeField] string KEY = "avsh547thsr";

	[Header("ShareVar")]
	[SerializeField] IntegerVariable userStarVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	private void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	private void OnEnable() {
		if(isFailedConfig)
			return;
		LoadFromDisk();
	}

	private void OnDisable() {
		if(isFailedConfig)
			return;
		SaveToDisk();
	}

	private void Awake() {
		if(isFailedConfig)
			return;
		LoadFromDisk();
	}
	#endregion// Init config


	#region Public
	public int GetUserStars() {
		if(isFailedConfig)
			return -1;
		return _data.stars;
	}

	public void IncreaseStars(int amount) {
		if(isFailedConfig)
			return;
		_data.stars += amount;
		SaveToDisk();
		SaveToShareVar();
	}

	public void ResetStars() {
		if(isFailedConfig)
			return;
		_data.stars = 0;
		SaveToDisk();
		SaveToShareVar();
	}

	public bool IsEnough(int amount) {
		if(isFailedConfig)
			return false;
		return _data.stars >= amount;
	}

	public void LoadFromDisk() {
		// already loaded, then do nothing
		//if(_data != null)
		//	return;
		// First time load game ? then create the default data
		if(!JsonFileWriter.Exist(SavePath)) {
			CreateDefaultData();
		}
		//ok. load. Cannot load ? then create default file
		else {
			//var success = BinaryFileWriter.Load<SaveForm>(SavePath, KEY, out _data, true);
			//if(!success) {
			//	Log.Warning(false, "Cannot read from previous save game, Reset to default");
			//	CreateDefaultData();
			//}
			_data = JsonFileWriter.Load<SaveForm>(SavePath, KEY);
			Log.Info($"Load [{SavePath} successful]");
		}
		// save to shareVar as well
		SaveToShareVar();
	}

	/// <summary> Save the current userData to file on LocalDisk </summary>
	public void SaveToDisk() {
		JsonFileWriter.Save<SaveForm>(_data, SavePath, KEY);
	}
	#endregion//Public


	#region Private
	[Header("Private")]
	[SerializeField] SaveForm _data;

	string _cachedSavePath = null;
	string SavePath {
		get {
			if(string.IsNullOrEmpty(_cachedSavePath)) {
#if UNITY_EDITOR
				_cachedSavePath = Application.dataPath + "/UserData/" + SAVE_NAME_EDTOR;
#else
				_cachedSavePath = Application.persistentDataPath + "/" + SAVE_NAME;
#endif
			}
			return _cachedSavePath;
		}
	}

	//void DeleteOldExistFile() {
	//	BinaryFileWriter.DeleteFile(SavePath);
	//}

	void CreateDefaultData() {
		_data = new SaveForm();
	}

	void SaveToShareVar() {
		userStarVar.Value = _data.stars;
	}
	#endregion//Private


	#region Interface
	public void ResetDataToDefault() {
		CreateDefaultData();
	}
	#endregion//Interface


	#region Classes
	[System.Serializable]
	public class SaveForm {
		public int stars;
	}
	#endregion//Class
}
