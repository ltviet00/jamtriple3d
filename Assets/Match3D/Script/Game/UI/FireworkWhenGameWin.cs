using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.Events;

public class FireworkWhenGameWin : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent gameWinEvent;

	[Header("Refs, config")]
	[SerializeField] float fireworkDuration = 8f;
	[SerializeField] GameObject fireworkVfxRef;

	[Header("Unity event out")]
	[SerializeField] UnityEvent requestOpenPopupAfterFireworkVfxDone;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		gameWinEvent.Subscribe(OnGameWin);
		// default disable firework
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		gameWinEvent.Unsubscribe(OnGameWin);
	}
	#endregion// Init config


	#region Private
	void OnGameWin() {
		fireworkVfxRef.SetActive(true);
		DelayRequestOpenNextPopup().Forget();
	}

	async UniTaskVoid DelayRequestOpenNextPopup() {
		await UniTask.Delay(TimeSpan.FromSeconds(fireworkDuration));
		requestOpenPopupAfterFireworkVfxDone.Invoke();
	}
	#endregion//Private
}
