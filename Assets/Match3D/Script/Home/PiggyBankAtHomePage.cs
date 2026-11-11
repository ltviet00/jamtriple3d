using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

/// <summary>
/// Handle logic for PiggyBank at Home page
/// </summary>
public class PiggyBankAtHomePage : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] UserPiggyBankInventory piggyBankInventory;
	[SerializeField] UserCoinInventory userCoinInventory;

	[Header("Refs")]
	[SerializeField] TMP_Text coinText;
	[SerializeField] GameObject iapButon;
	//[SerializeField] GameObject laterButton;

	[Header("Refs - coin fillbar")]
	[SerializeField] RectTransform coinProgressBarImage;
	[SerializeField] float coinProgressFullSize;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region UI event
	///<summary> Config UIPage to invoke this when page start open </summary>
	public void In_OnPageStartOpenning() {
		if(isFailedConfig)
			return;
		FirstDisplayConfig();
	}

	///<summary> Call this when purchase success </summary>
	public void In_OnPurchaseSuccess() {
		if(isFailedConfig)
			return;
		// save coin
		var coin = piggyBankInventory.GetCurrentCoin();
		piggyBankInventory.SetCoin(0);
		userCoinInventory.IncreaseCoin(coin);
		// visual
		ScaleCoinProgress(0);
		ConfigCoinTextFollowUserPiggyCoin();
		// disable button, to show up the can't buy one
		iapButon.SetActive(false);
	}
	#endregion//UI event


	#region Private
	void FirstDisplayConfig() {
		// iap button
		iapButon.SetActive(piggyBankInventory.IsEnoughToBuyWithIAP());
		// first coin text
		ConfigCoinTextFollowUserPiggyCoin();
		ScaleCoinProgress(piggyBankInventory.GetCurrentCoin());
	}

	void ScaleCoinProgress(int coinAmount) {
		var maxCoin = piggyBankInventory.coinCapacity;
		var ratio = (float)coinAmount / (float)maxCoin;

		var imageSize = coinProgressBarImage.sizeDelta;
		imageSize.y = ratio * coinProgressFullSize;
		coinProgressBarImage.sizeDelta = imageSize;
	}

	void ConfigCoinTextFollowUserPiggyCoin() {
		const string FULL_TEXT = "Full";
		coinText.text = piggyBankInventory.IsFull() ? FULL_TEXT : piggyBankInventory.GetCurrentCoin().ToString();
	}
	#endregion//Private
}
