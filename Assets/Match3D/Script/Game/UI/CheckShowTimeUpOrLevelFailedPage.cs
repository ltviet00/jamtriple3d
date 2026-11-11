using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CheckShowTimeUpOrLevelFailedPage will write this. When timeup, if user not use coin or ads to revive, then open the TimeUp page, if not, open LevelFailed popup
/// </summary>
public class CheckShowTimeUpOrLevelFailedPage : MonoBehaviour {
	#region Init, config
	[Header("Data, event in")]
	[SerializeField] BooleanVariable hasUserUseCoinOrAdsToReviveVar;
	[SerializeField] GameEvent timeUpEvent;

	[Header("Unity event out")]
	[SerializeField] UnityEvent openTimeUpPageCmd;
	[SerializeField] UnityEvent openLevelFailedPageCmd;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// event and reset var at first
		hasUserUseCoinOrAdsToReviveVar.Value = false;
		timeUpEvent.Subscribe(OnTimeUp);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		timeUpEvent.Unsubscribe(OnTimeUp);
	}
	#endregion// Init config


	#region Private
	void OnTimeUp() {
		//var eventToRaise = hasUserUseCoinOrAdsToReviveVar.Value ? openLevelFailedPageCmd : openTimeUpPageCmd;
		//eventToRaise.Invoke();
		//// sure after this one, user either choose revive or not. so turn off the flag
		//hasUserUseCoinOrAdsToReviveVar.Value = true;

		// UPDATE logic : now unlimit watchAds or UseCoin to revive
		openTimeUpPageCmd.Invoke();
	}
	#endregion//Private
}
