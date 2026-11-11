using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utils component, config weeklyToyDataSaver, SpriteMapper and Image refs, it will replace image.sprite at OnEnable()
/// </summary>
public class WeeklyToyImageReplacer : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] WeeklyToyIdToSpriteMapper toySpriteMapper;
	[SerializeField] WeeklyEventDataSaver weeklyEventData;
	[SerializeField] ToyStorage toyStorage;

	[Header("Refs")]
	[SerializeField] Image targetImage;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ReplaceImage();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Private
	void ReplaceImage() {
		var toyID = toyStorage.GetWeeklyEventToyIdWithName(weeklyEventData.Data.selectedToyName);
		var sprite = toySpriteMapper.GetSprite(toyID);
		if(sprite != null) {
			targetImage.sprite = sprite;
		}
	}
	#endregion//Private
}
