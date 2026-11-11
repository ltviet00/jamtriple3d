using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Map from boosterID to sprite </summary>
[CreateAssetMenu(fileName = "BoosterToSpriteMapper", menuName = "Game/Booster/BoosterToSpriteMapper")]
public class BoosterToSpriteMapper : ScriptableObject {
	#region Init, config
	[Header("Config")]
	[SerializeField] List<BoosterAndSprite> data;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> Turn the list into map for faster retrieving </summary>
	Dictionary<BoosterID, Sprite> _spriteMap = new Dictionary<BoosterID, Sprite>();
	Dictionary<BoosterID, Sprite> SpriteMap {
		get {
			if(_spriteMap.Count == 0) {
				for(int n = 0; n < data.Count; n++) {
					var item = data[n];
					_spriteMap.Add(item.boosterID, item.sprite);
				}
			}
			return _spriteMap;
		}
	}

	public Sprite GetSprite(BoosterID boosterID) {
		if(SpriteMap.ContainsKey(boosterID)) {
			return SpriteMap[boosterID];
		}
		Log.Error(false, this, $"Not found sprite for boosterID [{boosterID.name}]");
		return null;
	}
	#endregion//Public


	#region Classes
	[System.Serializable]
	public class BoosterAndSprite {
		public BoosterID boosterID;
		public Sprite sprite;
	}
	#endregion//Classes
}
