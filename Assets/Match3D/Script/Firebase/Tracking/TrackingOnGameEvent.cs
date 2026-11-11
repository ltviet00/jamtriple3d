using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

///<summary> Listen to game event, and track Firebase evet </summary>
public class TrackingOnGameEvent : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	[SerializeField] GameEvent eventToListen;
	[SerializeField] StringVariable trackingEventNameVar;

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
		FirebaseAnalytics.LogEvent(trackingEventNameVar.Value);
		if(logWhenTrack) {
			Log.Info($"Tracking, event [{trackingEventNameVar.Value}]");
		}
	}
	#endregion//Private
}
