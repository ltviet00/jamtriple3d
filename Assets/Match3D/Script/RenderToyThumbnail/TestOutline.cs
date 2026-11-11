using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestOutline : MonoBehaviour {
	[SerializeField] GameObject prefab;
	[SerializeField] float thick = 10f;

	GameObject _instance;
	[Button]
	void SpawnObject() {
		if(_instance != null) {
			Destroy(_instance);
		}
		_instance = Instantiate(prefab, transform);
		_instance.transform.localPosition = Vector3.zero;
		_instance.GetComponent<Rigidbody>().isKinematic = true;
	}

	[Button]
	void AddOutline() {
		if(_instance != null) {
			var outline = _instance.AddComponent<Outline>();
			outline.OutlineWidth = thick;
		}
	}
}
