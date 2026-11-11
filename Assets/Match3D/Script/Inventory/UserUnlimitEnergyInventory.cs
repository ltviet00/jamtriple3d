using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[CreateAssetMenu(fileName = "UserUnlimitEnergyInventory", menuName = "Game/User/UserUnlimitEnergyInventory")]
public class UserUnlimitEnergyInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserEnergyUnlimit-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "rtubasdfq3y7.data";
	[SerializeField] string KEY = "vs65yfg673";

	[Header("EventOUt")]
	[SerializeField] GameEvent expandUnlimitDurationEvent;

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
	public bool IsInUnlimitEnergyDuration() {
		if(isFailedConfig)
			return false;
		var now = DateTime.Now;
		var unlimitEnergyTime = DateTime.FromBinary(_data.unlimitEnergyTime);
		return unlimitEnergyTime > now;
	}

	///<summary> Call this when user buy IAP to get unlimit energy in an duration (6,12,24h) </summary>
	public void ExpandUnlimitDuration(int hours) {
		if(isFailedConfig)
			return;
		var currentUnlimitTime = DateTime.FromBinary(_data.unlimitEnergyTime);
		var now = DateTime.Now;
		// out of unlimit-duration, or first time buy
		if(currentUnlimitTime < now) {
			currentUnlimitTime = now;
		}
		// increase time
		var future = currentUnlimitTime.AddHours(hours);
		_data.unlimitEnergyTime = future.ToBinary();
		SaveToDisk();
		// raise event
		expandUnlimitDurationEvent.Raise();
	}

	///<summary> Call this when user receive daily reward or buy package with short duration (15m, 30m ..) </summary>
	public void ExpandUnlimitDurationByMinutes(int minutes) {
		if(isFailedConfig)
			return;
		var currentUnlimitTime = DateTime.FromBinary(_data.unlimitEnergyTime);
		var now = DateTime.Now;
		// out of unlimit-duration, or first time buy
		if(currentUnlimitTime < now) {
			currentUnlimitTime = now;
		}
		// increase time
		var future = currentUnlimitTime.AddMinutes(minutes);
		_data.unlimitEnergyTime = future.ToBinary();
		SaveToDisk();
		// raise event
		expandUnlimitDurationEvent.Raise();
	}

	///<summary> Return the time (in future) when currnet unlimit energy will out. Should check IsUnlimitenergyuDuration first </summary>
	public DateTime GetExpireUnlimitEnergyTime() {
		if(isFailedConfig)
			return DateTime.Now.AddDays(-1f);
		return DateTime.FromBinary(_data.unlimitEnergyTime);
	}
	#endregion//Public


	#region Public - Load/save
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
	#endregion//Public - Load/save


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
		//Log.Info("CreateDefauilt value");
	}

	void CreateDefautDataIfNull() {
		if(_data == null)
			CreateDefaultData();
	}

	//void WriteToShareVar() {
	//	remainEneryVar.Value = _data.energyRemain;
	//}
	#endregion//Private


	#region Cheat
	[Conditional("CHEAT")][Button]
	public void CheatAdd1hourUnlimit() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		ExpandUnlimitDuration(1);
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	public void CheatZeroUnlimitEnergy() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		var currentTime = DateTime.Now;
		var past = currentTime.AddHours(-1.0);
		_data.unlimitEnergyTime = past.ToBinary();
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
		///<summary> When user buy a package with unlimit energy in N hour (eg: 8hour), we get the current time, plus N hour, and save to this. So check current time with this time to know if user still in unlimit-energy durataion </summary>
		public long unlimitEnergyTime = 0;
	}
	#endregion//Class
}
