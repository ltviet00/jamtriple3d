using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Attach to button on UI, config button to invoke this to request and handle reward ads </summary>
public class WatchRewardAdsButton : MonoBehaviour {
	#region Init, config
	[Header("ShareData, event in")]
	[SerializeField] AdsMediator adsMediator;
	[SerializeField] GameEvent rewardAdsFinishEvent;
	[SerializeField] GameEvent rewardAdsFailedEvent;

	[Header("Unity event out")]
	[SerializeField] UnityEvent adsNotReadyEvent;
	[SerializeField] UnityEvent adsFailedEvent;
	[SerializeField] UnityEvent adsSuccessEvent;

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
		StopListenToRewardAdsEvents();
	}
	#endregion// Init config


	#region Public - UI
	///<summary> Config UI button to invoke this </summary>
	public void In_OnPressButton() {
		if(isFailedConfig)
			return;
		CheckShowRewardAds();
	}
	#endregion//Public - UI


	#region Private
	void CheckShowRewardAds() {
		if(!adsMediator.IsRewardVideoAdsReady()) {
			adsNotReadyEvent.Invoke();
			return;
		}
		// listen to event from showAds, and un-register when watch success or failed
		StartListenToRewardAdsEvents();
		adsMediator.ShowRewardVideoAds();
	}

	void StartListenToRewardAdsEvents() {
		StopListenToRewardAdsEvents();
		rewardAdsFailedEvent.Subscribe(OnWatchAdsFailed);
		rewardAdsFinishEvent.Subscribe(OnWatchAdsSucces);
	}

	void StopListenToRewardAdsEvents() {
		rewardAdsFailedEvent.Unsubscribe(OnWatchAdsFailed);
		rewardAdsFinishEvent.Unsubscribe(OnWatchAdsSucces);
	}

	void OnWatchAdsSucces() {
		StopListenToRewardAdsEvents();
		adsSuccessEvent.Invoke();
	}

	void OnWatchAdsFailed() {
		StopListenToRewardAdsEvents();
		adsFailedEvent.Invoke();
	}
	#endregion//Private
}
