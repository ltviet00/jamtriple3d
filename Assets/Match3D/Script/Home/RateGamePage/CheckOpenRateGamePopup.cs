using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put at Home scene, 
/// </summary>
public class CheckOpenRateGamePopup : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserWinStreakData winStreakData;

	[Header("Unity event out")]
	[SerializeField] UnityEvent openRateGameHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region UI callback
	///<summary> Config MainPage UI to invoke this func </summary>
	public void In_CheckOpenRateGamePopup() {
		if(isFailedConfig)
			return;
		// if user choosed never, or already rate
		if(winStreakData.Data.isUserChooseNeverRateGame || winStreakData.Data.hasUserRatedGame) {
			return;	
		}
		// we will open rate game when
		// - user win enough round (eg: win 5 round)
		// - last play was win
		var winEnoughRound = winStreakData.Data.winStreakCount >= winStreakData.winCountToShowRateGame;
		var lastPlayWasWin = winStreakData.IsLastPlayWasWin();
		if(winEnoughRound && lastPlayWasWin) {
			openRateGameHandler.Invoke();
		}
	}
	#endregion//UI callback
}
