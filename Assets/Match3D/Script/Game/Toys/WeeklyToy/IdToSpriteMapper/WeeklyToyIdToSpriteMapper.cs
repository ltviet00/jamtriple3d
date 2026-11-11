using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Map from weeklyEventToyID to sprite </summary>
[CreateAssetMenu(fileName = "WeeklyToyIdToSpriteMapper", menuName = "Game/WeeklyToyIdToSpriteMapper")]
public class WeeklyToyIdToSpriteMapper : ScriptableObject {
	#region Init, config
	[Header("Config")]
	[SerializeField] List<ToyAndSprite> data;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> Turn the list into map for faster retrieving </summary>
	Dictionary<WeeklyToyID, Sprite> _spriteMap = new Dictionary<WeeklyToyID, Sprite>();
	Dictionary<WeeklyToyID, Sprite> SpriteMap {
		get {
			if(_spriteMap.Count == 0) {
				for(int n = 0; n < data.Count; n++) {
					var item = data[n];
					_spriteMap.Add(item.toyID, item.sprite);
				}
			}
			return _spriteMap;
		}
	}

	public Sprite GetSprite(WeeklyToyID toyID) {
		if(isFailedConfig)
			return null;
		if(SpriteMap.ContainsKey(toyID)) {
			return SpriteMap[toyID];
		}
		Log.Error(false, this, $"Not found sprite for toyID [{toyID.name}]");
		return null;
	}
	#endregion//Public


	#region Classes
	[System.Serializable]
	public class ToyAndSprite {
		public WeeklyToyID toyID;
		public Sprite sprite;
	}
	#endregion//Classes
}
