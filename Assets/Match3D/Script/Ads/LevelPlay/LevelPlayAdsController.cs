using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Services.LevelPlay;

[CreateAssetMenu(fileName = "LevelPlayAdsController", menuName = "Game/Ads/LevelPlayAdsController")]
public class LevelPlayAdsController : BaseAdsController {
	[Header("App Key")]
	[SerializeField] StringVariable appKeyAndroidVar;
	[SerializeField] StringVariable appKeyIosVar;
	[SerializeField] StringVariable appKeyAmazonVar;

	[Header("Ad Units")]
	[SerializeField] AdUnitIdGroup adUnitAndroid;
	[SerializeField] AdUnitIdGroup adUnitIOS;
	[SerializeField] AdUnitIdGroup adUnitAmazon;

	[Header("Placement Names")]
	[SerializeField] string bannerPlacementName = "banner";
	[SerializeField] string interstitialPlacementName = "interstitial";
	[SerializeField] string rewardPlacementName = "rewarded";

	[Header("Banner")]
	[SerializeField] LevelPlayBannerPosition bannerPosition = LevelPlayBannerPosition.BottomCenter;
	[SerializeField] bool bannerDisplayOnLoad = true;
	[SerializeField] bool bannerRespectSafeArea = true;

	[Header("Retry")]
	[SerializeField] float retryDelaySeconds = 3.5f;
	[SerializeField] float maxRetryDelaySeconds = 64f;

	[Header("Init")]
	[SerializeField] bool preloadBannerAfterInit = true;
	[SerializeField] bool preloadRewardAfterInit = true;
	[SerializeField] bool preloadInterstitialAfterInit = true;
	[SerializeField] bool enableTestSuite = false;

	[Header("Privacy")]
	[SerializeField] AdmobConsentManager consentManager;
	[SerializeField] bool setGdprConsent = false;
	[SerializeField] bool gdprConsent = true;
	[SerializeField] bool setDoNotSell = false;
	[SerializeField] bool doNotSell = false;
	[SerializeField] bool childDirected = false;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;

	bool _isInitializing;
	bool _isInitialized;
	bool _canRequestAds;

	LevelPlayBannerAd _bannerAd;
	bool _isBannerLoading;
	bool _isBannerLoaded;
	int _bannerRetryAttempt;

	LevelPlayRewardedAd _rewardedAd;
	bool _isRewardLoading;
	bool _isRewardShowing;
	int _rewardRetryAttempt;

	LevelPlayInterstitialAd _interstitialAd;
	bool _isInterstitialLoading;
	bool _isInterstitialShowing;
	int _interstitialRetryAttempt;

	void OnValidate() {
		isFailedConfig = IsMissingAppKey() || IsMissingAdUnit(GetAdUnitGroupForCurrentPlatform());
	}

	void OnDisable() {
		DestroyAds();
		UnregisterInitEvents();
		_isInitializing = false;
		_isInitialized = false;
		_canRequestAds = false;
	}

	public override void Init() {
		if(isFailedConfig || _isInitialized || _isInitializing)
			return;

		_isInitializing = true;
		if(consentManager != null) {
			consentManager.Prepare(InitializeLevelPlay);
		} else {
			Log.Warning(false, this, "Missing shared consent manager. LevelPlay will initialize without UMP/ATT gating.");
			_canRequestAds = true;
			InitializeLevelPlay();
		}
	}

	void InitializeLevelPlay() {
		_canRequestAds = consentManager == null || consentManager.CanRequestAds;
		if(!_canRequestAds) {
			_isInitializing = false;
			Log.Warning(false, this, "LevelPlay consent does not allow requesting ads yet.");
			return;
		}

		var appKey = GetAppKeyForCurrentPlatform();
		if(string.IsNullOrEmpty(appKey)) {
			Log.Warning(false, this, $"Missing LevelPlay app key for platform [{Application.platform}]");
			_isInitializing = false;
			return;
		}

		ApplyPrivacyMetaData();
		RegisterInitEvents();

		if(enableTestSuite)
			LevelPlay.SetMetaData("is_test_suite", "enable");

		Log.Info($"Init LevelPlay [{name}], appKey [{appKey}], time [{Time.time}]");
		LevelPlay.Init(appKey);
	}

