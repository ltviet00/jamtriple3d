using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// handle function for Daily reward page
/// </summary>
public class DailyRewardPage : MonoBehaviour {
	#region Init, config
	[Header("Ref")]
	[SerializeField] List<DailyRewardItemUI> itemList;

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


	#region UI callback
	///<summary> Config UIPage component to invoke this </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		// currently simply call item list to refresh
		for(int n = 0; n < itemList.Count; n++) {
			itemList[n].ConfigContentFollowUserData();
		}
	}
	#endregion//UI callback
}
