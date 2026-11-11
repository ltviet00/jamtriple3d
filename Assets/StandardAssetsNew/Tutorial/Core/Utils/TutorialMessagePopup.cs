using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Work with Tutorial, simply hold ref to messageText component. CallSetMessage() to config content
/// </summary>
public class TutorialMessagePopup : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] TMP_Text text;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public void SetMessage(string message) {
		if(isFailedConfig)
			return;
		text.text = message;
	}
	#endregion//Public
}
