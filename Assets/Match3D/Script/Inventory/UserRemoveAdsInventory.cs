using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LoadSave purchased ads state
/// </summary>
[CreateAssetMenu(fileName = "UserRemoveAdsInventory", menuName = "Game/User/UserRemoveAdsInventory")]
public class UserRemoveAdsInventory : ScriptableObject, IUserInventory {
	#region Init, config
	[Header("Path - Editor, runtime, Key")]
	[SerializeField] string SAVE_NAME_EDTOR = "UserRemoveAds-EditorNameOnly.data";
	[SerializeField] string SAVE_NAME = "jhevsdffadfd.data";
	[SerializeField] string KEY = "sfgh54634";

	[Header("Event out")]
	[SerializeField] GameEvent purchasedAdsEvent;

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


	#region Public - UI
	///<summary> Same to PurchasedRemoveAds package, but use this to call from UI </summary>
	public void In_PurchasedAdsSuccess() {
		if(isFailedConfig)
			return;
		PurchasedRemoveAdsSuccess();
	}

	///<summary> Call when RestorePurchaseSuccess but found out that user not purchase yet, set purchased to false  </summary>
	public void RestorePurchaseButUserNotBuyYet() {
		if(isFailedConfig)
			return;
		_data.purchased = false;
		SaveToDisk();
	}

	/////<summary> Read the IAP purchaser script, take the eventOut when restorePurchased success to call this, 
	///// to reset the removeAds state. Currently on IOS user can restore purchase</summary>
	//public void In_RestoreTransaction() {
	//	if(isFailedConfig)
	//		return;
	//	_data.purchased = false;
	//	SaveToDisk();
	//}
	#endregion//Public - UI


	#region Public
	///<summary> Has user purchased this? </summary>
	public bool HasPurchased() {
		if(isFailedConfig)
			return true;
		return _data.purchased;
	}

	///<summary> Call this when user purchased the RemoveAds package </summary>
	public void PurchasedRemoveAdsSuccess() {
		if(isFailedConfig)
			return;
		_data.purchased = true;
		Log.Info($"Success purchase RemoveAds, status [{_data.purchased}]");
		purchasedAdsEvent.Raise();
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
		_data.purchased = false;
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
		public bool purchased = false;
	}
	#endregion//Class
}
