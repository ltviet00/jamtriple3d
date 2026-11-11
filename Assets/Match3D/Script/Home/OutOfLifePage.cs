using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OutOfLifePage : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] UIPage page;

	[Header("Data")]
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserEnergyInventory userEnergyInventory;
	[SerializeField] IntegerVariable coinToBuyFullLifeVar;

	[Header("Unity event out")]
	[SerializeField] UnityEvent openNextLevelPageCmd;
	[SerializeField] UnityEvent openShopPageCmd;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> Shortcut to get the UIPage </summary>
	public UIPage Page => page;
	#endregion//Public


	#region UI input
	///<summary> Config the Buy full energy by coin button to invoke this </summary>
	public void In_OnPressUseCoinToBuyFullLife() {
		if(isFailedConfig)
			return;
		// enough coin? deduct coin and open Level page
		if(userCoinInventory.IsEnough(coinToBuyFullLifeVar.Value)) {
			userCoinInventory.DecreaseCoin(coinToBuyFullLifeVar);
			userEnergyInventory.IncreaseEnergyAndSaveCurrentTime(userEnergyInventory.MaxEnergy);
			openNextLevelPageCmd.Invoke();
		}
		// not enough coin? open shop
		else {
			openShopPageCmd.Invoke();
		}
	}
	#endregion//UI input
}
