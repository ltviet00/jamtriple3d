using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Use for daily reward, show the time to midnight, when done it will enable next reward.
/// Counting also config target text
/// </summary>
public class AutoCountDownToMidnight : MonoBehaviour {
	#region Init, config
	[Header("Resf")]
	[SerializeField] TMP_Text timerText;

	[Header("Event out")]
	[SerializeField] UnityEvent onFinishCountingHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponent();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		StartClock();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}

	void CacheComponent() {
		if(timerText == null) {
			timerText = GetComponent<TMP_Text>();
		}
	}
	#endregion// Init config


	#region Private
	void StartClock() {
		ClearRunningTasks();
		RunCountDownClock().Forget();
	}

	async UniTask RunCountDownClock() {
		// in case user minimize the app, lose focus, let alway re-calculate each seconds
		// dont worry about this while(true) loop, we alway break it OnPageClose (clear running tasks)
		while(true) {
			var timeToWait = GetTimespanToMidnight();
			// check break
			if(timeToWait.TotalSeconds <= 0) {
				break;
			}
			// format text
			var hour = timeToWait.Hours;
			var minute = timeToWait.Minutes;
			var second = timeToWait.Seconds;

			var builder = new StringBuilder();
			builder.Append(hour >= 10 ? $"{hour}h " : $"0{hour}h ");
			builder.Append(minute >= 10 ? $"{minute}m " : $"0{minute}m ");
			builder.Append(second >= 10 ? $"{second}s" : $"0{second}s");
			timerText.text = builder.ToString();
			// wait and deduct time
			await UniTask.Delay(1000, cancellationToken: _disableToken.Token);
		}
		// raise event when finish counting
		onFinishCountingHandler.Invoke();
	}

	TimeSpan GetTimespanToMidnight() {
		var currentTime = DateTime.Now;
		// we take Date from currentTime, then add 1day, then it will be next midnight
		// NOTES : currentTime.Date -> will take only the date part from the currentTime, so hour,minute, second is zero, mean midnight
		var tomorrow = currentTime.Date.AddDays(1);
		var result = tomorrow - currentTime;
		return result;
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
