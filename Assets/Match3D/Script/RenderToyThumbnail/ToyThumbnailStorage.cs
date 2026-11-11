using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Storage thumbnail for all toy, function to query
/// </summary>
[CreateAssetMenu(fileName = "ToyThumbnailStorage", menuName = "Match3D/Toys/ToyThumbnailStorage")]
public class ToyThumbnailStorage : ScriptableObject {
	public ToyThumbnailMap thumbnailMap;

	public Sprite GetThumbnail(GameObject toyPrefab) {
		if(thumbnailMap.TryGetValue(toyPrefab, out var sprite)) {
			return sprite;
		}
		Log.Warning(false, this, $"Not found sprite for prefab [{toyPrefab}]");
		return null;
	}

	#region Classes
	[System.Serializable]
	public class ToyThumbnailMap : UnitySerializedDictionary<GameObject, Sprite> { }
	#endregion//Classes
}
