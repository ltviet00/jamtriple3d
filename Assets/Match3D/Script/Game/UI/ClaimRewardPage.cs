using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

/// <summary>
/// Function for ClaimRewardPage at endgame
/// </summary>
public class ClaimRewardPage : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] UserStarInventory userStarInventory;
	[SerializeField] IntegerVariable userStarCollectedVar;
	[SerializeField] UserStarChestInventory userStarChestInventory;

	[Header("ShareData - Level, to check open Unlock new toy")]
	[SerializeField] ToyStorage toyStorage;

	[Header("Refs - claim animation bar")]
	[SerializeField] TMP_Text multiplyNumberText;
	[SerializeField] Transform pointerTrans;
	[SerializeField] Transform pointerMoveFrom;
	[SerializeField] Transform pointerMoveTo;

	[Header("Refs, config - RewardArea")]
	[SerializeField] RewardArea[] rewardAreaList;
	[SerializeField] float pointerCycleDuration = 1.5f;
	[SerializeField] Easing.TweenType pointerTweenType = Easing.TweenType.easeInOutQuart;

	[Header("Unity event out")]
	[SerializeField] UnityEvent requestToyUnlockProgressPopupCmd;
	[SerializeField] UnityEvent requestNextLevelPopupCmd;

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
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI input
	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		MovingPointerAndConfigUIAsync().Forget();
	}

	///<summary> Config UIPage to invoke this </summary>
	public void In_OnWatchAdsSuccess() {
		if(isFailedConfig)
			return;
		// add coin, open next popup
		var totalStar = userStarCollectedVar.Value * _currentMultiplyFactor;
		userStarInventory.IncreaseStars(totalStar);
		userStarCollectedVar.Value = totalStar;
		// add coin to StarChest
		userStarChestInventory.AddStarsUserHasEarnMoreFromLevels(totalStar);
		CheckRequestNextPopup();
		ResetCollectStarVariable();
	}

	///<summary> Config UIPage to invoke this </summary>
	public void In_OnClaimStarWithoutAds() {
		if(isFailedConfig)
			return;
		// add coin, open next popup
		userStarInventory.IncreaseStars(userStarCollectedVar.Value);
		// add coin to StarChest
		userStarChestInventory.AddStarsUserHasEarnMoreFromLevels(userStarCollectedVar.Value);
		CheckRequestNextPopup();
		ResetCollectStarVariable();
	}
	#endregion//UI input


	#region Private
	///<summary> When pointer moving in claimReward bar, this var will changed follow position of pointer on the bar </summary>
	int _currentMultiplyFactor = 1;

	async UniTask MovingPointerAndConfigUIAsync() {
		var timer = 0f;
		var sign = 1f;
		var fromPos = pointerMoveFrom.localPosition.x;
		var toPos  = pointerMoveTo.localPosition.x;

		while(true) {
			await UniTask.Yield(cancellationToken:_token.Token);
			timer += Time.deltaTime * sign;
			var ratio = timer / pointerCycleDuration;
			var tweenPosX = Easing.EaseFloat(fromPos, toPos, ratio, pointerTweenType);
			// position for pointer
			var pointerPos = pointerTrans.localPosition;
			pointerPos.x = tweenPosX;
			pointerTrans.localPosition = pointerPos;
			// multiply value, set to ui text
			var factor = GetMultiplyFactor(tweenPosX);
			if(factor != _currentMultiplyFactor) {
				_currentMultiplyFactor = factor;
				multiplyNumberText.text = $"x{_currentMultiplyFactor}";
			}
			// check revert sign
			sign = timer > pointerCycleDuration ? -1f : sign;
			sign = timer < 0f ? 1f : sign;
		}
	}

	int GetMultiplyFactor(float positionX) {
		for(int n = 0; n < rewardAreaList.Length; n++) {
			var reward = rewardAreaList[n];
			if(reward.from.localPosition.x < positionX && reward.to.localPosition.x > positionX) {
				return reward.factor;
			}
		}
		return 1;
	}

	void CheckRequestNextPopup() {
		// check if there're still toy to unlock
		//var cmdToInvoke = toyStorage.IsStillGroupToUnlock() ? requestToyUnlockProgressPopupCmd : requestNextLevelPopupCmd;
		//cmdToInvoke.Invoke();
	}

	///<summary> After adding to inventory, reset it to zero for next level </summary>
	void ResetCollectStarVariable() {
		userStarCollectedVar.Value = 0;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	class RewardArea {
		public Transform from;
		public Transform to;
		public int factor;
	}
	#endregion//Classes
}
