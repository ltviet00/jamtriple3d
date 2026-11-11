using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

///<summary> Base class for Adscontroller: UnityAds, AdMob </summary>
public abstract class BaseAdsController : ScriptableObject {
	[Header("Name - use for switching using remote config ")]
	// name, use for filter
	public string identicalName;
	// init
	public abstract void Init();
	// banner
	public abstract bool IsBannerReady();
	public abstract void LoadBanner();
	public abstract void ShowBanner();
	public abstract void HideBanner();
	// reward
	public abstract bool IsRewardVideoReady();
	public abstract void LoadRewardVideo();
	public abstract void ShowRewardVideo();
	// video interstitial
	public abstract bool IsVideoReady();
	public abstract void LoadVideo();
	public abstract void ShowVideo();

	// general checking 
	public bool IsAdsTypeReady(AdsMediator.AdsType adsType) {
		switch(adsType) {
			case AdsMediator.AdsType.Banner:
				return IsBannerReady();

			case AdsMediator.AdsType.RewardVideo:
				return IsRewardVideoReady();

			case AdsMediator.AdsType.InterstitialVideo:
				return IsVideoReady();

			default:
				Log.Warning(false, this, $"Not handle adsType [{adsType}]");
				return false;
		}
	}

	public void LoadAdsType(AdsMediator.AdsType adsType) {
		switch(adsType) {
			case AdsMediator.AdsType.Banner:
				LoadBanner();
				return;

			case AdsMediator.AdsType.RewardVideo:
				LoadRewardVideo();
				return;

			case AdsMediator.AdsType.InterstitialVideo:
				LoadVideo();
				return;

			default:
				Log.Warning(false, this, $"Not handle adsType [{adsType}]");
				return;
		}
	}
	
	// reward ads event
	public event Action onRewardAdsFinishHandler;
	public event Action onRewardAdsFailedHandler;
	protected void InvokeRewardFinishEvent() {
		onRewardAdsFinishHandler?.Invoke();
	}
	protected void InvokeRewardFailedEvent() {
		onRewardAdsFailedHandler?.Invoke();
	}

	// video ads event
	public event Action onInterstitialAdsCloseHandler;
	protected void InvokeInterstitialAdsClose() {
		onInterstitialAdsCloseHandler?.Invoke();
	}
}
