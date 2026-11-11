using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Define reward list for the lucky draw </summary>
[CreateAssetMenu(fileName = "LuckyDrawRewardList", menuName = "Game/LuckyDrawRewardList")]
public class LuckyDrawRewardList : ScriptableObject {
	[Header("Reward list")]
	public Reward[] rewardList;

	//[Header("Validation")]
	//[SerializeField] bool isFailedConfig;
	//void OnValidate() {
	//	isFailedConfig = this.CheckNullAllSerializedFields();
	//}

	public int PickRandomRewardIndexBaseOnTheirWeight() {
		// create an array of increment weight
		var totalWeight = 0;
		var incrementWeighList = new List<int>();
		incrementWeighList.Add(0);
		for(int n = 0; n < rewardList.Length; n++) {
			totalWeight += rewardList[n].weight;
			incrementWeighList.Add(totalWeight);
		}
		// now random a number, then find an index where the weigh is higher than that index
		var randomWeight = Random.Range(0, totalWeight);
		var foundIndex = 0;
		for(int n = 1; n < incrementWeighList.Count; n++) {
			if(incrementWeighList[n] > randomWeight) {
				foundIndex = n - 1;
				break;
			}
		}
		return foundIndex;
	}

	[System.Serializable]
	public class Reward {
		[Header("Coin?")]
		///<summary> If use coin, mean no use booster, and so on </summary>
		public bool useCoin = false;
		public int coinAmount;

		[Header("Booster?")]
		public BoosterID boosterID;
		public int boosterAmount;

		///<summary> The more weight, the more chance the spin result pop to this </summary>
		[Header("Weight")]
		public int weight;
	}
}