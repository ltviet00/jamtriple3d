using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OutOfBoxPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserWatchAdsToReviveInventory userWatchAdsReviveInventory;
	//[SerializeField] BooleanVariable hasUserUseCoinOrAdsToReviveVar;
	[SerializeField] IntegerVariable coinToReviveVar;

	//[Header("Refs")]
	//[SerializeField] GameObject watchAdsToReviveContent;
	//[SerializeField] GameObject useCoinToReviveContent;

	//[Header("Event out")]
	//[SerializeField] GameEvent userClearBoxesEvent;
	//[SerializeField] GameEvent userLoseEvent;

	[Header("Unity Event out")]
	[SerializeField] UnityEvent openShopPageCmd;
	[SerializeField] UnityEvent closeSelfCmd;
	//[SerializeField] UnityEvent openLoseGamePageCmd;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		RefreshFreeClearLabel();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region UI input
	///<summary> Config the UIPage to invoke this </summary>
	public void In_OnPageStartOpen() {
		if(isFailedConfig)
			return;
		//bool stillAdsRemainToRevive = !userWatchAdsReviveInventory.IsReachFreeAdsToRevivePerDay();
		//watchAdsToReviveContent.SetActive(stillAdsRemainToRevive);
		//useCoinToReviveContent.SetActive(!stillAdsRemainToRevive);
	}

	///<summary> Config watch ads button to invoke this when user watch ads success </summary>
	public void In_WatchAdsSuccessToClearBoxes() {
		if(isFailedConfig)
			return;
		// reduce ads watched count, raise event
		userWatchAdsReviveInventory.IncreaseAdsWatched();
		//userClearBoxesEvent.Raise();
		ClearToyBox();
		closeSelfCmd.Invoke();
	}

	///<summary> Config the useCoin to revive button to invoke this </summary>
	public void In_PressUseCoinToClearBoxes() {
		if(isFailedConfig)
			return;
		// first clear in this level is free (like the reference video)
		if(!_freeClearUsed) {
			_freeClearUsed = true;
			closeSelfCmd.Invoke();
			ClearToyBox();
			RefreshFreeClearLabel();
			return;
		}
		// enough coin? then reduce coin, raise event revive, close self
		if(userCoinInventory.IsEnough(coinToReviveVar.Value)) {
			userCoinInventory.DecreaseCoin(coinToReviveVar.Value);
			closeSelfCmd.Invoke();
			//userClearBoxesEvent.Raise();
			ClearToyBox();
		}
		// not enough coin -> open Shop
		else {
			openShopPageCmd.Invoke();
		}
	}

	/////<summary> Config the Close to revive button to invoke this </summary>
	//public void In_OnUserPressClose() {
	//	if(isFailedConfig)
	//		return;
	//	// choose close mean don't want to survive by coin or ads, then lose game
	//	//userLoseEvent.Raise();
	//}
	#endregion//UI input


	#region Private
	///<summary> The page live inside Game scene, scene reload every level, so this reset every level </summary>
	bool _freeClearUsed;
	const string COIN_PRICE_TEXT_NAME = "CoinAmountText - follow var";
	const string COIN_ICON_NAME = "CoinIcon";
	const string FREE_TEXT = "FREE";

	///<summary> Show "FREE" instead of the coin price while the free clear is still available </summary>
	void RefreshFreeClearLabel() {
		var isFree = !_freeClearUsed;
		foreach(var follower in GetComponentsInChildren<IntegerVariableToTextMeshUI>(true)) {
			if(follower.name != COIN_PRICE_TEXT_NAME)
				continue;
			follower.enabled = !isFree;
			var text = follower.GetComponent<TMPro.TMP_Text>();
			if(isFree && text != null)
				text.text = FREE_TEXT;
			var coinIcon = follower.transform.parent != null ? follower.transform.parent.Find(COIN_ICON_NAME) : null;
			if(coinIcon != null)
				coinIcon.gameObject.SetActive(!isFree);
		}
	}

	void ClearToyBox() {
		var clearBoxAffector = SimpleDI.Instance.Get<ToyBoxClearAffector>();
		if(clearBoxAffector != null) {
			clearBoxAffector.ClearBox();
		}
		// timer was stopped when the bar got full, resume it
		var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
		if(levelTimer != null) {
			levelTimer.ResumeAfterBarCleared();
		}
	}
	#endregion//Private
}
