using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle logic, UI for Booster button in LevelInfo page
/// </summary>
public class NextLevelPageBoosterButton : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] BoosterID boosterID;
	[SerializeField] UserBoosterInventory userBoosterInventory;
	[SerializeField] BoosterToSpriteMapper boosterSpriteMapper;
	[SerializeField] IntegerVariable levelRequireToUnlockBoosterVar;
	[SerializeField] IntegerVariable userCurrentLevelVar;

	[Header("Data - active")]
	[SerializeField] BooleanVariable actiavateBoosterVar;

	[Header("Refs - locked")]
	[SerializeField] TMP_Text levelRequireText;
	[SerializeField] GameObject lockedContent;

	[Header("Refs - unlocked")]
	[SerializeField] Button mainButton;
	[SerializeField] GameObject unlockedContent;
	[SerializeField] GameObject pickedObject;
	[SerializeField] GameObject remainBoosterObject;

	[SerializeField] Image boosterIcon;
	[SerializeField] TMP_Text remainBoosterText;

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


	#region Public UI
	///<summary> Config UIPage or other component to invoke this when open this UI, to update content follow user level </summary>
	public void In_ConfigContentFollowUserLevel() {
		if(isFailedConfig)
			return;
		// reset state when page open
		_isUsedBooster = false;
		ConfigContentFollowUserCurrentLevel();
	}

	///<summary> If user toogle on booster, but close popup, then we not deduct in inventory.
	///Only deduct when user press Play, so call this function when user press Play (call from NextLevelPage)</summary>
	public void DeductBoosterInUserInventoryIfUserSelectBooster() {
		if(isFailedConfig)
			return;
		if(_isUsedBooster) {
			// save to inventory
			userBoosterInventory.DeductBooster(boosterID);
			// save to share var
			actiavateBoosterVar.Value = true;
		}
		else {
			actiavateBoosterVar.Value = false;
		}
	}
	#endregion//Public UI


	#region Private
	///<summary> True when user toggle select this booster to be used during level </summary>
	bool _isUsedBooster = false;

	void ConfigContentFollowUserCurrentLevel() {
		var unlocked = userCurrentLevelVar.Value >= levelRequireToUnlockBoosterVar.Value;
		//var enoughBooster = userBoosterInventory.GetBoosterRemain(boosterID) > 0;
		lockedContent.SetActive(!unlocked);
		unlockedContent.SetActive(unlocked);
		mainButton.interactable = unlocked;

		if(unlocked) {
			ConfigContentWhenUnlocked();
			// main button interaction. carefully remove listener first
			mainButton.onClick.RemoveListener(OnUserPressButton);
			mainButton.onClick.AddListener(OnUserPressButton);
		}
		else {
			levelRequireText.text = $"Lvl {levelRequireToUnlockBoosterVar.Value + 1}";
		}
	}

	void OnUserPressButton() {
		// if enoug booster, then toggle selection
		var enoughBooster = userBoosterInventory.GetBoosterRemain(boosterID) > 0;
		if(enoughBooster) {
			_isUsedBooster = !_isUsedBooster;
			ConfigContentWhenUnlocked();
		}
		// not enough booster, open popup to let user buy more
		else {
			var buyBoosterPage = SimpleDI.Instance.Get<BuyMorePreboosterPage>();
			buyBoosterPage.ConfigContentForBooster(boosterID);
			buyBoosterPage.Page.UI_KeepCurrentAndOpenMe();
		}
	}

	void ConfigContentWhenUnlocked() {
		pickedObject.SetActive(_isUsedBooster);
		remainBoosterObject.SetActive(!_isUsedBooster);
		remainBoosterText.text = userBoosterInventory.GetBoosterRemain(boosterID).ToString();
		boosterIcon.sprite = boosterSpriteMapper.GetSprite(boosterID);
	}
	#endregion//Private
}
