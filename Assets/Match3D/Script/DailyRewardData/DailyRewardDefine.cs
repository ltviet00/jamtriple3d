using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define reward for daily. 
/// </summary>
[CreateAssetMenu(fileName = "DailyRewardData", menuName = "Game/DailyReward/Define")]
public class DailyRewardDefine : ScriptableObject {
	[Header("Reward type")]
	public WeeklyEventRewardDefine.RewardType rewardType;

	[Header("Parameter, depend on reward type")]
	///<summary> Will be used if rewardType is IngameBooster </summary>
	public BoosterID boosterID;
	///<summary> Will be used if rewardType is Coin, IngameBooster </summary>
	public int quantity;
	///<summary> Will be used if rewardType is Unlimit life, PregameBooster </summary>
	public int duration;
}
