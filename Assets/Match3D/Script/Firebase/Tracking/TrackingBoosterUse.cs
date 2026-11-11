using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

///<summary> Tracking when user success use booster </summary>
public class TrackingBoosterUse : MonoBehaviour {

	#region Init, config
	[Header("Config")]
	[SerializeField] GameEvent successUseBoosterEvent;
	[SerializeField] StringVariable trackingEventNameVar;

	[Header("Booster parameter")]
	[SerializeField] StringVariable parameterBoosterUseName;
	[SerializeField] BoosterIDVariable userSelectedBoosterIDVar;

	[Header("Current level parameter")]
	[SerializeField] StringVariable parameterLevelName;
	[SerializeField] IntegerVariable userSelectedLevelVar;

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
		successUseBoosterEvent.Subscribe(OnEventRaised);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		successUseBoosterEvent.Unsubscribe(OnEventRaised);
	}
	#endregion// Init config


	#region Private
	void OnEventRaised() {
		// NOTES: don't know why Firebase can track some event, so we change the format (not upper case and user underscore, eg: LevelWin -> level_win
		// and put the paramter to the event name as well.  (eg: level_win_15)
		var boosterParam = new Parameter(parameterBoosterUseName.Value, userSelectedBoosterIDVar.Value.boosterName);
		var currentLevelParam = new Parameter(parameterLevelName.Value, userSelectedLevelVar.Value);
		//FirebaseAnalytics.LogEvent(trackingEventNameVar.Value, boosterParam, currentLevelParam);
		var combineEventNameAndParameter = $"{trackingEventNameVar.Value}_{userSelectedBoosterIDVar.Value.boosterName}_{userSelectedLevelVar.Value}";
		FirebaseAnalytics.LogEvent(combineEventNameAndParameter, boosterParam, currentLevelParam);
		if(logWhenTrack) {
			//Log.Info($"Tracking, event [{trackingEventNameVar.Value}], booster [{userSelectedBoosterIDVar.Value.boosterName}]");
			Log.Info($"Tracking, event [{combineEventNameAndParameter}]");
		}
	}
	#endregion//Private
}
