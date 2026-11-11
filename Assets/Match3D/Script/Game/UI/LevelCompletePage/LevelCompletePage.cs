using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handle level clear page in Endgame
/// </summary>
public class LevelCompletePage : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] UIPage mainPage;
	[SerializeField] List<LevelCompletePageStarItem> starList;

	[Header("Ref - weekly toy")]
	[SerializeField] GameObject rootWeeklyToyStats;

	[Header("Data")]
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;
	[SerializeField] UserEnergyInventory energyInventory;
	[SerializeField] UserStarChestInventory starChestInventory;

	[Header("Data - Level duration")]
	[SerializeField] IntegerVariable remainTimeInSecondVar;
	[SerializeField] IntegerVariable levelTimeVar;

	[Header("ShareData - weekly event")]
	[SerializeField] WeeklyEventRewardStorage weeklyEventStorage;
	[SerializeField] IntegerVariable userCurrentLevelVar;

	[Header("Unity event out")]
	[SerializeField] UnityEvent openNextLevelPageCmd;
	[SerializeField] UnityEvent openOutOfEnergyPageCmd;

	[Header("Config")]
	[SerializeField] float intervalBetweenStar = 0.2f;
	///<summary> User will get 3 stars if lot of time remain (compare to level duration).
	///So let say this array is [0.7, 0.3], mean you will got 3stars if remain time > 70% of level duration,
	///2 stars for 30%, and 1 star left for the rest</summary>
	[SerializeField] float[] remainTimePercentAndStarGain;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		mainPage.OnShowFinshed.AddListener(OnPageFinishOpening);
		mainPage.OnShowStart.AddListener(OnPageStartShowing);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		mainPage.OnShowFinshed.RemoveListener(OnPageFinishOpening);
		mainPage.OnShowStart.RemoveListener(OnPageStartShowing);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI callbacks
	///<summary> Config the Play button to invoke this </summary>
	public void In_OnPressPlay() {
		if(isFailedConfig)
			return;
		if(unlimitEnergyInventory.IsInUnlimitEnergyDuration()) {
			openNextLevelPageCmd.Invoke();
			return;
		}
		energyInventory.CheckFillEneryAfterIdleTime();
		var eventToRaise = energyInventory.IsEmpty() ? openOutOfEnergyPageCmd : openNextLevelPageCmd;
		eventToRaise.Invoke();
	}
	#endregion//UI callbacks


	#region Private
	void OnPageStartShowing() {
		// check disable the WeeklyToy session 
		var weeklyEventAvailable = weeklyEventStorage.IsEventAvailable(userCurrentLevelVar.Value);
		rootWeeklyToyStats.SetActive(weeklyEventAvailable);
	}

	void OnPageFinishOpening() {
		// show effect, also add to inventory
		var starsGain = CalculateStarGain();
		starChestInventory.AddStarsUserHasEarnMoreFromLevels(starsGain);
		ShowStarEffectAsync(starsGain).Forget();
	}

	async UniTask ShowStarEffectAsync(int starsGain) {
		// don't let user interact during effect time
		mainPage.CachedCanvasGroup.interactable = false;
		// delay a bit before showing stars
		await UniTask.Delay(100, cancellationToken: _token.Token);
		// show stars, one by one
		for(int n = 0; n < starsGain; n++) {
			var item = starList[n];
			item.ShowUp();
			await UniTask.Delay(TimeSpan.FromSeconds(intervalBetweenStar), cancellationToken: _token.Token);
		}
		// re-active input
		mainPage.CachedCanvasGroup.interactable = true;
	}

	int CalculateStarGain() {
		int result = 1;
		var remainTimePercent = (float)remainTimeInSecondVar.Value / (float)levelTimeVar.Value;
		if(remainTimePercent >= remainTimePercentAndStarGain[0]) {
			result = 3;
		}
		else if(remainTimePercent >= remainTimePercentAndStarGain[1]) {
			result = 2;
		}
		return result;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
