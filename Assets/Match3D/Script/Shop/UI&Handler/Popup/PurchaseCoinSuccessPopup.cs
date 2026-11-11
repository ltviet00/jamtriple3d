using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

///<summary> Config content for purchase coin success popup </summary>
public class PurchaseCoinSuccessPopup : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Canvas canvas;
	[SerializeField] TMP_Text coinText;
	[SerializeField] Image mainSprite;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
		if(canvas == null) {
			canvas = GetComponent<Canvas>();
		}
	}

	void Awake() {
		if(isFailedConfig)
			return;
		// by default, disable gameobject, will active in ConfigContent()
		//canvas.enabled = false;
	}
	#endregion// Init config


	#region Public - UI
	///<summary> Config the ShopCoinPackageUI to invoke this </summary>
	public void In_ActiveAndConfigContent(ShopCoinPackageDefine package) {
		if(isFailedConfig)
			return;
		//gameObject.SetActive(true);
		canvas.enabled = true;
		transform.SetAsLastSibling();
		coinText.text = $"x{package.coinAmount}";
		//mainSprite.sprite = package.mainSprite;
	}
	#endregion//Public - UI
}
