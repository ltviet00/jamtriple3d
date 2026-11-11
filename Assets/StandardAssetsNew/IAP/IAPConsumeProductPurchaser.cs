using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Events;
using UnityEngine.Purchasing.Extension;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor;
#endif//UNITY_EDITOR

/// <summary>
/// Working with UnityIAP, we now only need one instance of this component, do the init task, register listener
/// with Unity build-in IAP component. All purchase, register events ... will do via this component.
/// Should place this component at the first scene, set it to DontDestroy
/// </summary>
[CreateAssetMenu(fileName = "IAPConsumeProductPurchaser", menuName = "GameConfiguration/IAP/IAPConsumeProductPurchaser")]
public class IAPConsumeProductPurchaser : ScriptableObject, IStoreListener, IDetailedStoreListener {
	#region Validate, Config
	[Header("ShareData")]
	[SerializeField] IAPProductIDVariable selectedPackageVar;

	[Header("Config")]
	[SerializeField] FakeStoreUIMode fakeStoreUIMode = FakeStoreUIMode.StandardUser;

	[Header("Events - out")]
	/// <summary> Invoked when starting the purchase process </summary>
	[SerializeField] GameEvent purchaseStartingEvent;
	/// <summary> Invoked when successful purchase </summary>
	[SerializeField] GameEvent purchaseSuccessEvent;
	/// <summary> Invoked when failed purchase </summary>
	[SerializeField] GameEvent purchaseFailedEvent;

	[Header("Events - out - Restore purchase")]
	///<summary> Raised when Success restore purchased. Other module listen to this to reset user's inventory </summary>
	[SerializeField] GameEvent restorePurchaseSuccessEvent;
	///<summary> Raised when Failed restore purchased. Currently use for UI, no user's inventory modification need </summary>
	[SerializeField] GameEvent restorePurchaseFailedEvent;

