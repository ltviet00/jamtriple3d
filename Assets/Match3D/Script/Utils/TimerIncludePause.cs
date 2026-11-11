using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

/// <summary>
/// Utils component. Config duration, call StartClock, and it will raise unityEvent at last. The timer already take care the pause/unpause. Config these 2 event for it
/// </summary>
public class TimerIncludePause : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent gamePauseEvent;
	[SerializeField] GameEvent gameUnpauseEvent;

	[Header("Config")]
	public float duration;

	[Header("unity event out")]
	[SerializeField] UnityEvent OnStartClockEvent;
	[SerializeField] UnityEvent OnEndedClockEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		gamePauseEvent.Subscribe(OnGamePause);
		gameUnpauseEvent.Subscribe(OnGameUnPause);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		gamePauseEvent.Unsubscribe(OnGamePause);
		gameUnpauseEvent.Unsubscribe(OnGameUnPause);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> This function for calling from UI </summary>
	public void In_Process(float duration = 0f) {
		if(isFailedConfig)
			return;
		Process();
	}

	public void Process(float customDuration = 0f) {
		if(isFailedConfig)
			return;
		var durationToUse = customDuration > 0f ? customDuration : duration;
		//CountDownAsync(durationToUse).AttachExternalCancellation(_token.Token);
		CountDownAsync(durationToUse).Forget();
	}
	#endregion//Public


	#region Private
	bool _pause = false;

	void OnGamePause() {
		_pause = true;
	}

	void OnGameUnPause() {
		_pause = false;
	}

	async UniTask CountDownAsync(float duration) {
		// event first
		OnStartClockEvent.Invoke();
		// timer
		var timer = 0f;
		while(timer < duration) {
			await UniTask.Yield(cancellationToken: _token.Token);
			timer += (_pause ? 0f : 1f) * Time.deltaTime;
		}
		// release the clock
		ClearRunningTasks();
		// event
		OnEndedClockEvent.Invoke();
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
