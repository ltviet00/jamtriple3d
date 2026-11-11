using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Each event will last for 6days, we need to save StartTime and selected toy for the event
/// </summary>
[CreateAssetMenu(fileName = "WeeklyEventDataSaver", menuName = "Game/WeeklyEventReward/DataSaver")]
public class WeeklyEventDataSaver : BaseUserDataSO<WeeklyEventDataSaver.SaveForm> {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserWeeklyToyInventory userWeeklyInventory;

	[Header("Config")]
	[SerializeField] int eventDurationInDay = 7;
	#endregion// Init config


	#region Pubilc
	///<summary> Event duration in day counts (often 7 days) </summary>
	public int EventDuration => eventDurationInDay;

	///<summary> Return DateTime data, date = startDate of event, hour-miute-second are all zero (event start at midnight).
	///Update: simple deserialize from saved data. </summary>
	public DateTime GetEventStartDate() {
		SyncStarDateWithUserInventory();
		var result = DateTime.FromBinary(Data.startTime);
		return result;
	}

	///<summary> From EventStartDate and event duration, calculate the ended date of event </summary>
	public DateTime GetEventEndDate() {
		var startDate = GetEventStartDate();
		// make sure it's midnight, mean hour-minute-second are all zero, take the Date part only
		var endDate = startDate.AddDays(eventDurationInDay).Date;
		return endDate;
	}

	/////<summary> Base on starTime and duration, we know this event was ended or not, call this to reset startTime to
	/////closet event date, eg: event started at date 5, current date = 13, mean event ended, and the new event was
	/////started from 12, so reset startTime to date 12</summary>
	//public void CheckDurationToResetEvent() {
	//	if(isFailedConfig)
	//		return;
	//}
	#endregion//Public


	#region Private
	///<summary> We got unlocked date from UserWeeklyInventory, and the duration here, we can calculate the 
	///nearest startTime of current event.</summary>
	void SyncStarDateWithUserInventory() {
		var unlockedDate = DateTime.FromBinary(userWeeklyInventory.Data.unlockDate);
		var currentDate = DateTime.Now.Date;
		var span = currentDate - unlockedDate;
		// only update if the span is out of event duration. Just carefully for case that span is negative
 		if(span.TotalDays > eventDurationInDay || span.TotalDays <= 0) {
			// simple take the odd, and today minus odd => start date
			var odd = (int)span.TotalDays / eventDurationInDay;
			var startDate = currentDate.AddDays(odd);
			// save
			Data.startTime = startDate.ToBinary();
			SaveToDisk();
		}
	}
	#endregion//Private


	#region Override
	protected override void CreateDefaultData() {
		_saveData = new SaveForm();
	}
	#endregion//Override


	#region Classes
	[System.Serializable]
	public class SaveForm {
		///<summary> Currently we got diamond, star, rainbow. This the WeeklyToyID -> toyName. Will used to query via ToyStorage </summary>
		public string selectedToyName;
		///<summary> The startTime of currently weekly, used to countDown remain time </summary>
		public long startTime;

		public SaveForm() {
			// let default is "Star"
			const string DEFAULT_TOY = "Star";
			selectedToyName = DEFAULT_TOY;
			startTime = DateTime.Now.ToBinary();
		}
	}
	#endregion//Class
}
