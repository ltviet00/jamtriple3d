using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Diagnostics;

/// <summary>
/// Load/save information related to FreeGold (watchAds per day) ...
/// </summary>
[CreateAssetMenu(fileName = "UserFreeGoldInventory", menuName = "Game/User/UserFreeGoldInventory")]
public class UserFreeGoldInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserFreeGold-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "fgadfhdfsdf.data";
	[SerializeField] string KEY = "245gsdf535";

	[Header("Config - ads")]
	[SerializeField] IntegerVariable freeAdsToOpenPerDayVar;
	[SerializeField] IntegerVariable coinAmountPerAdsVar;

	[Header("Config - free coin")]
	[SerializeField] IntegerVariable freeCoinPerTimeVar;
	[SerializeField] IntegerVariable freeCoinCoolDownInMinuteVar;

	[Header("ShareData")]
	[SerializeField] IntegerVariable adsRemainToGetFreeCoinVar;

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


	#region Public - Ads
	public int FreeAdsPerDay => freeAdsToOpenPerDayVar.Value;
	public int CoinPerAds => coinAmountPerAdsVar.Value;
	public int AdsWatchedCount => _data.adsWatchedCount;

	public int GetRemainAdsCount() {
		if(isFailedConfig)
			return 0;
		CheckResetOpenCountIfDayHasPassed();
		return FreeAdsPerDay - _data.adsWatchedCount;
	}

	public bool IsReachFreeAdsOpenPerDay() {
		if(isFailedConfig)
			return true;
		CheckResetOpenCountIfDayHasPassed();
		return AdsWatchedCount >= FreeAdsPerDay;
	}

	public void IncreaseFreeAdsOpenUsed() {
		if(isFailedConfig)
			return;
		CheckResetOpenCountIfDayHasPassed();
		_data.adsWatchedCount++;
		SaveToDisk();
	}

	public void CheckResetOpenCountIfDayHasPassed() {
		var currentDate = DateTime.Now;
		var dateInSaveData = DateTime.FromBinary(_data.adsSaveDate);
		// different date? then reset drawCount, also save currentDay
		if(currentDate.Day != dateInSaveData.Day) {
			_data.adsWatchedCount = 0;
			_data.adsSaveDate = DateTime.Now.ToBinary();
			SaveToDisk();
		}
	}
	#endregion//Public - Ads


	#region Public - FreeCoin
	public int FreeCoinCoolDownInMinute => freeCoinCoolDownInMinuteVar.Value;
	public int FreeCoinPerTime => freeCoinPerTimeVar.Value;

	public bool IsFreeCoinReady() {
		if(isFailedConfig)
			return false;
		CheckFreeCoinCoolDown();
		return _data.isFreeCoinReady;
	}

	public DateTime GetLastTimeClaimFreeCoin() {
		if(isFailedConfig)
			return DateTime.Now;
		return DateTime.FromBinary(_data.lastTimeClaimFreeCoin);
	}

	///<summary> Call this when user just claim freecoin </summary>
	public void OnUserClaimFreeCoin() {
		if(isFailedConfig)
			return;
		_data.isFreeCoinReady = false;
		_data.lastTimeClaimFreeCoin = DateTime.Now.ToBinary();
		SaveToDisk();
	}

	public void ForceFreeCoinReady() {
		if(isFailedConfig)
			return;
		_data.isFreeCoinReady = true;
		_data.lastTimeClaimFreeCoin = DateTime.Now.ToBinary();
		SaveToDisk();
	}

	public void CheckFreeCoinCoolDown() {
		if(isFailedConfig)
			return;
		var currentTime = DateTime.Now;
		var lastTimeClaimFreeCoin = DateTime.FromBinary(_data.lastTimeClaimFreeCoin);
		var timePassed = currentTime - lastTimeClaimFreeCoin;
		var toMinutes = (int)timePassed.TotalMinutes;
		var waitEnough = toMinutes >= freeCoinCoolDownInMinuteVar.Value;
		if(waitEnough) {
			_data.isFreeCoinReady = true;
			SaveToDisk();
		}
	}
	#endregion//Public - FreeCoin


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

	void LoadFromDisk() {
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
	void SaveToDisk() {
		JsonFileWriter.Save<SaveForm>(_data, SavePath, KEY);
	}

	void CreateDefaultData() {
		_data = new SaveForm();
		_data.adsSaveDate = DateTime.Now.ToBinary();
		_data.lastTimeClaimFreeCoin= DateTime.Now.ToBinary();
		_data.adsWatchedCount = 0;
		_data.isFreeCoinReady = true;
	}

	void CreateDefautDataIfNull() {
		if(_data == null)
			CreateDefaultData();
	}
	#endregion//Private


	#region CHEAT
	[Conditional("CHEAT")][Button]
	void EditorZeroAdsWatched() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.adsWatchedCount = 0;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void EditorFullAdsWatched() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.adsWatchedCount = FreeAdsPerDay;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void EditorOneMoreAdsWatchedToFull() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.adsWatchedCount = FreeAdsPerDay - 1;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void Editor10SecondWaitForFreeCoin() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		var currentTime = DateTime.Now;
		var secondsToBackward = freeCoinCoolDownInMinuteVar.Value * 60 - 10;
		var thePast = currentTime - TimeSpan.FromSeconds(secondsToBackward);
		_data.lastTimeClaimFreeCoin = thePast.ToBinary();
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
		///<summary> The last time user claim, we start timer. Use this to compute how long user have waited.
		///It a long data, cause we'll use it with System.Timespan to convertToBinary and restore from binary</summary>
		public long lastTimeClaimFreeCoin = 0;
		///<summary> Date user watching ads, use this to reset freeAdsPerday.
		///It a long data, cause we'll use it with System.Timespan to convertToBinary and restore from binary</summary>
		public long adsSaveDate = 0;
		///<summary> How many time we has showed ads on the saveDate </summary>
		public int adsWatchedCount = 0;
		///<summary>  </summary>
		public bool isFreeCoinReady = false;
	}
	#endregion//Class
}
