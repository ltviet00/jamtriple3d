using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Diagnostics;

/// <summary>
/// Load/save information related to LuckyDraw (watchAds per day) ...
/// </summary>
[CreateAssetMenu(fileName = "UserLuckyDrawInventory", menuName = "Game/User/UserLuckyDrawInventory")]
public class UserLuckyDrawInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserLuckyDraw-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "madfubeqbyaf.data";
	[SerializeField] string KEY = "157352sffgsd";

	[Header("Config")]
	[SerializeField] IntegerVariable freeDrawPerDayVar;
	[SerializeField] IntegerVariable freeAdsToDrawPerDayVar;

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
	public int FreeDrawPerDay => freeDrawPerDayVar.Value;
	public int FreeAdsToDrawPerDay => freeAdsToDrawPerDayVar.Value;

	public bool IsReachFreeDrawPerDay() {
		if(isFailedConfig)
			return true;
		CheckResetDrawCountIfDayHasPassed();
		return _data.freeDrawUsed >= freeDrawPerDayVar.Value;
	}

	public void IncreaseFreeDrawUsed() {
		if(isFailedConfig)
			return;
		CheckResetDrawCountIfDayHasPassed();
		_data.freeDrawUsed++;
		SaveToDisk();
	}

	public int GetFreeDrawUsedCount() {
		if(isFailedConfig)
			return freeDrawPerDayVar.Value;
		return _data.freeDrawUsed;
	}

	public void IncreaseAdsWatchedCount() {
		if(isFailedConfig)
			return;
		_data.adsWatchedCount++;
		SaveToDisk();
	}

	public bool IsReachFreeAdsToDrawPerDay() {
		if(isFailedConfig)
			return true;
		CheckResetDrawCountIfDayHasPassed();
		return _data.adsWatchedCount >= freeAdsToDrawPerDayVar.Value;
	}

	public int GetAdsWatchedCount() {
		if(isFailedConfig)
			return freeAdsToDrawPerDayVar.Value;
		return _data.adsWatchedCount;
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

	void CreateDefaultData() {
		_data = new SaveForm();
		_data.saveDateBinary = DateTime.Now.ToBinary();
		_data.freeDrawUsed = 0;
		_data.adsWatchedCount = 0;
	}

	void CreateDefautDataIfNull() {
		if(_data == null)
			CreateDefaultData();
	}

	void CheckResetDrawCountIfDayHasPassed() {
		var currentDate = DateTime.Now;
		var dateInSaveData = DateTime.FromBinary(_data.saveDateBinary);
		// different date? then reset drawCount, also save currentDay
		if(currentDate.Day != dateInSaveData.Day) {
			_data.freeDrawUsed = 0;
			_data.adsWatchedCount = 0;
			_data.saveDateBinary = DateTime.Now.ToBinary();
			SaveToDisk();
		}
	}

	//void SaveToShareVar() {
	//	userCoinVar.Value = _data.coin;
	//}
	#endregion//Private

	#region Cheat
	[Conditional("CHEAT")][Button]
	void EditorZeroAdsWatched() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.adsWatchedCount = 0;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void EditorZeroFreeDrawused() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.freeDrawUsed = 0;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void EditorFullAdsWatched() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.adsWatchedCount = freeAdsToDrawPerDayVar.Value;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void EditorFullFreeDrawUsed() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.freeDrawUsed = freeDrawPerDayVar.Value;
		SaveToDisk();
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
		///<summary> The last time we check/modify remainTroop. Use this to compute how long user have waited.
		///It a long data, cause we'll use it with System.Timespan to convertToBinary and restore from binary</summary>
		public long saveDateBinary = 0;
		///<summary> How many time we has showed ads on the saveDate </summary>
		public int freeDrawUsed = 0;
		///<summary> Ads watched per day </summary>
		public int adsWatchedCount = 0;
	}
	#endregion//Class
}
