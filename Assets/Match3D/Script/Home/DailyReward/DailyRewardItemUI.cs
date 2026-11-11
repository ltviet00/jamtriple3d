using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle logic display for daily reward item on DailyRewardPage
/// </summary>
public class DailyRewardItemUI : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] TMP_Text dayIndexText;
	[SerializeField] GameObject readyToCollectContent;
	[SerializeField] GameObject collectedContent;
	[SerializeField] GameObject notReadyToCollectContent;

	[Header("Refs - reward details")]
	[SerializeField] Image rewardIconImage;
	[SerializeField] TMP_Text rewardQuantityText;

	[Header("Config")]
	[SerializeField] int dayIndex;

	[Header("User inventory")]
	[SerializeField] UserDailyRewardInventory userDailyInventory;
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserUnlimitEnergyInventory userUnlimitEnergyInventory;
	[SerializeField] UserBoosterInventory userBoosterInventory;

	[Header("Data")]
	[SerializeField] DailyRewardStorage dailyRewardStorage;
	[SerializeField] WeeklyEventRewardIconMapper rewardIconMapper;
	[SerializeField] BoosterToSpriteMapper boosterIconMapper;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ConfigContentFollowUserData();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Public - UI callback
	///<summary>  </summary>
	public void In_OnPressCollected() {
		if(isFailedConfig)
			return;
		AddRewardToUserInventory();
		ConfigContentFollowUserData();
	}
	#endregion//Public - UI callback


	#region Public
	///<summary> Config ready, collected of not ready content </summary>
	public void ConfigContentFollowUserData() {
		if(isFailedConfig)
			return;
		var currentProgress = userDailyInventory.GetCurrentProgressDate();
		const string DAY_INDEX_FORMAT = "DAY {0}";
		dayIndexText.text = string.Format(DAY_INDEX_FORMAT, dayIndex + 1);
		// condition to show each content
		var isClaimedCurrentReward = userDailyInventory.IsUserClaimTheCurrentDayReward();
		var showCollected = currentProgress > dayIndex || (currentProgress == dayIndex && isClaimedCurrentReward);
		var showReady = currentProgress == dayIndex && !isClaimedCurrentReward;
		var showNotReady = currentProgress < dayIndex;

		collectedContent.SetActive(showCollected);
		readyToCollectContent.SetActive(showReady);
		notReadyToCollectContent.SetActive(showNotReady);

		// show reward icon ,text only when user collected
		if(showCollected) {
			ConfigRewardIconAndText();
		}
	}
	#endregion//Public


	#region Private
	void ConfigRewardIconAndText() {
		// icon
		var reward = dailyRewardStorage.GetRewardForDay(dayIndex);
		var icon = reward.rewardType == WeeklyEventRewardDefine.RewardType.IngameBooster
			? boosterIconMapper.GetSprite(reward.boosterID)
			: rewardIconMapper.GetIcon(reward.rewardType);
		rewardIconImage.sprite = icon;
		// quantity text depend on reward type
		switch(reward.rewardType) {
			case WeeklyEventRewardDefine.RewardType.Coin:
				const string COIN_FORMAT = "x{0}";
				rewardQuantityText.text = string.Format(COIN_FORMAT, reward.quantity);
				break;

			case WeeklyEventRewardDefine.RewardType.UnlimitLifeForDuration:
				const string LIVE_FORMAT = "{0}m";
				rewardQuantityText.text = string.Format(LIVE_FORMAT, reward.duration);
				break;

			case WeeklyEventRewardDefine.RewardType.IngameBooster:
				const string BOOSTER_FORMAT = "x{0}";
				rewardQuantityText.text = string.Format(BOOSTER_FORMAT, reward.quantity);
				break;

			default:
				Log.Warning(false, this, $"Not handle for rewardType [{reward.rewardType}], disable icon");
				rewardIconImage.enabled = false;
				break;
		}
	}

	void AddRewardToUserInventory() {
		var reward = dailyRewardStorage.GetRewardForDay(dayIndex);
		switch(reward.rewardType) {
			case WeeklyEventRewardDefine.RewardType.Coin:
				userCoinInventory.IncreaseCoin(reward.quantity);
				break;

			case WeeklyEventRewardDefine.RewardType.UnlimitLifeForDuration:
				userUnlimitEnergyInventory.ExpandUnlimitDurationByMinutes(reward.duration);
				break;

			case WeeklyEventRewardDefine.RewardType.IngameBooster:
				userBoosterInventory.AddBooster(reward.boosterID, reward.quantity);
				break;

			default:
				Log.Warning(false, this, $"Not handle for rewardType [{reward.rewardType}], disable icon");
				break;
		}
		// mark claim reward and re-config icon
		userDailyInventory.MarkUserClaimCurrentReward();
	}
	#endregion//Private
}
