using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using System;

///<summary> Listen to event, delay the handler </summary>
public class GameEventListenerAndDelayCall : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent eventIn;

	[Header("Response, config")]
	[SerializeField] float delayTime;
	[SerializeField] UnityEvent response;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	private void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	private void OnEnable() {
		if(isFailedConfig)
			return;
		eventIn.Subscribe(OnEventRaised);
	}

	private void OnDisable() {
		if(isFailedConfig)
			return;
		eventIn.Unsubscribe(OnEventRaised);
	}
	#endregion// Init config


	#region Private
	CompositeDisposable _allCoroutines = new CompositeDisposable();

	void OnEventRaised() {
		Observable.FromCoroutine(IE_DelayCall).Subscribe().AddTo(_allCoroutines);
	}

	IEnumerator IE_DelayCall() {
		var timer = delayTime;
		while(timer > 0f) {
			yield return null;
			timer -= Time.deltaTime;
		}
		response.Invoke();
	}
	#endregion//Private
}
