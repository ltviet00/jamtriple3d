using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

///<summary> Control user coin </summary>
[CreateAssetMenu(fileName = "UserLevelScoreInventory", menuName = "Game/User/UserLevelScoreInventory")]
public class UserLevelScoreInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserData-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "bmaonyebngasdi.data";
	[SerializeField] string KEY = "7sdfgg245624";

	[Header("ShareData")]
	[SerializeField] IntegerVariable reachLevelVar;

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


	#region Public - for UI
	public void In_IncreaseCurrentLevel() {
		IncreaseCurrentLevel();
	}
	#endregion//Public - for UI


	#region Public - Get info
	public int GetCurrentReachLevel() {
		if(isFailedConfig)
			return 0;
		return Data.reachedLevel;
	}

	public void IncreaseCurrentLevel() {
		if(isFailedConfig)
			return;
		Data.reachedLevel++;
		SaveToDisk();
	}
	#endregion//Public - Get info


	#region Public - LoadSave
	///<summary> Use this at main method to get data, already check null to load from disk, or create default data </summary>
	public SaveForm Data {
		get {
			if(_data == null) {
				LoadFromDisk();
			}
			return _data;
		}
	}

	public void LoadFromDisk() {
		// First time load game ? then create the default data
		if(!JsonFileWriter.Exist(SavePath)) {
			CreateDefaultData();
		}
		//ok. load. Cannot load ? then create default file
		else {
			_data = JsonFileWriter.Load<SaveForm>(SavePath, KEY);
			Log.Info($"Load [{SavePath} successful]");
		}
		// save to shareVar
		SaveInfoToShareVars();
	}

	/// <summary> Save the current userData to file on LocalDisk </summary>
	public void SaveToDisk() {
		JsonFileWriter.Save<SaveForm>(_data, SavePath, KEY);
		SaveInfoToShareVars();
	}
	#endregion// Public - LoadSave


	#region Private - ShareVar
	void SaveInfoToShareVars() {
		reachLevelVar.Value = _data.reachedLevel;
	}
	#endregion//Private - ShareVar


	#region Private - Load,save
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

	///<summary> Create default save file, add first level score </summary>
	void CreateDefaultData() {
		_data = new SaveForm();
	}
	#endregion//Private - Load,save

	#region Interface
	public void ResetDataToDefault() {
		CreateDefaultData();
	}
	#endregion//Interface


	#region Cheat
	[Conditional("CHEAT")]
	public void CheatSkipLevel(int amount) {
		if(isFailedConfig)
			return;
		_data.reachedLevel += amount;
		SaveToDisk();
		SaveInfoToShareVars();
	}

	[Conditional("CHEAT")]
	public void CheatResetLevel() {
		if(isFailedConfig)
			return;
		_data.reachedLevel = 0;
		SaveToDisk();
		SaveInfoToShareVars();
	}
	#endregion//Cheat


	#region Classes
	[System.Serializable]
	public class SaveForm {
		///<summary> Start from 0. Mind that, so when you show on UI, remember to +1 </summary>
		public int reachedLevel;

		public SaveForm() {
			// first score, zero at level 0
			reachedLevel = 0;
		}
	}
	#endregion//Class
}
