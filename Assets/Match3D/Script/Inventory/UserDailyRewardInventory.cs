using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Diagnostics;

///<summary> Save info about claiming daily reward of user </summary>
[CreateAssetMenu(fileName = "UserDailyRewardInventory", menuName = "Game/User/UserDailyRewardInventory")]
public class UserDailyRewardInventory : BaseUserDataSO<UserDailyRewardInventory.SaveForm> {
	#region Public
	///<summary> Raise when user claim current daily reward </summary>
	public event Action OnUserCollectCurrentReward;

	///<summary> Current date (from 0-6) </summary>
	public int GetCurrentProgressDate() {
		CheckResetClaimStatusIfDayHasPassed();
		return Data.claimProgress;
	}

	public bool IsUserClaimTheCurrentDayReward() {
		CheckResetClaimStatusIfDayHasPassed();
		return Data.isClaimedCurrentDay;
	}

	///<summary> Call this when user claim reward of current day </summary>
	public void MarkUserClaimCurrentReward() {
		// mark claim, save this day as last claim reward. Also increase the progress
		Data.isClaimedCurrentDay = true;
		Data.lastClaimRewardDate = DateTime.Now.ToBinary();
		SaveToDisk();
		// raise event
		OnUserCollectCurrentReward?.Invoke();
	}
	#endregion//Public


	#region Private
	void CheckResetClaimStatusIfDayHasPassed() {
		var currentDate = DateTime.Now;
		var dateInSaveData = DateTime.FromBinary(Data.lastClaimRewardDate);
		// different date? and user already claim the reward? Then increase the claim progress, and reset isClaim
		if(currentDate.Day != dateInSaveData.Day && Data.isClaimedCurrentDay) {
			Data.isClaimedCurrentDay = false;
			// increase the progress, and wrap it if it reach 7
			Data.claimProgress++;
			if(Data.claimProgress >= 7) {
				Data.claimProgress = 0;
			}
			SaveToDisk();
		}
	}
	#endregion//Private


	#region Cheat
	[Conditional("CHEAT")][Button]
	void EditorUnlockNextReward() {
		if(!Application.isPlaying)
			LoadFromDiskOrCreateDefault();
		// simple reduce the lastClaimRewardDate to one
		var date = DateTime.FromBinary(Data.lastClaimRewardDate);
		var previousDate = date.AddDays(-1);
		Data.lastClaimRewardDate = previousDate.ToBinary();
		Data.isClaimedCurrentDay = false;
		SaveToDisk();
	}

	[Conditional("CHEAT")][Button]
	void EditorResetProgress() {
		if(!Application.isPlaying)
			LoadFromDiskOrCreateDefault();
		// reset progress to zero, and last claim to current day
		Data.claimProgress = 0;
		Data.isClaimedCurrentDay = false;
		Data.lastClaimRewardDate = DateTime.Now.ToBinary();
		SaveToDisk();
	}
	#endregion//Cheat


	#region Classes
	[System.Serializable]
	public class SaveForm {
		///<summary> Last time user claim daily reward. Use this to know user has claimed current date reward or not.
		///It a long data, cause we'll use it with System.Timespan to convertToBinary and restore from binary</summary>
		public long lastClaimRewardDate = 0;
		///<summary> User has claim at date 3th or 6th..? To know if user claim a week reward </summary>
		public int claimProgress = 0;
		///<summary> Is user claimed current reward of current day? </summary>
		public bool isClaimedCurrentDay = false;
	}
	#endregion//Class
}