	[Header("Product List")]
	/// <summary> Reference to the ScriptableObject define all product in game </summary>
	[SerializeField] List<IAPProductID> productsList;

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
	#endregion// Validate, Config


#if UNITY_EDITOR
	[Button("Load all product in same folder")]
	void Editor_LoadAllProductIDInSameFolder() {
		productsList.Clear();
		// get the current directory path to retrieve sub folders
		var root = AssetDatabase.GetAssetPath(this);
		var folderPath = root.Substring(0, root.LastIndexOf("/"));
		var allProduct = AssetDatabase.FindAssets("t:IAPProductID", new string[] { folderPath });

		for(int n = 0; n < allProduct.Length; n++) {
			var guid = allProduct[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var toProduct = AssetDatabase.LoadAssetAtPath<IAPProductID>(toPath);
			if(toProduct == null) {
				Log.Error(false, this, $"Cant load IAPProductID at path [{toPath}]");
				continue;
			}
			// save to list
			productsList.Add(toProduct);
		}
		// finish, let asset refresh
		AssetDatabase.Refresh();
		UnityEditor.EditorUtility.SetDirty(this);
	}
#endif//UNITY_EDITOR


	#region Public - request purchase
	///<summary> Sometime we need to Init it from Splash. Simple call Init(), no magic </summar	y>
	public void InitModule() {
		if(isFailedConfig)
			return;
		Init();
	}

	/// <summary> Use this func to start the purchasing process. </summary>
	public void BuyConsumableProduct(IAPProductID productID) {
		if(isFailedConfig) {
			Log.Error(false, this, "Purchaser instance is FailedConfig, or not init yet, cannot process purchasing [{0}]", productID);
			purchaseFailedEvent.Raise();
			return;
		}
		if(!IsExistIdInProductList(productID)) {
			Log.Error(false, this, $"Purchaser product list not contain [{productID.name}], can't process purchase");
			purchaseFailedEvent.Raise();
			return;
		}
		// save to share var and process
		selectedPackageVar.Value = productID;
		ProcessBuyConsumableProduct(productID.id);
	}

	void ProcessBuyConsumableProduct(string productID) {
		// make sure it initialized
		Init();
		// process buying
		if(IsInitializedIAP()) {
			// ... look up the Product reference with the general product identifier and the Purchasing
			// system's products collection.
			Product product = _storeController.products.WithID(productID);
			// If the look up found a product for this device's store and that product is ready to be sold ...
			if(product != null && product.availableToPurchase) {
				Log.Info(this, "Purchasing product asychronously [{0}]", product.definition.id);
				// ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed
				// asynchronously.
				_storeController.InitiatePurchase(product);
				// raise event
				purchaseStartingEvent.Raise();
			}
			// Otherwise ...
			else {
				// ... report the product look-up failure situation
				Log.Error(false, this, "BuyProductID: FAIL. [{0}] Not purchasing product, either is not found or is not available for purchase", productID);
				Log.Warning(false, this, $"Fail product info, ProductID[{productID}], Avaialbe[{product.availableToPurchase}], HasReceipt[{product.hasReceipt}], LocalizedString[{product.metadata.localizedPriceString}]");
				purchaseFailedEvent.Raise();
			}
		}
		// Otherwise ...
		else {
			// ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or
			// retrying initiailization.
			Log.Error(false, this, $"BuyProductID [{productID}] FAIL. Not initialized.");
			purchaseFailedEvent.Raise();
		}
	}
	#endregion//Public - request purchase


	#region Public - Check receipt
	///<summary> Check if a product has a receipt. Currently use for check non-consume product has purchase (eg: removeAds).
	///Check this, an answer about check receipt https://answers.unity.com/questions/1284733/how-google-play-restore-in-app-purchases.html
	///and this: API about Product in purchasing https://docs.unity3d.com/540/Documentation/ScriptReference/Purchasing.Product.html </summary>
	public bool HasReceipt(IAPProductID productID) {
		if(isFailedConfig)
			return false;
		// make sure it initialized
		Init();
		var product = _storeController.products.WithID(productID.id);
		if(product == null) {
			Log.Error(false, this, $"Not found product [{productID.id}], cant check hasReceipt");
			return false;
		}
		return product.hasReceipt;
	}
	#endregion// Public - Check receipt


	#region Public - Restore transaction
	///<summary> Currently,there're only one API for restoring transition, read more here
	/// https://docs.unity3d.com/Manual/UnityIAPRestoringTransactions.html. No parameter, just 1 function for restore (maybe all)
	/// non-consumable product in the game. We have to implement the behavior to reset state of purchased product (eg: removeAds).
	/// NOTES: currently take care IOS only
	///</summary>
	public void RestorePurchase() {
		if(isFailedConfig) {
			Log.Error(false, this, "RestorePurchase instance is FailedConfig, or not init yet, cannot process purchasing");
			purchaseFailedEvent.Raise();
			return;
		}
		// make sure it initialized
		Init();
		// restore transaction
		Log.Info($"Start Restore transaction, time [{Time.time}]");
		var appleExtension = _storeExtensionProvider.GetExtension<IAppleExtensions>();
		// follow unity docs, on platform that support auto restore, it already invoke at begining of init, so only AppleStore need these.
		// So let just call success (this will be on other platform GooglePlay, universal ...)
		if(appleExtension == null) {
			restorePurchaseSuccessEvent.Raise();
			return;
		}
		appleExtension.RestoreTransactions(OnRestoreTransactions);
	}

	void OnRestoreTransactions(bool success, string error) {
		Log.Info($"Finish Restore transaction, time [{Time.time}], result [{success}]");
		// success restore? Raise event, other module will follow that to reset user's inventory
		if(success) {
			Log.Info($"Success Restore transition");
			restorePurchaseSuccessEvent.Raise();
		}
		// failed restore? nothing but UI only, just raise event
		else {
			Log.Warning(false, $"FAILED Restore transition, error [{error}]");
			restorePurchaseFailedEvent.Raise();
		}
	}
	#endregion//Public - Restore transaction


	#region Init IAP
	/// <summary> Reference to the Unity Purchasing system, will be assigned in Init function.
	/// Use in BuyProdcut functions </summary>
	IStoreController _storeController = null;
	///<summary> Reference to the The store-specific Purchasing subsystems,
	///will be assigned in Init function. Use in Restore purchase function (iOS) </summary>
	IExtensionProvider _storeExtensionProvider = null;

	/// <summary> Do the init task, register events with UnityPurchasing </summary>
	void Init() {
		if(IsInitializedIAP()) {
			return;
		}
		var module = StandardPurchasingModule.Instance();
		// The FakeStore supports: no-ui (always succeeding), basic ui (purchase pass/fail), and
		// developer ui (initialization, purchase, failure code setting). These correspond to
		// the FakeStoreUIMode Enum values passed into StandardPurchasingModule.useFakeStoreUIMode.
		//module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
		module.useFakeStoreUIMode = fakeStoreUIMode;
		// Create a builder, first passing in a suite of Unity provided stores.
		var builder = ConfigurationBuilder.Instance(module);
		// Add a product to sell / restore by way of its identifier, associating the general identifier
		// with its store-specific identifiers.
		for(int index = 0; index < productsList.Count; index++) {
			var product = productsList[index];
			// productType: Consumable, NonConsumable, Subscription
			builder.AddProduct(product.id, product.productType);
		}
		// Kick off the remainder of the set-up with an asynchrounous call, passing the configuration
		// and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
		UnityPurchasing.Initialize(this, builder);
	}

	public bool IsInitializedIAP() {
		// Only say we are initialized if both the Purchasing references are set.
		return _storeController != null && _storeExtensionProvider != null;
	}
	#endregion//Init IAP


	#region IStoreListener
	public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
		// Purchasing has succeeded initializing. Collect our Purchasing references.
		Log.Info(this, "IAPListener, OnInitialized: PASS");
		// Overall Purchasing system, configured with products for this application.
		_storeController = controller;
		// Store specific subsystem, for accessing device-specific store features.
		_storeExtensionProvider = extensions;
		// do other things that need StoreController or Extension
		FillLocalizedPriceStringToProductDefineList();
		// check restore transaction. WHy do it here? read more in https://docs.unity3d.com/Manual/UnityIAPRestoringTransactions.html
		CheckRestoreTransaction();
	}

