using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Work with Unity Image component ,where ImageType = slice, and we want to control it's length by giving the percent.
/// Use for case we want to show energy/life bar, but the fillImage is 9-slice.
/// </summary>
public class ImageSlicePercentAnimator : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Image displayImage;

	[Header("Config")]
	///<summary> The length of Image corressponding to percent we want to map, eg: 0% -> size = 50, 100% -> size = 250.
	///Adjust this base on your UI</summary>
	[SerializeField] Vector2 customSizeRange = new Vector2(0f, 1f);
	[SerializeField] Direction affectDirection;

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
		var toCustomRange = Mathf.Lerp(customSizeRange.x, customSizeRange.y, percent);
		SetImageSizeDelta(toCustomRange);
	}

	public async UniTask FillProgressAnimation(float fromPercent, float toPercent, float duration) {
		if(isFailedConfig)
			return;
		var from = Mathf.Lerp(customSizeRange.x, customSizeRange.y, fromPercent);
		var to = Mathf.Lerp(customSizeRange.x, customSizeRange.y, toPercent);
		var timer = 0f;
		while(timer < duration) {
			await UniTask.Yield(cancellationToken: _disableToken.Token);
			timer += Time.deltaTime;
			var ratio = timer / duration;
			var sizeForImage = Mathf.Lerp(from, to, ratio);
			SetImageSizeDelta(sizeForImage);
		}
	}
	#endregion//Public


	#region Private
	void SetImageSizeDelta(float length) {
		var size = displayImage.rectTransform.sizeDelta;
		switch(affectDirection) {
			case Direction.Horizontal:
				size.x = length;
				break;
			case Direction.Vertical:
				size.y = length;
				break;
			default:
				Log.Warning(false, this, $"Not handle for direction [{affectDirection}]");
				break;
		}
		displayImage.rectTransform.sizeDelta = size;
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private


	#region Classes
	public enum Direction {
		Horizontal, Vertical
	}
	#endregion//Classes
}
