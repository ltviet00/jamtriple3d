using UnityEngine;
using System.Collections;

/// <summary>
/// Have some function to let you spawn object using ObjectPool.
/// Use: often attach to GameObject, use some unityEvent to invoke this component to spawn something.
/// By default, it will spawn object at the center position of the transform
/// Read public functions for more details.
/// </summary>
public class SpawnObjectUsingPoolRequester : MonoBehaviour {
	public void IE_Spawn(GameObject prefab) {
		if(!ObjectPool.HasInstance()) {
			Log.Warning(false, this, "ObjectPool not exist, cannot spawn [{0}]", prefab.name);
		}
		var obj = ObjectPool.SpawnObject(prefab);
		obj.transform.position = transform.position;
	}
}
