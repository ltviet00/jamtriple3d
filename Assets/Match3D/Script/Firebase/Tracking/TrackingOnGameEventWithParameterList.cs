using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

///<summary> Listen to game event, and track Firebase event, has list of parameter </summary>
public class TrackingOnGameEventWithParameterList : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	[SerializeField] GameEvent eventToListen;
	[SerializeField] StringVariable trackingEventNameVar;

	[Header("List of parameter")]
	[SerializeField] List<IntegerParameter> integerParamList;

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
		eventToListen.Subscribe(OnEventRaised);
	}
	#endregion// Init config


	#region Private
	void OnEventRaised() {
		FirebaseAnalytics.LogEvent(trackingEventNameVar.Value);
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	class IntegerParameter {
		public StringVariable name;
		public IntegerParameter value;
	}
	#endregion//Classes
}
