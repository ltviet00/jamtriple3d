using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// We got freecoin item in shop, let user watch ads to get freecoin, limit 10 ads/day.
/// </summary>
public class ShopFreeCoinUI : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserFreeGoldInventory userFreeCoinInventory;

	[Header("Refs")]
	[SerializeField] Button freeCoinByAdsButton;
	[SerializeField] TMP_Text freeAdsRemainText;


	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ConfigFreeAdsTextFollowUserData();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI callbacks
	///<summary> Config UI button to invoke this </summary>
	public void In_OnSuccessWatchAdsToGetFreeCoin() {
		if(isFailedConfig)
			return;
		userCoinInventory.IncreaseCoin(userFreeCoinInventory.CoinPerAds);
		userFreeCoinInventory.IncreaseFreeAdsOpenUsed();
		ConfigFreeAdsTextFollowUserData();
	}
	#endregion//UI callback

	#region Private
	void ConfigFreeAdsTextFollowUserData() {
		userFreeCoinInventory.CheckResetOpenCountIfDayHasPassed();
		var adsWatched = userFreeCoinInventory.AdsWatchedCount;
		freeAdsRemainText.text = (userFreeCoinInventory.FreeAdsPerDay - adsWatched).ToString();
		freeCoinByAdsButton.interactable = adsWatched < userFreeCoinInventory.FreeAdsPerDay;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
