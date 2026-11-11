using System;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

[CreateAssetMenu(fileName = "AdmobConsentManager", menuName = "Game/Ads/AdmobConsentManager")]
public class AdmobConsentManager : ScriptableObject {
	[Header("UMP")]
	[SerializeField] bool enableUmpConsent = true;
	[SerializeField] bool tagForUnderAgeOfConsent = false;
	[SerializeField] bool enableConsentDebugInEditor = true;
	[SerializeField] bool resetConsentOnInitInEditor = false;
	[SerializeField] List<string> testDeviceHashedIds = new List<string>();
	[SerializeField] DebugGeography debugGeography = DebugGeography.Disabled;

	[Header("iOS ATT")]
	[SerializeField] bool requestAttBeforeMobileAdsInit = true;
	[SerializeField] float attTimeoutSeconds = 8f;

	public bool CanRequestAds { get; private set; }
	public bool IsReady { get; private set; }
	public bool IsPrivacyOptionsRequired =>
		ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

	bool _isPreparing;
	readonly List<Action> _prepareCallbacks = new List<Action>();

	public void Prepare(Action onFinished) {
		if(onFinished != null) {
			_prepareCallbacks.Add(onFinished);
		}
		if(IsReady) {
			InvokePrepareCallbacks();
			return;
		}
		if(_isPreparing) {
			return;
		}
		_isPreparing = true;
		CanRequestAds = false;
		PrepareInternal();
	}

	void PrepareInternal() {
#if UNITY_EDITOR
		if(resetConsentOnInitInEditor) {
			ConsentInformation.Reset();
		}
#endif
		if(!enableUmpConsent) {
			RequestAttIfNeeded(() => Finish(true));
			return;
		}

		var request = new ConsentRequestParameters {
			TagForUnderAgeOfConsent = tagForUnderAgeOfConsent
		};

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if(enableConsentDebugInEditor || testDeviceHashedIds.Count > 0) {
			request.ConsentDebugSettings = new ConsentDebugSettings {
				DebugGeography = debugGeography,
				TestDeviceHashedIds = testDeviceHashedIds
			};
		}
#endif

		ConsentInformation.Update(request, updateError => {
			if(updateError != null) {
				Log.Warning(false, this, $"UMP update failed [{updateError.ErrorCode}] {updateError.Message}");
				RequestAttIfNeeded(() => Finish(ConsentInformation.CanRequestAds()));
				return;
			}

			ConsentForm.LoadAndShowConsentFormIfRequired(formError => {
				if(formError != null) {
					Log.Warning(false, this, $"UMP form failed [{formError.ErrorCode}] {formError.Message}");
				}
				RequestAttIfNeeded(() => Finish(ConsentInformation.CanRequestAds()));
			});
		});
	}

	void RequestAttIfNeeded(Action onFinished) {
#if UNITY_IOS && !UNITY_EDITOR
		if(!requestAttBeforeMobileAdsInit) {
			onFinished?.Invoke();
			return;
		}
		AdmobIosAttBridge.RequestTrackingAuthorization(attTimeoutSeconds, onFinished);
#else
		onFinished?.Invoke();
#endif
	}

	void Finish(bool canRequestAds) {
		CanRequestAds = canRequestAds;
		IsReady = true;
		_isPreparing = false;
		InvokePrepareCallbacks();
	}

	void InvokePrepareCallbacks() {
		var callbacks = _prepareCallbacks.ToArray();
		_prepareCallbacks.Clear();
		for(int i = 0; i < callbacks.Length; i++) {
			callbacks[i]?.Invoke();
		}
	}

	public void OpenPrivacyOptions(Action onFinished = null) {
		ConsentForm.ShowPrivacyOptionsForm(formError => {
			if(formError != null) {
				Log.Warning(false, this, $"UMP privacy options failed [{formError.ErrorCode}] {formError.Message}");
			}
			onFinished?.Invoke();
		});
	}
}
