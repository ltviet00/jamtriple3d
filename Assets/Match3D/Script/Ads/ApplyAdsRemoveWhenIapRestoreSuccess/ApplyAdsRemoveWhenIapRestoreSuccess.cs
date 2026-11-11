using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Put at Splash, use GameEventListener to catch event when user RestorePurchase success, then invoke this 
///component to check restore removeAds purchase</summary>
public class ApplyAdsRemoveWhenIapRestoreSuccess : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onRestorePurchaseSuccessEvent;

	[Header("ShareData, IAP")]
	[SerializeField] UserRemoveAdsInventory removeAdsInventory;
	[SerializeField] IAPProductID removeAdsProductID;
	[SerializeField] IAPConsumeProductPurchaser iapPurchaser;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onRestorePurchaseSuccessEvent.Subscribe(OnRestorePurchaseSuccesss);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onRestorePurchaseSuccessEvent.Unsubscribe(OnRestorePurchaseSuccesss);
	}

	///<summary> use GameEventListener to catch event when user RestorePurchase success, then invoke this 
	/// component to check restore removeAds purchase </summary>
	void OnRestorePurchaseSuccesss() {
		// on Android the ProcessPurchase will alway be called when restorePurchase success. 
		// https://forum.unity.com/threads/closed-restore-purchase-using-unity-iap.517731/
		// but IOS is not, we have to make a restore purchase button for user. So this func should only wrapped for IOS, but for now
		// just open for all
		if(isFailedConfig)
			return;
		// check has receipt, then change removeAds inventory
		if(iapPurchaser.HasReceipt(removeAdsProductID)) {
			removeAdsInventory.In_PurchasedAdsSuccess();
			Log.Info($"Restore Purchase success, and user has purchased RemoveAds, so restore it here. status [{removeAdsInventory.HasPurchased()}]");
		}
		else {
			removeAdsInventory.RestorePurchaseButUserNotBuyYet();
			Log.Info($"Restore Purchase, User not purchase RemoveAds yet. status [{removeAdsInventory.HasPurchased()}]");
		}
	}
	#endregion// Init config
}
