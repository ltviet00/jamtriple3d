using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Store data for Double Event inventory, this reward show up in WeeklyEvent
/// </summary>
[CreateAssetMenu(fileName = "UserDoubleEventToyInventory", menuName = "Game/User/UserDoubleEventToyInventory")]
public class UserDoubleEventToyInventory : BaseUserDataSO<UserDoubleEventToyInventory.SaveForm> {
	#region Public
	public bool IsInDoubleToyDuration() {
		var now = DateTime.Now;
		var unlimitEnergyTime = DateTime.FromBinary(Data.doubleToyTime);
		return unlimitEnergyTime > now;
	}

	///<summary> Call this when user buy IAP to get unlimit energy in an duration (6,12,24h) </summary>
	public void ExpandDuration(int hours) {
		var currentUnlimitTime = DateTime.FromBinary(Data.doubleToyTime);
		var now = DateTime.Now;
		// out of unlimit-duration, or first time buy
		if(currentUnlimitTime < now) {
			currentUnlimitTime = now;
		}
		// increase time
		var future = currentUnlimitTime.AddHours(hours);
		Data.doubleToyTime = future.ToBinary();
		SaveToDisk();
	}

	///<summary> Call this when user receive daily reward or buy package with short duration (15m, 30m ..) </summary>
	public void ExpandDurationByMinutes(int minutes) {
		var currentUnlimitTime = DateTime.FromBinary(Data.doubleToyTime);
		var now = DateTime.Now;
		// out of unlimit-duration, or first time buy
		if(currentUnlimitTime < now) {
			currentUnlimitTime = now;
		}
		// increase time
		var future = currentUnlimitTime.AddMinutes(minutes);
		Data.doubleToyTime = future.ToBinary();
		SaveToDisk();
	}

	///<summary> Return the time (in future) when currnet unlimit energy will out. Should check IsUnlimitenergyuDuration first </summary>
	public DateTime GetExpireTime() {
		return DateTime.FromBinary(Data.doubleToyTime);
	}
	#endregion//Public


	#region Classes
	[System.Serializable]
	public class SaveForm {
		///<summary> When user buy a package with unlimit energy in N hour (eg: 8hour), we get the current time, plus N hour,
		/// and save to this. So check current time with this time to know if user still in unlimit-energy durataion </summary>
		public long doubleToyTime = 0;
	}
	#endregion//Class
}
