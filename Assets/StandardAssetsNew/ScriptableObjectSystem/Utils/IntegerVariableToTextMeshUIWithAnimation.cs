using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Same to IntegerVariableToTextMeshUI, but have counting animation
/// </summary>
public class IntegerVariableToTextMeshUIWithAnimation : MonoBehaviour {
	#region Init
	[Header("Reference")]
	[SerializeField] TMP_Text displayText;
	[SerializeField] IntegerVariable integerVarToFollow;
	[SerializeField] string textFormat = "{0}";
	[SerializeField] bool formatWithDecimalCommas = false;

	[Header("Config")]
	[SerializeField] float countingEffectDuration = 2f;
	[SerializeField] float tweenTextEffectDuration = 1f;
	[SerializeField] LeanTweenType tweenEffectType;

	void OnValidate() {
		Log.ValidateNotNull(displayText, this, "displayText");
		Log.ValidateNotNull(integerVarToFollow, this, "integerVarToFollow");
	}

	bool IsFailedConfig() {
		return displayText == null
			|| integerVarToFollow == null;
	}

	void Start() {
		// in this game, some variable be changed in Awake(),
		// dont know somehow the OnEnable was called before Awake. So carefully call UpdateText at Start()
		if(IsFailedConfig())
			return;
		SetupForCountingEffect();
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
		integerVarToFollow.OnValueChangedHandler += OnIntegerValueChanged;
		// first text
		displayText.text = FormatIntegerToTextToDisplay(integerVarToFollow.Value);
		// some start effect
		_fromTextDisplayNumber = 0;
		_defaultScale = displayText.transform.localScale;
		SetupForCountingEffect();
	}

	void DeactiveWorking() {
		integerVarToFollow.OnValueChangedHandler -= OnIntegerValueChanged;
	}
	#endregion//Init


	#region Private
	void OnIntegerValueChanged() {
		// check if the number is not changed, then do nothing
		if(integerVarToFollow.Value == _targetTextDisplayNumber) {
			return;
		}
		PlayTweenTextEffect();
		SetupForCountingEffect();
	}

	void PlayTweenTextEffect() {
		// put it at zero, then scale to [1,1,1]
		displayText.transform.localScale = Vector3.zero;
		LeanTween.scale(displayText.gameObject, _defaultScale, tweenTextEffectDuration).setEase(tweenEffectType);
	}

	void SetupForCountingEffect() {
		// reset time, set target display. leave the current display
		_countingTextRemainTime = countingEffectDuration;
		_fromTextDisplayNumber = _currentTextDisplayNumber;
		_targetTextDisplayNumber = integerVarToFollow.Value;
	}

	Vector3 _defaultScale;
	float _countingTextRemainTime = 0f;
	int _fromTextDisplayNumber;
	int _targetTextDisplayNumber;
	int _currentTextDisplayNumber;

	void Update() {
		if(_countingTextRemainTime > 0f) {
			var ratio = _countingTextRemainTime / countingEffectDuration;
			_currentTextDisplayNumber = (int)Mathf.Lerp(_targetTextDisplayNumber, _fromTextDisplayNumber, ratio);
			var text = FormatIntegerToTextToDisplay(_currentTextDisplayNumber);
			displayText.text = text;
			_countingTextRemainTime -= Time.deltaTime;
		}
		// carefully check. Sometime because of the lerp process above, the displayTExt not reach the target text yet
		else {
			//if(_currentTextDisplayNumber != _targetTextDisplayNumber) {
				var text = FormatIntegerToTextToDisplay(_targetTextDisplayNumber);
				displayText.text = text;
			//}
		}
	}

	string FormatIntegerToTextToDisplay(int integerValue) {
		string result;
		if(formatWithDecimalCommas) {
			const string COMMAS_FORMAT = "#,###";
			var integerWithCommas = integerValue.ToString(COMMAS_FORMAT);
			result = string.Format(textFormat, integerWithCommas);
		}
		else {
			result = string.Format(textFormat, integerValue);
		}
		return result;
	}
	#endregion//Private
}
