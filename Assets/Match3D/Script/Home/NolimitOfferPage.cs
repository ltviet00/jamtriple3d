using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Handle logic for Nolimit offer page
/// </summary>
public class NolimitOfferPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory coinInventory;
	[SerializeField] UserBoosterInventory boosterInventory;
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;

	[Header("Config")]
	[SerializeField] IntegerVariable coinAmountRewardCfg;
	[SerializeField] IntegerVariable boosterAmountRewardCfg;
	[SerializeField] IntegerVariable unlimitEnergyHourewardCfg;

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
		unlimitEnergyInventory.ExpandUnlimitDuration(unlimitEnergyHourewardCfg.Value);
	}
	#endregion//UI callbacks
}
