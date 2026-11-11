using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Diagnostics;

///<summary> Energy (heart) of user </summary>
[CreateAssetMenu(fileName = "UserEnergyInventory", menuName = "Game/User/UserEnergyInventory")]
public class UserEnergyInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserEnergy-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "ghdajyykfdsf.data";
	[SerializeField] string KEY = "ndf6846khsf";

	[Header("Config")]
	[SerializeField] int maxEnergy = 5;
	[SerializeField] int timeToGetOneEnergyInMinutes = 30;

	[Header("ShareVar")]
	[SerializeField] IntegerVariable remainEneryVar;

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


	#region Public - UI callbacks
	///<summary> Currently call this at endgame by GameEventListener to increase user energy. Cause if user win ,we dont deduct energy. So cause we duduct one when w </summary>
	public void In_IncreaseEnergyAndKeepCurrentTime() {
		if(isFailedConfig)
			return;
		IncreaseEnergyAndKeepCurrentTime();
	}
	#endregion//Public - UI callbacks


	#region Public
	public int MaxEnergy => maxEnergy;
	public int TimeToGetOneEnergyInMinutes => timeToGetOneEnergyInMinutes;

	public void IncreaseEnergyAndKeepCurrentTime() {
		if(isFailedConfig)
			return;
		ModifyEnergyAndCheckClamp(1);
		SaveToDisk();
	}

	public void IncreaseOneEnergyAndSaveCurrentTime() {
		if(isFailedConfig)
			return;
		ModifyEnergyAndCheckClamp(1);
		SaveCurrentTimeToData();
		SaveToDisk();
	}

	public void IncreaseEnergyAndSaveCurrentTime(int amount) {
		if(isFailedConfig)
			return;
		ModifyEnergyAndCheckClamp(1);
		SaveCurrentTimeToData();
		SaveToDisk();
	}

	public void DecreaseOneEnergy() {
		if(isFailedConfig)
			return;
		ModifyEnergyAndCheckClamp(-1);
		SaveCurrentTimeToData();
		SaveToDisk();
	}

	public bool IsFull() {
		if(isFailedConfig)
			return false;
		return _data.energyRemain == maxEnergy;
	}

	public bool IsEmpty() {
		if(isFailedConfig)
			return true;
		return _data.energyRemain == 0;
	}

	public DateTime GetLastTimeDeductEnergy() {
		if(isFailedConfig)
			return DateTime.Now;
		return DateTime.FromBinary(_data.lastDeductEnergy);
	}

	public void CheckFillEneryAfterIdleTime() {
		if(isFailedConfig)
			return;
		var currentTime = DateTime.Now;
		var lastTimeDeductEnergy = DateTime.FromBinary(_data.lastDeductEnergy);
		var timePassed = currentTime - lastTimeDeductEnergy;
		var toMinutes = (int)timePassed.TotalMinutes;

		var convertToEnery = toMinutes / timeToGetOneEnergyInMinutes;
		if(convertToEnery > 0) {
			_data.energyRemain += convertToEnery;
			_data.energyRemain = Mathf.Clamp(_data.energyRemain, 0, maxEnergy);
			if(IsFull()) {
				SaveCurrentTimeToData();
			}
			else {
				var minuteWaitedRemain = toMinutes % timeToGetOneEnergyInMinutes;
				var timeToSave = currentTime.AddMinutes(minuteWaitedRemain);
				_data.lastDeductEnergy = timeToSave.ToBinary();
			}
			SaveToDisk();
		}
	}

	///<summary> Next time we Full energy, should check IsFull() first, if we already full, then return current time.
	///NOTEs: should call CheckFillEnergyAfterIdle before calling this. </summary>
	public DateTime GetNextTimeWhenFullEnergy() {
		if(isFailedConfig)
			return DateTime.Now;
		if(IsFull()) {
			return DateTime.Now;
		}
		// we're in the middle of fill 1 energy, find that time first
		var currentTime = DateTime.Now;
		var lastTimeDeductEnergy = DateTime.FromBinary(_data.lastDeductEnergy);
		var timePassed = currentTime - lastTimeDeductEnergy;
		var secondToWaitMoreToGetOneEnergy = timeToGetOneEnergyInMinutes * 60 - timePassed.TotalSeconds;
		// if we get 1 energy and full, then just return this nextTimeFull
		var nextTimeFull = DateTime.Now.AddSeconds(secondToWaitMoreToGetOneEnergy);
		// not? so wait to full 1 enery + other remain
		if(_data.energyRemain + 1 < maxEnergy) {
			var timeToWaitMoreAfterAddOne = (maxEnergy - (_data.energyRemain + 1)) * (timeToGetOneEnergyInMinutes * 60);
			nextTimeFull.AddSeconds(timeToWaitMoreAfterAddOne);
		}
		return nextTimeFull;
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
		// write to shareVar any cases
		WriteToShareVar();
	}

	/// <summary> Save the current userData to file on LocalDisk </summary>
	public void SaveToDisk() {
		JsonFileWriter.Save<SaveForm>(_data, SavePath, KEY);
		WriteToShareVar();
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

	void ModifyEnergyAndCheckClamp(int amount) {
		var result = _data.energyRemain += amount;
		result = Mathf.Clamp(result, 0, maxEnergy);
		_data.energyRemain = result;
	}

	void CreateDefaultData() {
		_data = new SaveForm();
		SaveCurrentTimeToData();
		_data.energyRemain = maxEnergy;
		//Log.Info("CreateDefauilt value");
	}

	void CreateDefautDataIfNull() {
		if(_data == null)
			CreateDefaultData();
	}

	void SaveCurrentTimeToData() {
		_data.lastDeductEnergy = DateTime.Now.ToBinary();
	}

	void WriteToShareVar() {
		remainEneryVar.Value = _data.energyRemain;
	}
	#endregion//Private


	#region Cheat
	[Conditional("CHEAT")][Button]
	public void CheatZeroEnergy() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		_data.energyRemain = 0;
		SaveCurrentTimeToData();
		SaveToDisk();
	}

	[Conditional("CHEAT")]
	public void CheatFastForward10minutes() {
		if(isFailedConfig)
			return;
		CreateDefautDataIfNull();
		var lastTimeDeduct = DateTime.FromBinary(_data.lastDeductEnergy);
		var reduced = lastTimeDeduct.AddMinutes(-10);
		_data.lastDeductEnergy = reduced.ToBinary();
		SaveToDisk();
	}

	[Conditional("CHEAT")]
	public void CheatFullEnergy() {
		if(isFailedConfig)
			return;
		_data.energyRemain = maxEnergy;
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
		///<summary> Last time user deduct energy, use this to know timespan from it to current, to increase energy
		///It a long data, cause we'll use it with System.Timespan to convertToBinary and restore from binary</summary>
		public long lastDeductEnergy = 0;
		public int energyRemain;
	}
	#endregion//Class
}
