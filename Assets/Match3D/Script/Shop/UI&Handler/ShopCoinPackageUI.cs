using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

///<summary> Same to ShopBundlePackage but for the Coinpackage </summary>
public class ShopCoinPackageUI : MonoBehaviour {
	#region Init, config
	[Header("Package")]
	[SerializeField] ShopCoinPackageDefine targetPackage;

	[Header("User")]
	[SerializeField] UserCoinInventory userCoin;

	[Header("Refs")]
	[SerializeField] TMP_Text coinText;
	[SerializeField] TMP_Text priceText;
	[SerializeField] Image coinImage;

	[Header("Ref - purchase components")]
	[SerializeField] IAPConsumeProductButton iapButton;
	[SerializeField] IAPSpecificConsumeProductListener iapListener;

	[Header("Event out - purchase success content")]
	[SerializeField] UnityEventCoinPackage requestPurchaseCoinSuccessContent;
	[SerializeField] UnityEvent requestPurchaseFailedPopup;


	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ConfigUIFollowPackage();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}

	void Awake() {
		if(isFailedConfig)
			return;
		ConfigUIFollowPackage();
	}
	#endregion// Init config


#if UNITY_EDITOR
	[Button("Config UI follow package")]
	void Editor_ConfigUIFollowPackage() {
		if(isFailedConfig)
			return;
		ConfigUIFollowPackage();
	}
#endif//UNITY_EDITOR

	#region UI callbacks
	///<summary> COnfig the IAP button to invoke this </summary>
	public void In_OnPurchaseSuccess() {
		if(isFailedConfig)
			return;
		AddRewardToUserInventory();
		// request popup
		requestPurchaseCoinSuccessContent.Invoke(targetPackage);
	}

	///<summary> COnfig the IAP button to invoke this </summary>
	public void In_OnPurchaseFailed() {
		if(isFailedConfig)
			return;
		OnFailedPurchase();
	}
	#endregion//UI callbacks


	#region Private
	void ConfigUIFollowPackage() {
		// content
		coinText.text = $"x{targetPackage.coinAmount}";
		// sprite
		coinImage.sprite = targetPackage.displaySprite;
		// price text, need to get through IAPproduct
		var localizedPriceText = targetPackage.productID.localizedPriceString;
		if(!string.IsNullOrEmpty(localizedPriceText)) {
			priceText.text = localizedPriceText;
		}
		// productID
		iapButton.SetProductID(targetPackage.productID);
		iapListener.SetProductID(targetPackage.productID);
	}
	#endregion//Private


	#region Events
	void AddRewardToUserInventory() {
		// apply to user's inventory
		var coinAmount = targetPackage.coinAmount;
		userCoin.IncreaseCoin(coinAmount);
	}

	void OnFailedPurchase() {
		requestPurchaseFailedPopup.Invoke();
	}
	#endregion//Event


	#region Classes
	[System.Serializable]
	public class UnityEventCoinPackage : UnityEvent<ShopCoinPackageDefine> {
	}
	#endregion//Classes
}
