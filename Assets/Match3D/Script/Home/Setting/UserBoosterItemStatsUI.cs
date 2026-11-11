using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Config icon and remain booster amount for specific booster id.
/// </summary>
public class UserBoosterItemStatsUI : MonoBehaviour {
	#region Init, config
	[Header("Data, config")]
	[SerializeField] BoosterID targetBoosterID;
	[SerializeField] UserBoosterInventory inventory;
	[SerializeField] BoosterToSpriteMapper spriteMap;

	[Header("Refs")]
	[SerializeField] TMP_Text amountText;
	[SerializeField] Image iconImage;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ConfigContent();
	}
	#endregion// Init config


	#region Private
	[Button]
	void ConfigContent() {
		if(isFailedConfig)
			return;
		var amount = inventory.GetBoosterRemain(targetBoosterID);
		var sprite = spriteMap.GetSprite(targetBoosterID);

		const string TEXT_FORMAT = "x{0}";
		amountText.text = string.Format(TEXT_FORMAT, amount);
		iconImage.sprite = sprite;
	}
	#endregion//Private
}
