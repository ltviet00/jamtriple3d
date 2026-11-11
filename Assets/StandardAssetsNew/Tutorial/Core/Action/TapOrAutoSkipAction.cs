using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tap to screen to skip, or wait for duration to auto skip
/// </summary>
[CreateAssetMenu(fileName = "TapOrAutoSkipAction", menuName = "Tutorial/Action/TapOrAutoSkipAction")]
public class TapOrAutoSkipAction : TutorialActionBase {
	#region Init, config
	[Header("Config")]
	public float autoSkipDuration = 0;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public override async UniTask<bool> Execute() {
		// turn on the autoSkip button on tutorial canvas
		var skipButton = TutorialCanvas.Instance.SkipButton;
		skipButton.gameObject.SetActive(true);

		// if autoSkipDuration > 0, then wait for them both
		if(autoSkipDuration > 0) {
			var countDown = UniTask.Delay(TimeSpan.FromSeconds(autoSkipDuration));
			var userPressSkip = skipButton.OnTapCompleteSource.Task;
			await UniTask.WhenAny(countDown, userPressSkip);
		}
		// not config skipDuration, then await for tap onyly
		else {
			await skipButton.OnTapCompleteSource.Task;
		}
		// done, deactive skip button
		skipButton.gameObject.SetActive(false);
		return true;
	}
	#endregion//Public
}
