using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

[CreateAssetMenu(fileName = "AdmobController", menuName = "Game/Ads/AdmobController")]
public class AdmobController : BaseAdsController {
	[Header("Placement")]
	[SerializeField] PlacementIdGroup placementAndroid;
	[SerializeField] PlacementIdGroup placementIOS;
	[SerializeField] PlacementIdGroup placementAmazon;

	[Header("Consent")]
	[SerializeField] AdmobConsentManager consentManager;

	[Header("Config")]
	[SerializeField] float delayReloadAdsWhenLoadFailed = 3.5f;
	[SerializeField] float maxRetryDelaySeconds = 64f;
	[SerializeField] bool preloadBannerAfterInit = true;
	[SerializeField] bool preloadRewardAfterInit = true;
	[SerializeField] bool preloadInterstitialAfterInit = true;
	[SerializeField] bool bannerDisplayOnLoad = true;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;

	bool _isInitialized;
	bool _isInitializing;
	bool _canRequestAds;
	int _requestGeneration;

	BannerView _banner;
	bool _isBannerLoading;
	bool _isBannerLoaded;
	bool _shouldShowBanner;
	int _bannerRetryAttempt;

	RewardedAd _videoReward;
	bool _isLoadingReward;
	bool _isShowingReward;
	int _rewardRetryAttempt;

	InterstitialAd _videoInterstitial;
	bool _isLoadingInterstitial;
	bool _isShowingInterstitial;
	bool _showInterstitialWhenLoaded;
	int _interstitialRetryAttempt;

	void OnValidate() {
		isFailedConfig = IsPlacementMissing(placementAndroid) || IsPlacementMissing(placementIOS);
	}

	void OnDisable() {
		_requestGeneration++;
		DestroyAds();
		_isInitialized = false;
		_isInitializing = false;
		_canRequestAds = false;
		_isBannerLoading = false;
		_shouldShowBanner = false;
		_isLoadingReward = false;
		_isShowingReward = false;
		_isLoadingInterstitial = false;
		_isShowingInterstitial = false;
		_showInterstitialWhenLoaded = false;
	}

	public override void Init() {
		if(isFailedConfig || _isInitialized || _isInitializing) {
			return;
		}
		_isInitializing = true;
		Log.Info($"Start init AdMob controller [{name}], time [{Time.time}]");

		if(consentManager != null) {
			consentManager.Prepare(InitializeMobileAds);
		} else {
			_canRequestAds = true;
			InitializeMobileAds();
		}
	}

	void InitializeMobileAds() {
		int generation = _requestGeneration;
		_canRequestAds = consentManager == null || consentManager.CanRequestAds;
		if(!_canRequestAds) {
			_isInitializing = false;
			Log.Warning(false, this, "AdMob consent does not allow requesting ads yet.");
			return;
		}

		MobileAds.SetiOSAppPauseOnBackground(true);
		MobileAds.Initialize(initStatus => {
			RunOnMainThread(() => {
				if(generation != _requestGeneration) {
					return;
				}
				_isInitialized = true;
				_isInitializing = false;
				Log.Info($"Finish init AdMob [{initStatus}], time [{Time.time}]");
				if(preloadBannerAfterInit) LoadBanner();
				if(preloadRewardAfterInit) LoadRewardVideo();
				if(preloadInterstitialAfterInit) LoadVideo();
			});
		});
	}

	public override bool IsBannerReady() {
		return !isFailedConfig && _banner != null && _isBannerLoaded && _isInitialized && _canRequestAds;
	}

	public override void LoadBanner() {
		if(!CanLoadAds() || _isBannerLoading || _banner != null) {
			return;
		}
		RequestBanner();
	}

	public override void ShowBanner() {
		_shouldShowBanner = true;
		if(_banner == null) {
			LoadBanner();
			return;
		}
		_banner.Show();
	}

	public override void HideBanner() {
		_shouldShowBanner = false;
		_banner?.Hide();
	}

	public override bool IsRewardVideoReady() {
		if(!CanLoadAds()) {
			return false;
		}
		return _videoReward != null && _videoReward.CanShowAd();
	}

