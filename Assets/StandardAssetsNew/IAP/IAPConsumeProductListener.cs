using UnityEngine;
using System.Collections;
using UnityEngine.Purchasing;
using UnityEngine.Events;

/// <summary>
/// Working with Purchaser components. The Button component will invoke Purchaser to process purchasing, this component
/// will register with Purchaser to receive purchase events (Success, Failed ....), then output some events. Use those
/// output event to apply IAP (add coin for user ...) and config UI ...
/// </summary>
public class IAPConsumeProductListener : MonoBehaviour {
	/*
	#region Init, config
	[Header("ShareData")]
	[SerializeField] IAPConsumeProductPurchaser purchaser;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		RegisterWithPurchaser();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		UnRegisterWithPurchaser();
	}
	#endregion// Init config


	#region Events
	/// <summary> Invoke when user has success purchase a product </summary>
	[SerializeField] IAPConsumeProductPurchaser.UnityEventProduct OnSuccessfulPurchaseHandler;
	/// <summary> Invoke when user has failed purchase a product </summary>
	[SerializeField] IAPConsumeProductPurchaser.UnityEventProduct OnFailedPurchaseHandler;
	/// <summary> Invoked when user request purchase but Pruchaser not Init yet (failed init, or network problem ..) </summary>
	[SerializeField] UnityEvent OnFailedPurchaseCauseNotInitializeHandler;

	/// <summary> Receive callback events from Purchaser </summary>
	void OnSuccessfulPurchase(Product product) {
		OnSuccessfulPurchaseHandler.Invoke(product);
	}

	/// <summary> Receive callback events from Purchaser </summary>
	void OnFailedPurchase(Product product) {
		OnFailedPurchaseHandler.Invoke(product);
	}

	/// <summary> Receive callback events from Purchaser </summary>
	void OnFailedPurcahseCauseNotInitialized() {
		OnFailedPurchaseCauseNotInitializeHandler.Invoke();
	}
	#endregion//Events


	#region Private work with Purchaser
	void RegisterWithPurchaser() {
		purchaser.OnSuccessfulPurchaseHandler.AddListener(OnSuccessfulPurchase);
		purchaser.OnFailedPurchaseHandler.AddListener(OnFailedPurchase);
		purchaser.OnFailedPurchaseCauseNotInitializeHandler.AddListener(OnFailedPurcahseCauseNotInitialized);
	}

	void UnRegisterWithPurchaser() {
		purchaser.OnSuccessfulPurchaseHandler.RemoveListener(OnSuccessfulPurchase);
		purchaser.OnFailedPurchaseHandler.RemoveListener(OnFailedPurchase);
		purchaser.OnFailedPurchaseCauseNotInitializeHandler.RemoveListener(OnFailedPurcahseCauseNotInitialized);
	}
	#endregion// Private work with Purchaser
	*/
}