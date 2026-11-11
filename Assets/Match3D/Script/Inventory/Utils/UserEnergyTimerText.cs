using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.Events;
using System;
using System.Text;

/// <summary>
/// Show timer text counting down, base on UserEnertyInventory
/// </summary>
public class UserEnergyTimerText : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserEnergyInventory userEnergyInventory;
	[SerializeField] UserUnlimitEnergyInventory userUnlimitEnergyInventory;
	[SerializeField] IntegerVariable userRemainEnergyVar;

	[Header("Refs")]
	[SerializeField] TMP_Text targetText;

	[Header("Config")]
	[SerializeField] string textWhenFullEnergy = "FULL";
	[SerializeField] bool keepHourTextIfZero = false;

	[Header("Event in")]
	[SerializeField] GameEvent expandUnlimitDurationEvent;

	[Header("Event out")]
	[SerializeField] UnityEvent OnGainOneEnergyByWaitingEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheReferences();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void CacheReferences() {
		if(targetText == null) {
			targetText = GetComponent<TMP_Text>();
			if(targetText != null) {
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif//UNITY_EDITOR
			}
		}
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		userEnergyInventory.CheckFillEneryAfterIdleTime();
		userRemainEnergyVar.OnValueChangedHandler += OnRemainEnergyChanged;
		expandUnlimitDurationEvent.Subscribe(OnUserExpandUnlimitDuration);
		OnRemainEnergyChanged();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
		userRemainEnergyVar.OnValueChangedHandler -= OnRemainEnergyChanged;
		expandUnlimitDurationEvent.Unsubscribe(OnUserExpandUnlimitDuration);
	}
	#endregion// Init config


	#region Private
	void OnUserExpandUnlimitDuration() {
		// let simple call OnRemainEnergyChanged(), to recheck everything and config text
		OnRemainEnergyChanged();
	}

	void OnRemainEnergyChanged() {
		ClearRunningTasks();
		if(userUnlimitEnergyInventory.IsInUnlimitEnergyDuration()) {
			//CountDownForUnlimitCase().AttachExternalCancellation(_token.Token);
			CountDownForUnlimitCase().Forget();
			return;
		}
		// Full, nothing to do.
		if(userEnergyInventory.IsFull()) {
			targetText.text = textWhenFullEnergy;
		}
		// not full, start timer
		else {
			//CountDownTimerAsync().AttachExternalCancellation(_token.Token);
			CountDownTimerAsync().Forget();
		}
	}

	async UniTask CountDownForUnlimitCase() {
		// in case user minimize the app, lose focus, let alway re-calculate each seconds
		// dont worry about this while(true) loop, we alway break it OnPageClose (clear running tasks)
		var expireTime = userUnlimitEnergyInventory.GetExpireUnlimitEnergyTime();
		while(true) {
			var currentTime = DateTime.Now;
			var timeToWait = expireTime - currentTime;
			// check break
			if(timeToWait.TotalSeconds <= 0) {
				break;
			}
			// format text
			var text = CreateTimerText(timeToWait);
			targetText.text = text;
			// wait and deduct time
			await UniTask.Delay(1000, cancellationToken: _token.Token);
		}
		// reach here mean out of unlimit time. Simple call RemainEnergyChanged to check other status and start other clock
		OnRemainEnergyChanged();
	}

	async UniTask CountDownTimerAsync() {
		// in case user minimize the app, lose focus, let alway re-calculate each seconds
		// dont worry about this while(true) loop, we alway break it OnPageClose (clear running tasks)
		var lastDeduct = userEnergyInventory.GetLastTimeDeductEnergy();
		var nextTimeGetOneEnergy = lastDeduct.AddMinutes(userEnergyInventory.TimeToGetOneEnergyInMinutes);
		while(true) {
			var currentTime = DateTime.Now;
			var timeToWait = nextTimeGetOneEnergy - currentTime;
			// check break
			if(timeToWait.TotalSeconds <= 0) {
				break;
			}
			// format text
			var text = CreateTimerText(timeToWait);
			targetText.text = text;
			// wait and deduct time
			await UniTask.Delay(1000, cancellationToken: _token.Token);
		}
		//reach here mean got one energy. No need to check energy, the OnRemainEnergChanged() will auto get called to run another counting (if need)
		OnGainOneEnergyByWaitingEvent.Invoke();
		userEnergyInventory.IncreaseOneEnergyAndSaveCurrentTime();
	}

	string CreateTimerText(TimeSpan timeToWait) {
		var hour = timeToWait.Hours;
		var minute = timeToWait.Minutes;
		var second = timeToWait.Seconds;
		var builder = new StringBuilder();
		const string TWO_DOT = ":";
		// hour
		if(hour == 0 && keepHourTextIfZero) {
			const string ZERO_HOUR = "00:";
			builder.Append(ZERO_HOUR);
		}
		else if(hour > 0) {
			builder.Append(hour >= 10 ? hour.ToString() : $"0{hour}");
			builder.Append(TWO_DOT);
		}
		// minute and second
		builder.Append(minute >= 10 ? minute.ToString() : $"0{minute}");
		builder.Append(TWO_DOT);
		builder.Append(second >= 10 ? second.ToString() : $"0{second}");
		return builder.ToString();
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
