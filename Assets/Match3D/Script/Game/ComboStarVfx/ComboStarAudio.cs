using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Play combo audio with pitch increasing.
/// The star VFX will head to star Icon on UI, and increase it when finish flying. We catch that event, play audio depend on current combo count
/// </summary>
public class ComboStarAudio : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] IntegerVariable comboCountVar;
	[SerializeField] IntegerVariable levelStarCollectionVar;

	[Header("Audio")]
	[SerializeField] AudioCommandVariableList comboAudioList;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		levelStarCollectionVar.OnValueChangedHandler += OnStarChanged;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		levelStarCollectionVar.OnValueChangedHandler -= OnStarChanged;
	}
	#endregion// Init config


	#region Private
	void OnStarChanged() {
		// first time open, combo = 0 , no play audio
		if(comboCountVar.Value == 0) {
			return;
		}
		// get the audio, clamp it to the comboAudioList count
		// The combo start from 1, so -1 here cause the list start from 0
		var audioIndex = comboCountVar.Value - 1;
		// notes: remember to -1 at comboAudioList count, the clamp include maxvalue, not like random func
		audioIndex = Mathf.Clamp(audioIndex, 0, comboAudioList.Count - 1);
		var audio = comboAudioList[audioIndex];
		audio.Execute();
	}
	#endregion//Private
}
