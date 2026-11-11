using UnityEngine;
using System.Collections;
// using NaughtyAttributes;
using System.Collections.Generic;

/// <summary>
/// IAP product is define by an ID (string) in Google, and also defined with a stringID in IAP catalog, we can
/// retrive them via product.definition.id function (read CodelessIAP session again). Then we should have ScriptableObject
/// to define all those products to be use in game.
/// When we received a callback from IAPButton, we take out the productID (string), look it up in our ScriptableObject,
/// know how many coin, booster to give user, then write to Profile
/// </summary>
[CreateAssetMenu(fileName = "IAPProductDefine", menuName = "GameConfiguration/IAP/IAPProductDefine")]
public class IAPProductDefine : ScriptableObject {
	///<summary> Define all product will use in game</summary>
	[SerializeField] ProductConfig[] products;

	/// <summary> Convert the products List to Dictionary for faster accessing </summary>
	Dictionary<string, ProductConfig> _cachedProductMap = new Dictionary<string, ProductConfig>();
	Dictionary<string, ProductConfig> CachedProductMap {
		get {
			if(_cachedProductMap.Count != products.Length) {
				_cachedProductMap.Clear();
				for(int n = 0; n < products.Length; n++) {
					var prod = products[n];
					// careful check duplicate here. ONly need to do in Editor, cause we will define this SO in Editor only
#if UNITY_EDITOR
					if(_cachedProductMap.ContainsKey(prod.id)) {
						Log.Error(false, this, $"Duplicate product [{prod.id}] in the list, check again and fix it !!!");
						continue;
					}
#endif//UNITY_EDITOR
					_cachedProductMap.Add(prod.id, prod);
				}
			}
			return _cachedProductMap;
		}
	}

	public ProductConfig[] GetProductList() {
		return products;
	}

	public ProductConfig GetProduct(string id) {
		CachedProductMap.TryGetValue(id, out ProductConfig result);
		if(result == null) {
			Log.Error(false, this, "Not define product with ID [{0}]", id);
			return null;
		}
		return result;
	}

	public void SaveLocalizedPriceStringForProduct(string productID, string localizedText) {
		var product = GetProduct(productID);
		if(product != null) {
			product.localizedPriceString = localizedText;
		}
	}

	public string GetLocalizedPriceStringForProduct(string productID) {
		// dont need to fire exception here, already done it in GetProdoct()
		var prod = GetProduct(productID);
		if(prod != null) return prod.localizedPriceString;
		return null;
	}

	///<summary> Define product, the ID must be the same with ProductID in Store and in IapCatalog</summary>
	[System.Serializable]
	public class ProductConfig {
		public string id;
		/// <summary> The localizedPrice string (VND, Euro, USD ..). This will be filled up by IAPConsumeProductPurchaser when it finished initing </summary>
		public string localizedPriceString;
	}
}