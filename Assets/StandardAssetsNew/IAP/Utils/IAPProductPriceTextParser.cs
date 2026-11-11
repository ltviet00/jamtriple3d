using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

///<summary> Utils component, assume you got the coin_1_99 (2$) package, and on android devices, you want it to become "vnd23000" (or other currency text base on device's location), then attach this component to Text object, config the package name, it will translate for you </summary>
public class IAPProductPriceTextParser : MonoBehaviour {
	#region Init
	[Header("Package, config")]
	[SerializeField] IAPProductID productID;
	[SerializeField] StartType startType = StartType.Awake;

	[Header("Refs, format")]
	[SerializeField] TMP_Text displayText;
	[SerializeField] string textFormat = "{0}";

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		if(displayText == null) {
			displayText = GetComponent<TMP_Text>();
		}
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	
	void OnEnable() {
		if(isFailedConfig)
			return;
		if(startType == StartType.OnEnable) {
			ApplyLocalizedText();
		}
	}

	private void Awake() {
		if(isFailedConfig)
			return;
		if(startType == StartType.Awake) {
			ApplyLocalizedText();
		}
	}

	void ApplyLocalizedText() {
		var localizedText = productID.localizedPriceString;
		if(localizedText != null) {
			var formatted = string.Format(textFormat, localizedText);
			displayText.text = formatted;
		}
		else {
			Log.Warning(false, this, $"Not found localized price text for package [{productID.name}]");
		}
	}
	#endregion//Init


	#region Classes
	enum StartType {
		Awake,
		OnEnable,
	}
	#endregion//Classes
}
