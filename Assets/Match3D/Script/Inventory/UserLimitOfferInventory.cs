using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UserLimitOfferInventory", menuName = "Game/User/UserLimitOfferInventory")]
public class UserLimitOfferInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserLimitOffer-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "fg4y4677ywret7.data";
	[SerializeField] string KEY = "sfgj45626sa";

	[Header("Config")]
	[SerializeField] int offerDurationInHour = 48;

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
	public DateTime GetEndOfferTime() {
		if(isFailedConfig)
			return DateTime.Now;
		CreateDefautDataIfNull();
		var result = DateTime.FromBinary(_data.offerEndTime);
		// loop forever
		var now = DateTime.Now;
		if(result < now) {
			result = now.AddHours(offerDurationInHour);
		}
		return result;
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
		var now = DateTime.Now;
		var future = now.AddHours(offerDurationInHour);
		_data.offerEndTime = future.ToBinary();
	}

	void CreateDefautDataIfNull() {
		if(_data == null)
			CreateDefaultData();
	}

	//void WriteToShareVar() {
	//	remainEneryVar.Value = _data.energyRemain;
	//}
	#endregion//Private


	#region Interface
	public void ResetDataToDefault() {
		CreateDefaultData();
	}
	#endregion//Interface


	//#if UNITY_EDITOR
	//	[Button]
	//	void EditorAdd1hourUnlimit() {
	//		if(isFailedConfig)
	//			return;
	//		CreateDefautDataIfNull();
	//		var currentTime = DateTime.Now;
	//		currentTime.AddHours(1.0);
	//		_data.offerEndTime = currentTime.ToBinary();
	//		SaveToDisk();
	//	}
	//#endif//UNITY_EDITOR


	#region Classes
	[System.Serializable]
	public class SaveForm {
		///<summary> We alway reset this to a time in future, 48h from now </summary>
		public long offerEndTime = 0;
	}
	#endregion//Class
}
