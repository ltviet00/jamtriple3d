using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put it to ShopPage at home
/// </summary>
public class HomeShopPage : MonoBehaviour {
	#region Init, config
	[Header("Resf")]
	[SerializeField] UIPage page;

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
	///<summary> Shortcut to get UIPage </summary>
	public UIPage Page => page;
	#endregion//Public
}
