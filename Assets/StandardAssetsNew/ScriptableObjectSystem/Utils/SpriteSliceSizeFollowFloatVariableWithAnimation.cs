using System;
using System.Collections;
using UniRx;
using UnityEngine;

/// <summary>
/// Config the size of sprite, follow the value of floatvariable. Value of Float is from 0-1
/// </summary>
public class SpriteSliceSizeFollowFloatVariableWithAnimation : MonoBehaviour {
	#region Init
	[Header("Refs")]
	[SerializeField] FloatVariable variableToFollow;
	[SerializeField] SpriteRenderer displaySprite;

	[Header("Config")]
	[SerializeField] Vector2 floatVarRange = new Vector2(0, 1f);
	[SerializeField] float widthSizeWhenFull;
	[SerializeField] float animationTime = 1f;
	[SerializeField] float delayBeforeAnimation = 1f;

	void OnValidate() {
		Log.ValidateNotNull(variableToFollow, this, "variableToFollow");
		Log.ValidateNotNull(displaySprite, this, "displaySprite");
	}

	bool IsFailedConfig() {
		return variableToFollow == null
			|| displaySprite == null;
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
		variableToFollow.OnValueChangedHandler += OnFloatChanged;
		// first config. let call the OnFloatChanged the first time to let it config the sprite onEnable
		ResetTimerBeforeAnimation();
		ConfigSpriteSize(0f);
		OnFloatChanged();
	}

	void DeactiveWorking() {
		variableToFollow.OnValueChangedHandler -= OnFloatChanged;
		ClearAllCorountine();
	}

	void OnFloatChanged() {
		// reset time, target size
		ResetTimerBeforeAnimation();
		ReCalculateInfomationForAnimation();
		//// if already run animation, then do nothing, if not, start the corountine
		// Update: let alway start new animation
		//if(!IsRunngingAnimation()) {
		//_mainCoroutine = Observable.FromMicroCoroutine(IE_Animation).Subscribe();
		//}
		ClearAllCorountine();
		_mainCoroutine = Observable.FromMicroCoroutine(IE_Animation).Subscribe();
	}
	#endregion//Init


	#region Private
	IDisposable _mainCoroutine = null;
	void ClearAllCorountine() {
		_mainCoroutine?.Dispose();
		_mainCoroutine = null;
	}

	bool IsRunngingAnimation() {
		return _mainCoroutine != null;
	}

	float _delayBeforeAnimationCounting = 0f;
	float _animationDurationCounting = 0f;
	float _scaleFromSize;
	float _scaleToSize;

	void ResetTimerBeforeAnimation() {
		_delayBeforeAnimationCounting = 0f;
		_animationDurationCounting = 0f;
	}

	void ReCalculateInfomationForAnimation() {
		var floatValue = Mathf.Clamp(variableToFollow.Value, floatVarRange.x, floatVarRange.y);
		var floatRange = Mathf.Abs(floatVarRange.y - floatVarRange.x);
		var floatToPercent = floatValue / floatRange;
		_scaleFromSize = displaySprite.size.x;
		_scaleToSize = floatToPercent * widthSizeWhenFull;
	}

	IEnumerator IE_Animation() {
		// delay before animation
		while(_delayBeforeAnimationCounting < delayBeforeAnimation) {
			yield return null;
			_delayBeforeAnimationCounting += Time.deltaTime;
		}
		// animation
		while(_animationDurationCounting < animationTime) {
			yield return null;
			_animationDurationCounting += Time.deltaTime;
			var ratio = _animationDurationCounting / animationTime;
			ConfigSpriteSize(ratio);
		}
		// finish animation
		ConfigSpriteSize(1f);
		ClearAllCorountine();
	}


	void ConfigSpriteSize(float ratio) {
		var size = displaySprite.size;
		size.x = Mathf.Lerp(_scaleFromSize, _scaleToSize, ratio);
		displaySprite.size = size;
	}
	#endregion// Private
}
