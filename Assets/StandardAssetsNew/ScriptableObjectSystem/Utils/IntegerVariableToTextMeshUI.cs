using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Follow changes of a IntegerVariable, then update the TextMesh follow variable
/// </summary>
public class IntegerVariableToTextMeshUI : MonoBehaviour {
	#region Init
	[Header("ShareData, refs")]
	[SerializeField] TMP_Text displayText;
	[SerializeField] IntegerVariable integerVarToFollow;

	[Header("Config")]
	///<summary> Want to +1, or -1 to the value? insert to this </summary>
	[SerializeField] int valueModify = 0;
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
			var integerWithCommas = (integerVarToFollow.Value + valueModify).ToString(COMMAS_FORMAT);
			var finalText = string.Format(textFormat, integerWithCommas);
			displayText.text = finalText;
		}
		else {
			string text = string.Format(textFormat, integerVarToFollow.Value + valueModify);
			displayText.text = text;
		}
	}
	#endregion//Init
}
