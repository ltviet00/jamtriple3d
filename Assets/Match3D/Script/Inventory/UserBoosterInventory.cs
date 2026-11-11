using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

///<summary> Control booster amount </summary>
[CreateAssetMenu(fileName = "UserBoosterInventory", menuName = "Game/User/UserBoosterInventory")]
public class UserBoosterInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "Booster-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "bmaonyebngasdi.data";
	[SerializeField] string KEY = "7sdfgg245624";

	[Header("All boosterID")]
	public BoosterID[] allBoosterID;

	[Header("ShareVar")]
	[SerializeField] SyncToShareVarForm[] shareVarList;

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
	public bool IsEnoughBooster(BoosterID id, int require) {
		if(isFailedConfig)
			return false;
		if(id == null) {
			Log.Error(false, this, $"IsEnoughBooster Null booster id");
			return false;
		}
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		// carefully check
		if(!_dataDiction.ContainsKey(id)) {
			Log.Error(false, this, $"Not contain booster key [{id.name}], forgot to declare in AllBoosterID?");
			return false;
		}
		return _dataDiction[id] >= require;
	}

	public void AddBooster(BoosterID id, int amount) {
		if(isFailedConfig)
			return;
		if(id == null) {
			Log.Error(false, this, $"AddBooster Null booster id");
			return;
		}
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		// carefully check
		if(!_dataDiction.ContainsKey(id)) {
			Log.Error(false, this, $"Not contain booster key [{id.name}], forgot to declare in AllBoosterID?");
			return;
		}
		_dataDiction[id] += amount;
		SyncDictionaryToShareVar();
		SaveToDisk();
	}

	public void DeductBooster(BoosterID id, int amount = 1) {
		if(isFailedConfig)
			return;
		if(id == null) {
			Log.Error(false, this, $"DeductBooster Null booster id");
			return;
		}
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		_dataDiction[id] -= amount;
		SyncDictionaryToShareVar();
		SaveToDisk();
	}

	public int GetBoosterRemain(BoosterID id) {
		if(isFailedConfig)
			return 0;
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		return _dataDiction[id];
	}

	public void AddRandomBoosterAmount(int amount) {
		if(isFailedConfig)
			return;
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		// simple random booster
		for(int n = 0; n < amount; n++) {
			var randomIndex = Random.Range(0, allBoosterID.Length);
			var boosterID = allBoosterID[randomIndex];
			// we manuall add here, will manual save at last, instead of call AdBooster, it will call save many time
			_dataDiction[boosterID]++;
		}
		SyncDictionaryToShareVar();
		SaveToDisk();
	}
	#endregion//Public


	#region Public - Load, save
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
			//SaveForm defaultData;
			//var success = JsonFileWriter.Load<SaveForm>(SavePath, KEY, out defaultData, true);
			//if(!success) {
			//	Log.Warning(false, "Cannot read from previous save game, Reset to default");
			//	defaultData = CreateDefaultData();
			//}
			var defaultData = JsonFileWriter.Load<SaveForm>(SavePath, KEY);
			FromSaveDataToDictionary(defaultData, _dataDiction);
			SyncDictionaryToShareVar();
			Log.Info($"Load [{SavePath} successful]");
		}
	}

	/// <summary> Save the current userData to file on LocalDisk </summary>
	public void SaveToDisk() {
		var writeToDisk = FromDictionaryToSaveData(_dataDiction);
		JsonFileWriter.Save<SaveForm>(writeToDisk, SavePath, KEY);
	}
	#endregion//Public - Load, save


	#region Private - convert between save & runtime
	void FromSaveDataToDictionary(SaveForm saveData, Dictionary<BoosterID, int> dictionary) {
		dictionary.Clear();
		for(int n = 0; n < saveData.data.Count; n++) {
			var item = saveData.data[n];
			var foundBooster = FindBoosterIdWithName(item.name);
			if(foundBooster == null) {
				continue;
			}
			dictionary.Add(foundBooster, item.count);
		}
	}

	SaveForm FromDictionaryToSaveData(Dictionary<BoosterID, int> dictionary) {
		var result = new SaveForm();
		foreach(var pair in dictionary) {
			var toSave = new BoosterSaveInfo();
			toSave.name = pair.Key.name;
			toSave.count = pair.Value;
			result.data.Add(toSave);
		}
		return result;
	}

	BoosterID FindBoosterIdWithName(string name) {
		for(int n = 0; n < allBoosterID.Length; n++) {
			var boosterID = allBoosterID[n];
			if(boosterID.name == name)
				return boosterID;
		}
		Log.Warning(false, this, $"Not found booster with name [{name}]");
		return null;
	}

	void CreateDefaultAllBoosterKeyIfNotExistInDictionary() {
		for(int n = 0; n < allBoosterID.Length; n++) {
			var boosterID = allBoosterID[n];
			if(!_dataDiction.ContainsKey(boosterID)) {
				_dataDiction.Add(boosterID, 0);
			}
		}
	}

	void SyncDictionaryToShareVar() {
		for(int n = 0; n < shareVarList.Length; n++) {
			// reset them first
			var item = shareVarList[n];
			item.variable.Value = 0;
			// then check sync with save data, in case saveData empty, then shareVar must zero
			if(_dataDiction.ContainsKey(item.boosterID)) {
				var boosterCount = _dataDiction[item.boosterID];
				item.variable.Value = boosterCount;
			}
		}
	}
	#endregion//Private - convert between save & runtime


	#region Private - Load,save
	///<summary> Convert the data (in list) to dictionary </summary>
	Dictionary<BoosterID, int> _dataDiction = new Dictionary<BoosterID, int>();

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

	void CreateDefaultData() {
		_dataDiction.Clear();
	}
	#endregion//Private - Load save


	#region Cheat
	[Button]
	[Conditional("CHEAT")]
	void ResetAllBooster() {
		CreateDataIfNotExist();
		// foreach and modify diction inside foreach loop will cause error
		// https://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute
		//foreach(var item in _dataDiction) {
		//	_dataDiction[item.Key] = 0;
		//}
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		for(int n = 0; n < allBoosterID.Length; n++) {
			_dataDiction[allBoosterID[n]] = 0;
		}
		SyncDictionaryToShareVar();
		SaveToDisk();
	}

	[Button]
	[Conditional("CHEAT")]
	void Add10BoosterEachType() {
		CreateDataIfNotExist();
		CreateDefaultAllBoosterKeyIfNotExistInDictionary();
		for(int n = 0; n < allBoosterID.Length; n++) {
			_dataDiction[allBoosterID[n]] += 10;
		}
		SyncDictionaryToShareVar();
		SaveToDisk();
	}

	void CreateDataIfNotExist() {
		if(_dataDiction == null) {
			LoadFromDisk();
		}
	}
	#endregion//Cheat


	#region Interface
	public void ResetDataToDefault() {
		CreateDefaultData();
	}
	#endregion//Interface


	#region Classes
	[System.Serializable]
	public class SaveForm {
		public List<BoosterSaveInfo> data = new List<BoosterSaveInfo>();
	}

	[System.Serializable]
	public class BoosterSaveInfo {
		public string name;
		public int count;
	}

	[System.Serializable]
	struct SyncToShareVarForm {
		public BoosterID boosterID;
		public IntegerVariable variable;
	}
	#endregion//Class
}
