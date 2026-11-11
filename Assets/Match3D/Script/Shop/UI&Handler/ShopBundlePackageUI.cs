using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

///<summary> Handle UI display for ShopBundle package </summary>
public class ShopBundlePackageUI : MonoBehaviour {
	#region Init, config
	[Header("Package")]
	[SerializeField] ShopBundlePackageDefine targetPackage;

	[Header("User")]
	[SerializeField] UserCoinInventory userCoin;
	[SerializeField] UserBoosterInventory userBooster;
	[SerializeField] UserUnlimitEnergyInventory userUnilimitEnergy;

	[Header("Refs - title, coin, booster")]
	[SerializeField] TMP_Text titleText;
	[SerializeField] TMP_Text coinText;
	
	[SerializeField] TMP_Text magnetText;
	[SerializeField] TMP_Text undoText;
	[SerializeField] TMP_Text fanText;
	[SerializeField] TMP_Text freezeText;

	[SerializeField] TMP_Text extraTimeText;
	[SerializeField] TMP_Text prefanText;
	[SerializeField] TMP_Text lightingStormText;
	[SerializeField] Image chestSpriteRender;

	[Header("Refs - title, coin, booster")]
	[SerializeField] GameObject unlimitEnergySession;
	[SerializeField] TMP_Text unlimitEnergyText;

	[Header("Refs - price text")]
	[SerializeField] TMP_Text priceText;

	[Header("Event out - - purchase components")]
	[SerializeField] IAPConsumeProductButton iapButton;
	[SerializeField] IAPSpecificConsumeProductListener iapListener;
	

	//[Header("IAP : Event, shareData")]
	//[SerializeField] IAPProductIDVariable selectedPackageVar;
	//[SerializeField] GameEvent iapStartPurchasingEvent;
	//[SerializeField] GameEvent iapSuccessPurchaseEvent;
	//[SerializeField] GameEvent iapFailedPurchaseEvent;

	[Header("BoosterID")]
	[SerializeField] BoosterID boosterMagnet;
	[SerializeField] BoosterID boosterUndo;
	[SerializeField] BoosterID boosterFan;
	[SerializeField] BoosterID boosterFreeze;

	[SerializeField] BoosterID preBoosterExtraTime;
	//[SerializeField] BoosterID preBoosterFan;
	[SerializeField] BoosterID preBoosterLightingStorm;

	[Header("Refs - purchase success content")]
	[SerializeField] UnityEventCoinPackage requestPurchasePackageSuccessContent;
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
	}
	#endregion// Init config

#if UNITY_EDITOR
	[Button("Config UI follow package")]
	void Editor_ConfigUIFollowPackage() {
		if(isFailedConfig)
			return;
		ConfigUIFollowPackage();
		UnityEditor.EditorUtility.SetDirty(this.gameObject);
	}
#endif//UNITY_EDITOR

	#region Private
	void ConfigUIFollowPackage() {
		// title
		titleText.text = targetPackage.productName;
		// content
		coinText.text = $"x{targetPackage.coinAmount}";
		magnetText.text = $"x{targetPackage.boosterMagnetAmount}";
		undoText.text = $"x{targetPackage.boosterUndoAmount}";
		fanText.text = $"x{targetPackage.boosterFanAmount}";
		freezeText.text = $"x{targetPackage.boosterFreezeAmount}";

		extraTimeText.text = $"x{targetPackage.boosterExtraTimeAmount}";
		//prefanText.text = $"x{targetPackage.boosterPreFanAmount}";
		lightingStormText.text = $"x{targetPackage.boosterLightingStormAmount}";

		// unlimit energy
		unlimitEnergySession.SetActive(targetPackage.unlimitEnergyDuration > 0);
		unlimitEnergyText.text = $"{targetPackage.unlimitEnergyDuration}h";
		// sprite
		chestSpriteRender.sprite = targetPackage.chestSprite;
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


	#region UI callbacks
	///<summary> COnfig the IAP button to invoke this </summary>
	public void In_OnPurchaseSuccess() {
		if(isFailedConfig)
			return;
		AddRewardToUserInventory();
		// request popup
		requestPurchasePackageSuccessContent.Invoke(targetPackage);
	}

	///<summary> COnfig the IAP button to invoke this </summary>
	public void In_OnPurchaseFailed() {
		if(isFailedConfig)
			return;
		OnFailedPurchase();
	}
	#endregion//UI callbacks


	#region Events
	void AddRewardToUserInventory() {
		// apply to user's inventory
		userCoin.IncreaseCoin(targetPackage.coinAmount);
		// booster
		userBooster.AddBooster(boosterMagnet, targetPackage.boosterMagnetAmount);
		userBooster.AddBooster(boosterUndo, targetPackage.boosterUndoAmount);
		userBooster.AddBooster(boosterFan, targetPackage.boosterFanAmount);
		userBooster.AddBooster(boosterFreeze, targetPackage.boosterFreezeAmount);
		// prebooster
		userBooster.AddBooster(preBoosterExtraTime, targetPackage.boosterExtraTimeAmount);
		//userBooster.AddBooster(preBoosterFan, targetPackage.boosterPreFanAmount);
		userBooster.AddBooster(preBoosterLightingStorm, targetPackage.boosterLightingStormAmount);
		// energy
		if(targetPackage.unlimitEnergyDuration > 0) {
			userUnilimitEnergy.ExpandUnlimitDuration(targetPackage.unlimitEnergyDuration);
		}
	}

	void OnFailedPurchase() {
		requestPurchaseFailedPopup.Invoke();
	}
	#endregion//Event


	#region Classes
	[System.Serializable]
	public class UnityEventCoinPackage : UnityEvent<ShopBundlePackageDefine> {
	}
	#endregion//Classes
}