	public override void LoadRewardVideo() {
		if(CanLoadAds()) {
			CheckRequestVideoReward();
		}
	}

	public override void ShowRewardVideo() {
		if(!CanLoadAds() || _isShowingReward) {
			InvokeRewardFailedEvent();
			return;
		}
		if(_videoReward == null || !_videoReward.CanShowAd()) {
			CheckRequestVideoReward();
			InvokeRewardFailedEvent();
			return;
		}

		_isShowingReward = true;
		_videoReward.Show(reward => {
			RunOnMainThread(InvokeRewardFinishEvent);
		});
	}

	public override bool IsVideoReady() {
		if(!CanLoadAds()) {
			return false;
		}
		return _videoInterstitial != null && _videoInterstitial.CanShowAd();
	}

	public override void LoadVideo() {
		if(CanLoadAds()) {
			CheckRequestVideoInterstitial();
		}
	}

	public override void ShowVideo() {
		_showInterstitialWhenLoaded = true;
		if(!CanLoadAds() || _isShowingInterstitial) {
			return;
		}
		if(_videoInterstitial == null || !_videoInterstitial.CanShowAd()) {
			CheckRequestVideoInterstitial();
			return;
		}
		_showInterstitialWhenLoaded = false;
		_isShowingInterstitial = true;
		_videoInterstitial.Show();
	}

	bool CanLoadAds() {
		if(isFailedConfig) {
			return false;
		}
		if(!_isInitialized) {
			Init();
			return false;
		}
		return _canRequestAds;
	}

	PlacementIdGroup GetPlacementGroupForCurrentPlatform() {
#if UNITY_ANDROID
		return placementAndroid;
#elif UNITY_IOS
		return placementIOS;
#elif UNITY_AMAZON
		return placementAmazon;
#else
		return placementAndroid;
#endif
	}

	string PlacementIdBanner => GetPlacementGroupForCurrentPlatform().banner.Value;
	string PlacementIdVideo => GetPlacementGroupForCurrentPlatform().video.Value;
	string PlacementIdReward => GetPlacementGroupForCurrentPlatform().reward.Value;

	void RequestBanner() {
		if(string.IsNullOrEmpty(PlacementIdBanner)) {
			Log.Warning(false, this, "AdMob banner placement id is empty.");
			return;
		}
		int generation = _requestGeneration;
		_isBannerLoading = true;
		_isBannerLoaded = false;
		_banner = new BannerView(PlacementIdBanner, GetAdaptiveBannerSize(), AdPosition.Bottom);
		_banner.OnBannerAdLoaded += () => RunOnMainThread(() => OnBannerLoaded(generation));
		_banner.OnBannerAdLoadFailed += error => RunOnMainThread(() => OnBannerLoadFailed(error, generation));
		_banner.OnAdClicked += () => RunOnMainThread(() => Log.Info("AdMob banner clicked"));
		_banner.OnAdImpressionRecorded += () => RunOnMainThread(() => Log.Info("AdMob banner impression recorded"));
		_banner.OnAdPaid += OnAdPaid;
		_banner.LoadAd(BuildAdRequest());
		if(bannerDisplayOnLoad || _shouldShowBanner) {
			_banner.Show();
		}
	}

	void OnBannerLoaded(int generation) {
		if(generation != _requestGeneration) {
			return;
		}
		_isBannerLoading = false;
		_isBannerLoaded = true;
		_bannerRetryAttempt = 0;
		if(bannerDisplayOnLoad || _shouldShowBanner) {
			_banner?.Show();
		}
	}

	AdSize GetAdaptiveBannerSize() {
		var width = GetSafeAreaWidthInDp();
		if(width <= 0) {
			Log.Warning(false, this, "Invalid adaptive banner width, fallback to standard banner.");
			return AdSize.Banner;
		}
		return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(width);
	}

	int GetSafeAreaWidthInDp() {
		var safeAreaWidth = Screen.safeArea.width > 0 ? Screen.safeArea.width : Screen.width;
		var scale = MobileAds.Utils.GetDeviceScale();
		if(scale <= 0) {
			return 0;
		}
		return Mathf.Max(320, Mathf.RoundToInt(safeAreaWidth / scale));
	}

