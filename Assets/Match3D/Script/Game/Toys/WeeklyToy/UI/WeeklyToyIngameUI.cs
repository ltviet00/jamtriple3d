using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple logic for WeeklyToyUI on rightTop header: active UI only when user catch the first toy.
/// Also config Toy Icon
/// </summary>
public class WeeklyToyIngameUI : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] TMP_Text countingText;
	[SerializeField] Image toyIconImage;

	[Header("ShareData")]
	[SerializeField] IntegerVariable currentCollectedToyVar;
	[SerializeField] WeeklyToyIdToSpriteMapper toySpriteMapper;
	[SerializeField] ToyStorage toyStorage;
	[SerializeField] WeeklyEventDataSaver weeklyEventData;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// by default disable display content
		SetActiveContent(false);
		// events, sprite
		currentCollectedToyVar.OnValueChangedHandler += OnToyCollectChanged;
		ConfigToySprite();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		currentCollectedToyVar.OnValueChangedHandler -= OnToyCollectChanged;
	}
	#endregion// Init config


	#region Private
	void OnToyCollectChanged() {
		// if toy amount > 0, then stop listening, active display content
		if(currentCollectedToyVar.Value > 0) {
			currentCollectedToyVar.OnValueChangedHandler -= OnToyCollectChanged;
			SetActiveContent(true);
		}
	}

	void ConfigToySprite() {
		var toyID = toyStorage.GetWeeklyEventToyIdWithName(weeklyEventData.Data.selectedToyName);
		var sprite = toySpriteMapper.GetSprite(toyID);
		if(sprite != null) {
			toyIconImage.sprite = sprite;
		}
	}

	void SetActiveContent(bool active) {
		countingText.enabled = active;
		toyIconImage.enabled = active;
	}
	#endregion//Private
}
