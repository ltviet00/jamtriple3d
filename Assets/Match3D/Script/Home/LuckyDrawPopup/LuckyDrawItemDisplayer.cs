using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

///<summary> Handle display for item in LuckyDraw </summary>
public class LuckyDrawItemDisplayer : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] BoosterToSpriteMapper boosterToSpriteMapper;
	[SerializeField] Sprite coinIconSprite;

	[Header("Refs")]
	[SerializeField] Image rewardIconImage;
	[SerializeField] TMP_Text rewardAmountText;

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


	#region Public
	public void ConfigContentFollowReward(LuckyDrawRewardList.Reward reward) {
		if(isFailedConfig)
			return;
		// coin?
		if(reward.useCoin) {
			rewardIconImage.sprite = coinIconSprite;
			rewardAmountText.text = $"+{reward.coinAmount}";
		}
		// booster
		else {
			rewardIconImage.sprite = boosterToSpriteMapper.GetSprite(reward.boosterID);
			rewardAmountText.text = $"+{reward.boosterAmount}";
		}
	}
	#endregion//Public
}