	void OnBannerLoadFailed(LoadAdError error, int generation) {
		if(generation != _requestGeneration) {
			return;
		}
		Log.Info($"AdMob banner load failed [{error?.GetMessage()}]");
		_isBannerLoading = false;
		_isBannerLoaded = false;
		SafeDestroyBanner();
		RetryBannerWithBackoff(generation).Forget();
	}

	void CheckRequestVideoReward() {
		if((_videoReward == null || !_videoReward.CanShowAd()) && !_isLoadingReward && !_isShowingReward) {
			RequestVideoReward();
		}
	}

	void RequestVideoReward() {
		if(string.IsNullOrEmpty(PlacementIdReward)) {
			Log.Warning(false, this, "AdMob rewarded placement id is empty.");
			return;
		}
		int generation = _requestGeneration;
		_isLoadingReward = true;
		SafeDestroyReward();
		RewardedAd.Load(PlacementIdReward, BuildAdRequest(), (ad, error) => {
			RunOnMainThread(() => {
				if(generation != _requestGeneration) {
					ad?.Destroy();
					return;
				}
				_isLoadingReward = false;
				if(error != null || ad == null) {
					Log.Info($"AdMob rewarded load failed [{error?.GetMessage()}]");
					RetryRewardWithBackoff(generation).Forget();
					return;
				}
				_rewardRetryAttempt = 0;
				_videoReward = ad;
				_videoReward.OnAdFullScreenContentOpened += () => RunOnMainThread(() => Log.Info("AdMob rewarded opened"));
				_videoReward.OnAdFullScreenContentClosed += () => RunOnMainThread(() => OnRewardClosed(generation));
				_videoReward.OnAdFullScreenContentFailed += error => RunOnMainThread(() => OnRewardFailedToShow(error, generation));
				_videoReward.OnAdClicked += () => RunOnMainThread(() => Log.Info("AdMob rewarded clicked"));
				_videoReward.OnAdImpressionRecorded += () => RunOnMainThread(() => Log.Info("AdMob rewarded impression recorded"));
				_videoReward.OnAdPaid += OnAdPaid;
			});
		});
	}

	void OnRewardClosed(int generation) {
		if(generation != _requestGeneration) {
			return;
		}
		_isShowingReward = false;
		RequestVideoReward();
	}

	void OnRewardFailedToShow(AdError error, int generation) {
		if(generation != _requestGeneration) {
			return;
		}
		Log.Info($"AdMob rewarded show failed [{error?.GetMessage()}]");
		_isShowingReward = false;
		InvokeRewardFailedEvent();
		RequestVideoReward();
	}

	void CheckRequestVideoInterstitial() {
		if((_videoInterstitial == null || !_videoInterstitial.CanShowAd()) && !_isLoadingInterstitial && !_isShowingInterstitial) {
			RequestVideoInterstitial();
		}
	}

	void RequestVideoInterstitial() {
		if(string.IsNullOrEmpty(PlacementIdVideo)) {
			Log.Warning(false, this, "AdMob interstitial placement id is empty.");
			return;
		}
		int generation = _requestGeneration;
		_isLoadingInterstitial = true;
		SafeDestroyInterstitial();
		InterstitialAd.Load(PlacementIdVideo, BuildAdRequest(), (ad, error) => {
			RunOnMainThread(() => {
				if(generation != _requestGeneration) {
					ad?.Destroy();
					return;
				}
				_isLoadingInterstitial = false;
				if(error != null || ad == null) {
					Log.Info($"AdMob interstitial load failed [{error?.GetMessage()}]");
					RetryInterstitialWithBackoff(generation).Forget();
					return;
				}
				_interstitialRetryAttempt = 0;
				_videoInterstitial = ad;
				_videoInterstitial.OnAdFullScreenContentOpened += () => RunOnMainThread(() => Log.Info("AdMob interstitial opened"));
				_videoInterstitial.OnAdFullScreenContentClosed += () => RunOnMainThread(() => OnInterstitialClosed(generation));
				_videoInterstitial.OnAdFullScreenContentFailed += error => RunOnMainThread(() => OnInterstitialFailedToShow(error, generation));
				_videoInterstitial.OnAdClicked += () => RunOnMainThread(() => Log.Info("AdMob interstitial clicked"));
				_videoInterstitial.OnAdImpressionRecorded += () => RunOnMainThread(() => Log.Info("AdMob interstitial impression recorded"));
				_videoInterstitial.OnAdPaid += OnAdPaid;
				if(_showInterstitialWhenLoaded && !_isShowingInterstitial && _videoInterstitial.CanShowAd()) {
					_showInterstitialWhenLoaded = false;
					_isShowingInterstitial = true;
					_videoInterstitial.Show();
				}
			});
		});
	}

