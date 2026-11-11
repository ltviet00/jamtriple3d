using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// Simple component for replacing the build-ing IAPButton component, to do the simple buy-consumeable product.
/// Often attach it to the Product button on UI, config the UIButton to invoke Purchasing function of this component
/// to start purchasing process
/// </summary>
public class IAPSamplePurchaseCode : MonoBehaviour, IStoreListener, IDetailedStoreListener {
	#region Init
	/// <summary> Reference to the Unity Purchasing system, will be assigned in Init function.
	/// Use in BuyProdcut functions </summary>
	IStoreController _storeController;
	///<summary> Reference to the The store-specific Purchasing subsystems,
	///will be assigned in Init function. Use in Restore purchase function (iOS) </summary>
	IExtensionProvider _storeExtensionProvider;

	// Product identifiers for all products capable of being purchased: 
	// "convenience" general identifiers for use with Purchasing, and their store-specific identifier 
	// counterparts for use with and outside of Unity Purchasing. Define store-specific identifiers 
	// also on each platform's publisher dashboard (iTunes Connect, Google Play Developer Console, etc.)

	// General product identifiers for the consumable, non-consumable, and subscription products.
	// Use these handles in the code to reference which product to purchase. Also use these values 
	// when defining the Product Identifiers on the store. Except, for illustration purposes, the 
	// kProductIDSubscription - it has custom Apple and Google identifiers. We declare their store-
	// specific mapping to Unity Purchasing's AddProduct, below.

	[SerializeField] string productIDConsumable = "consumable";
	[SerializeField] string productIDNonConsumable = "nonconsumable";
	[SerializeField] string productIDSubscription = "subscription";

	// Apple App Store-specific product identifier for the subscription product.
	string kProductNameAppleSubscription = "com.unity3d.subscription.new";

	// Google Play Store-specific product identifier subscription product.
	string kProductNameGooglePlaySubscription = "com.unity3d.subscription.original";

	void Awake() {
		// If we haven't set up the Unity Purchasing reference
		InitializePurchasing();
	}

	void InitializePurchasing() {
		// If we have already connected to Purchasing ...
		if(IsInitialized()) {
			return;
		}

		// Create a builder, first passing in a suite of Unity provided stores.
		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
		// Add a product to sell / restore by way of its identifier, associating the general identifier
		// with its store-specific identifiers.
		builder.AddProduct(productIDConsumable, ProductType.Consumable);
		// Continue adding the non-consumable product.
		builder.AddProduct(productIDNonConsumable, ProductType.NonConsumable);
		// And finish adding the subscription product. Notice this uses store-specific IDs, illustrating
		// if the Product ID was configured differently between Apple and Google stores. Also note that
		// one uses the general kProductIDSubscription handle inside the game - the store-specific IDs 
		// must only be referenced here. 
		builder.AddProduct(productIDSubscription, ProductType.Subscription, new IDs(){
			{ kProductNameAppleSubscription, AppleAppStore.Name },
			{ kProductNameGooglePlaySubscription, GooglePlay.Name },
			{ kProductNameGooglePlaySubscription, AmazonApps.Name },
		});

		// Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
		// and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
		UnityPurchasing.Initialize(this, builder);
	}

	bool IsInitialized() {
		// Only say we are initialized if both the Purchasing references are set.
		return _storeController != null && _storeExtensionProvider != null;
	}
	#endregion//Init


	#region Events
	/// <summary> Invoked when successful purchase </summary>
	[Header("Purchase events")]
	public UnityEvent OnSuccessfulPurchaseHandler;
	/// <summary> Invoked when failed purchase </summary>
	public UnityEvent OnFailedPurchaseHandler;
	#endregion//Events


	#region Receive UI invokes
	/// <summary> Config UIButton Buy button to invoke this to start the Purchasing process </summary>
	public void In_BuyConsumable() {
		if(!IsInitialized())
			InitializePurchasing();
		BuyConsumable();
	}
	#endregion//Receive UI invokes


	#region Public
	/// <summary> Buy the Consumeable product with above predefined productID. We often use the In_BuyComsumable()
	/// function above via UIButton.OnClick() event </summary>
	public void BuyConsumable() {
		// Buy the consumable product using its general identifier. Expect a response either 
		// through ProcessPurchase or OnPurchaseFailed asynchronously.
		BuyProductID(productIDConsumable);
	}

	/// <summary> Buy the NonConsumeable product with above predefined productID </summary>
	public void BuyNonConsumable() {
		// Buy the non-consumable product using its general identifier. Expect a response either 
		// through ProcessPurchase or OnPurchaseFailed asynchronously.
		BuyProductID(productIDNonConsumable);
	}

	/// <summary> Buy the Consumeable product with above predefined productID </summary>
	public void BuySubscription() {
		// Buy the subscription product using its the general identifier. Expect a response either 
		// through ProcessPurchase or OnPurchaseFailed asynchronously.
		// Notice how we use the general product identifier in spite of this ID being mapped to
		// custom store-specific identifiers above.
		BuyProductID(productIDSubscription);
	}

