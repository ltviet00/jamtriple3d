using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// Handle logic, display for FreeCoin at Home
/// </summary>
public class FreeCoinPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserFreeGoldInventory userFreeCoinInventory;

	[Header("Refs - FreeCoin")]
	[SerializeField] GameObject claimFreeCoinButton;
	[SerializeField] TMP_Text freeCoinCoolDownTimerText;

	[Header("Refs - FreeAds")]
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
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI callback
	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		// check active freecoin text
		bool isFreeCoinReady = userFreeCoinInventory.IsFreeCoinReady();
		claimFreeCoinButton.SetActive(isFreeCoinReady);
		// not ready? then start timer
		if(!isFreeCoinReady) {
			//StartTimerForFreeCoinCoolDownAsync().AttachExternalCancellation(_token.Token);
			StartTimerForFreeCoinCoolDownAsync().Forget();
		}
		// freeAds
		ConfigFreeAdsTextFollowUserData();
	}

	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageClose() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}

	///<summary> Config UI button to invoke this </summary>
	public void In_OnPressClaimFreeCoin() {
		if(isFailedConfig)
			return;
		// increase coin, start cool down
		userCoinInventory.IncreaseCoin(userFreeCoinInventory.FreeCoinPerTime);
		userFreeCoinInventory.OnUserClaimFreeCoin();
		claimFreeCoinButton.SetActive(false);
		//StartTimerForFreeCoinCoolDownAsync().AttachExternalCancellation(_token.Token);
		StartTimerForFreeCoinCoolDownAsync().Forget();
	}

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
	async UniTask StartTimerForFreeCoinCoolDownAsync() {
		// in case user minimize the app, lose focus, let alway re-calculate each seconds
		// dont worry about this while(true) loop, we alway break it OnPageClose (clear running tasks)
		while(true) {
			var currentTime = DateTime.Now;
			var lastClaim = userFreeCoinInventory.GetLastTimeClaimFreeCoin();
			var nextTimeGetOneEnergy = lastClaim.AddMinutes(userFreeCoinInventory.FreeCoinCoolDownInMinute);
			var timeToWait = nextTimeGetOneEnergy - currentTime;
			// check break
			if(timeToWait.TotalSeconds <= 0) {
				break;
			}
			// format text
			var hour = timeToWait.Hours;
			var minute = timeToWait.Minutes;
			var second = timeToWait.Seconds;

			var builder = new StringBuilder();
			builder.Append(hour >= 10 ? $"{hour}:" : $"0{hour}:");
			builder.Append(minute >= 10 ? $"{minute}:" : $"0{minute}:");
			builder.Append(second >= 10 ? $"{second}:" : $"0{second}:");
			freeCoinCoolDownTimerText.text = builder.ToString();
			// wait and deduct time
			await UniTask.Delay(1000, cancellationToken: _token.Token);
		}
		// reach here mean cooldown finish
		userFreeCoinInventory.ForceFreeCoinReady();
		claimFreeCoinButton.SetActive(true);
	}

	void ConfigFreeAdsTextFollowUserData() {
		userFreeCoinInventory.CheckResetOpenCountIfDayHasPassed();
		var adsWatched = userFreeCoinInventory.AdsWatchedCount;
		freeAdsRemainText.text = $"({adsWatched}/{userFreeCoinInventory.FreeAdsPerDay})";
		freeCoinByAdsButton.interactable = adsWatched < userFreeCoinInventory.FreeAdsPerDay;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
