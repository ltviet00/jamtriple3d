using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;

/// <summary>
/// Same function to IAPConsumeProductListener, but only listen to one specific product
/// </summary>
public class IAPSpecificConsumeProductListener : MonoBehaviour {
	#region Init, config
	[Header("Package")]
	[SerializeField] IAPProductID productID;
	[SerializeField] IAPProductIDVariable currentRequestIapProduct;

	[Header("Event in")]
	[SerializeField] GameEvent purchasedSuccessEvent;
	[SerializeField] GameEvent purchasedFailedEvent;

	[Header("Unity event out")]
	[SerializeField] UnityEvent onPurchasePackageSuccessHandler;
	[SerializeField] UnityEvent onPurchasePackageFailedHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		purchasedSuccessEvent.Subscribe(OnPurchasedSuccesss);
		purchasedFailedEvent.Subscribe(OnPurchasedFailed);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		purchasedSuccessEvent.Unsubscribe(OnPurchasedSuccesss);
		purchasedFailedEvent.Unsubscribe(OnPurchasedFailed);
	}
	#endregion// Init config


	#region Events
	void OnPurchasedSuccesss() {
		// check same product
		if(currentRequestIapProduct.Value != null && currentRequestIapProduct.Value == productID) {
			onPurchasePackageSuccessHandler.Invoke();
		}
	}

	void OnPurchasedFailed() {
		// check same product
		if(currentRequestIapProduct.Value != null && currentRequestIapProduct.Value == productID) {
			onPurchasePackageFailedHandler.Invoke();
		}
	}
	#endregion//Events


	#region Public
	/// <summary> In case we want to add this component manually, use this to config the productID </summary>
	public void SetProductID(IAPProductID productID) {
		this.productID = productID;
	}
	#endregion//Public

	/*
	#region Init, config
	[Header("Purchaser, Config")]
	[SerializeField] IAPConsumeProductPurchaser purchaser;
	[SerializeField] StringVariable productIdVar;

	[Header("Event out")]
	public UnityEvent OnPurchaseSuccessEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		purchaser.OnSuccessfulPurchaseHandler.AddListener(OnSuccessPurchase);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		purchaser.OnSuccessfulPurchaseHandler.RemoveListener(OnSuccessPurchase);
	}
	#endregion//init, config


	#region Private
	void OnSuccessPurchase(Product product) {
		// same product?
		if(string.Equals(product.definition.id, productIdVar.Value)) {
			OnPurchaseSuccessEvent.Invoke();
		}
	}
	#endregion//Private
	*/
}
