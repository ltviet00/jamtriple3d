using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// At main page, there a curve, not sure what the usage in orignal game, but looks like it follow the starChest progress
/// </summary>
public class LevelChestProgressImage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserLevelChestInventory userLevelChestInventory;
	[SerializeField] UserLevelScoreInventory userScoreInventory;

	[Header("Refs")]
	[SerializeField] Image progressImage;

	[Header("Event in")]
	[SerializeField] GameEvent userClaimLevelChestEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		userClaimLevelChestEvent.Subscribe(ConfigDisplay);
		ConfigDisplay();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		userClaimLevelChestEvent.Unsubscribe(ConfigDisplay);
	}
	#endregion// Init config


	#region Private
	void ConfigDisplay() {
		var isClaimedPreviousBonus = userLevelChestInventory.IsClaimedPreviousBonus();
		// already claim, so in progress for current, config content
		var levelCountToGetChest = userLevelChestInventory.LevelCountToGetChest;
		if(isClaimedPreviousBonus) {
			var previousBonusLevel = userLevelChestInventory.GetPreviousBonusLevel();
			var currentLevel = userScoreInventory.GetCurrentReachLevel();
			var levelPassed = currentLevel - previousBonusLevel;
			var progressInPercent = (float)levelPassed / (float)levelCountToGetChest;
			progressImage.fillAmount = progressInPercent;
		}
		else {
			progressImage.fillAmount = 1f;
		}
	}
	#endregion//Private
}
