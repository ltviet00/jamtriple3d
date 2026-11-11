using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simpler than the IAPButton, this one invoke to the IAPProductPurchaser to call RestorePurchase() function
/// of Unity purchasing lib. WIll output 2 gameEvents when susscess/failed, check the Purchaser for more detail
/// </summary>
public class IAPRestorePurchaseButton : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] IAPConsumeProductPurchaser purchaser;

	[Header("Event in")]
	[SerializeField] GameEvent restorePurchaseSuccessEvent;
	[SerializeField] GameEvent restorePurchaseFailedEvent;

	[Header("Unity Event out")]
	[SerializeField] UnityEvent requestOpenLoadingWhenStartRestoring;
	[SerializeField] UnityEvent requestOpenRestoreSuccess;
	[SerializeField] UnityEvent requestOpenRestoreFailed;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		restorePurchaseSuccessEvent.Subscribe(OnRestorePurchaseSuccess);
		restorePurchaseFailedEvent.Subscribe(OnRestorePurchaseFailed);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		restorePurchaseSuccessEvent.Unsubscribe(OnRestorePurchaseSuccess);
		restorePurchaseFailedEvent.Unsubscribe(OnRestorePurchaseFailed);
	}
	#endregion// Init config


	#region Public - UI
	///<summary> Config UI button to invoke this </summary>
	public void In_RestoreTransaction() {
		if(isFailedConfig)
			return;
		// request loading UI
		requestOpenLoadingWhenStartRestoring.Invoke();
		// call restore
		purchaser.RestorePurchase();
	}
	#endregion//Public - UI


	#region Events
	void OnRestorePurchaseSuccess() {
		requestOpenRestoreSuccess.Invoke();
	}

	void OnRestorePurchaseFailed() {
		requestOpenRestoreFailed.Invoke();
	}
	#endregion//Events
}
