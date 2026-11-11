using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Listen to Match success event, increase combo count. Stopwatch for combo cool down
/// </summary>
public class ScoreComboCounter : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent successMatchToyPairEvent;
	[SerializeField] GameEvent gamePauseEvent;
	[SerializeField] GameEvent gameUnPauseEvent;

	[Header("ShareData - out")]
	[SerializeField] IntegerVariable comboCountVar;
	[SerializeField] FloatVariable remainComboTimeInPercentVar;

	[Header("Config")]
	[SerializeField] FloatVariable beginCoolDownTimeCfg;
	[SerializeField] FloatVariable minCoolDownTimeCfg;
	[SerializeField] FloatVariable refillTimeWhenContinueComboCfg;
	[SerializeField] IntegerVariable matchCountToReachMinDurationCfg;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		successMatchToyPairEvent.Subscribe(OnUserMatchSucces);
		gamePauseEvent.Subscribe(OnGamePause);
		gameUnPauseEvent.Subscribe(OnGameUnPause);
		ResetShareVarValues();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		successMatchToyPairEvent.Unsubscribe(OnUserMatchSucces);
		gamePauseEvent.Unsubscribe(OnGamePause);
		gameUnPauseEvent.Unsubscribe(OnGameUnPause);
		ResetShareVarValues();
		ClearRunningTasks();
	}

	void ResetShareVarValues() {
		comboCountVar.Value = 0;
		remainComboTimeInPercentVar.Value = 0f;
	}
	#endregion// Init config


	#region Private
	bool _isGamePause = false;

	void OnGamePause() {
		_isGamePause = true;
	}

	void OnGameUnPause() {
		_isGamePause = false;
	}

	void OnUserMatchSucces() {
		// increase combo. reset timer
		comboCountVar.Value++;
		// calculate comboDuration, need -1, cause if we got combo at first (=1), then timer start from beginCoolDown
		var ratio = (float)(comboCountVar.Value - 1) / (float)matchCountToReachMinDurationCfg.Value;
		var comboDuration = Mathf.Lerp(beginCoolDownTimeCfg.Value, minCoolDownTimeCfg.Value, ratio);
		// clear current countdown process, start new
		ClearRunningTasks();
		//CountDownAsync(comboDuration).AttachExternalCancellation(_token.Token);
		CountDownAsync(comboDuration).Forget();
	}

	async UniTask CountDownAsync(float duration) {
		// in case combo is counting down, and user got another match. Let do refil effect
		if(remainComboTimeInPercentVar.Value > 0f) {
			var refillTimer = 0f;
			var currentComboPercent = remainComboTimeInPercentVar.Value;
			while(refillTimer < refillTimeWhenContinueComboCfg.Value) {
				await UniTask.Yield(_token.Token);
				// check pause
				if(_isGamePause) {
					continue;
				}
				// normal
				refillTimer += Time.deltaTime;
				var ratio = refillTimer / refillTimeWhenContinueComboCfg.Value;
				var targetPercent = Mathf.Lerp(currentComboPercent, 1f, ratio);
				remainComboTimeInPercentVar.Value = targetPercent;
			}
		}

		// normal case
		var timer = duration;
		remainComboTimeInPercentVar.Value = 1f;
		while(timer > 0f) {
			await UniTask.Yield(_token.Token);
			// check pause
			if(_isGamePause) {
				continue;
			}
			timer -= Time.deltaTime;
			remainComboTimeInPercentVar.Value = timer / duration;
		}
		// finish timer, reset combo
		remainComboTimeInPercentVar.Value = 0f;
		comboCountVar.Value = 0;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
