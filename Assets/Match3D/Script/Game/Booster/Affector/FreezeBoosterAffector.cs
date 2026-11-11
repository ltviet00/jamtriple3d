using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class FreezeBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onUserRequestFreezeEvent;
	[SerializeField] GameEvent gamePauseEvent;
	[SerializeField] GameEvent gameUnpauseEvent;

	[Header("Config")]
	[SerializeField] IntegerVariable freezeDurationVar;
	[SerializeField] TransformVariable headerClockIconPositionVar;
	[SerializeField] GameObject freezeVfxAtClockPrefab;


	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onUserRequestFreezeEvent.Subscribe(OnRequestFreezeBooster);
		gamePauseEvent.Subscribe(OnGamePause);
		gameUnpauseEvent.Subscribe(OnGameUnPause);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onUserRequestFreezeEvent.Unsubscribe(OnRequestFreezeBooster);
		gamePauseEvent.Unsubscribe(OnGamePause);
		gameUnpauseEvent.Unsubscribe(OnGameUnPause);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	bool _pause = false;

	void OnRequestFreezeBooster() {
		// clear current running process before running new one
		ClearRunningTasks();
		//Process().AttachExternalCancellation(_token.Token);
		Process().Forget();
	}

	void OnGamePause() {
		_pause = true;
	}

	void OnGameUnPause() {
		_pause = false;
	}

	async UniTask Process() {
		var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
		// call freeze on CountDownClock first, release it at last
		levelTimer.SetActiveFreeze(true);
		// spawn VFX cover the clock
		var vfx = ObjectPool.SpawnObject(freezeVfxAtClockPrefab);
		vfx.transform.position = headerClockIconPositionVar.Value.position;
		// timer
		var timer = 0f;
		var duration = (float)freezeDurationVar.Value;
		while(timer < duration) {
			await UniTask.Yield(cancellationToken:_token.Token);
			timer += (_pause ? 0f : 1f) * Time.deltaTime;
		}
		// release the clock
		levelTimer.SetActiveFreeze(false);
		ClearRunningTasks();
		// disable vfx to let it back to pool
		vfx.SetActive(false);
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
