using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.Events;
using System;
using System.Text;

/// <summary>
/// Show timertext count down base on limit offer 
/// </summary>
public class UserLimitOfferTimerText : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserLimitOfferInventory limitOfferInventory;

	[Header("Refs")]
	[SerializeField] TMP_Text targetText;

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
		//CountDownTimerAsync().AttachExternalCancellation(_token.Token);
		// Use Forget() to fix exception : the operation already cancelled. we already use _token inside CountDownTimerAsync, so no need attach here
		CountDownTimerAsync().Forget();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	async UniTask CountDownTimerAsync() {
		var endOfferTime = limitOfferInventory.GetEndOfferTime();
		// in case user minimize the app, lose focus, let alway re-calculate each seconds
		// dont worry about this while(true) loop, we alway break it OnPageClose (clear running tasks)
		while(true) {
			var currentTime = DateTime.Now;
			var timeToWait = endOfferTime - currentTime;
			// check break
			if(timeToWait.TotalSeconds <= 0) {
				// just call GetEndOfferTime one more to let it reset the offer time
				endOfferTime = limitOfferInventory.GetEndOfferTime();
			}
			// format text
			var hour = timeToWait.Hours;
			var minute = timeToWait.Minutes;
			var second = timeToWait.Seconds;
			var builder = new StringBuilder();
			builder.Append(hour >= 10 ? $"{hour}:" : $"0{hour}:");
			builder.Append(minute >= 10 ? $"{minute}:" : $"0{minute}:");
			builder.Append(second >= 10 ? second.ToString() : $"0{second}");
			targetText.text = builder.ToString();
			// wait and deduct time
			await UniTask.Delay(1000, cancellationToken: _token.Token);
		}
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
