using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

///<summary> Attach to button on UI, config button to invoke this to request and handle Interstitial ads </summary>
public class InterstitialAdsButton : MonoBehaviour {
	#region Init, config
	[Header("User")]
	[SerializeField] UserRemoveAdsInventory userRemoveAdsInventory;

	[Header("ShareData, event in")]
	[SerializeField] AdsMediator adsMediator;
	[SerializeField] GameEvent interstitialAdsClosedEvent;

	[Header("Unity event out")]
	[SerializeField] UnityEvent adsClosedHandler;
	[SerializeField] UnityEvent adsNotReadyHandler;

	[Header("Refs")]
	[SerializeField] Button mainButton;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheMainButton();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void CacheMainButton() {
#if UNITY_EDITOR
		if(mainButton == null) {
			mainButton = GetComponent<Button>();
			if(mainButton != null) {
				UnityEditor.EditorUtility.SetDirty(this);
			}
		}
#endif//UNITY_EDITOR
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		mainButton.onClick.AddListener(OnPressButton);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		mainButton.onClick.RemoveListener(OnPressButton);
		StopListenToInterstitialAdsEvents();
	}
	#endregion// Init config


	#region Private
	void OnPressButton() {
		CheckShowInterstitialAds();
	}

	void CheckShowInterstitialAds() {
		// check user purchased remove ads
		if(userRemoveAdsInventory.HasPurchased()) {
			Log.Info("User has purchase RemoveAds, wont show Inter");
			return;
		}
		// check ads ready
		if(!adsMediator.IsInterstitialVideoAdsReady()) {
			Log.Info("InterAds not ready, request show when loaded");
			StartListenToInterstitialAdsEvents();
			adsMediator.ShowInterstitialVideoAds();
			return;
		}
		// listen to event from showAds, and un-register when watch success or failed
		Log.Info("Show InterAds");
		StartListenToInterstitialAdsEvents();
		adsMediator.ShowInterstitialVideoAds();
	}

	void StartListenToInterstitialAdsEvents() {
		StopListenToInterstitialAdsEvents();
		interstitialAdsClosedEvent.Subscribe(OnInterstialAdsClosed);
	}

	void StopListenToInterstitialAdsEvents() {
		interstitialAdsClosedEvent.Unsubscribe(OnInterstialAdsClosed);
	}

	void OnInterstialAdsClosed() {
		StopListenToInterstitialAdsEvents();
		adsClosedHandler.Invoke();
	}
	#endregion//Private
}
