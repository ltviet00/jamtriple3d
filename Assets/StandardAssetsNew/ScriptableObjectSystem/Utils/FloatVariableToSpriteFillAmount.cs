using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Follow changes of FLoatVariable, apply to width/height of a Sprite (with renderMode = tile).
/// Config the sprite at SLice render, then make it full width/heigh (like at 100%), this component will scale it follow valueToFollowVar </summary>
public class FloatVariableToSpriteFillAmount : MonoBehaviour {
	#region Init, config
	[Header("Refs, data")]
	[SerializeField] SpriteRenderer targetSprite;
	[SerializeField] FloatVariable valueToFollowVar;

	[Header("Config")]
	[SerializeField] Dimension selectDimension;
	[SerializeField] AnchorType anchorType;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponent();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		valueToFollowVar.OnValueChangedHandler += OnValueChanged;
		// force sprite render as Slice mode
		targetSprite.drawMode = SpriteDrawMode.Sliced;
		// save some default values
		_defaultSize = targetSprite.size;
		_defaultLocalPosition = targetSprite.transform.localPosition;
		// first changes
		OnValueChanged();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		valueToFollowVar.OnValueChangedHandler -= OnValueChanged;
	}

	void CacheComponent() {
		if(targetSprite == null) {
			targetSprite = GetComponent<SpriteRenderer>();
			if(targetSprite != null) {
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif//UNITY_EDITOR
			}
		}
	}
	#endregion// Init config


	#region Private
	///<summary> Default size of sprite at OnEnable(), will used as Max-size </summary>
	Vector2 _defaultSize;
	Vector3 _defaultLocalPosition;

	void OnValueChanged() {
		// clamp to 1 cause image fillAmount is from 0-1
		var clamped = Mathf.Clamp01(valueToFollowVar.Value);
		// size for sprite
		var targetSize = _defaultSize;
		switch(selectDimension) {
			case Dimension.Width:
				targetSize.x *= clamped;
				targetSprite.size = targetSize;
				AdjustPositionForHorizontal();
				break;

			case Dimension.Height:
				targetSize.y *= clamped;
				targetSprite.size = targetSize;
				AdjustPositionForVertical();
				break;

			default:
				Log.Error(false, this, $"Not handle for Dimension type : [{selectDimension}]");
				break;
		}
	}

	void AdjustPositionForHorizontal() {
		var targetLocalPos = _defaultLocalPosition;
		var distanceToMove = (_defaultSize.x - targetSprite.size.x) * 0.5f;
		switch(anchorType) {
			case AnchorType.Left:
				targetLocalPos.x -= distanceToMove;
				break;

			case AnchorType.Right:
				targetLocalPos.x += distanceToMove;
				break;

			case AnchorType.Center:
				// nothing for center
				break;

			default:
				Log.Warning(false, this, $"Not handle for anchor type [{anchorType}] in horizontal");
				break;
		}
		targetSprite.transform.localPosition = targetLocalPos;
	}

	void AdjustPositionForVertical() {
		var targetLocalPos = _defaultLocalPosition;
		var distanceToMove = (_defaultSize.y - targetSprite.size.y) * 0.5f;
		switch(anchorType) {
			case AnchorType.Top:
				targetLocalPos.y -= distanceToMove;
				break;

			case AnchorType.Bottom:
				targetLocalPos.y += distanceToMove;
				break;

			case AnchorType.Center:
				// nothing for center
				break;

			default:
				Log.Warning(false, this, $"Not handle for anchor type [{anchorType}] in vertical");
				break;
		}
		targetSprite.transform.localPosition = targetLocalPos;
	}
	#endregion//Private


	#region Classes
	public enum Dimension {
		Width, Height
	}

	public enum AnchorType {
		Center, Left, Right, Top, Bottom,
	}
	#endregion//Classes
}
