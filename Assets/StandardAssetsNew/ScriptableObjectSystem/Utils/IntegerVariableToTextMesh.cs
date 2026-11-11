using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary> Watch the IntegerVariable, then config the displayText </summary>
public class IntegerVariableToTextMesh : MonoBehaviour {
	#region Init
	[Header("Refs, config")]
	[SerializeField] TMP_Text displayText;
	[SerializeField] IntegerVariable integerVarToFollow;
	[SerializeField] string textFormat = "{0}";
	[SerializeField] bool formatWithDecimalCommas = false;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		//Log.ValidateNotNull(displayText, this, "displayText");
		//Log.ValidateNotNull(integerVarToFollow, this, "integerVarToFollow");
		if(displayText == null) {
			displayText = GetComponent<TMP_Text>();
		}
		isFailedConfig = this.CheckNullAllSerializedFields();
	}


	void Start() {
		// in this game, some variable be changed in Awake(),
		// dont know somehow the OnEnable was called before Awake. So carefully call UpdateText at Start()
		if(isFailedConfig)
			return;
		UpdateText();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ActiveWorking();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		DeactiveWorking();
	}

	void ActiveWorking() {
		// register, also update text the first time
		integerVarToFollow.OnValueChangedHandler += OnIntegerValueChanged;
		UpdateText();
	}

	void DeactiveWorking() {
		integerVarToFollow.OnValueChangedHandler -= OnIntegerValueChanged;
	}

	void OnIntegerValueChanged() {
		UpdateText();
	}

	void UpdateText() {
		if(formatWithDecimalCommas) {
			const string COMMAS_FORMAT = "#,###";
			var integerWithCommas = integerVarToFollow.Value.ToString(COMMAS_FORMAT);
			var finalText = string.Format(textFormat, integerWithCommas);
			displayText.text = finalText;
		}
		else {
			string text = string.Format(textFormat, integerVarToFollow.Value);
			displayText.text = text;
		}
	}
	#endregion//Init
}
