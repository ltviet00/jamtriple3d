using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

/// <summary>
/// Show unlock progress new toy at end game
/// </summary>
public class ToyUnlockProgressPage : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] ToyStorage toyStorage;
	[SerializeField] IntegerVariable userCurrentLevelVar;

	[Header("Refs")]
	[SerializeField] Image progressFillImage;
	[SerializeField] TMP_Text percentText;
	[SerializeField] float fillProgressDuration;

	[Header("UnityEvent")]
	[SerializeField] UnityEvent requestOpenNewToyPageCmd;
	[SerializeField] UnityEvent requestOpenNextLevelPageCmd;

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
	}
	#endregion// Init config


	#region UI input
	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageStartOpen() {
		if(isFailedConfig)
			return;
		FirstConfigDisplay();
	}

	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		ShowProgressAndRaiseNextPopupAsync().Forget();
	}
	#endregion//UI input


	#region Private
	ToyPrefabGroup _groupToUnlock;

	void FirstConfigDisplay() {
		Log.Error(false, this, $"ToyUnlockProgressPage nolonger functio");
		//_groupToUnlock = toyStorage.GetCurrentGroupToUnlock();
		//if(_groupToUnlock == null) {
		//	Log.Warning(false, this, $"All toy was unlock, no need this popup");
		//	requestOpenNewToyPageCmd.Invoke();
		//}
		//// config percent text. when this popup called,. mean user level already ++
		//var groupStartLevel = _groupToUnlock.levelToUnlock - toyStorage.levelCountToUnlockGroup;
		//var userLevel = userCurrentLevelVar.Value;
		//var fromPercent = (float)(userLevel - groupStartLevel) / (float)toyStorage.levelCountToUnlockGroup;

		//percentText.text = $"{(int)(fromPercent * 100f)}%";
		//progressFillImage.fillAmount = fromPercent;
	}

	async UniTaskVoid ShowProgressAndRaiseNextPopupAsync() {
		Log.Error(false, this, $"ToyUnlockProgressPage nolonger functio");
		//var groupStartLevel = _groupToUnlock.levelToUnlock - toyStorage.levelCountToUnlockGroup;
		//var userLevel = userCurrentLevelVar.Value;
		//var fromPercent = (float)(userLevel - groupStartLevel) / (float)toyStorage.levelCountToUnlockGroup;
		//var toPercent = (float)(userLevel - groupStartLevel + 1) / (float)toyStorage.levelCountToUnlockGroup;

		//var timer = 0f;
		//while(timer < fillProgressDuration) {
		//	await UniTask.Yield();
		//	timer += Time.deltaTime;
		//	var ratio = timer / fillProgressDuration;
		//	var percent = Mathf.Lerp(fromPercent, toPercent, ratio);
		//	percentText.text = $"{(int)(percent * 100f)}%";
		//	progressFillImage.fillAmount = percent;
		//}

		////wait a bit and raise next popup
		//await UniTask.Delay(1000);
		//var unlockedNewToy = userLevel + 1 >= _groupToUnlock.levelToUnlock;
		//var eventToRaise = unlockedNewToy ? requestOpenNewToyPageCmd : requestOpenNextLevelPageCmd;
		//eventToRaise.Invoke();
	}
	#endregion//Private
}
