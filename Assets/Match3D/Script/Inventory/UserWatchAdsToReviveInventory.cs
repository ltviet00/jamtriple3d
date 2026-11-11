using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "UserWatchAdsToReviveInventory", menuName = "Game/User/UserWatchAdsToReviveInventory")]
public class UserWatchAdsToReviveInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserAdsToRevive-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "sffgag64567.data";
	[SerializeField] string KEY = "ads34fhsdfh";

	[Header("Config")]
	[SerializeField] int freeAdsToRevivePerDay = 1;

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
	public bool IsReachFreeAdsToRevivePerDay() {
		if(isFailedConfig)
			return true;
		CheckResetOpenCountIfDayHasPassed();
		return _data.adsWatchedCount >= freeAdsToRevivePerDay;
	}

	public void IncreaseAdsWatched() {
		if(isFailedConfig)
			return;
		CheckResetOpenCountIfDayHasPassed();
		_data.adsWatchedCount++;
		SaveToDisk();
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
		_data.adsWatchedCount = 0;
	}

	void CheckResetOpenCountIfDayHasPassed() {
		var currentDate = DateTime.Now;
		var dateInSaveData = DateTime.FromBinary(_data.saveDateBinary);
		// different date? then reset drawCount, also save currentDay
		if(currentDate.Day != dateInSaveData.Day) {
			_data.adsWatchedCount = 0;
			_data.saveDateBinary = DateTime.Now.ToBinary();
			SaveToDisk();
		}
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
		///<summary> The last time we check/modify remainTroop. Use this to compute how long user have waited.
		///It a long data, cause we'll use it with System.Timespan to convertToBinary and restore from binary</summary>
		public long saveDateBinary = 0;
		///<summary> How many time we has showed ads on the saveDate </summary>
		public int adsWatchedCount = 0;
	}
	#endregion//Class
}
