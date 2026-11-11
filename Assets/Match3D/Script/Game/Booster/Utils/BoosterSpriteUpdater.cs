using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utils component, add to object has SpriteRenderer, config the BoosterID
/// it will replace the sprite for renderer follow BoosterID, on OnEnable()
/// </summary>
public class BoosterSpriteUpdater : MonoBehaviour {
	#region Init, config
	[Header("Refs, Data, config")]
	[SerializeField] SpriteRenderer targetRenderer;
	[SerializeField] BoosterID boosterID;
	[SerializeField] BoosterToSpriteMapper spriteMapper;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		var sprite = spriteMapper.GetSprite(boosterID);
		if(sprite != null) {
			targetRenderer.sprite = sprite;
		}
	}
	#endregion// Init config
}
