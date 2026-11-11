using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatVariableToImageFillAmount : MonoBehaviour {
	#region Init, config
	[Header("Refs, data")]
	[SerializeField] Image targetImage;
	[SerializeField] FloatVariable valueToFollowVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		valueToFollowVar.OnValueChangedHandler += OnValueChanged;
		// first changes
		OnValueChanged();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		valueToFollowVar.OnValueChangedHandler -= OnValueChanged;
	}
	#endregion// Init config


	#region Private
	void OnValueChanged() {
		// clamp to 1 cause image fillAmount is from 0-1
		var clamped = Mathf.Clamp01(valueToFollowVar.Value);
		targetImage.fillAmount = clamped;
	}
	#endregion//Private
}
