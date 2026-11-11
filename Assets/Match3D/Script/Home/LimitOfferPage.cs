using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitOfferPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory coinInventory;
	[SerializeField] UserBoosterInventory boosterInventory;

	[Header("Config")]
	[SerializeField] IntegerVariable coinAmountRewardCfg;
	[SerializeField] IntegerVariable boosterAmountRewardCfg;

	//[Header("Refs")]
	//[SerializeField] TMP_Text coinAmountText;
	//[SerializeField] TMP_Text boosterAmountText;

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


	#region UI callbacks
	///<summary> Config IAP button to invoke this </summary>
	public void In_OnPurchaseSuccess() {
		if(isFailedConfig)
			return;
		// save reward to inventory
		coinInventory.IncreaseCoin(coinAmountRewardCfg.Value);
		boosterInventory.AddRandomBoosterAmount(boosterAmountRewardCfg.Value);
	}
	#endregion//UI callbacks
}
