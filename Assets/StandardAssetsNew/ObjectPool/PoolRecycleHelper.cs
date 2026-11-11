using UnityEngine;
using System.Collections;

/// <summary>
/// Dont add this component to any prefab, It'll cause OnDisable() be invoke when
/// ObjectPool extend the Pool (it'll instantiate and disable the object),
/// let ObjectPool auto add this commponent to prefab when extending the pool. 
/// By default, this will make gameobject back to pool each time this gameObject was disabled.
/// </summary>
public class PoolRecycleHelper : MonoBehaviour {
	/// <summary>
	/// Use it on ObjectPool.Recycle(). This var will be set from ObjectPool
	/// when cloning a prefab and push to Pool stacks </summary>
	public GameObject keyPrefabWasUsedToClone;

	#region Private
	void OnDisable() {
		BackToPool();
	}

	void BackToPool() {
		if(keyPrefabWasUsedToClone == null) {
			Log.Warning(false, this, "Cannot turn back to pool, missing keyPrefab");
			return;
		}
		ObjectPool.RecyleObject(keyPrefabWasUsedToClone, gameObject);
	}
	#endregion
}