using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define reward for each milestone of weekly event
/// </summary>
[CreateAssetMenu(fileName = "WeeklyEventRewardDefine", menuName = "Game/WeeklyEventReward/Define")]
public class WeeklyEventRewardDefine : ScriptableObject {
	[Header("Reward type")]
	public RewardType rewardType;
	///<summary> Amount of weekly event need to collected to claim this reward </summary>
	public int toyCollectRequire;

	[Header("Parameter, depend on reward type")]
	///<summary> Will be used if rewardType is IngameBooster </summary>
	public BoosterID boosterID;
	///<summary> Will be used if rewardType is Coin, IngameBooster </summary>
	public int quantity;
	///<summary> Will be used if rewardType is Unlimit life, PregameBooster </summary>
	public int duration;

	#region Classes
	public enum RewardType {
		Coin = 0,
		IngameBooster = 100,

		PregameBoosterForDuration_Fan = 200,
		PregameBoosterForDuration_LightingStorm,
		PregameBoosterForDuration_ExtraTime,

		UnlimitLifeForDuration = 300,
		DoubleWeeklyToyCollectAmount = 400,
	}
	#endregion//Classes
}