	public override bool IsBannerReady() {
		return !isFailedConfig && _isInitialized && _canRequestAds && _bannerAd != null && _isBannerLoaded;
	}

	public override void LoadBanner() {
		if(!CanLoadAds() || _isBannerLoading || _bannerAd != null)
			return;

		CreateBannerAd();
		_bannerAd.LoadAd();
		_isBannerLoading = true;
	}

	public override void ShowBanner() {
		if(!CanLoadAds())
			return;

		if(_bannerAd == null) {
			LoadBanner();
			return;
		}

		_bannerAd.ShowAd();
	}

	public override void HideBanner() {
		_bannerAd?.HideAd();
	}

	public override bool IsRewardVideoReady() {
		if(!CanLoadAds())
			return false;

		return _rewardedAd != null
			&& _rewardedAd.IsAdReady()
			&& !LevelPlayRewardedAd.IsPlacementCapped(rewardPlacementName);
	}

	public override void LoadRewardVideo() {
		if(CanLoadAds())
			CheckLoadRewardedAd();
	}

	public override void ShowRewardVideo() {
		if(!CanLoadAds() || _isRewardShowing) {
			InvokeRewardFailedEvent();
			return;
		}

		if(!IsRewardVideoReady()) {
			InvokeRewardFailedEvent();
			return;
		}

		_isRewardShowing = true;
		_rewardedAd.ShowAd(rewardPlacementName);
	}

	public override bool IsVideoReady() {
		if(!CanLoadAds())
			return false;

		return _interstitialAd != null
			&& _interstitialAd.IsAdReady()
			&& !LevelPlayInterstitialAd.IsPlacementCapped(interstitialPlacementName);
	}

	public override void LoadVideo() {
		if(CanLoadAds())
			CheckLoadInterstitialAd();
	}

	public override void ShowVideo() {
		if(!CanLoadAds() || _isInterstitialShowing)
			return;

		if(!IsVideoReady())
			return;

		_isInterstitialShowing = true;
		_interstitialAd.ShowAd(interstitialPlacementName);
	}

	void OnInitSuccess(LevelPlayConfiguration config) {
		Log.Info($"LevelPlay init success [{config}]");

		_isInitializing = false;
		_isInitialized = true;
		UnregisterInitEvents();

		CreateRewardedAd();
		CreateInterstitialAd();

		if(preloadBannerAfterInit) LoadBanner();
		if(preloadRewardAfterInit) LoadRewardVideo();
		if(preloadInterstitialAfterInit) LoadVideo();
	}

	void OnInitFailed(LevelPlayInitError error) {
		Log.Warning(false, this, $"LevelPlay init failed [{error}]");

		_isInitializing = false;
		_isInitialized = false;
		UnregisterInitEvents();

		RetryInitAsync().Forget();
	}

	void RegisterInitEvents() {
		LevelPlay.OnInitSuccess -= OnInitSuccess;
		LevelPlay.OnInitFailed -= OnInitFailed;
		LevelPlay.OnInitSuccess += OnInitSuccess;
		LevelPlay.OnInitFailed += OnInitFailed;
	}

	void UnregisterInitEvents() {
		LevelPlay.OnInitSuccess -= OnInitSuccess;
		LevelPlay.OnInitFailed -= OnInitFailed;
	}

	async UniTaskVoid RetryInitAsync() {
		await UniTask.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
		Init();
	}

	bool CanLoadAds() {
		if(isFailedConfig)
			return false;

		if(!_isInitialized) {
			Init();
			return false;
		}

		return _canRequestAds;
	}

	void CreateBannerAd() {
		DestroyBanner();

		var config = new LevelPlayBannerAd.Config.Builder()
			.SetSize(LevelPlayAdSize.BANNER)
			.SetPlacementName(bannerPlacementName)
			.SetPosition(bannerPosition)
			.SetDisplayOnLoad(bannerDisplayOnLoad)
			.SetRespectSafeArea(bannerRespectSafeArea)
			.Build();

		_bannerAd = new LevelPlayBannerAd(AdUnitBanner, config);
		_isBannerLoaded = false;
		_bannerAd.OnAdLoaded += OnBannerLoaded;
		_bannerAd.OnAdLoadFailed += OnBannerLoadFailed;
		_bannerAd.OnAdDisplayFailed += OnBannerDisplayFailed;
	}

