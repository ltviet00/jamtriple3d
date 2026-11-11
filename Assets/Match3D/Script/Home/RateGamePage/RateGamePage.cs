#if UNITY_ANDROID
using Google.Play.Review;
#endif//UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RateGamePage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserWinStreakData winStreakData;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		if(Application.platform == RuntimePlatform.Android) {
#if UNITY_ANDROID
			CheckSetupAndroidReview();
#endif//UNITY_ANDROID
		}
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region UI callbacks
	///<summary> Config Later button to invoke this </summary>
	public void In_OnUserPressLater() {
		if(isFailedConfig)
			return;
		// reset winstreak, so we won't show the 
		winStreakData.ResetWinStreak();
	}

	///<summary> Config Never button to invoke this </summary>
	public void In_OnUserPressNever() {
		if(isFailedConfig)
			return;
		winStreakData.OnUserSelectNeverRateGame();
	}

	///<summary> Config Later button to invoke this </summary>
	public void In_OnUserPressYes() {
		if(isFailedConfig)
			return;
		// open rateGame
		winStreakData.OnUserRateGame();
		switch(Application.platform) {
			case RuntimePlatform.Android:
				OpenRateGameAndroid();
				break;

			case RuntimePlatform.IPhonePlayer:
				OpenRateGameIOS();
				break;

			default:
				Log.Warning(false, this, $"RateGame not handle for platform [{Application.platform}]");
				break;
		}
	}
	#endregion//UI callbacks


	#region Private
	void OpenRateGameAndroid() {
#if UNITY_ANDROID
		IE_OpenReviewForm();
#endif//UNITY_ANDROID
	}

	void OpenRateGameIOS() {
#if UNITY_IOS
		UnityEngine.iOS.Device.RequestStoreReview();
#endif//UNITY_IOS
	}
	#endregion//Private


#if UNITY_ANDROID
	///<summary> Create 1 instance of Android ReviewManager. Read more in manual https://developer.android.com/guide/playcore/in-app-review/unity </summary>
	static ReviewManager s_reviewManager = null;
	static PlayReviewInfo s_reviewInfo = null;

	static bool CanOpenAndroidInAppReview() {
		return s_reviewManager != null && s_reviewInfo != null;
	}

	static void CheckSetupAndroidReview() {
		// already init, then ignore
		if(s_reviewManager != null && s_reviewInfo != null) {
			return;
		}
		s_reviewManager = new ReviewManager();
		IE_RequestAndroidReviewFlow();
	}

	static IEnumerator IE_RequestAndroidReviewFlow() {
		var request = s_reviewManager.RequestReviewFlow();
		yield return request;
		if(request.Error != ReviewErrorCode.NoError) {
			Log.Error(false, $"Failed to request ReviewFlow. Error [{request.Error.ToString()}]");
			yield break;
		}
		s_reviewInfo = request.GetResult();
	}

	static IEnumerator IE_OpenReviewForm() {
		if(!CanOpenAndroidInAppReview()) {
			Log.Error(false, $"NOt init the ReviewManager yet !");
			yield break;
		}
		var launchProcess = s_reviewManager.LaunchReviewFlow(s_reviewInfo);
		yield return launchProcess;
		// reset the object
		s_reviewInfo = null;
		if(launchProcess.Error != ReviewErrorCode.NoError) {
			Log.Error(false, $"Failed to open ReviewFlow. Error [{launchProcess.Error.ToString()}]");
			yield break;
		}
		// The flow has finished. The API does not indicate whether the user
		// reviewed or not, or even whether the review dialog was shown. Thus, no
		// matter the result, we continue our app flow.
	}
#endif//UNITY_ANDROID

}
