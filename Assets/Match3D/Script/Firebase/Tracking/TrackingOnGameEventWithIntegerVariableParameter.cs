using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

///<summary> Listen to game event, and track Firebase event and one parameter  </summary>
public class TrackingOnGameEventWithIntegerVariableParameter : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	[SerializeField] GameEvent eventToListen;
	[SerializeField] StringVariable trackingEventNameVar;
	[SerializeField] StringVariable parameterName;
	[SerializeField] IntegerVariable parameterValue;

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
		eventToListen.Subscribe(OnEventRaised);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		eventToListen.Unsubscribe(OnEventRaised);
	}
	#endregion// Init config


	#region Private
	void OnEventRaised() {
		// NOTES: don't know why Firebase can NOT track some event, so we change the format (not upper case and user underscore, eg: LevelWin -> level_win
		// and put the paramter to the event name as well.  (eg: level_win_15)
		var parameter = new Parameter(parameterName.Value, parameterValue.Value);
		//FirebaseAnalytics.LogEvent(trackingEventNameVar.Value, parameter);
		var combineEventAndParameter = $"{trackingEventNameVar.Value}_{parameterValue.Value}";
		FirebaseAnalytics.LogEvent(combineEventAndParameter, parameter);

		if(logWhenTrack) {
			Log.Info($"Tracking, event [{combineEventAndParameter}]");
		}
	}
	#endregion//Private
}
