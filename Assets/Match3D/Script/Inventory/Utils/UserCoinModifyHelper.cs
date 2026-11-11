using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Some public function, using for modify booster, currently , use for ads and IAP button </summary>

public class UserCoinModifyHelper : MonoBehaviour {
	#region Init, config
	[Header("UserData")]
	[SerializeField] UserCoinInventory userCoinInventory;

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
	///<summary> Use UI event or button to invoke this </summary>
	public void In_DeductCoinAmount(int amount) {
		if(isFailedConfig)
			return;
		userCoinInventory.DecreaseCoin(amount);
	}

	///<summary> Use UI event or button to invoke this </summary>
	public void In_AddCoinAmount(int amount) {
		if(isFailedConfig)
			return;
		userCoinInventory.IncreaseCoin(amount);
	}

	public void In_DeductCoinByVariable(IntegerVariable coinAmountVar) {
		if(isFailedConfig)
			return;
		userCoinInventory.DecreaseCoin(coinAmountVar.Value);
	}

	public void In_AddCoinByVariable(IntegerVariable coinAmountVar) {
		if(isFailedConfig)
			return;
		userCoinInventory.IncreaseCoin(coinAmountVar.Value);
	}
	#endregion//Public
}
