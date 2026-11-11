using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handle logic and UI for LevelFailed in game.
/// Functons mostly same to the NextLevelPage at Home
/// </summary>
public class LevelFailedPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;
	[SerializeField] UserEnergyInventory energyInventory;

	[Header("Refs")]
	[SerializeField] List<NextLevelPageBoosterButton> boosterButtons;

	[Header("Event out")]
	[SerializeField] UnityEvent requestReloadSceneHandler;

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

	///<summary> Config the RePlay button to invoke this </summary>
	public void In_OnPressRePlay() {
		if(isFailedConfig)
			return;
		// unlimit? no deduct energy
		if(unlimitEnergyInventory.IsInUnlimitEnergyDuration()) {
			return;
		}
		// not unlimit, check if not enough energy
		if(energyInventory.IsEmpty()) {
			var outOfLifePage = SimpleDI.Instance.Get<OutOfLifePage>();
			outOfLifePage.Page.UI_KeepCurrentAndOpenMe();
			return;
		}
		// enough energy, then deduct energy
		energyInventory.DecreaseOneEnergy();
		// save booster select
		for(int n = 0; n < boosterButtons.Count; n++) {
			boosterButtons[n].DeductBoosterInUserInventoryIfUserSelectBooster();
		}
		// request reload scene
		requestReloadSceneHandler.Invoke();
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
