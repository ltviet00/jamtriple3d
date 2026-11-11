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
	void ClearToyBox() {
		var clearBoxAffector = SimpleDI.Instance.Get<ToyBoxClearAffector>();
		if(clearBoxAffector != null) {
			clearBoxAffector.ClearBox();
		}
	}
	#endregion//Private
}
