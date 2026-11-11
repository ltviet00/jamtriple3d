using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Attach to button on UI, config button to invoke this to request and handle buy booster using coin </summary>
public class BuyBoosterByCoinButton : MonoBehaviour {
	#region Init, config
	[Header("User, share data booster")]
	[SerializeField] UserBoosterInventory userBoosterInventory;
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] BoosterIDVariable selectedBoosterIDVar;
	[SerializeField] IntegerVariable boosterPriceVar;

	[Header("Unity event out")]
	[SerializeField] UnityEvent notEnoughCoinEvent;
	[SerializeField] UnityEvent buySuccessEvent;

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


	#region Public - UI
	///<summary> Config UI button to invoke this </summary>
	public void In_OnPressButton() {
		if(isFailedConfig)
			return;
		// check enough coin
		var coinRequire = boosterPriceVar.Value;
		if(!userCoinInventory.IsEnough(coinRequire)) {
			notEnoughCoinEvent.Invoke();
			return;
		}
		// reduce coin and increase booster
		const int ITEM_PER_BUY = 1;
		userCoinInventory.DecreaseCoin(coinRequire);
		userBoosterInventory.AddBooster(selectedBoosterIDVar.Value, ITEM_PER_BUY);
		buySuccessEvent.Invoke();
	}
	#endregion//Public - UI
}
