using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Follow the gamePause/Unpause event to adjust the global custom timescale value
/// </summary>
public class ChangeCustomTimescaleWhenGamePause : MonoBehaviour {
	#region Init, config
	[Header("ShareData, event")]
	[SerializeField] GameEvent gamePauseEvent;
	[SerializeField] GameEvent gameUnPauseEvent;
	[SerializeField] FloatVariable customTimeScaleVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		gamePauseEvent.Subscribe(OnGamePause);
		gameUnPauseEvent.Subscribe(OnGameUnPause);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		gamePauseEvent.Subscribe(OnGamePause);
		gameUnPauseEvent.Subscribe(OnGameUnPause);
		// reset timescale variable
		customTimeScaleVar.Value = 1f;
	}
	#endregion// Init config


	#region Private
	void OnGamePause() {
		customTimeScaleVar.Value = 0f;
	}

	void OnGameUnPause() {
		customTimeScaleVar.Value = 1f;
	}
	#endregion//Private
}
