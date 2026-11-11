using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

///<summary> Work with Purchaser, simply store the string productID, use for init with IAP service.
/// now also store some further info from store such as localized string ...</summary>
[CreateAssetMenu(fileName = "IAPProductID", menuName = "GameConfiguration/IAP/IAPProductID")]
public class IAPProductID : ScriptableObject {
	[Header("Config same to ID on Google/Apple store")]
	public string id;

	[Header("Consume / non-consumable")]
	public ProductType productType;

	[Header("Will be parsed from store")]
	public string localizedPriceString;
}
