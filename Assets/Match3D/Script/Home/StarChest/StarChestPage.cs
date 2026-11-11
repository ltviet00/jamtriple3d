using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarChestPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserStarChestInventory starChestInventory;
	[SerializeField] UserCoinInventory coinInventory;
	[SerializeField] UserBoosterInventory boosterInventory;
	[SerializeField] BoosterToSpriteMapper boosterToSpriteMapper;

	[Header("Refs")]
    [SerializeField] GameObject notReadyContent;
    [SerializeField] GameObject readyContent;
	[SerializeField] ImageSlicePercentAnimator starProgressBar;
	[SerializeField] TMP_Text starProgerssText;

	[Header("Refs - reward text, icon")]
	[SerializeField] TMP_Text coinAmountText;
	[SerializeField] TMP_Text boosterAmountText;
	[SerializeField] Image boosterIconImage;

	[Header("Event out")]
	[SerializeField] GameEvent userClaimStarChestEvent;

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
	public void In_OnPressClaim() {
		if(isFailedConfig)
			return;
		// claim reward
		coinInventory.IncreaseCoin(_coinRewardAmount);
		boosterInventory.AddBooster(_boosterRewardID, _boosterRewardAmount);
		// reset star
		starChestInventory.ResetSavedStars();
		// raise event
		userClaimStarChestEvent.Raise();
	}
	#endregion//UI callback


	#region Private - config content
	int _coinRewardAmount;
	int _boosterRewardAmount;
	BoosterID _boosterRewardID;

	void ConfigContent() {
		var isChestFull = starChestInventory.IsAlreadyFulled();
		notReadyContent.SetActive(!isChestFull);
		readyContent.SetActive(isChestFull);
		// full, config reward content
		if(isChestFull) {
			ConfigRewardDisplay();
		}
		// inprogress, config text
		else {
			ConfigProgressDisplay();
		}
	}

	void ConfigProgressDisplay() {
		// fill bar
		var percent = starChestInventory.GetPercentOfStarInChest();
		starProgressBar.SetProgress(percent);
		// text
		var text = $"{starChestInventory.GetStarSavedInChest()}/{starChestInventory.StarChestCapacity}";
		starProgerssText.text = text;
	}

	///<summary> Let config reward each time user open this popup </summary>
	void ConfigRewardDisplay() {
		// get reward from inventory
		_coinRewardAmount = starChestInventory.GetCoinReward();
		_boosterRewardAmount = starChestInventory.GetBoosterRewardAmount();
		_boosterRewardID = starChestInventory.GetRewardBoosterID();
		// display
		coinAmountText.text = $"+{_coinRewardAmount}";
		boosterAmountText.text = $"+{_boosterRewardAmount}";
		boosterIconImage.sprite = boosterToSpriteMapper.GetSprite(_boosterRewardID);
	}
	#endregion//Private - config content
}
