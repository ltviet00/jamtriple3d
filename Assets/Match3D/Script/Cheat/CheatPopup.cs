using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Cheat popup at home </summary>
public class CheatPopup : MonoBehaviour {
	#region Init, config
	[Header("User - Coin, Booster, Levle")]
	[SerializeField] UserBoosterInventory userBooster;
	[SerializeField] UserCoinInventory userCoin;
	[SerializeField] UserLevelScoreInventory userLevel;

	[Header("Energy")]
	[SerializeField] UserEnergyInventory userEnergy;
	[SerializeField] UserUnlimitEnergyInventory userUnlimitEnergy;

	[Header("InGame")]
	[SerializeField] GameEvent gameWinEvent;
	[SerializeField] GameEvent gameLoseEvent;

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


#if CHEAT
	//------ Booster
	public void In_BoosterAdd(int amount) {
		if(isFailedConfig)
			return;
		var allBoosterID = userBooster.allBoosterID;
		for(int n = 0; n < allBoosterID.Length; n++) {
			userBooster.AddBooster(allBoosterID[n], amount);
		}
	}

	public void In_Booster_Reset() {
		if(isFailedConfig)
			return;
		var allBoosterID = userBooster.allBoosterID;
		for(int n = 0; n < allBoosterID.Length; n++) {
			var boosterID = allBoosterID[n];
			var remain = userBooster.GetBoosterRemain(boosterID);
			userBooster.DeductBooster(boosterID, remain);
		}
	}

	//------ Coin
	public void In_CoinAdd(int amount) {
		if(isFailedConfig)
			return;
		userCoin.IncreaseCoin(amount);
	}

	public void In_CoinReset() {
		if(isFailedConfig)
			return;
		var currentCoin = userCoin.GetUserCoin();
		userCoin.DecreaseCoin(currentCoin);
	}

	//------- Level
	public void In_LevelAddMore(int amount) {
		if(isFailedConfig)
			return;
		userLevel.CheatSkipLevel(amount);
	}

	public void In_LevelReset() {
		if(isFailedConfig)
			return;
		userLevel.CheatResetLevel();
	}

	//------ Energy
	public void In_EnergyFull() {
		if(isFailedConfig)
			return;
		userEnergy.CheatFullEnergy();
	}

	public void In_EnergyZero() {
		if(isFailedConfig)
			return;
		userEnergy.CheatZeroEnergy();
	}

	public void In_UnlimitEnergyAdd1Hour() {
		if(isFailedConfig)
			return;
		userUnlimitEnergy.CheatAdd1hourUnlimit();
	}

	public void In_UnlimitEnergyReset() {
		if(isFailedConfig)
			return;
		userUnlimitEnergy.CheatZeroUnlimitEnergy();
	}

	//----- win,lose game
	public void In_GameWin() {
		if(isFailedConfig)
			return;
		gameWinEvent.Raise();
	}

	public void In_GameLose() {
		if(isFailedConfig)
			return;
		gameLoseEvent.Raise();
	}

#endif//CHEAT
}
