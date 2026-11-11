using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Work with Image unity, with imageType = filled.
/// Some functions for animate filling progress (eg; fill from 30% - 90% in 0.5s).
/// Or clamp the progress
/// </summary>
public class ImageFillAnimator : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Image displayImage;

	[Header("Config")]
	///<summary> In Match3D, the levelChest, the fillImage range is not full circle (cause it got some path hidden behind)
	///so we want it to run in customRange (0.2 - 0.8) </summary>
	[SerializeField] Vector2 customFillRange = new Vector2(0f, 1f);

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponents();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void CacheComponents() {
		if(displayImage == null) {
			displayImage = GetComponent<Image>();
		}
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Direct set progress bar value (value be claimped 0-1) </summary>
	public void SetProgress(float percent) {
		if(isFailedConfig) {
			return;
		}
		percent = Mathf.Clamp01(percent);
		var toCustomRange = Mathf.Lerp(customFillRange.x, customFillRange.y, percent);
		displayImage.fillAmount = toCustomRange;
	}

	public async UniTask FillProgressAnimation(float fromPercent, float toPercent, float duration) {
		if(isFailedConfig)
			return;
		var from = Mathf.Lerp(customFillRange.x, customFillRange.y, fromPercent);
		var to = Mathf.Lerp(customFillRange.x, customFillRange.y, toPercent);
		var timer = 0f;
		while(timer < duration) {
			await UniTask.Yield(cancellationToken: _disableToken.Token);
			timer += Time.deltaTime;
			var ratio = timer / duration;
			var valueForImage = Mathf.Lerp(from, to, ratio);
			displayImage.fillAmount = valueForImage;
		}
	}
	#endregion//Public


	#region Private
	/////<summary> Turn 0-1 value to the customFillRange value </summary>
	//float ToCustomFillRangeValue(float value) {
	//	var ratio = (value - 0f) / 1f;
	//	var customRangeLength = customFillRange.y - customFillRange.x;
	//	var result = customFillRange.x + customRangeLength * ratio;
	//	return result;
	//}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