	///<summary> Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
	/// Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
	/// Readmore https://docs.unity3d.com/Packages/com.unity.purchasing@4.9/manual/UnityIAPRestoringTransactions.html</summary>>
	public void RestorePurchases() {
		// If Purchasing has not yet been set up ...
		if(!IsInitialized()) {
			// ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
			Debug.Log("RestorePurchases FAIL. Not initialized.");
			return;
		}

		// If we are running on an Apple device ... 
		if(Application.platform == RuntimePlatform.IPhonePlayer ||
			Application.platform == RuntimePlatform.OSXPlayer) {
			// ... begin restoring purchases
			Debug.Log("RestorePurchases started ...");

			// Fetch the Apple store-specific subsystem.
			var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
			// Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
			// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
			apple.RestoreTransactions((result, error) => {
				//// The first phase of restoration. If no more responses are received on ProcessPurchase then 
				//// no purchases are available to be restored.
				//Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");

				if(result) {
					// This does not mean anything was restored,
					// merely that the restoration process succeeded.
					Debug.Log($"Restore successfull, time [{Time.time}]");
				}
				else {
					// Restoration failed. `error` contains the failure reason.
					Debug.LogWarning($"Restore FAILED, [{error}] time [{Time.time}]");
				}
			});
		}
		// Otherwise ...
		else {
			// We are not running on an Apple device. No work is necessary to restore purchases.
			Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
		}
	}
	#endregion//Public


	#region Private
	/// <summary> Main function for executing purchasing process </summary>
	void BuyProductID(string productId) {
		// If Purchasing has been initialized ...
		if(IsInitialized()) {
			// ... look up the Product reference with the general product identifier and the Purchasing 
			// system's products collection.
			Product product = _storeController.products.WithID(productId);

			// If the look up found a product for this device's store and that product is ready to be sold ... 
			if(product != null && product.availableToPurchase) {
				Log.Info(this, "Purchasing product asychronously [{0}]", product.definition.id);
				// ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
				// asynchronously.
				_storeController.InitiatePurchase(product);
			}
			// Otherwise ...
			else {
				// ... report the product look-up failure situation  
				Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
			}
		}
		// Otherwise ...
		else {
			// ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
			// retrying initiailization.
			Debug.Log("BuyProductID FAIL. Not initialized.");
		}
	}
	#endregion//Private


	#region IStoreListener
	public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
		// Purchasing has succeeded initializing. Collect our Purchasing references.
		Log.Info(this, "[{0}] OnInitialized: PASS", name);
		// Overall Purchasing system, configured with products for this application.
		_storeController = controller;
		// Store specific subsystem, for accessing device-specific store features.
		_storeExtensionProvider = extensions;
	}

	public void OnInitializeFailed(InitializationFailureReason error) {
		// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
		Log.Error(false, this, $"OnInitializeFailed InitializationFailureReason: [{error}]");
	}

	public void OnInitializeFailed(InitializationFailureReason error, string message) {
		// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
		Log.Error(false, this, $"OnInitializeFailed InitializationFailureReason: [{error}], message [{message}]");
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
		// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
		// this reason with the user to guide their troubleshooting actions.
		var productID = product.definition.storeSpecificId;
		Log.Warning(false, this, "OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", productID, failureReason);
		// failed purchase, invoke events
		OnFailedPurchaseHandler.Invoke();
	}

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
		var productID = args.purchasedProduct.definition.id;
		// A consumable product has been purchased by this user.
		if(String.Equals(productID, productIDConsumable, StringComparison.Ordinal)) {
			Log.Info(this, "ProcessPurchase: PASS. Product [{0}]", productID);
			// The consumable item has been successfully purchased, invoke event.
			OnSuccessfulPurchaseHandler.Invoke();
		}
		// Or ... a non-consumable product has been purchased by this user.
		else if(String.Equals(productID, productIDNonConsumable, StringComparison.Ordinal)) {
			Debug.LogFormat("ProcessPurchase: PASS. Product [{0}]", productID);
			// TODO: The non-consumable item has been successfully purchased, grant this item to the player.
		}
		// Or ... a subscription product has been purchased by this user.
		else if(String.Equals(productID, productIDSubscription, StringComparison.Ordinal)) {
			Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", productID));
			// TODO: The subscription item has been successfully purchased, grant this to the player.
		}
		// Or ... an unknown product has been purchased by this user. Fill in additional products here....
		else {
			Log.Error(false, this, "ProcessPurchase: FAIL. Unrecognized product [{0}]", productID);
			// failed purchase, invoke events
			OnFailedPurchaseHandler.Invoke();
		}

		// Return a flag indicating whether this product has completely been received, or if the application needs 
		// to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
		// saving purchased products to the cloud, and when that save is delayed. 
		return PurchaseProcessingResult.Complete;
	}
	#endregion//IStoreListener


	#region IDetailedStoreListener
	public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) {
		throw new NotImplementedException();
	}
	#endregion//IDetailedStoreListener
}
