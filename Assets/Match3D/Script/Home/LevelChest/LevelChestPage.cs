using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle logic for the LevelChest page
/// </summary>
public class LevelChestPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserLevelChestInventory levelChestInventory;
	[SerializeField] UserCoinInventory coinInventory;
	[SerializeField] UserLevelScoreInventory scoreInventory;
	[SerializeField] UserBoosterInventory boosterInventory;
	[SerializeField] BoosterToSpriteMapper boosterToSpriteMapper;

	[Header("Refs")]
	[SerializeField] GameObject notReadyContent;
	[SerializeField] GameObject readyContent;
	[SerializeField] TMP_Text reachLevelToOpenRewardText;

	[Header("Refs - reward text, icon")]
	[SerializeField] TMP_Text coinAmountText;
	[SerializeField] TMP_Text boosterAmountText;
	[SerializeField] Image boosterIconImage;

	[Header("Event out")]
	[SerializeField] GameEvent userClaimLevelChestEvent;

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


	#region UI callback
	///<summary> Config UIPage to invoke this when page open </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		ConfigContent();
	}

	///<summary> Config claim button to invoke this </summary>
	public void In_ClaimReward() {
		if(isFailedConfig)
			return;
		// claim reward
		coinInventory.IncreaseCoin(_coinRewardAmount);
		boosterInventory.AddBooster(_boosterRewardID, _boosterRewardAmount);
		// reset claim
		var currentLevel = scoreInventory.GetCurrentReachLevel();
		levelChestInventory.ResetClaimAndSaveLastPreviousLevel(currentLevel);
		// raise event
		userClaimLevelChestEvent.Raise();
	}
	#endregion//UI callback


	#region Private - config content
	int _coinRewardAmount;
	int _boosterRewardAmount;
	BoosterID _boosterRewardID;

	void ConfigContent() {
		var isClaimedPreviousBonus = levelChestInventory.IsClaimedPreviousBonus();
		// not claimed? then show the open button
		notReadyContent.SetActive(isClaimedPreviousBonus);
		readyContent.SetActive(!isClaimedPreviousBonus);
		// info text to let user know when to get the next reward
		if(!isClaimedPreviousBonus) {
			const string INFO_FORMAT = "Reach level {0} to open";
			var nextLevelToGetBonus = levelChestInventory.GetNextBonusLevel(scoreInventory.GetCurrentReachLevel());
			var text = string.Format(INFO_FORMAT, nextLevelToGetBonus);
			reachLevelToOpenRewardText.text = text;
		}
		// let random reward each time user open this popup
		ConfigRewardDisplay();
		// already claim, so in progress for current, config content
		if(isClaimedPreviousBonus) {
			var previousBonusLevel = levelChestInventory.GetPreviousBonusLevel();
			var currentLevel = scoreInventory.GetCurrentReachLevel();
			var levelPassed = currentLevel - previousBonusLevel;
			// passed enough level to claim
			var levelCountToGetChest = levelChestInventory.LevelCountToGetChest;
			if(levelPassed >= levelCountToGetChest) {
				notReadyContent.SetActive(false);
				readyContent.SetActive(true);
			}
		}
	}

	void ConfigRewardDisplay() {
		// get reward from inventory
		_coinRewardAmount = levelChestInventory.GetCoinReward();
		_boosterRewardAmount = levelChestInventory.GetBoosterRewardAmount();
		_boosterRewardID = levelChestInventory.GetRewardBoosterID();
		// display
		coinAmountText.text = $"+{_coinRewardAmount}";
		boosterAmountText.text = $"+{_boosterRewardAmount}";
		boosterIconImage.sprite = boosterToSpriteMapper.GetSprite(_boosterRewardID);
	}
	#endregion//Private - config content
}
