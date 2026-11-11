using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle sprite for Weekly reward on UIBar at home (and for WeeklyEventPage)
/// </summary>
public class WeeklyEventRewardIcon : MonoBehaviour {
	#region Init, config
	[Header("Data - icon mapper")]
	[SerializeField] WeeklyEventRewardIconMapper rewardIconMapper;
	[SerializeField] BoosterToSpriteMapper boosterIconMapper;
	[SerializeField] WeeklyToyIdToSpriteMapper weeklyToySpriteMapper;

	[Header("Data")]
	[SerializeField] WeeklyEventDataSaver weeklyEventData;
	[SerializeField] ToyStorage toyStorage;

	[Header("Refs - reward details")]
	[SerializeField] Image rewardIconImage;
	[SerializeField] TMP_Text rewardQuantityText;

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


	#region Public
	///<summary> Set the WeeklyRewardDefine, it will lookup for sprite and config display (Image, Text) </summary>
	public void ConfigDisplayForReward(WeeklyEventRewardDefine reward) {
		if(isFailedConfig)
			return;
		// sprite icon first
		Sprite icon = null;
		switch(reward.rewardType) {
			// special case : double event toy
			case WeeklyEventRewardDefine.RewardType.DoubleWeeklyToyCollectAmount:
				var toyID = toyStorage.GetWeeklyEventToyIdWithName(weeklyEventData.Data.selectedToyName);
				icon = weeklyToySpriteMapper.GetSprite(toyID);
				break;

			case WeeklyEventRewardDefine.RewardType.IngameBooster:
				icon = boosterIconMapper.GetSprite(reward.boosterID);
				break;
			// other case, get from rewardIconMapper
			default:
				icon = rewardIconMapper.GetIcon(reward.rewardType);
				break;
		}
		// set sprite for icon
		if(icon != null) {
			rewardIconImage.sprite = icon;
		}
		else {
			Log.Warning(false, this, $"Not found icon for reward [{reward.rewardType}]");
		}

		// quantity text depend on reward type
		switch(reward.rewardType) {
			case WeeklyEventRewardDefine.RewardType.Coin:
			case WeeklyEventRewardDefine.RewardType.IngameBooster:
			case WeeklyEventRewardDefine.RewardType.PregameBoosterForDuration_ExtraTime:
			//case WeeklyEventRewardDefine.RewardType.PregameBoosterForDuration_Fan:
			case WeeklyEventRewardDefine.RewardType.PregameBoosterForDuration_LightingStorm:
                const string PREFIX_X_FORMAT = "x{0}";
                rewardQuantityText.text = string.Format(PREFIX_X_FORMAT, reward.quantity);
				break;

			case WeeklyEventRewardDefine.RewardType.DoubleWeeklyToyCollectAmount:
			case WeeklyEventRewardDefine.RewardType.UnlimitLifeForDuration:
				const string SUFFIX_M_FORMAT = "{0}m";
				rewardQuantityText.text = string.Format(SUFFIX_M_FORMAT, reward.duration);
				break;

			default:
				Log.Warning(false, this, $"Not handle for rewardType [{reward.rewardType}], disable icon");
				rewardIconImage.enabled = false;
				break;
		}
	}
	#endregion//Public
}
