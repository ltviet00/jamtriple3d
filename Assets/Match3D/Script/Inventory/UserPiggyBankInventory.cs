using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Store Piggy bank coin info
/// </summary>
[CreateAssetMenu(fileName = "UserPiggyBankInventory", menuName = "Game/User/UserPiggyBankInventory")]
public class UserPiggyBankInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "PiggyBank-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "frthcdaS.data";
	[SerializeField] string KEY = "s5y27tfh275";

	[Header("Config")]
	public int canBuyThreahold = 300;
	public int coinCapacity = 600;
	public int coinGainPerLevel = 30;

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


	#region Public - Get info
	public int GetCurrentCoin() {
		if(isFailedConfig)
			return 0;
		return _data.coin;
	}

	public void SetCoin(int coin) {
		if(isFailedConfig)
			return;
		_data.coin = coin;
		SaveToDisk();
	}

	public bool IsFull() {
		if(isFailedConfig)
			return false;
		return _data.coin >= coinCapacity;
	}

	public bool IsEnoughToBuyWithIAP() {
		if(isFailedConfig)
			return false;
		return _data.coin >= canBuyThreahold;
	}
	#endregion//Public - Get info


	#region Public - LoadSave
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
	#endregion// Public - LoadSave


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


	#region Classes
	[System.Serializable]
	public class SaveForm {
		public int coin;

		public SaveForm() {
			// first score, zero at level 0
			coin = 0;
		}
	}
	#endregion//Class
}
