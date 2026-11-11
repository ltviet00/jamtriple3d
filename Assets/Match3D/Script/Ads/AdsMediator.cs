using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AdsMediator", menuName = "Game/Ads/AdsMediator")]
public class AdsMediator : ScriptableObject {
	#region Init, config
	///<summary> List of AdsNetwork, use as priorty order as well </summary>
	[Header("AdsController List - used as priority as well")]
	[SerializeField] BaseAdsController[] adsControllerList;

	[Header("Legacy provider selection (not used by waterfall)")]
	[SerializeField] StringVariable selectedAdsNetworkNameVar;

	[Header("Remote config")]
	[SerializeField] BooleanVariable enableAdsGloballyVar;

	[Header("AdsBlock checker")]
	[SerializeField] AdsBlockByVersionChecker adsBlockVersionChecker;

	[Header("Event out")]
	[SerializeField] GameEvent rewardAdsSuccessEvent;
	[SerializeField] GameEvent rewardAdsFailedEvent;
	[SerializeField] GameEvent interstitialAdsCloseEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		RegisterAdsEventFromControllers();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		UnregisterAdsEventFromControllers();
	}
	#endregion// Init config


	#region Public
	///<summary> Init all ads </summary>
	public void Init() {
		if(isFailedConfig)
			return;
		// init all ads
		for(int n = 0; n < adsControllerList.Length; n++) {
			adsControllerList[n].Init();
		}
	}

	///<summary> Got called by AdsMediatorAwaker to preload ads when app open </summary>
	public void PreloadAllNeccessaryAds() {
		if(isFailedConfig)
			return;
		if(!IsAdsEnable()) {
			return;
		}
		for(int n = 0; n < adsControllerList.Length; n++) {
			var adsController = adsControllerList[n];
			adsController.LoadBanner();
			adsController.LoadRewardVideo();
			adsController.LoadVideo();
		}
	}

	//------- Banner
	public bool IsBannerReady() {
		if(isFailedConfig)
			return false;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return false;
		}
		return PickAdsControllerReadyForAdsType(AdsType.Banner) != null;
	}

	[Obsolete("Use IsBannerReady instead.")]
	public bool IsBannderReady() {
		return IsBannerReady();
	}

	public void ShowBanner() {
		if(isFailedConfig)
			return;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return;
		}
		var bannerReady = PickAdsControllerReadyForAdsType(AdsType.Banner);
		if(bannerReady != null) {
			bannerReady.ShowBanner();
		}
		else {
			if(adsControllerList.Length > 0) {
				adsControllerList[0].ShowBanner();
			}
			Log.Info("Banner not ready");
		}
	}

	public void HideBanner() {
		if(isFailedConfig)
			return;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return;
		}
		// not like showBanner, we have to pick active ads, here, we hide all banner
		for(int n = 0; n < adsControllerList.Length; n++) {
			adsControllerList[n].HideBanner();
		}
	}

	//------- RewardAds
	public bool IsRewardVideoAdsReady() {
		if(isFailedConfig)
			return false;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return false;
		}
		return PickAdsControllerReadyForAdsType(AdsType.RewardVideo) != null;
	}

	public void ShowRewardVideoAds() {
		if(isFailedConfig)
			return;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return;
		}
		var rewardVideoReady = PickAdsControllerReadyForAdsType(AdsType.RewardVideo);
		if(rewardVideoReady != null) {
			rewardVideoReady.ShowRewardVideo();
		}
		else {
			Log.Info("RewardVideo not ready");
		}
	}


	//------ Video ads
	public bool IsInterstitialVideoAdsReady() {
		if(isFailedConfig)
			return false;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return false;
		}
		return PickAdsControllerReadyForAdsType(AdsType.InterstitialVideo) != null;
	}

	public void ShowInterstitialVideoAds() {
		if(isFailedConfig)
			return;
		// update: check enable/disable ads globally
		if(!IsAdsEnable()) {
			return;
		}
		var videoReady = PickAdsControllerReadyForAdsType(AdsType.InterstitialVideo);
		if(videoReady != null) {
			videoReady.ShowVideo();
		}
		else {
			if(adsControllerList.Length > 0) {
				adsControllerList[0].ShowVideo();
			}
			Log.Info("Video not ready");
		}
	}

	//-------Check
	///<summary> Call this when selectedAdsController name changed, often call when re-fetch data from remote </summary>
	public void CheckRePickAdsControllerBaseOnProviderName() {
		if(isFailedConfig)
			return;
		PickAdsControllerBaseOnSelectedAdsProviderName();
	}
	#endregion//Public


	#region Public - UI callback
	public void In_ShowBanner() {
		ShowBanner();
	}

	public void In_HideBanner() {
		HideBanner();
	}

	public void In_ShowRewardVideo() {
		ShowRewardVideoAds();
	}

	public void In_ShowVideo() {
		ShowInterstitialVideoAds();
	}

	public void In_CheckRePickAdsControllerBaseOnProviderName() {
		CheckRePickAdsControllerBaseOnProviderName();
	}
	#endregion//Public - UI callback


	#region Private - pick available network
	///<summary>
	/// Waterfall order is the serialized list order. The first ready controller
	/// wins; controllers below it are fallbacks.
	/// </summary>
	BaseAdsController PickAdsControllerReadyForAdsType(AdsType adsType) {
		for(int n = 0; n < adsControllerList.Length; n++) {
			var controller = adsControllerList[n];
			if(controller.IsAdsTypeReady(adsType)) {
				return controller;
			}
			controller.LoadAdsType(adsType);
		}
		return null;
	}
	#endregion//Private - pick available network


	#region Private
	///<summary> selected ads depend on the picked ads network stringVar. NOTES: we now using the priority instead of this</summary>
	BaseAdsController _selectedAdsController = null;
	///<summary> selected ads depend on the picked ads network stringVar. NOTES: we now using the priority instead of this</summary>
	BaseAdsController SelectedAdsController {
		get {
			if(_selectedAdsController == null)
				PickAdsControllerBaseOnSelectedAdsProviderName();
			return _selectedAdsController;
		}
	}

	void PickAdsControllerBaseOnSelectedAdsProviderName() {
		// check priority or other factors
		if(string.IsNullOrEmpty(selectedAdsNetworkNameVar.Value) || string.IsNullOrEmpty(selectedAdsNetworkNameVar.Value)) {
			_selectedAdsController = adsControllerList[0];
			Log.Info($"Null data in [{selectedAdsNetworkNameVar.name}] var, so pick the first ads [{_selectedAdsController.identicalName}]");
			return;
		}
		for(int n = 0; n < adsControllerList.Length; n++) {
			var controller = adsControllerList[n];
			if(controller.identicalName == selectedAdsNetworkNameVar.Value) {
				_selectedAdsController = controller;
				return;
			}
		}
		_selectedAdsController = adsControllerList[0];
		Log.Warning(false, this, $"Not found adsController with namme [{selectedAdsNetworkNameVar.Value}], pick first one [{_selectedAdsController.identicalName}]");
	}

	///<summary> Current 2 checking for the "false" cases:
	///- Ads enable globaly?
	///- if ads enable, then check if this version in the adsBlockList</summary>
	bool IsAdsEnable() {
		// ads disable globaly, then just return false, no further checking
		if(!enableAdsGloballyVar.Value) {
			return false;
		}
		// okay, ads enable global, check blockGameVersion
		if(adsBlockVersionChecker.IsCurrentGameVersionGotAdsBlock()) {
			return false;
		}
		// passed all check, ads enable
		return true;
	}
	#endregion//Private


	#region Events from AdController
	// currently, we only forcus on reward video from controller
	void RegisterAdsEventFromControllers() {
		for(int n = 0; n < adsControllerList.Length; n++) {
			var controller = adsControllerList[n];
			controller.onRewardAdsFailedHandler += OnRewardVideoFailed;
			controller.onRewardAdsFinishHandler += OnRewardVideoSuccess;
			controller.onInterstitialAdsCloseHandler += OnInterstitialAdsClose;
		}
	}

	void UnregisterAdsEventFromControllers() {
		for(int n = 0; n < adsControllerList.Length; n++) {
			var controller = adsControllerList[n];
			controller.onRewardAdsFailedHandler -= OnRewardVideoFailed;
			controller.onRewardAdsFinishHandler -= OnRewardVideoSuccess;
			controller.onInterstitialAdsCloseHandler -= OnInterstitialAdsClose;
		}
	}

	void OnRewardVideoSuccess() {
		Log.Info(this, "SUCCESS watch rewardAds");
		rewardAdsSuccessEvent.Raise();
	}

	void OnRewardVideoFailed() {
		Log.Info(this, "FAILED watch rewardAds");
		rewardAdsFailedEvent.Raise();
	}

	void OnInterstitialAdsClose() {
		Log.Info(this, "InterstitialAds closed");
		interstitialAdsCloseEvent.Raise();
	}
	#endregion//Events from AdController


	#region Classes
	public enum AdsType {
		Banner, RewardVideo, InterstitialVideo
	}

	///<summary> Firebase remote : we will list verions that won't be ads showing, these list get sync-ed in a remote string variabe.
	///this class to wrap some function related to it</summary>
	[System.Serializable]
	public class AdsBlockByVersionChecker {
		[SerializeField] StringVariable adsBlockByGameVersionVar;
		///<summary> Parse from the adsBlockByGameVersionVar var </summary>
		[NonSerialized] List<string> blockVersionList = null;
		///<summary> Will cache value, at first time parse to blockVersionList </summary>
		bool isCurrentVersionGotAdsBlock;

		///<summary> True if current gameversion in the list of blockAdsVersion </summary>
		public bool IsCurrentGameVersionGotAdsBlock() {
			CheckParseBlockVersionListFromSyncVar();
			return isCurrentVersionGotAdsBlock;
		}

		void CheckParseBlockVersionListFromSyncVar() {
			if(string.IsNullOrEmpty(adsBlockByGameVersionVar.Value)) {
				// just generate list to mark it as done checking. Nothing to check, no ads block
				blockVersionList= new List<string>();
				isCurrentVersionGotAdsBlock = false;
				return;
			}
			// we only need to perform this check once, then save in the bool var;
			// ops, the blockVerionList alway not null, so checking counting, then add a decoy value later
			if (blockVersionList == null || (blockVersionList != null && blockVersionList.Count == 0)) {
				blockVersionList = new List<string>();
				// remember to use this to splitted version in Firebase remote config. eg : "1.1.1, 1.2.0"
				const string SPLITTER = ",";
				var splittedList = adsBlockByGameVersionVar.Value.Split(SPLITTER);
				// trim these version text
				for(int n = 0; n < splittedList.Length; n++) {
					blockVersionList.Add(splittedList[n].Trim());
				}
				// check version
				var appVersion = Application.version;
				isCurrentVersionGotAdsBlock = blockVersionList.Contains(appVersion);
				// if the list is empty, then add a decoy value to mark that we already check version
				if(blockVersionList.Count == 0) {
					blockVersionList.Add(string.Empty);
				}
			}
		}
	}
	#endregion//Classes
}
