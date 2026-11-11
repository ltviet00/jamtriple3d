using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Count the raised of the event, then raise unityEvent 
/// </summary>
public class GameEventCounter : MonoBehaviour {
	#region Init
	[Header("Event to track")]
	[SerializeField] GameEvent gameEVent;

	[Header("Unity event out")]
	[SerializeField] UnityEvent OnFinishCountHandler;

	[Header("Config")]
	[SerializeField] int threshold;
	[SerializeField] float delayBeforeInvoke = 0f;

	void OnValidate() {
		Log.ValidateNotNull(gameEVent, this, "gameEVent");
	}

	bool IsFailedConfig() {
		return gameEVent == null;
	}

	void OnEnable() {
		if(IsFailedConfig())
			return;
		ActiveWorking();
	}

	void OnDisable() {
		if(IsFailedConfig())
			return;
		DeactiveWorking();
	}

	void ActiveWorking() {
		gameEVent.Subscribe(OnEventInvoke);
		_count = 0;
		ClearCorountines();
	}

	void DeactiveWorking() {
		gameEVent.Unsubscribe(OnEventInvoke);
		_count = 0;
		ClearCorountines();
	}
	#endregion//Init


	#region Private
	int _count = 0;

	void OnEventInvoke() {
		_count++;
		if(_count >= threshold) {
			if(delayBeforeInvoke > 0f) {
				ClearCorountines();
				_delayCorountine = Observable.FromMicroCoroutine(_ => IE_DelayInvoke(delayBeforeInvoke)).Subscribe();
			}
			else {
				OnFinishCountHandler.Invoke();
				enabled = false;
			}
		}
	}

	IEnumerator IE_DelayInvoke(float delay) {
		yield return null;
		var count = 0f;
		while(count < delay) {
			count += Time.deltaTime;
			yield return null;
		}
		// finished counting
		OnFinishCountHandler.Invoke();
		enabled = false;
	}

	IDisposable _delayCorountine;
	void ClearCorountines() {
		_delayCorountine?.Dispose();
		_delayCorountine = null;
	}
	#endregion//Private
}
