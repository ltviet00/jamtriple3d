using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Store user Weekly toy catching 
/// </summary>
[CreateAssetMenu(fileName = "UserWeeklyToyInventory", menuName = "Game/User/UserWeeklyToyInventory")]
public class UserWeeklyToyInventory : BaseUserDataSO<UserWeeklyToyInventory.SaveForm> {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserLevelScoreInventory userScoreInventory;
	[SerializeField] WeeklyEventRewardStorage weeklyEventRewardStorage;
	#endregion// Init config


	#region Public
	//public void IncreaseCollectedToy(int amount = 1) {
	//	Data.collectedItemAmount += amount;
	//}

	//public void ResetCollectedToy() {
	//	Data.collectedItemAmount = 0;
	//}

	//public void SetCurrentMileStone(int milestone) {
	//	Data.currentMilestone = milestone;
	//}

	///<summary> Check if user already pass target level (and not save unlock date yet), then update to it.
	///Only do once, so let call this at somewhere at homeapge </summary>
	public void CheckUnlockStatusAndSaveUnlockDate() {
		// if already save, then ignore
		if(Data.isLocked && userScoreInventory.GetCurrentReachLevel() >= weeklyEventRewardStorage.LevelToUnlock) {
			Data.isLocked = false;
			// let save the current date, keep it at midnight (simple take the Date part)
			var unlockDate = DateTime.Now.Date;
			Data.unlockDate = unlockDate.ToBinary();
			// save to disk after modifying
			SaveToDisk();
		}
	}

	///<summary> When return home, we move toy Collect from level to the "inventory" </summary>
	public void MoveCollectMoreFromLevelToAmount(int customAmount = 0) {
		if(customAmount > Data.collectedMoreFromLevels) {
			Log.Warning(false, this, $"Request move [{customAmount}] larger than exist toy [{Data.collectedMoreFromLevels}]. Clamped ");
			customAmount = Data.collectedMoreFromLevels;
		}
		// move to collectedAmount and save
		Data.collectedMoreFromLevels -= customAmount;
		Data.collectedItemAmount += customAmount;
		SaveToDisk();
	}
	#endregion//Public


	#region Classes
	[System.Serializable]
	public class SaveForm {
		///<summary> Turn to false when user passed the target level to active weekly event (currently level 11) </summary>
		public bool isLocked = true;
		///<summary> The date user pass target level to unlock weekly event, will used it to calclate event start date later </summary>
		public long unlockDate;

		///<summary> The weeklyEvent collect more from levels, not move to itemAmount yet </summary>
		public int collectedMoreFromLevels;
		///<summary> Amount of collected weekly event toy </summary>
		public int collectedItemAmount;
		///<summary> Each weekly events, we got 30 milestones. This's the index of milestone user has reach </summary>
		public int currentMilestone;

		public SaveForm() {
		}
	}
	#endregion//Class
}
