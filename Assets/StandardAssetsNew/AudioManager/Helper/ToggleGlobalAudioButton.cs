using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Sirenix.OdinInspector;

// <summary>
/// Work with AudioManager. Attach it to object that have UI Toggle,
/// it will register toggle events with that button, and play audio each click
/// </summary>
public class ToggleGlobalAudioButton : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Toggle _targetToggle;

	[Header("Config")]
	[SerializeField] AudioManager.AudioClipType _controlChannel;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponents();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		_targetToggle.onValueChanged.AddListener(OnToggleValueChanged);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		_targetToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
	}

	void CacheComponents() {
		if(_targetToggle == null)
			_targetToggle = GetComponent<Toggle>();
	}

	void Start() {
		if(isFailedConfig)
			return;
		// call Config at Start() instead of OnEnable() : wait for AudioManager to Init
		ConfigToggleBaseOnMuteStatus();
	}
	#endregion// Init config


	#region Private
	void ConfigToggleBaseOnMuteStatus() {
		var isChannelMuted = IsChannelMuted();
		_targetToggle.isOn = isChannelMuted;
	}

	void OnToggleValueChanged(bool isOn) {
		// cause this is mute button, on mean mute, off mean active. So have to use "!isOn"
		SetActiveChannel(!isOn);
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

	[Button]
	void ToggleOn() {
		_targetToggle.isOn = true;
	}

	[Button]
	void ToggleOff() {
		_targetToggle.isOn = false;
	}
	#endregion//Private
}