using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StringVariableToTextMeshUI : MonoBehaviour {
	#region Init
	[SerializeField] TMP_Text displayText;
	[SerializeField] StringVariable stringVarToFollow;
	[SerializeField] string textFormat = "{0}";
	[SerializeField] bool applyEvenValueNull = false;

	void OnValidate() {
		Log.ValidateNotNull(displayText, this, "displayText");
		Log.ValidateNotNull(stringVarToFollow, this, "stringVarToFollow");
	}

	bool IsFailedConfig() {
		return displayText == null
			|| stringVarToFollow == null;
	}

	void Start() {
		// in this game, some variable be changed in Awake(),
		// dont know somehow the OnEnable was called before Awake. So carefully call UpdateText at Start()
		if(IsFailedConfig())
			return;
	}

	void OnEnable() {
		if(IsFailedConfig())
			return;
		ActiveWorking();
	}

	void OnDisable() {
		if(IsFailedConfig())
			return;
		DeactiveWorking();
	}

	void ActiveWorking() {
		// register, also update text the first time
		stringVarToFollow.OnValueChangedHandler += OnStringValueChanged;
		UpdateText();
	}

	void DeactiveWorking() {
		stringVarToFollow.OnValueChangedHandler -= OnStringValueChanged;
	}

	void OnStringValueChanged(string newValue) {
		UpdateText();
	}

	void UpdateText() {
		if(!applyEvenValueNull && string.IsNullOrEmpty(stringVarToFollow.Value))
			return;
		var text = string.Format(textFormat, stringVarToFollow.Value);
		displayText.text = text;
	}
	#endregion//Init
}
