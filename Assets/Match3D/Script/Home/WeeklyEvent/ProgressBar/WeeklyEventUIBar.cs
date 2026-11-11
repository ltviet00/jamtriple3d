using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Handle logic display and VFX for the WeeklyEvent bar at mainpage
/// </summary>
public class WeeklyEventUIBar : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] WeeklyEventDataSaver dataSaver;
	[SerializeField] WeeklyEventRewardStorage rewardStorage;
	[SerializeField] WeeklyEventRewardIconMapper iconMapper;

	[Header("Inventory")]
	[SerializeField] UserWeeklyToyInventory userWeeklyInventory;
	[SerializeField] UserLevelScoreInventory scoreInventory;
	[SerializeField] UserCoinInventory coinInventory;
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;
	[SerializeField] UserBoosterInventory boosterInventory;
	[SerializeField] UserDoubleEventToyInventory doubleEventToyInventory;

	[Header("Refs")]
	[SerializeField] Button mainButton;
	[SerializeField] GameObject lockedContent;
	[SerializeField] GameObject unlockedContent;

	[Header("Refs - Locked content")]
	[SerializeField] TMP_Text lockedToLevelText;

	[Header("Refs - Inprogress content")]
	[SerializeField] WeeklyEventProgressBarUI progressBar;
	[SerializeField] WeeklyEventRewardIcon rewardIcon;
	[SerializeField] UIElement collectMoreToyAnimator;
	[SerializeField] TMP_Text collectMoreToyText;

	[Header("Config animation")]
	[SerializeField] AnimationDurationConfig animDuration;

	[Header("Unity event out")]
	[SerializeField] UnityEvent openLockedEventPopupHandler;
	[SerializeField] UnityEvent openInprogressEventPopupHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
		userWeeklyInventory.CheckUnlockStatusAndSaveUnlockDate();
		ConfigContentOnEnable();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> New logic, at begin of home scene, wait for levelchest, starChest, Weekly event bar .. to show fill up effect, allow user input later </summary>
	public async UniTask PlayFillUpEffect() {
		if(isFailedConfig)
			return;
		// fillup. if no special toy collected, then do nothing
		var toyCollectMore = userWeeklyInventory.Data.collectedMoreFromLevels;
		if(toyCollectMore <= 0) {
			return;
		}
		// disable main button until finishing all VFX
		mainButton.interactable = false;
		// config text and play collectMoretoy animation
		await ShowCollectMoreToyIconAnmiation(toyCollectMore);

		// now bring collectMore to itemAmont till it out
		while(toyCollectMore > 0) {
			var currentMilestone = userWeeklyInventory.Data.currentMilestone;
			var rewardData = rewardStorage.GetRewardDataForMilestone(currentMilestone);
			var toyRequireToGetReward = rewardData.toyCollectRequire;
			var currentToyInInventory = userWeeklyInventory.Data.collectedItemAmount;

			// eg: got require 10, current got 5, and collect 8 more.
			// Then we move 5 (from 8) to inventory, show progress + collect reward

			// first case: move collectMoreToy to inventory, still not enough to get reward
			if(toyCollectMore + currentToyInInventory < toyRequireToGetReward) {
				await progressBar.PlayFillUpAniamtion(toyRequireToGetReward, currentToyInInventory, toyCollectMore + currentToyInInventory, animDuration.fillUpBar);
				userWeeklyInventory.MoveCollectMoreFromLevelToAmount(toyCollectMore);
				break;
			}
			// case: user will collect reward
			else {
				// play fill full bar. Reduce the toyCollectMore for next fill up after this
				toyCollectMore -= toyRequireToGetReward - currentToyInInventory;
				await progressBar.PlayFillUpAniamtion(toyRequireToGetReward, currentToyInInventory, toyRequireToGetReward, animDuration.fillUpBar);

				// play effect show up reward. let simply zoom in the reward icon. delay a bit
				LeanTween.scale(rewardIcon.gameObject, Vector3.one * 2f, 0.5f).setEase(LeanTweenType.easeOutElastic);
				await UniTask.Delay(1500, cancellationToken: _disableToken.Token);
				// save reward to inventory, increase milestone
				AddRewardToUserInventory(rewardData);
				userWeeklyInventory.Data.collectedMoreFromLevels = toyCollectMore;
				userWeeklyInventory.Data.collectedItemAmount = 0;
				currentMilestone++;

				// if milestone > total, mean final reward collected, then break
				if(currentMilestone >= rewardStorage.TotalMilestone) {
					break;
				}
				// still got milestone to collect? show up new reward
				userWeeklyInventory.Data.currentMilestone = currentMilestone;
				// scale down the reward icon, 
				LeanTween.scale(rewardIcon.gameObject, Vector3.zero, 0.5f).setEase(LeanTweenType.easeInBack);
				await UniTask.Delay(500, cancellationToken: _disableToken.Token);
				// config new display, and zoom it out again
				var nextReward = rewardStorage.GetRewardDataForMilestone(currentMilestone);
				rewardIcon.ConfigDisplayForReward(nextReward);
				LeanTween.scale(rewardIcon.gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutElastic);
				await UniTask.Delay(500, cancellationToken: _disableToken.Token);
				// reset progress
				progressBar.SetProgress(0, nextReward.toyCollectRequire);
			}
		}
		// relase the main button
		mainButton.interactable = true;
	}
	#endregion//Public


	#region Public - UI callbacks
	///<summary> Config button to invoke this (will request lock/unlock popup) </summary>
	public void In_OnClick() {
		if(isFailedConfig)
			return;
		var isLocked = userWeeklyInventory.Data.isLocked;
		var eventToRaise = isLocked ? openLockedEventPopupHandler : openInprogressEventPopupHandler;
		eventToRaise.Invoke();
	}
	#endregion//Public - UI callbacks


	#region Private
	///<summary> Some content need to be config at OnEnable, before any animation come </summary>
	void ConfigContentOnEnable() {
		var isLocked = userWeeklyInventory.Data.isLocked;
		lockedContent.SetActive(isLocked);
		unlockedContent.SetActive(!isLocked);
		// lock/unlocked content
		if(isLocked) {
			ConfigLockedcontent();
		}
		else {
			ConfigInProgressContent();
			// disable the collectMoreToy VFX by default
			collectMoreToyAnimator.gameObject.SetActive(false);
		}
	}

	void ConfigLockedcontent() {
		const string LOCK_TO_LEVEL_FORMAT = "Unlock at level {0}";
		// +1, cause level start from 0. To display, plus 1
		var text = string.Format(LOCK_TO_LEVEL_FORMAT, rewardStorage.LevelToUnlock + 1);
		lockedToLevelText.text = text;
	}

	void ConfigInProgressContent() {
		// reward icon
		var currentMilestone = userWeeklyInventory.Data.currentMilestone;
		var reward = rewardStorage.GetRewardDataForMilestone(currentMilestone);
		rewardIcon.ConfigDisplayForReward(reward);
		// progress bar
		var collectedAmount = userWeeklyInventory.Data.collectedItemAmount;
		var capacity = reward.toyCollectRequire;
		progressBar.SetProgress(collectedAmount, capacity);
	}

	///<summary> Show star collect more VFX, then fill bar up animation </summary>
	async UniTask ShowCollectMoreToyIconAnmiation(int toyCollectMore) {
		collectMoreToyText.text = $"+{toyCollectMore}";
		// show toy collect first.
		collectMoreToyAnimator.gameObject.SetActive(true);
		collectMoreToyAnimator.Show();
		await UniTask.Delay(TimeSpan.FromSeconds(collectMoreToyAnimator.GetShowDuration()), cancellationToken: _disableToken.Token);
		// delay abit more before hide
		await UniTask.Delay(400, cancellationToken: _disableToken.Token);
		collectMoreToyAnimator.Hide();
		await UniTask.Delay(TimeSpan.FromSeconds(collectMoreToyAnimator.GetHideDuration()), cancellationToken: _disableToken.Token);
		collectMoreToyAnimator.gameObject.SetActive(false);
	}

	void AddRewardToUserInventory(WeeklyEventRewardDefine reward) {
		switch(reward.rewardType) {
			case WeeklyEventRewardDefine.RewardType.Coin:
				coinInventory.IncreaseCoin(reward.quantity);
				break;

			case WeeklyEventRewardDefine.RewardType.UnlimitLifeForDuration:
				unlimitEnergyInventory.ExpandUnlimitDurationByMinutes(reward.duration);
				break;

			case WeeklyEventRewardDefine.RewardType.IngameBooster:
			case WeeklyEventRewardDefine.RewardType.PregameBoosterForDuration_ExtraTime:
			//case WeeklyEventRewardDefine.RewardType.PregameBoosterForDuration_Fan:
			case WeeklyEventRewardDefine.RewardType.PregameBoosterForDuration_LightingStorm:
                boosterInventory.AddBooster(reward.boosterID, reward.quantity);
				break;

			case WeeklyEventRewardDefine.RewardType.DoubleWeeklyToyCollectAmount:
				doubleEventToyInventory.ExpandDurationByMinutes(reward.duration);
				break;

			default:
				Log.Warning(false, this, $"Not handle for rewardType [{reward.rewardType}], can't add to user inventory");
				break;
		}
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	public class AnimationDurationConfig {
		public float fillUpBar;
	}
	#endregion//Classes
}
