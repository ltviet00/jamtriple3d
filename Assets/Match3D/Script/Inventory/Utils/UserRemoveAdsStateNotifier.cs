using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UserRemoveAdsStateNotifier : MonoBehaviour {
	#region Init, config
	[Header("UserData")]
	[SerializeField] UserRemoveAdsInventory removeAdsInventoty;

	[Header("Config")]
	[SerializeField] StartType notifyType = StartType.Awake;

	[Header("Unity event out")]
	[SerializeField] UnityEvent purchasedRemoveAdsHandler;
	[SerializeField] UnityEvent notPurchasedRemoveAdsHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void Awake() {
		if(isFailedConfig)
			return;
		if(notifyType == StartType.Awake) {
			CheckRaiseEvent();
		}
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		if(notifyType == StartType.OnEnable) {
			CheckRaiseEvent();
		}
	}

	void CheckRaiseEvent() {
		var purchasedRemoveAds = removeAdsInventoty.HasPurchased();
		var eventToRaised = purchasedRemoveAds ? purchasedRemoveAdsHandler : notPurchasedRemoveAdsHandler;
		eventToRaised.Invoke();
	}
	#endregion// Init config


	#region Classes
	enum StartType {
		Awake,
		OnEnable,
	}
	#endregion//Classes
}
