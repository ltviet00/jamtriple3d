using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenInterstitialAdsWhenScreenIsCovered : MonoBehaviour {
	#region Init, config
	[Header("ShareData, event in")]
	[SerializeField] AdsMediator adsMediator;
	[SerializeField] GameEvent screenGotCoverInLoadingEvent;

	[Header("Config")]
	[SerializeField] int ignoreSomeFirstLoadingCount = 3;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		screenGotCoverInLoadingEvent.Subscribe(OnScreenGotCover);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		screenGotCoverInLoadingEvent.Unsubscribe(OnScreenGotCover);
	}
	#endregion// Init config


	#region Private
	void OnScreenGotCover() {
		// check ignore some first loading
		if(ignoreSomeFirstLoadingCount > 0) {
			ignoreSomeFirstLoadingCount--;
				return;
		}
		adsMediator.ShowInterstitialVideoAds();
	}
	#endregion//Private
}
