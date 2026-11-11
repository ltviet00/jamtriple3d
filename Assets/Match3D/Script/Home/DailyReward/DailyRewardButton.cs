using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handle behavior fo the Daily Reward button
/// </summary>
public class DailyRewardButton : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserDailyRewardInventory userDailyInventory;

	[Header("Refs")]
	[SerializeField] GameObject countingInProgressObject;
	[SerializeField] GameObject readyToClaimObject;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ConfigContent();
		userDailyInventory.OnUserCollectCurrentReward += OnUserCollectDailyReward;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		userDailyInventory.OnUserCollectCurrentReward -= OnUserCollectDailyReward;
	}
	#endregion// Init config


	#region UI callbacks
	///<summary> Config the AutoCountDownToMidnight to invoke this when counting finish </summary>
	public void In_OnFinishCounting() {
		if(isFailedConfig)
			return;
		countingInProgressObject.SetActive(false);
		readyToClaimObject.SetActive(true);
	}
	#endregion//UI callbacks


	#region Private
	void OnUserCollectDailyReward() {
		ConfigContent();
	}

	void ConfigContent() {
		var claimedReward = userDailyInventory.IsUserClaimTheCurrentDayReward();
		countingInProgressObject.SetActive(claimedReward);
		readyToClaimObject.SetActive(!claimedReward);
	}
	#endregion//Private
}
