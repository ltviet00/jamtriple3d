using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

///<summary> Work in BoosterUI button ingame mainpage, config UI, handle click event </summary>
public class BoosterUIButton : MonoBehaviour {
	#region Init, config
	[Header("Refs - locked")]
	[SerializeField] GameObject rootLockedContent;
	[SerializeField] TMP_Text levelToUnlockText;

	[Header("Refs - unlocked")]
	[SerializeField] GameObject rootUnlockedContent;
	[SerializeField] Image iconImage;

	[Header("User")]
	[SerializeField] UserBoosterInventory userBoosterInventory;
	[SerializeField] IntegerVariable userReachedLevelVar;

	[Header("Config")]
	[SerializeField] IntegerVariable levelToUnlockVar;

	[Header("BoosterID and ShareVar")]
	[SerializeField] BoosterID boosterID;
	[SerializeField] BoosterIDVariable selectedBoosterVar;
	[SerializeField] BoosterToSpriteMapper boosterSpriteMapper;

	[Header("Command out")]
	[SerializeField] GameEvent cmdRequestBooster;

	[Header("Event out")]
	[SerializeField] GameEvent successUseBoosterEvent;
	//[SerializeField] UnityEvent OnUserOutOfBoosterEvent;

	[Header("Refs, config for coolDown")]
	[SerializeField] TimerIncludePause timerToReactiveButton;
	[SerializeField] FloatVariable coolDownDurationVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ActiveContentBaseOnUseLevel();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}

	//void Start() {
	//	if(isFailedConfig)
	//		return;
	//}
	#endregion// Init config


	#region Public - UI
	///<summary> Config booster button to invoke this </summary>
	public void In_OnPress() {
		if(isFailedConfig)
			return;
		// save select booster ID (booster affectir Or buy more booster both need it anyway)
		selectedBoosterVar.Value = boosterID;
		// check remain booster
		var boosterRemain = userBoosterInventory.GetBoosterRemain(boosterID);
		if(boosterRemain > 0) {
			userBoosterInventory.DeductBooster(boosterID);
			cmdRequestBooster.Raise();
			successUseBoosterEvent.Raise();
			// cooldown
			timerToReactiveButton.Process(coolDownDurationVar.Value);
		}
		// out of booster, popup to let user buy more
		else {
			//OnUserOutOfBoosterEvent.Invoke();
			var buyBoosterPage = SimpleDI.Instance.Get<BuyMorePreboosterPage>();
			buyBoosterPage.ConfigContentForBooster(boosterID);
			buyBoosterPage.Page.UI_KeepCurrentAndOpenMe();
		}
	}
	#endregion//Public - UI


	#region Private
	void ActiveContentBaseOnUseLevel() {
		// active root content
		var locked = userReachedLevelVar.Value < levelToUnlockVar.Value;
		rootLockedContent.SetActive(locked);
		rootUnlockedContent.SetActive(!locked);
		// other content
		if(locked) {
			// +1 cause level start from 0
			const string FORMAT = "LVL {0}";
			var text = string.Format(FORMAT, levelToUnlockVar.Value + 1);
			levelToUnlockText.text = text;
		}
		else {
			UpdateSpriteIcon();
		}
	}

	void UpdateSpriteIcon() {
		var sprite = boosterSpriteMapper.GetSprite(boosterID);
		if(sprite != null) {
			iconImage.sprite = sprite;
		}
	}
	#endregion//Private
}
