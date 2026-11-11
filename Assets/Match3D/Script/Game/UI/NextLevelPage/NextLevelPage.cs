using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// We often check energy enough before call this popup, so just need to deduct enery when user press Play
/// </summary>
public class NextLevelPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;
	[SerializeField] UserEnergyInventory energyInventory;

	[Header("Refs")]
	[SerializeField] List<NextLevelPageBoosterButton> boosterButtons;

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
	///<summary> Config UIPage component to invoke this </summary>
	public void In_OnPageStartOpen() {
		if(isFailedConfig)
			return;
		// let boosterButtons to config content
		for(int n = 0; n < boosterButtons.Count; n++) {
			boosterButtons[n].In_ConfigContentFollowUserLevel();
		}
	}

	///<summary> Config the Play button to invoke this </summary>
	public void In_OnPressPlay() {
		if(isFailedConfig)
			return;
		// unlimit? no deduct energy
		if(unlimitEnergyInventory.IsInUnlimitEnergyDuration()) {
			return;
		}
		// not unlimit, deduct energy
		energyInventory.DecreaseOneEnergy();
		// save booster select
		for(int n = 0; n < boosterButtons.Count; n++) {
			boosterButtons[n].DeductBoosterInUserInventoryIfUserSelectBooster();
		}
	}

	///<summary> Config UIPage to call this (update content when user buy more pre booster) </summary>
	public void In_OnPageFocus() {
		if(isFailedConfig)
			return;
		// let boosterButtons to re-config content
		for(int n = 0; n < boosterButtons.Count; n++) {
			boosterButtons[n].In_ConfigContentFollowUserLevel();
		}
	}
	#endregion//UI callbacks
}
