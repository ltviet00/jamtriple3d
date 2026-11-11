using UnityEngine;
using System.Collections;

public static class GameObjectUtils {
	public static T GetOrAddComponent<T> (this GameObject obj) where T : Component {
		var result = obj.GetComponent<T>();
		if (result == null) result = obj.AddComponent<T>();
		return result;
	}

	public static T GetOrAddComponent<T>(this Component com) where T : Component {
		var result = com.gameObject.GetComponent<T>();
		if(result == null)
			result = com.gameObject.AddComponent<T>();
		return result;
	}

	public static void RemoveComponent<T>(this Component com) where T : Component {
		var addedComponent = com.gameObject.GetComponent<T>();
		if(addedComponent != null)
			GameObject.Destroy(addedComponent);
	}

	/// <summary>
	/// gameObject.layer is in range [0..31], this func will convert a LayerMask to
	/// an int value in range [0..31], so we can use the result to change object's layer
	/// </summary>
	public static int FromLayerMaskToLayerIndexForGameObject(LayerMask layerMask) {
		if(layerMask.value == 0) {
			// config mask is set to "Nothing", no layer was selected
			return 0;
		}
		int result = 1;
		var mask = layerMask;
		// shift it to the right, until we meet the first "1" bit
		while(true) {
			mask = mask >> 1;
			if((mask & 1) == 1)
				break;
			else
				result++;
		}
		return result;
	}
}

