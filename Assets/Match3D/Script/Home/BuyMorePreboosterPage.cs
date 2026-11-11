using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// At home, when enter game, let user buy more pre-booster
/// </summary>
public class BuyMorePreboosterPage : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] UIPage page;
	[SerializeField] TMP_Text boosterNameText;
	[SerializeField] TMP_Text boosterDecsText;
	[SerializeField] Image boosterIcon;

	[Header("Data")]
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserBoosterInventory userBoosterInventory;
	[SerializeField] BoosterToSpriteMapper boosterToSpriteMapper;
	[SerializeField] BoosterDescStorage boosterDecsStorage;

	[Header("Config")]
	[SerializeField] IntegerVariable amountBoosterPerBuy;
	[SerializeField] IntegerVariable pricePerBuy;

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


	#region UI callbacks
	///<summary> Config the Buy button to invoke this </summary>
	public void UI_OnPressBuyBooster() {
		if(isFailedConfig)
			return;
		var enoughCoin = userCoinInventory.IsEnough(pricePerBuy.Value);
		if(enoughCoin) {
			// deduct coin, add booster
			userCoinInventory.DecreaseCoin(pricePerBuy.Value);
			userBoosterInventory.AddBooster(_trackingBooster, amountBoosterPerBuy.Value);
			// close it
			page.UI_CloseSelfOnly();
		}
		// open Shop
		else {
			var shopPage = SimpleDI.Instance.Get<HomeShopPage>();
			shopPage.Page.UI_KeepCurrentAndOpenMe();
		}
	}
	#endregion//UI callbacks


	#region Public
	///<summary> The boosterID cache from COnfigContent function </summary>
	BoosterID _trackingBooster;
	///<summary> Shortcut to get the page refs </summary>
	public UIPage Page => page;

	///<summary> Call this before open page to let it config content </summary>
	public void ConfigContentForBooster(BoosterID boosterID) {
		if(isFailedConfig)
			return;
		_trackingBooster = boosterID;
		// decs of booster
		var decs = boosterDecsStorage.GetDecs(boosterID);
		if(decs != null) {
			boosterNameText.text = decs.displayName;
			boosterDecsText.text = decs.decscription;
		}
		var icon = boosterToSpriteMapper.GetSprite(boosterID);
		if(icon != null) {
			boosterIcon.sprite = icon;
		}
	}
	#endregion//Public
}
