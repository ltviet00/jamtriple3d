using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Define content for a product (how many coins? remove ads? booster? </summary>
[CreateAssetMenu(fileName = "ShopProductContentDefine", menuName = "Game/Shop/ShopProductContentDefine")]
public class ShopBundlePackageDefine : ScriptableObject {
	#region Init, config
	[Header("ProductID")]
	public IAPProductID productID;

	[Header("Name")]
	public string productName;

	[Header("Content")]
	//public bool removeAds;
	public int coinAmount;
	// ingame boosters
	public int boosterMagnetAmount;
	public int boosterUndoAmount;
	public int boosterFanAmount;
	public int boosterFreezeAmount;
	// pregame boosters
	public int boosterExtraTimeAmount;
	public int boosterLightingStormAmount;
	//public int boosterPreFanAmount;
	// unlimit energy, in hour
	public int unlimitEnergyDuration;

	[Header("Sprite")]
	public Sprite chestSprite;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config
}
