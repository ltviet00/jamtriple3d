using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchasePackSuccessPopup : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Canvas canvas;
	[SerializeField] TMP_Text coinText;
	[SerializeField] TMP_Text hintText;
	[SerializeField] TMP_Text shuffleText;
	[SerializeField] TMP_Text skinText;
	[SerializeField] GameObject removeAdsIcon;

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
	public void In_ActiveAndConfigContent(ShopBundlePackageDefine package) {
		if(isFailedConfig)
			return;
		//gameObject.SetActive(true);
		canvas.enabled = true;
		transform.SetAsLastSibling();
		// content
		coinText.text = $"x{package.coinAmount}";
		hintText.text = $"x{package.boosterExtraTimeAmount}";
		shuffleText.text = $"x{package.boosterUndoAmount}";
		skinText.text = $"x{package.boosterUndoAmount}";
		//removeAdsIcon.SetActive(package.removeAds);
	}
	#endregion//Public - UI
}
