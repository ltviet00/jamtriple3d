using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add start resource for user, at first time launch app
/// </summary>
public class StartUserResourceInit : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory coinInventory;
	[SerializeField] UserBoosterInventory boosterInventory;

	[Header("Config")]
	[SerializeField] int coin;
	[SerializeField] List<BoosterResource> boosterResources;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void Start() {
		if(isFailedConfig)
			return;
		// check if we already done
		if(IsAlreadyAddFirstResource())
			return;
		// coin
		coinInventory.LoadFromDisk();
		coinInventory.SetUserCoin(coin);
		// booster
		boosterInventory.LoadFromDisk();
		for(int n = 0; n < boosterResources.Count; n++) {
			var resource = boosterResources[n];
			boosterInventory.AddBooster(resource.boosterID, resource.amount);
		}
		// save
		MarkDoneInitFirstResource();
	}

	const string SaveKey = "InitFirstUserResource";
	bool IsAlreadyAddFirstResource() {
		// will turn to 1 when we save
		var result = PlayerPrefs.GetInt(SaveKey, 0);
		return result == 1;
	}

	void MarkDoneInitFirstResource() {
		PlayerPrefs.SetInt(SaveKey, 1);
	}
	#endregion// Init config


	#region Classes
	[System.Serializable]
	public class BoosterResource {
		public BoosterID boosterID;
		public int amount;
	}
	#endregion//Classes
}
