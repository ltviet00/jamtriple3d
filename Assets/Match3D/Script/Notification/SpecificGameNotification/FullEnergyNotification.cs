using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schedule Full-energy notification
/// </summary>
public class FullEnergyNotification : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserEnergyInventory energyInventory;

	[Header("Config")]
	[SerializeField] string notificationTitle = "Your energy are fulled !!";
	[SerializeField] string notificationBody = "Let get back to your toys";

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


	#region Private
	///<summary> Read more in this thread to understand when Focus, Pause are call on device
	/// https://answers.unity.com/questions/496290/can-somebody-explain-the-onapplicationpausefocus-s.html </summary>
	void OnApplicationPause(bool pause) {
		// when application go to background, check send notification full energy
		if(pause) {
			CheckScheduleNotification();
		}
	}

	void CheckScheduleNotification() {
		// carefully check Fill energy after idling time
		energyInventory.CheckFillEneryAfterIdleTime();
		// already full energy? then ignore
		if(energyInventory.IsFull())
			return;
		// not? then notification next time when we full
		var nextTimeFull = energyInventory.GetNextTimeWhenFullEnergy();
		// should add a small margin
		const int MARGIN_IN_SECOND = 5;
		nextTimeFull.AddSeconds(MARGIN_IN_SECOND);
		// schedule notification
		NotificationSenderWrapper.Instance.SendNotification(notificationTitle, notificationBody, nextTimeFull);
	}
	#endregion//Private


	//#region Classes
	//[System.Serializable]
	//class Config {
	//	public string title;
	//	public string body;
	//}
	//#endregion//Classes
}