	public void OnInitializeFailed(InitializationFailureReason error) {
		// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
		Log.Error(false, this, $"OnInitializeFailed InitializationFailureReason: [{error}]");
	}

	public void OnInitializeFailed(InitializationFailureReason error, string message) {
		// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
		Log.Error(false, this, $"OnInitializeFailed InitializationFailureReason: [{error}], message [{message}]");
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) {
		// failed purchase, invoke events
		Log.Error(false, this, $"Purchase FAILED, product [{product.definition.id}], reason [{reason}]");
		purchaseFailedEvent.Raise();
	}

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs arg) {
		// simple check if the product is exist in out defined product list
		var productID = arg.purchasedProduct.definition.id;
		if(IsExistIdInProductList(productID)) {
			Log.Info(this, $"Purchase SUCCESS, Product [{productID}]");
			purchaseSuccessEvent.Raise();
		}
		else {
			Log.Warning(false, this, $"Purchase FAILED, un-recognized product [{productID}]");
			purchaseFailedEvent.Raise();
		}
		// Return a flag indicating whether this product has completely been received, or if the application needs
		// to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still
		// saving purchased products to the cloud, and when that save is delayed.
		return PurchaseProcessingResult.Complete;
	}
	#endregion//IStoreListener


	#region IDetailedStoreListener
	public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) {
		// failed purchase, invoke events
		Log.Error(false, this, $"Purchase FAILED, product [{product.definition.id}], reason [{failureDescription}]");
		purchaseFailedEvent.Raise();
	}
	#endregion//IDetailedStoreListener


	#region Private
	bool IsExistIdInProductList(string productID) {
		for(int index = 0; index < productsList.Count; index++) {
			var product = productsList[index];
			if(System.String.Equals(productID, product.id, System.StringComparison.Ordinal))
				return true;
		}
		// reach here, mean not meet any in productList
		return false;
	}

	bool IsExistIdInProductList(IAPProductID productID) {
		for(int index = 0; index < productsList.Count; index++) {
			var product = productsList[index];
			if(productID == product)
				return true;
		}
		// reach here, mean not meet any in productList
		return false;
	}

	void FillLocalizedPriceStringToProductDefineList() {
		var products = _storeController.products.all;
		Log.Info(this, $"Product regconize amount [{products.Length}]");
		for(int n = 0; n < products.Length; n++) {
			var prod = products[n];
			var meta = prod.metadata;
			Log.Info($"Products ID[{prod.definition.id}], isoCurrencyCode[{meta.isoCurrencyCode}], LocalizedTitle[{meta.localizedTitle}], LocalizedDesc[{meta.localizedDescription}], LocalizedPrice[{meta.localizedPriceString}]");
			// save to productDefine
			//productsList.SaveLocalizedPriceStringForProduct(prod.definition.id, meta.localizedPriceString);
			productsList[n].localizedPriceString = meta.localizedPriceString;
		}
	}

	///<summary> Read more in https://docs.unity3d.com/Manual/UnityIAPRestoringTransactions.html.
	/// Check the IAPButton.cs script from unity sample </summary>
	void CheckRestoreTransaction() {
		// microsoft platform
		if(Application.platform == RuntimePlatform.WSAPlayerX86 ||
			Application.platform == RuntimePlatform.WSAPlayerX64 ||
			Application.platform == RuntimePlatform.WSAPlayerARM) {
			_storeExtensionProvider.GetExtension<IMicrosoftExtensions>().RestoreTransactions();
		}
		// IOS
		else if(Application.platform == RuntimePlatform.IPhonePlayer ||
				 Application.platform == RuntimePlatform.OSXPlayer ||
				 Application.platform == RuntimePlatform.tvOS) {
			_storeExtensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestoreTransactions);
		}
		// samsung
		//else if(Application.platform == RuntimePlatform.Android &&
		//		 StandardPurchasingModule.Instance().appStore == AppStore.SamsungApps) {
		//	_storeExtensionProvider.GetExtension<ISamsungAppsExtensions>().RestoreTransactions(OnRestoreTransactions);
		//}
		// Android
		else if(Application.platform == RuntimePlatform.Android &&
			StandardPurchasingModule.Instance().appStore == AppStore.GooglePlay) {
			_storeExtensionProvider.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(OnRestoreTransactions);
		}
		// not support
		else {
			Log.Warning(false, this, $"[{Application.platform.ToString()}]is not a supported platform for the Codeless IAP restore button");
		}
	}
	#endregion//Private
}
