using UnityEngine;
using System.Collections;

/// <summary>
/// Audo deactive GameObject after x second from OnEnable()
/// </summary>
public class AutoDeactiveObjectHelper : MonoBehaviour {
	[SerializeField] float lifeTime = 1f;
	// field
	float _count;

	void OnEnable () {
		_count = lifeTime;
	}


	void Update () {
		_count -= Time.deltaTime;
		if (_count < 0) {
			_count = 0;
			gameObject.SetActive(false);
		}
	}
}
