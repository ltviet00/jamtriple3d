using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class CheckRaiseWinGameEvent : MonoBehaviour {
	#region Init, config
	[Header("Event, ShareData in")]
	[SerializeField] GameEvent successCaptureToyEvent;
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Event out")]
	[SerializeField] GameEvent allTargetClearEvent;
	[SerializeField] GameEvent gameWinEvent;

	[Header("Config")]
	[SerializeField] float delayFromToyClearToGameWinEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		successCaptureToyEvent.Subscribe(OnSuccessCaptureToy);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		successCaptureToyEvent.Unsubscribe(OnSuccessCaptureToy);
	}
	#endregion// Init config


	#region Private
	void OnSuccessCaptureToy() {
		DelayCheckRaiseWingameEvent().Forget();
	}

	///<summary> Destroy toy from active list, cost 1 frame, so we need to delay 1 frame </summary>
	async UniTaskVoid DelayCheckRaiseWingameEvent() {
		await UniTask.Yield();
		if(toyStats.RemainTargetcount == 0) {
			// all toy clear raise first, some module need to stop (clock, input..)
			allTargetClearEvent.Raise();
			// then delay for VFX, and raise game win
			await UniTask.Delay(TimeSpan.FromSeconds(delayFromToyClearToGameWinEvent));
			gameWinEvent.Raise();
		}
	}
	#endregion//Private
}
