using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Define content for a CoinPackage (how many coins...icon) </summary>
[CreateAssetMenu(fileName = "ShopCoinPackageDefine", menuName = "Game/Shop/ShopCoinPackageDefine")]
public class ShopCoinPackageDefine : ScriptableObject {
	#region Init, config
	[Header("ProductID")]
	public IAPProductID productID;

	[Header("Sprite")]
	public Sprite displaySprite;

	[Header("Content")]
	public int coinAmount;

	//[Header("Sprite")]
	//public Sprite mainSprite;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config
}
