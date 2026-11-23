using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

///<summary> Init AdsMediator, keep reference to them </summary>
public class AdsMediatorAwaker : MonoBehaviour {
	#region Init, config
	[Header("Refs, config")]
	[SerializeField] AdsMediator mediator;
	//[SerializeField] float delayBeforeCallPreloadAds = 3f;

	[Header("Banner")]
	[SerializeField] bool autoShowBannerAfterInit = true;
	[SerializeField] float bannerShowRetryDelaySeconds = 2f;
	[SerializeField] int bannerShowRetryCount = 10;

	[Header("Banner space - UI keeps room for the large anchored banner (SafeAreaAdjuster.reserveBottomForBanner)")]
	[SerializeField] UserRemoveAdsInventory removeAdsInventory;
	[Tooltip("Large anchored banner height (dp) to reserve before any banner has loaded")]
	[SerializeField] float fallbackBannerHeightDp = 150f;
	[SerializeField] float bannerSpaceUpdateInterval = 1f;
	[Tooltip("Every N space updates, show the banner again if it is ready (late loads, after fullscreen ads). Show is idempotent")]
	[SerializeField] int reshowBannerEveryUpdates = 3;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void Start() {
		if(isFailedConfig) {
			Log.Warning(false, this, "Missing config, destroy self");
			Destroy(this.gameObject);
			return;
		}
		// let ads init. Also dontdestroy to keep this alive
		mediator.Init();
		DontDestroyOnLoad(gameObject);
		if(autoShowBannerAfterInit) {
			StartCoroutine(IE_ShowBannerAfterInit());
		}
		UpdateBannerSpace();
		// not bound to this GameObject: it is not a root object, so DontDestroyOnLoad fails and it dies on scene change
		Observable.FromMicroCoroutine(IE_UpdateBannerSpace).Subscribe();
		// delay call preload ads
		//Observable.FromMicroCoroutine(IE_Delay).Subscribe();
		// Update logic, have to wait for fetching remote config, cause we got enable_Global_Ads
		// wait for that config then call preload ads
		//remoteConfigManager.OnFetchFinishEvent.Subscribe(OnFetchRemoteConfigFinish);
	}

	//IEnumerator IE_Delay() {
	//	var timer = delayBeforeCallPreloadAds;
	//	while(timer > 0) {
	//		yield return null;
	//		timer -= Time.deltaTime;
	//	}
	//	mediator.PreloadAllNeccessaryAds();
	//}
	#endregion// Init config

	///<summary> UI reads BannerSpace when a screen opens (SafeAreaAdjuster.Awake), keep it up to date </summary>
	IEnumerator IE_UpdateBannerSpace() {
		var count = 0;
		while(true) {
			var timer = bannerSpaceUpdateInterval;
			while(timer > 0) {
				yield return null;
				timer -= Time.unscaledDeltaTime;
			}
			UpdateBannerSpace();
			// keep the banner slot filled: the AdMob banner can finish loading long after the first show calls
			if(autoShowBannerAfterInit && ++count % Mathf.Max(1, reshowBannerEveryUpdates) == 0
				&& !removeAdsInventory.HasPurchased() && mediator.IsBannerReady())
				mediator.ShowBanner();
		}
	}

	void UpdateBannerSpace() {
		if(removeAdsInventory.HasPurchased()) {
			BannerSpace.BottomPixels = 0f;
			return;
		}
		var height = mediator.GetBannerHeightPixels();
		if(height <= 0f)
			height = fallbackBannerHeightDp * (Screen.dpi > 0 ? Screen.dpi / 160f : 1f);
		BannerSpace.BottomPixels = height;
	}

	IEnumerator IE_ShowBannerAfterInit() {
		for(int i = 0; i < bannerShowRetryCount; i++) {
			yield return new WaitForSeconds(bannerShowRetryDelaySeconds);
			mediator.ShowBanner();
		}
	}


	#region Events
	void OnFetchRemoteConfigFinish() {
		// unrgister and call preload as
		//remoteConfigManager.OnFetchFinishEvent.Unsubscribe(OnFetchRemoteConfigFinish);
		mediator.PreloadAllNeccessaryAds();
		// nolonger need this
		Destroy(gameObject);
	}
	#endregion//Events
}
