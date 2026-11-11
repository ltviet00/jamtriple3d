using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// At home, simple config the content for Event locked page
/// </summary>
public class WeeklyEventLockedPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] WeeklyEventRewardStorage rewardStorage;

	[Header("REfs")]
	[SerializeField] TMP_Text lockedInfoText;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ConfigContent();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Private
	void ConfigContent() {
		const string FORMAT = "Unlock at level {0}";
		// plus one, cause level start from 0, we need +1 for UI text
		var text = string.Format(FORMAT, rewardStorage.LevelToUnlock + 1);
		lockedInfoText.text = text;
	}
	#endregion//Private
}
