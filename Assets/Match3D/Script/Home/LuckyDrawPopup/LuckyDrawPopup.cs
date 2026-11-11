using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using TMPro;

///<summary> Handle logic for LuckeyDraw popup at main menu </summary>
public class LuckyDrawPopup : MonoBehaviour {
	#region Init, config
	[Header("User")]
	[SerializeField] UserLuckyDrawInventory userLuckyDrawInventory;
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserBoosterInventory userBoosterInventory;

	[Header("RewardList, prefab")]
	[SerializeField] LuckyDrawRewardList rewardData;
	[SerializeField] GameObject luckyDrawItemPrefab;

	[Header("Config")]
	[SerializeField] SpinConfig spinConfig;

	[Header("Refs")]
	//[SerializeField] Transform luckyDrawPointer;
	[SerializeField] Transform luckyBoardRootItem;
	[SerializeField] Button closeButton;
	[SerializeField] Button freeSpinButton;
	[SerializeField] Button watchAdsToSpinButton;
	[SerializeField] TMP_Text freeDrawRemainText;
	[SerializeField] TMP_Text freeAdsRemainText;

	[Header("Refs - reice reward")]
	[SerializeField] GameObject receiveRewardPage;
	[SerializeField] LuckyDrawItemDisplayer receiveRewardDisplayItem;

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
		_token.ClearRunningTasks();
	}

	void Awake() {
		if(isFailedConfig)
			return;
		SpawnRewardItem();
	}
	#endregion// Init config


	#region UI callback
	///<summary> Config UIPage to invoke this when page show to config content </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		CheckActiveDrawButtonBaseOnFreeDrawUsedPerDay();
		SetActiveReceiveRewardPage(false);
	}

	///<summary> Config the FreeSpin button to invoke this </summary>
	public void In_FreeSpinPress() {
		if(isFailedConfig)
			return;
		// increase freeSpin count
		userLuckyDrawInventory.IncreaseFreeDrawUsed();
		// spin function. For safe, let just clear current running tasks first
		_token.ClearRunningTasks();
		TaskSpin().Forget();
	}

	///<summary> Invoke this when user finish watch ads </summary>
	public void In_FinishWatchAdsForFreeSpin() {
		if(isFailedConfig)
			return;
		userLuckyDrawInventory.IncreaseAdsWatchedCount();
		// spin function. For safe, let just clear current running tasks first
		_token.ClearRunningTasks();
		TaskSpin().Forget();
	}

	///<summary> Config the receiveReward button to invoke this to config content </summary>
	public void In_OnPressReceiveReward() {
		if(isFailedConfig)
			return;
		CheckActiveDrawButtonBaseOnFreeDrawUsedPerDay();
	}
	#endregion//UI callback


	#region Private - UI
	void SpawnRewardItem() {
		var rewards = rewardData.rewardList;
		var angleBetweenItem = 360f / rewards.Length;

		for(int n = 0; n < rewards.Length; n++) {
			var reward = rewards[n];
			// let displayer config display
			var displayer = Instantiate(luckyDrawItemPrefab, luckyBoardRootItem).GetComponent<LuckyDrawItemDisplayer>();
			displayer.ConfigContentFollowReward(reward);
			// rotate. NOTES: in unity, rotateZ increase mean rotate counter clockwise. In the lucky draw process, we rotate the board by increasing Z, mean counter clockwise. so, if we random to item 3, in the formular in TaskSpin(), the angle will be 3 * anglePerItem. To make it right, the item spread by clockwise direction. So, put the "-" in the rotate.z for the displayer
			var rotate = Vector3.zero;
			rotate.z = - (n * angleBetweenItem);
			displayer.transform.localEulerAngles = rotate;
		}
	}

	void CheckActiveDrawButtonBaseOnFreeDrawUsedPerDay() {
		var isReachFreeDrawPerDay = userLuckyDrawInventory.IsReachFreeDrawPerDay();
		freeSpinButton.gameObject.SetActive(!isReachFreeDrawPerDay);
		watchAdsToSpinButton.gameObject.SetActive(isReachFreeDrawPerDay);
		// no free draw? use ads
		if(isReachFreeDrawPerDay) {
			freeAdsRemainText.text = $"({userLuckyDrawInventory.GetAdsWatchedCount()}/{userLuckyDrawInventory.FreeAdsToDrawPerDay})";
			var active = !userLuckyDrawInventory.IsReachFreeAdsToDrawPerDay();
			watchAdsToSpinButton.interactable = active;
		}
		// still free draw
		else {
			freeDrawRemainText.text = $"SPIN ({userLuckyDrawInventory.GetFreeDrawUsedCount()}/{userLuckyDrawInventory.FreeDrawPerDay})";
		}
		closeButton.interactable = true;
	}

	void DisableButtonAtStartSpinning() {
		freeSpinButton.gameObject.SetActive(false);
		watchAdsToSpinButton.gameObject.SetActive(false);
		closeButton.interactable = false;
	}

	void SetActiveReceiveRewardPage(bool active) {
		receiveRewardPage.SetActive(active);
	}
	#endregion//Private - UI


	#region Private - Spin
	///<summary> Helper to work with UniTask </summary>
	UniTaskTokenHelper _token = new UniTaskTokenHelper();

	async UniTaskVoid TaskSpin() {
		// disable close and freespin button
		DisableButtonAtStartSpinning();
		// rotate some circle first
		const float CIRCLE_ANGLE = 360f;
		var beginSpeed = spinConfig.circleToRotateBeforeSlowingDown * CIRCLE_ANGLE / spinConfig.duration;
		var rotationForBoard = Vector3.zero;
		luckyBoardRootItem.localEulerAngles = rotationForBoard;
		// rotate
		var timer = 0f;
		while(timer < spinConfig.duration) {
			await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, _token.Token);
			timer += Time.deltaTime;
			var rotateZ = timer * beginSpeed;
			rotationForBoard.z = rotateZ;
			luckyBoardRootItem.localEulerAngles = rotationForBoard;
		}
		// start slowing down. calculate the reward here
		var rewardIndex = rewardData.PickRandomRewardIndexBaseOnTheirWeight();
		// let it rotate 2 circle more and stop at rewardIndex 
		var anglePerItem = CIRCLE_ANGLE / rewardData.rewardList.Length;
		var angleToRotateMore = rewardIndex * anglePerItem + spinConfig.circleToRoateMoreBeforeStop * CIRCLE_ANGLE;
		// we has start speed, we got angleToRotate more, we got slowDownDuration, now calculate the reduce acceleration
		// got S = Vo * t + 0.5 * a * t * t => calculate the A
		var reduceAcceleration = (angleToRotateMore - beginSpeed * spinConfig.slowDownToResultDuration) / (0.5f * spinConfig.slowDownToResultDuration.Sqr());

		// slowing down. let reset to zero first
		rotationForBoard = Vector3.zero;
		timer = 0f;
		while(timer < spinConfig.slowDownToResultDuration) {
			await UniTask.Yield();
			timer += Time.deltaTime;
			rotationForBoard.z = beginSpeed * timer + 0.5f * reduceAcceleration * timer * timer;
			luckyBoardRootItem.transform.localEulerAngles = rotationForBoard;
		}
		// finish? show reward?
		await UniTask.Delay(1000, cancellationToken: _token.Token);
		SetActiveReceiveRewardPage(true);
		var reward = rewardData.rewardList[rewardIndex];
		receiveRewardDisplayItem.ConfigContentFollowReward(reward);
		// also save reward to inventory
		SaveRewardToUserInventory(reward);
		// config text
		CheckActiveDrawButtonBaseOnFreeDrawUsedPerDay();
	}

	void SaveRewardToUserInventory(LuckyDrawRewardList.Reward reward) {
		if(reward.useCoin) {
			userCoinInventory.IncreaseCoin(reward.coinAmount);
		}
		else {
			userBoosterInventory.AddBooster(reward.boosterID, reward.boosterAmount);
		}
	}
	#endregion//Private - Spin


	#region Classes
	[System.Serializable]
	struct SpinConfig {
		///<summary> Maintain the speed in this duration, then slowDown </summary>
		public float duration;
		///<summary> Number of circle to rotate before slowing down to show result </summary>
		public int circleToRotateBeforeSlowingDown;
		///<summary> Rotate N circle more + angle to the selected item before stop </summary>
		public int circleToRoateMoreBeforeStop;
		///<summary> After duration, speed will be reduce by this reduce/second </summary>
		public float slowDownToResultDuration;
	}
	#endregion//Classes
}
