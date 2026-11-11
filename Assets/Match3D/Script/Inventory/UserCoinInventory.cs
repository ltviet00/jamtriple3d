using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Control user coin </summary>
[CreateAssetMenu(fileName = "UserCoinInventory", menuName = "Game/User/UserCoinInventory")]
public class UserCoinInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserData-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "bmaonyebngasdi.data";
	[SerializeField] string KEY = "7sdfgg245624";

	[Header("ShareVar")]
	[SerializeField] IntegerVariable userCoinVar;

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
	public int GetUserCoin() {
		if(isFailedConfig)
			return -1;
		return _data.coin;
	}

	///<summary> Ignore current coin, force new value </summary>
	public void SetUserCoin(int amount) {
		if(isFailedConfig)
			return;
		_data.coin = 0;
		IncreaseCoin(amount);
	}

	public void IncreaseCoin(IntegerVariable amountVar) {
		if(isFailedConfig)
			return;
		IncreaseCoin(amountVar.Value);
	}

	public void DecreaseCoin(IntegerVariable amountVar) {
		if(isFailedConfig)
			return;
		DecreaseCoin(amountVar.Value);
	}

	public void IncreaseCoin(int amount) {
		if(isFailedConfig)
			return;
		_data.coin += amount;
		SaveToDisk();
		SaveToShareVar();
	}

	public void DecreaseCoin(int amount) {
		if(isFailedConfig)
			return;
		if(!IsEnough(amount)) {
			Log.Error(false, this, "Decrease larger than remain coin");
			return;
		}
		_data.coin -= amount;
		SaveToDisk();
		SaveToShareVar();
	}

	public bool IsEnough(int amount) {
		if(isFailedConfig)
			return false;
		return _data.coin >= amount;
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
		userCoinVar.Value = _data.coin;
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
		public int coin;
	}
	#endregion//Class
}
