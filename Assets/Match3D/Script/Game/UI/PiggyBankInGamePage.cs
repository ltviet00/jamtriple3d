using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

/// <summary>
/// Behavior for the Piggy bank in game, at the end level page
/// </summary>
public class PiggyBankInGamePage : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] UserPiggyBankInventory piggyBankInventory;
	[SerializeField] UserCoinInventory userCoinInventory;

	[Header("Refs")]
	[SerializeField] TMP_Text coinText;
	[SerializeField] GameObject iapButon;
	[SerializeField] GameObject laterButton;

	[Header("Refs - coin fillbar")]
	[SerializeField] RectTransform coinProgressBarImage;
	[SerializeField] float coinProgressFullSize;

	[Header("Config duration")]
	[SerializeField] float coinIncreaseEffectDuration = 1.5f;
	[SerializeField] float autoCloseIfNotEnoughCoin = 1f;
	[SerializeField] float delayShowLaterButton = 1f;

	[Header("Unity event out")]
	[SerializeField] UnityEvent requestNextPopupCmd;

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

	///<summary> Config UIPage to invoke this when page open finish </summary>
	public void In_OnPageFinishOpen() {
		if(isFailedConfig)
			return;
		// effect
		WorkingAsync().Forget();
	}

	public void In_OnPressLater() {
		if(isFailedConfig)
			return;
		requestNextPopupCmd.Invoke();
	}

	///<summary> Config the Okay button on purchase sucecsss popup to invoike this </summary>
	public void In_PurchaseSucecssAndGoToNextPage() {
		if(isFailedConfig)
			return;
		requestNextPopupCmd.Invoke();
	}

	///<summary> Call this when purchase success </summary>
	public void In_SaveUserPurchasedCoinToInventory() {
		if(isFailedConfig)
			return;
		// save coin
		var coin = piggyBankInventory.GetCurrentCoin();
		piggyBankInventory.SetCoin(0);
		userCoinInventory.IncreaseCoin(coin);
		// visual
		ScaleCoinProgress(0);
		ConfigCoinTextFollowUserPiggyCoin();
	}
	#endregion//UI event


	#region Private
	void FirstDisplayConfig() {
		// later button and disable first
		laterButton.SetActive(false);
		// iap button
		iapButon.SetActive(piggyBankInventory.IsEnoughToBuyWithIAP());
		// first coin text
		ConfigCoinTextFollowUserPiggyCoin();
		ScaleCoinProgress(piggyBankInventory.GetCurrentCoin());
	}

	async UniTaskVoid WorkingAsync() {
		// full coin? then delay show later button
		if(piggyBankInventory.IsFull()) {
			await UniTask.Delay((int)(delayShowLaterButton * 1000f));
			laterButton.SetActive(true);
		}
		// not full? then show progress effect
		else {
			await ScaleCoinProgressEffectAsync();
			// active iAP button after scale coin
			iapButon.SetActive(piggyBankInventory.IsEnoughToBuyWithIAP());
			// delay a bit
			await UniTask.Delay((int)(delayShowLaterButton * 1000));
			// user can buy? delay show later
			if(piggyBankInventory.IsEnoughToBuyWithIAP()) {
				laterButton.SetActive(true);
			}
			// if user can not buy? Then go next popup
			else {
				requestNextPopupCmd.Invoke();
			}
		}
	}

	async UniTask ScaleCoinProgressEffectAsync() {
		var timer = 0f;
		var currentCoin = piggyBankInventory.GetCurrentCoin();
		var targetCoin = currentCoin + piggyBankInventory.coinGainPerLevel;
		while(timer < coinIncreaseEffectDuration) {
			await UniTask.Yield();
			timer += Time.deltaTime;
			var ratio = timer / coinIncreaseEffectDuration;
			var toDisplay = (int)Mathf.Lerp(currentCoin, targetCoin, ratio);
			coinText.text = toDisplay.ToString();
			ScaleCoinProgress(toDisplay);
		}
		// finish? set text, also save to disk
		piggyBankInventory.SetCoin(targetCoin);
		// config display one more time
		coinText.text = targetCoin.ToString();
		ScaleCoinProgress(targetCoin);
		ConfigCoinTextFollowUserPiggyCoin();

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
