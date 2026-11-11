using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UniRx;
using Cysharp.Threading.Tasks;

///<summary> Countdown time on level </summary>
public class LevelTimer : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent readyForUserToInputEvent;
	[SerializeField] GameEvent gamePauseEvent;
	[SerializeField] GameEvent gameUnpauseEvent;
	[SerializeField] GameEvent allToyClearEvent;
	[SerializeField] GameEvent toyBoxFullEvent;

	[Header("Revive")]
	[SerializeField] GameEvent userReviveByAdsOrCoinEvent;
	[SerializeField] IntegerVariable levelTimeAfterReviveVar;

	[Header("ShareData out - level time")]
	[SerializeField] LevelDataStorage levelStorage;
	[SerializeField] IntegerVariable userCurrentLevelVar;

	[Header("Event out")]
	[SerializeField] GameEvent timeUpEvent;

	[Header("ShareData - out")]
	[SerializeField] IntegerVariable remainTimeInSecondVar;
	[SerializeField] StringVariable remainTimeStringVar;

	[Header("ShareData - in")]
	[SerializeField] IntegerVariable levelTimeVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// events
		readyForUserToInputEvent.Subscribe(OnGameStart);
		gamePauseEvent.Subscribe(OnGamePause);
		gameUnpauseEvent.Subscribe(OnGameUnPause);
		allToyClearEvent.Subscribe(OnAllToyClear);
		userReviveByAdsOrCoinEvent.Subscribe(OnUserRevive);
		toyBoxFullEvent.Subscribe(OnToyBoxFull);
		// DI
		SimpleDI.RegisterType(this);
		// Level duration now calculated from LevelDataStorage when generate level, no more calculate need here
		var levelData = levelStorage.GetLevelData(userCurrentLevelVar.Value);
		levelTimeVar.Value = levelData.levelDuration;
		remainTimeInSecondVar.Value = levelTimeVar.Value;
		ConfigRemainTimeString();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		// events
		readyForUserToInputEvent.Unsubscribe(OnGameStart);
		gamePauseEvent.Unsubscribe(OnGamePause);
		gameUnpauseEvent.Unsubscribe(OnGameUnPause);
		allToyClearEvent.Unsubscribe(OnAllToyClear);
		userReviveByAdsOrCoinEvent.Unsubscribe(OnUserRevive);
		toyBoxFullEvent.Unsubscribe(OnToyBoxFull);
		// DI
		SimpleDI.UnRegisterType(this);
		// reset data
		ClearRunningTasks();
		//NOTES: nolonger need to reset sharevar. Cause other module (LevelCompletepage will need this value to display stars on UI)
		//ResetShareVars();
	}
	#endregion// Init config


	#region Public
	///<summary> For now, get call from FreezeBoosterAffector </summary>
	public void SetActiveFreeze(bool active) {
		if(isFailedConfig)
			return;
		_freeze = active;
	}

	///<summary> Increase remain time by amount, also update timerStringShareVar (to let UI update). Currently used for booster </summary>
	public void IncreaseRemainTimeAndUpdateShareVar(int seconds) {
		if(isFailedConfig)
			return;
		remainTimeInSecondVar.Value += seconds;
		ConfigRemainTimeString();
	}

	///<summary> Set remain time by amount, also update timerStringShareVar (to let UI update). Currently used for booster </summary>
	public void SetRemainTimeAndUpdateShareVar(int seconds) {
		if(isFailedConfig)
			return;
		remainTimeInSecondVar.Value = seconds;
		ConfigRemainTimeString();
	}

	///<summary> Called from Timeup page, when user revive (by ads or coin). Replace fo the event OnuserRevive </summary>
	public void ResetClockWhenUserRevive(int secondToAddMore) {
		if(isFailedConfig)
			return;
		// like a restart game
		ClearRunningTasks();
		remainTimeInSecondVar.Value += secondToAddMore;
		ReduceTimerAsync().Forget();
	}
	#endregion//Public


	#region Private
	bool _pause = false;
	bool _freeze = false;

	bool IsFreezeOrPause() {
		return _pause || _freeze;
	}

	void OnGameStart() {
		// we now wait for first touch from user to start the count.
		// but at start game, we must update the remainTimeStringVar to let UI display the timer
		// so increase +1s, then call the ReduceTimer() (it will reduce -1) and format timer text
		// UPDATE: we got prebooster extra time. so, just counting down, not reset remainTimeInSecond to levelTime
		remainTimeInSecondVar.Value++;
		ReduceTimerAsync().Forget();
	}

	void OnGamePause() {
		_pause = true;
	}

	void OnGameUnPause() {
		_pause = false;
	}

	void OnToyBoxFull() {
		// stop the timer
		ClearRunningTasks();
	}

	void OnAllToyClear() {
		// disable this component. no longer need
		enabled = false;
	}

	void OnUserRevive() {
		// like a restart game
		//StartClock();
		ClearRunningTasks();
		remainTimeInSecondVar.Value = levelTimeAfterReviveVar.Value + 1;
		ReduceTimerAsync().Forget();
	}

	//void StartClock() {
	//	_pause = false;
	//	//_corountine = Observable.Interval(TimeSpan.FromSeconds(1f)).Subscribe(_ => ReduceTimer());
	//	remainTimeInSecondVar.Value = levelTimeVar.Value;
	//}

	//void ResetShareVars() {
	//	remainTimeInSecondVar.Value = 0;
	//	remainTimeStringVar.Value = null;
	//}

	async UniTask ReduceTimerAsync() {
		while(true) {
			await UniTask.Delay(1000, cancellationToken: _token.Token);
			// check pause
			if(IsFreezeOrPause()) {
				continue;
			}
			// reduce time
			remainTimeInSecondVar.Value -= 1 * (IsFreezeOrPause() ? 0 : 1);
			// config share var
			ConfigRemainTimeString();
			// end game, user lose
			if(remainTimeInSecondVar.Value <= 0) {
				timeUpEvent.Raise();
				ClearRunningTasks();
				// remember to break the loop (for carefully, we already dispose the loop by ClearRunningTasks()
				break;
			}
		}
	}

	void ConfigRemainTimeString() {
		const string TIME_FORMAT = "{0}:{1}";
		var minute = remainTimeInSecondVar.Value / 60;
		var second = remainTimeInSecondVar.Value % 60;
		var secondText = second >= 10 ? second.ToString() : $"0{second}";
		remainTimeStringVar.Value = string.Format(TIME_FORMAT, minute, secondText);
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
