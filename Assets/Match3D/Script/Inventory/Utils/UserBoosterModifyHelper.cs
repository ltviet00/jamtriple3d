using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Some public function, using for modify booster, currently , use for ads and IAP button </summary>
public class UserBoosterModifyHelper : MonoBehaviour {
	#region Init, config
	[Header("UserData")]
	[SerializeField] UserBoosterInventory userBoosterInventory;
	[SerializeField] BoosterID boosterID;

	[Header("Use variable?")]
	[SerializeField] bool useVariable;
	[SerializeField] BoosterIDVariable selectedBoosterID;

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
	public void In_DeductBoosterAmount(int amount) {
		if(isFailedConfig)
			return;
		var pickBoosterID = useVariable ? selectedBoosterID.Value : boosterID;
		userBoosterInventory.DeductBooster(pickBoosterID, amount);
	}

	///<summary> Use UI event or button to invoke this </summary>
	public void In_AddBoosterAmount(int amount) {
		if(isFailedConfig)
			return;
		var pickBoosterID = useVariable ? selectedBoosterID.Value : boosterID;
		userBoosterInventory.AddBooster(pickBoosterID, amount);
	}
	#endregion//Public
}