	void OnBannerLoaded(LevelPlayAdInfo adInfo) {
		_bannerRetryAttempt = 0;
		_isBannerLoading = false;
		_isBannerLoaded = true;
		Log.Info($"LevelPlay banner loaded [{adInfo}]");
	}

	void OnBannerLoadFailed(LevelPlayAdError error) {
		_isBannerLoading = false;
		Log.Info($"LevelPlay banner load failed [{error}]");
		DestroyBanner();
		RetryBannerAsync().Forget();
	}

	void OnBannerDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error) {
		Log.Info($"LevelPlay banner display failed [{adInfo}], error [{error}]");
		DestroyBanner();
		RetryBannerAsync().Forget();
	}

	async UniTaskVoid RetryBannerAsync() {
		_bannerRetryAttempt++;
		await UniTask.Delay(TimeSpan.FromSeconds(GetRetryDelay(_bannerRetryAttempt)));

		if(CanLoadAds())
			LoadBanner();
	}

	void CreateRewardedAd() {
		if(_rewardedAd != null)
			return;

		_rewardedAd = new LevelPlayRewardedAd(AdUnitReward);
		_rewardedAd.OnAdLoaded += OnRewardLoaded;
		_rewardedAd.OnAdLoadFailed += OnRewardLoadFailed;
		_rewardedAd.OnAdDisplayFailed += OnRewardDisplayFailed;
		_rewardedAd.OnAdRewarded += OnRewarded;
		_rewardedAd.OnAdClosed += OnRewardClosed;
	}

	void CheckLoadRewardedAd() {
		CreateRewardedAd();

		if(_rewardedAd != null && !_rewardedAd.IsAdReady() && !_isRewardLoading && !_isRewardShowing) {
			_isRewardLoading = true;
			_rewardedAd.LoadAd();
		}
	}

	void OnRewardLoaded(LevelPlayAdInfo adInfo) {
		_rewardRetryAttempt = 0;
		_isRewardLoading = false;
		Log.Info($"LevelPlay rewarded loaded [{adInfo}]");
	}

	void OnRewardLoadFailed(LevelPlayAdError error) {
		_isRewardLoading = false;
		Log.Info($"LevelPlay rewarded load failed [{error}]");
		RetryRewardAsync().Forget();
	}

	void OnRewardDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error) {
		_isRewardShowing = false;
		Log.Info($"LevelPlay rewarded display failed [{adInfo}], error [{error}]");
		InvokeRewardFailedEvent();
		RetryRewardAsync().Forget();
	}

	void OnRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward) {
		Log.Info($"LevelPlay rewarded user [{adInfo}], reward [{reward}]");
		InvokeRewardFinishEvent();
	}

	void OnRewardClosed(LevelPlayAdInfo adInfo) {
		_isRewardShowing = false;
		CheckLoadRewardedAd();
	}

	async UniTaskVoid RetryRewardAsync() {
		_rewardRetryAttempt++;
		await UniTask.Delay(TimeSpan.FromSeconds(GetRetryDelay(_rewardRetryAttempt)));

		if(CanLoadAds())
			CheckLoadRewardedAd();
	}

	void CreateInterstitialAd() {
		if(_interstitialAd != null)
			return;

		_interstitialAd = new LevelPlayInterstitialAd(AdUnitVideo);
		_interstitialAd.OnAdLoaded += OnInterstitialLoaded;
		_interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
		_interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
		_interstitialAd.OnAdClosed += OnInterstitialClosed;
	}

	void CheckLoadInterstitialAd() {
		CreateInterstitialAd();

		if(_interstitialAd != null && !_interstitialAd.IsAdReady() && !_isInterstitialLoading && !_isInterstitialShowing) {
			_isInterstitialLoading = true;
			_interstitialAd.LoadAd();
		}
	}

	void OnInterstitialLoaded(LevelPlayAdInfo adInfo) {
		_interstitialRetryAttempt = 0;
		_isInterstitialLoading = false;
		Log.Info($"LevelPlay interstitial loaded [{adInfo}]");
	}

	void OnInterstitialLoadFailed(LevelPlayAdError error) {
		_isInterstitialLoading = false;
		Log.Info($"LevelPlay interstitial load failed [{error}]");
		RetryInterstitialAsync().Forget();
	}

	void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error) {
		_isInterstitialShowing = false;
		Log.Info($"LevelPlay interstitial display failed [{adInfo}], error [{error}]");
		InvokeInterstitialAdsClose();
		RetryInterstitialAsync().Forget();
	}

	void OnInterstitialClosed(LevelPlayAdInfo adInfo) {
		_isInterstitialShowing = false;
		InvokeInterstitialAdsClose();
		CheckLoadInterstitialAd();
	}

	async UniTaskVoid RetryInterstitialAsync() {
		_interstitialRetryAttempt++;
		await UniTask.Delay(TimeSpan.FromSeconds(GetRetryDelay(_interstitialRetryAttempt)));

		if(CanLoadAds())
			CheckLoadInterstitialAd();
	}

	void ApplyPrivacyMetaData() {
		if(setGdprConsent)
			LevelPlay.SetConsent(gdprConsent);

		if(setDoNotSell)
			LevelPlay.SetMetaData("do_not_sell", doNotSell ? "true" : "false");

		LevelPlay.SetMetaData("is_child_directed", childDirected ? "true" : "false");
	}

	float GetRetryDelay(int attempt) {
		return Mathf.Min(retryDelaySeconds * Mathf.Pow(2f, attempt - 1), maxRetryDelaySeconds);
	}

	string GetAppKeyForCurrentPlatform() {
#if UNITY_ANDROID
		return appKeyAndroidVar != null ? appKeyAndroidVar.Value : null;
#elif UNITY_IOS
		return appKeyIosVar != null ? appKeyIosVar.Value : null;
#elif UNITY_AMAZON
		return appKeyAmazonVar != null ? appKeyAmazonVar.Value : null;
#else
		return appKeyAndroidVar != null ? appKeyAndroidVar.Value : null;
#endif
	}

	AdUnitIdGroup GetAdUnitGroupForCurrentPlatform() {
#if UNITY_ANDROID
		return adUnitAndroid;
#elif UNITY_IOS
		return adUnitIOS;
#elif UNITY_AMAZON
		return adUnitAmazon;
#else
		return adUnitAndroid;
#endif
	}

	string AdUnitBanner => GetAdUnitGroupForCurrentPlatform().banner.Value;
	string AdUnitVideo => GetAdUnitGroupForCurrentPlatform().video.Value;
	string AdUnitReward => GetAdUnitGroupForCurrentPlatform().reward.Value;

	bool IsMissingAppKey() {
		return appKeyAndroidVar == null || appKeyIosVar == null;
	}

	bool IsMissingAdUnit(AdUnitIdGroup group) {
		return group == null || group.banner == null || group.video == null || group.reward == null;
	}

	void DestroyAds() {
		DestroyBanner();
		DestroyReward();
		DestroyInterstitial();
	}

	void DestroyBanner() {
		if(_bannerAd == null)
			return;

		_bannerAd.DestroyAd();
		_bannerAd = null;
		_isBannerLoading = false;
		_isBannerLoaded = false;
	}

	void DestroyReward() {
		if(_rewardedAd == null)
			return;

		_rewardedAd.DestroyAd();
		_rewardedAd = null;
		_isRewardLoading = false;
		_isRewardShowing = false;
	}

	void DestroyInterstitial() {
		if(_interstitialAd == null)
			return;

		_interstitialAd.DestroyAd();
		_interstitialAd = null;
		_isInterstitialLoading = false;
		_isInterstitialShowing = false;
	}

	[Serializable]
	public class AdUnitIdGroup {
		public StringVariable banner;
		public StringVariable video;
		public StringVariable reward;
	}
}
