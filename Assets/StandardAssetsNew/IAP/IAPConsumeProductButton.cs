using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// IAPConsumeProductPurchaser is now Scriotableobject, take the Init task, register listener ....
/// This component will use to attach to Product button on UI, config UIButton to invoke the
/// BuyProduct() function, it will call Purchaser to process the purchasing task
/// </summary>
public class IAPConsumeProductButton : MonoBehaviour {
	#region Init, config
	[Header("Purchaser, config")]
	[SerializeField] IAPConsumeProductPurchaser purchaser;
	[SerializeField] IAPProductID productIdVar;

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

	void Awake() {
		// Find the button, then register button callback
		var btn = GetComponent<Button>();
		if(btn != null) {
			btn.onClick.AddListener(In_BuyProduct);
		}
		else {
			Log.Info("IAPConsumeProductButton should attach to object that have Button component");
		}
	}
	#endregion//Init, config


	#region Public - receive UI invokes
	/// <summary> Config UIButton OnClick to invoke this func, start purchasing process </summary>
	public void In_BuyProduct() {
		if(isFailedConfig) {
			Log.Error(false, this, "Empty productID, quit");
			return;
		}
		Log.Info(this, "IAPButon, request buy [{0}]", productIdVar.id);
		purchaser.BuyConsumableProduct(productIdVar);
	}

	/// <summary> In case we want to add this component manually, use this to config the productID </summary>
	public void SetProductID(IAPProductID productID) {
		this.productIdVar = productID;
	}
	#endregion//Public - receive UI invokes
}