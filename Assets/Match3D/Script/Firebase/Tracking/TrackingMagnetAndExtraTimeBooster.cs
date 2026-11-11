using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

/// <summary>
/// Cause magnet and extraTime will be triggered automatically at level start, we just need to check if user enable them, and track
/// </summary>
public class TrackingMagnetAndExtraTimeBooster : MonoBehaviour {
	#region Init, config
	[Header("Data, config")]
	[SerializeField] BooleanVariable activeMagnetBoosterVar;
	[SerializeField] BooleanVariable activeExtraTimeBoosterVar;
	[SerializeField] IntegerVariable userReachLevelVar;

	[Header("BoosterID")]
	[SerializeField] BoosterID magnetID;
	[SerializeField] BoosterID extraTimeID;

	[Header("Config")]
	[SerializeField] StringVariable trackingEventNameVar;
	[SerializeField] StringVariable parameterBoosterUseName;
	[SerializeField] StringVariable parameterLevelName;

	[Header("Logging")]
	[SerializeField] bool logWhenTrack;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		CheckTracking();
	}
	#endregion// Init config


	#region Private
	void CheckTracking() {
		if(activeMagnetBoosterVar.Value) {
			TrackBoosterUse(magnetID);
		}
		if(activeExtraTimeBoosterVar.Value) {
			TrackBoosterUse(extraTimeID);
		}
	}

	void TrackBoosterUse(BoosterID boosterID) {
		var boosterParam = new Parameter(parameterBoosterUseName.Value, boosterID.boosterName);
		var currentLevelParam = new Parameter(parameterLevelName.Value, userReachLevelVar.Value + 1);
		var combineEventNameAndParameter = $"{trackingEventNameVar.Value}_{boosterID.boosterName}_{userReachLevelVar.Value + 1}";
		FirebaseAnalytics.LogEvent(combineEventNameAndParameter, boosterParam, currentLevelParam);
		if(logWhenTrack) {
			//Log.Info($"Tracking, event [{trackingEventNameVar.Value}], booster [{userSelectedBoosterIDVar.Value.boosterName}]");
			Log.Info($"Tracking, event [{combineEventNameAndParameter}]");
		}
	}
	#endregion//Private
}
