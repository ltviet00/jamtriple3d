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
