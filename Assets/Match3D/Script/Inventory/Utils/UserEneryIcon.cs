using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User got Enery and UnlimitEnergy
/// - when user has unlimit enery, show the heart icon with unlimit icon iside
/// - when user don't purchase unlimit energy, show the remain heart icon
/// </summary>
public class UserEneryIcon : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserEnergyInventory energyInventory;
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;
	[SerializeField] IntegerVariable userRemainEnergyVar;

	[Header("Event in")]
	[SerializeField] GameEvent expandUnlimitDurationEvent;

	[Header("Refs")]
	[SerializeField] TMP_Text energyText;
	[SerializeField] Image energyImage;

	[Header("Config")]
	[SerializeField] Sprite spriteNormal;
	[SerializeField] Sprite spriteUnlimit;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		userRemainEnergyVar.OnValueChangedHandler += OnRemainEnergyChanged;
		expandUnlimitDurationEvent.Subscribe(OnUserExpandUnlimitDuration);
		ConfigDisplay();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		userRemainEnergyVar.OnValueChangedHandler -= OnRemainEnergyChanged;
		expandUnlimitDurationEvent.Unsubscribe(OnUserExpandUnlimitDuration);
	}
	#endregion// Init config


	#region Private
	void OnUserExpandUnlimitDuration() {
		ConfigDisplay();
	}

	void OnRemainEnergyChanged() {
		ConfigDisplay();
	}

	void ConfigDisplay() {
		// icon
		var isUnlimitEnergy = unlimitEnergyInventory.IsInUnlimitEnergyDuration();
		var spriteForEnergyIcon = isUnlimitEnergy ? spriteUnlimit : spriteNormal;
		energyImage.sprite = spriteForEnergyIcon;
		// text
		energyText.enabled = !isUnlimitEnergy;
		energyText.text = userRemainEnergyVar.Value.ToString();
	}
	#endregion//Private
}
