using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// <summary>
/// Work with AudioManager. Attach it to object that have UI Toggle,
/// it will register toggle events with that button, and play audio each click
/// </summary>
public class ToggleButtonAudioV2 : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Button mainButton;

	[Header("Config")]
	[SerializeField] AudioManager.AudioClipType _controlChannel;

	[Header("Unity event")]
	[SerializeField] UnityEvent AudioOnHandler;
	[SerializeField] UnityEvent AudioOffHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponents();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		mainButton?.onClick.AddListener(OnButtonClick);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		mainButton?.onClick.RemoveListener(OnButtonClick);
	}

	void CacheComponents() {
		if(mainButton == null)
			mainButton = GetComponent<Button>();
	}

	void Start() {
		// call raise event here in Start, not OnEnable(), cause it will call to AudioManager
		// which is singleton, so not call any singleton in Awake, OnEnable
		RaiseEventBaseOnMuteStatus();
	}
	#endregion// Init config


	#region Private
	void RaiseEventBaseOnMuteStatus() {
		var isChannelOn = !IsChannelMuted();
		var eventToRaise = isChannelOn ? AudioOnHandler : AudioOffHandler;
		eventToRaise.Invoke();
	}

	void OnButtonClick() {
		var isOn = !IsChannelMuted();
		SetActiveChannel(!isOn);
		RaiseEventBaseOnMuteStatus();
	}

	bool IsChannelMuted() {
		switch(_controlChannel) {
			case AudioManager.AudioClipType.BGM:
				return AudioManager.Instance.IsMutingBGM;

			case AudioManager.AudioClipType.SFX:
				return AudioManager.Instance.IsMutingSFX;

			default:
				Log.Warning(false, this, $"NOt handle for AudioClipType [{_controlChannel}]");
				return false;
		}
	}

	void SetActiveChannel(bool active) {
#if UNITY_EDITOR
		if(AudioManager.Instance.logToggleClick)
			Log.Info(this, "ToggleButtonAudio [{0}]", name);
#endif
		switch(_controlChannel) {
			case AudioManager.AudioClipType.BGM:
				AudioManager.Instance.ActiveVolumeBGM(active);
				break;

			case AudioManager.AudioClipType.SFX:
				AudioManager.Instance.ActiveVolumeSFX(active);
				break;

			default:
				Log.Warning(false, this, $"NOt handle for AudioClipType [{_controlChannel}]");
				break;
		}
	}
	#endregion//Private


#if UNITY_EDITOR
	[SerializeField] bool _tempToogleState;
	[Button]
	void Toggle() {
		_tempToogleState = !_tempToogleState;
		var eventToRaise = _tempToogleState ? AudioOnHandler : AudioOffHandler;
		eventToRaise.Invoke();
	}
#endif//UNITY_EDITOR
}
