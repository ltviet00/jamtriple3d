using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Config content for timer text of Weekly event UIBar.
/// It's countdown clock timer
/// </summary>
public class WeeklyEventTimerText : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] WeeklyEventDataSaver dataSaver;

	[Header("Refs")]
	[SerializeField] TMP_Text displayText;

	[Header("Event out")]
	public UnityEvent onFinishCountingHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		// caching
		if(displayText == null) {
			displayText = GetComponent<TMP_Text>();
		}
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		PeriodicUpdateText().Forget();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	async UniTask PeriodicUpdateText() {
		// in case user minimize the app, lose focus, let alway re-calculate each seconds
		// dont worry about this while(true) loop, we alway break it OnPageClose (clear running tasks)
		var eventEndTime = dataSaver.GetEventEndDate();
		while(true) {
			var currentTime = DateTime.Now;
			var remainTime = eventEndTime - currentTime;
			// if out of time, then break
			if(remainTime.TotalSeconds <= 0) {
				onFinishCountingHandler.Invoke();
				break;
			}
			// config text
			var text = FormatRemainTime(remainTime);
			displayText.text = text;
			// optimize if d:h:m then don't need to check every second
			var interval = remainTime.Days > 0 ? 60000 : 1000;
			await UniTask.Delay(interval, cancellationToken: _disableToken.Token);
		}
	}

	string FormatRemainTime(TimeSpan duration) {
		var builder = new StringBuilder();
		const string Space = " ";
		// for now, just these cases:
		// if day > 0, then "1d 5h 10m"
		if(duration.Days > 0) {
			var day = duration.Days;
			var hour = duration.Hours;
			var minute = duration.Minutes;
			//const string TWO_DOT = ":";
			builder.Append($"{day}d ");
			builder.Append(hour >= 10 ? $"{hour}h " : $"0{hour}h ");
			builder.Append(minute >= 10 ? $"{minute}m" : $"0{minute}m");
		}
		// if day == 0, then 5h 15m 20s
		else {
			var hour = duration.Hours;
			var minute = duration.Minutes;
			var second = duration.Seconds;
			//builder.Append(hour >= 10 ? $"{hour}h" : $"0{hour}h");
			builder.Append($"{hour}h ");
			builder.Append(minute >= 10 ? $"{minute}m " : $"0{minute}m ");
			builder.Append(second >= 10 ? $"{second}s" : $"0{second}s");
		}
		return builder.ToString();
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