	void OnInterstitialClosed(int generation) {
		if(generation != _requestGeneration) {
			return;
		}
		_isShowingInterstitial = false;
		InvokeInterstitialAdsClose();
		RequestVideoInterstitial();
	}

	void OnInterstitialFailedToShow(AdError error, int generation) {
		if(generation != _requestGeneration) {
			return;
		}
		Log.Info($"AdMob interstitial show failed [{error?.GetMessage()}]");
		_isShowingInterstitial = false;
		_showInterstitialWhenLoaded = false;
		InvokeInterstitialAdsClose();
		RequestVideoInterstitial();
	}

	AdRequest BuildAdRequest() {
		return new AdRequest();
	}

	async UniTaskVoid RetryBannerWithBackoff(int generation) {
		_bannerRetryAttempt++;
		var seconds = GetRetryDelay(_bannerRetryAttempt);
		await UniTask.Delay(TimeSpan.FromSeconds(seconds));
		if(generation == _requestGeneration && CanLoadAds()) {
			RequestBanner();
		}
	}

	async UniTaskVoid RetryRewardWithBackoff(int generation) {
		_rewardRetryAttempt++;
		var seconds = GetRetryDelay(_rewardRetryAttempt);
		await UniTask.Delay(TimeSpan.FromSeconds(seconds));
		if(generation == _requestGeneration && CanLoadAds()) {
			RequestVideoReward();
		}
	}

	async UniTaskVoid RetryInterstitialWithBackoff(int generation) {
		_interstitialRetryAttempt++;
		var seconds = GetRetryDelay(_interstitialRetryAttempt);
		await UniTask.Delay(TimeSpan.FromSeconds(seconds));
		if(generation == _requestGeneration && CanLoadAds()) {
			RequestVideoInterstitial();
		}
	}

	float GetRetryDelay(int attempt) {
		return Mathf.Min(delayReloadAdsWhenLoadFailed * Mathf.Pow(2f, attempt - 1), maxRetryDelaySeconds);
	}

	bool IsPlacementMissing(PlacementIdGroup placement) {
		return placement == null || placement.banner == null || placement.video == null || placement.reward == null;
	}

	void OnAdPaid(AdValue adValue) {
		RunOnMainThread(() => Log.Info($"AdMob paid event value [{adValue.Value}] currency [{adValue.CurrencyCode}] precision [{adValue.Precision}]"));
	}

	void RunOnMainThread(Action action) {
		GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(action);
	}

	void DestroyAds() {
		SafeDestroyBanner();
		SafeDestroyReward();
		SafeDestroyInterstitial();
	}

	void SafeDestroyBanner() {
		if(_banner == null) return;
		_banner.Destroy();
		_banner = null;
		_isBannerLoaded = false;
	}

	void SafeDestroyReward() {
		if(_videoReward == null) return;
		_videoReward.Destroy();
		_videoReward = null;
	}

	void SafeDestroyInterstitial() {
		if(_videoInterstitial == null) return;
		_videoInterstitial.Destroy();
		_videoInterstitial = null;
	}

	[Serializable]
	public class PlacementIdGroup {
		public StringVariable banner;
		public StringVariable video;
		public StringVariable reward;
	}
}
