using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Work with WeeklyEventUIBar, show progress of toy Collection on UI
/// </summary>
public class WeeklyEventProgressBarUI : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] ImageSlicePercentAnimator progressImage;
	[SerializeField] TMP_Text toyStatsText;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> config progress, need currentCollect and bar capacity </summary>
	public void SetProgress(int collectAmount, int capacity) {
		if(isFailedConfig)
			return;
		// text
		toyStatsText.text = $"{collectAmount}/{capacity}";
		// progress
		var percent = (float)collectAmount / (float)capacity;
		progressImage.SetProgress(percent);
	}

	///<summary> Play fillUp animation: increase progressBar + toyCollect text </summary>
	public async UniTask PlayFillUpAniamtion(int capacity, int fromAmount, int toAmount, float duration) {
		if(isFailedConfig)
			return;
		var fromPercent = (float)fromAmount / (float)capacity;
		var toPercent = (float)toAmount / (float)capacity;
		var timer = 0f;
		while(timer < duration) {
			var ratio = Mathf.Lerp(fromPercent, toPercent, timer / duration);
			// progress
			progressImage.SetProgress(ratio);
			// text. NOTES: cash to int, cause toy amount is integer, not float
			var lerpAmount = (int)Mathf.Lerp(fromAmount, toAmount, ratio);
			var text = $"{lerpAmount}/{capacity}";
			toyStatsText.text = text;
			// wait
			await UniTask.Yield(cancellationToken: _disableToken.Token);
			timer += Time.deltaTime;
		}
		// finish, set to target
		progressImage.SetProgress(toPercent);
		toyStatsText.text = $"{toAmount}/{capacity}";
	}
	#endregion//Public


	#region Private
	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
