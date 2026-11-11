using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UserStarChestInventory", menuName = "Game/User/UserStarChestInventory")]
public class UserStarChestInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserStarChest-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "hjeiorjanfuava.data";
	[SerializeField] string KEY = "dt30u68jv93";

	//[Header("ShareVar")]
	//[SerializeField] IntegerVariable userCoinVar;

	[Header("Config")]
	[SerializeField] int starChestCapacity = 1000;
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


	#region UI callback
	///<summary> Same to AddStarsUserHasEarnMoreFromLevels, but use for UI call </summary>
	public void In_AddStarsUserHasEarnMoreFromLevels(IntegerVariable amountVar) {
		if(isFailedConfig)
			return;
		AddStarsUserHasEarnMoreFromLevels(amountVar.Value);
	}
	#endregion//UI callback


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


	#region Public
	public int StarChestCapacity => starChestCapacity;

	public bool IsAlreadyFulled() {
		if(isFailedConfig)
			return false;
		return _data.starInChest >= starChestCapacity;
	}

	///<summary> How much full of stars in chest (starInChest / capacity), return is value 0f-1f </summary>
	public float GetPercentOfStarInChest() {
		if(isFailedConfig)
			return 0f;
		var result = (float)_data.starInChest / (float)starChestCapacity;
		return result;
	}

	public int GetStarSavedInChest() {
		if(isFailedConfig)
			return 0;
		return _data.starInChest;
	}

	public int GetStarUserHasEarnMoreInLevels() {
		if(isFailedConfig)
			return 0;
		return _data.starEarnMoreFromLevels;
	}

	///<summary> Call this at Endgame, to add stars user earned in level (1,2,3) </summary>
	public void AddStarsUserHasEarnMoreFromLevels(int amount) {
		if(isFailedConfig)
			return;
		// we cap it so when add more, the starInChest not over the capacity
		if(_data.starInChest + _data.starEarnMoreFromLevels + amount > starChestCapacity) {
			amount = starChestCapacity - _data.starInChest - _data.starEarnMoreFromLevels;
			amount = amount < 0 ? 0 : amount;
		}
		_data.starEarnMoreFromLevels += amount;
		SaveToDisk();
	}

	///<summary> Move star earned to the saved, and reset the earn. Often call this at menu when user showing effect StarChest </summary>
	public void MoveStarEarnedToSavedAndResetEarnedStarToZero() {
		if(isFailedConfig)
			return;
		_data.starInChest += _data.starEarnMoreFromLevels;
		_data.starInChest = Mathf.Clamp(_data.starInChest, 0, starChestCapacity);
		_data.starEarnMoreFromLevels = 0;
		SaveToDisk();
	}

	///<summary> Call this when user claim stars </summary>
	public void ResetSavedStars() {
		if(isFailedConfig)
			return;
		_data.starInChest = 0;
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
		_data.starInChest = 0;
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
		///<summary> Star that alreay in the chest </summary>
		public int starInChest;
		///<summary> Stars user earn more from playing level, will add to the star chest in the menu </summary>
		public int starEarnMoreFromLevels;
	}
	#endregion//Class
}
