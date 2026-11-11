using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Save user win streak data, currently use for RateGame page only </summary>
[CreateAssetMenu(fileName = "UserWinStreakData", menuName = "Game/User/UserWinStreakData")]
public class UserWinStreakData : BaseUserDataSO<UserWinStreakData.SaveForm> {
	#region Config
	[Header("Extra config")]
	public int winCountToShowRateGame = 5;
	#endregion//Config


	#region Public
	public void OnUserWinLevel() {
		// increase win streak and mark last play is win
		Data.winStreakCount++;
		Data.isLastPlayWin = true;
		SaveToDisk();
	}

	public void OnUserFailedLevel() {
		// currently only set lastPlayWinStat, not reset winStreak
		Data.isLastPlayWin = false;
		SaveToDisk();
	}

	public bool IsLastPlayWasWin() {
		return Data.isLastPlayWin;
	}

	public void ResetWinStreak() {
		Data.winStreakCount = 0;
		Data.isLastPlayWin = false;
		SaveToDisk();
	}

	public void OnUserSelectNeverRateGame() {
		Data.isUserChooseNeverRateGame = true;
		SaveToDisk();
	}

	public void OnUserRateGame() {
		Data.hasUserRatedGame = true;
		SaveToDisk();
	}
	#endregion//Public


	#region Classes
	[System.Serializable]
	public class SaveForm {
		public int winStreakCount = 0;
		public bool isLastPlayWin = false;
		public bool isUserChooseNeverRateGame = false;
		public bool hasUserRatedGame = false;
	}
	#endregion//Class
}
