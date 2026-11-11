using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Stay in-game, listen to user win/fail level, save data to UserWinStreak </summary>
public class SaveUserWinStreakData : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserWinStreakData winStreakData;

	[Header("Event in")]
	[SerializeField] GameEvent userWinEvent;
	[SerializeField] GameEvent userLoseEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		userWinEvent.Subscribe(OnUserWin);
		userLoseEvent.Subscribe(OnUserLose);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		userWinEvent.Unsubscribe(OnUserWin);
		userLoseEvent.Unsubscribe(OnUserLose);
	}
	#endregion// Init config


	#region Event in
	void OnUserWin() {
		winStreakData.OnUserWinLevel();
	}

	void OnUserLose() {
		winStreakData.OnUserFailedLevel();
	}
	#endregion//Event in
}
