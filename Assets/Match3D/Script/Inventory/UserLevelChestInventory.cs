using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// save info LevelChest for user
/// </summary>
[CreateAssetMenu(fileName = "UserLevelChestInventory", menuName = "Game/User/UserLevelChestInventory")]
public class UserLevelChestInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserLevelChest-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "sfgtjjjfafd.data";
	[SerializeField] string KEY = "456afdht";

	//[Header("ShareVar")]
	//[SerializeField] IntegerVariable userCoinVar;

	[Header("Config")]
	[SerializeField] int levelCountToGetChest = 10;
	[SerializeField] Vector2Int randomCoinRewardAmount;
	[SerializeField] Vector2Int randomBoosterAmountReward;
	[SerializeField] List<BoosterID> randomBoosterList;

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
	public int LevelCountToGetChest => levelCountToGetChest;

	public int GetPreviousBonusLevel() {
		if(isFailedConfig)
			return 0;
		return _data.previousBonusLevel;
	}

	///<summary> REturn closest next level to get bonus. Eg: LevelCountToGetChest = 10, then if currentLevle = 15, then next = 20 </summary>
	public int GetNextBonusLevel(int currentLevel) {
		if(isFailedConfig)
			return currentLevel;
		var previousBonusLevel = (currentLevel / levelCountToGetChest) * levelCountToGetChest;
		var nextLevel = previousBonusLevel + levelCountToGetChest;
		return nextLevel;
	}

	public bool IsClaimedPreviousBonus() {
		if(isFailedConfig)
			return true;
		return _data.claimed;
	}

	///<summary> Call this when user claim reward, and to save the closest previous claimable-level (eg: levelCount to claim = 10, we at 25, then last claimable level is 20), input the current level to here </summary>
	public void ResetClaimAndSaveLastPreviousLevel(int currentLevel) {
		if(isFailedConfig)
			return;
		_data.claimed = true;
		var lastClaimable = (currentLevel / levelCountToGetChest) * levelCountToGetChest;
		// cause level start from 0, need -1 here
		//lastClaimable--;
		_data.previousBonusLevel = lastClaimable;
		SaveToDisk();
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
			_data = JsonFileWriter.Load<SaveForm>(SavePath, KEY);
			Log.Info($"Load [{SavePath} successful]");
		}
		//// save to shareVar as well
		//SaveToShareVar();
	}

	/// <summary> Save the current userData to file on LocalDisk </summary>
	public void SaveToDisk() {
		JsonFileWriter.Save<SaveForm>(_data, SavePath, KEY);
	}
	#endregion//Public


	#region Public - reward
	public int GetCoinReward() {
		if(isFailedConfig)
			return 0;
		// cause random will exclude the max, so need +1 here
		return Random.Range(randomCoinRewardAmount.x, randomCoinRewardAmount.y + 1);
	}

	public int GetBoosterRewardAmount() {
		if(isFailedConfig)
			return 0;
		// cause random will exclude the max, so need +1 here
		return Random.Range(randomBoosterAmountReward.x, randomBoosterAmountReward.y);
	}

	public BoosterID GetRewardBoosterID() {
		if(isFailedConfig)
			return null;
		var randomIndex = Random.Range(0, randomBoosterList.Count);
		return randomBoosterList[randomIndex];
	}
	#endregion//Public - reward


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

	void CreateDefautDataIfNull() {
		if(_data == null)
			CreateDefaultData();
	}

	void CreateDefaultData() {
		_data = new SaveForm();
		// first create, mean no bonus, claim = true, previous = 0;
		_data.previousBonusLevel = 0;
		_data.claimed = true;
	}

	//void SaveToShareVar() {
	//	userCoinVar.Value = _data.coin;
	//}
	#endregion//Private


	#region Interface
	public void ResetDataToDefault() {
		CreateDefaultData();
	}
	#endregion//Interface

	#region Classes
	[System.Serializable]
	public class SaveForm {
		public int previousBonusLevel;
		public bool claimed = false;
	}
	#endregion//Class
}
