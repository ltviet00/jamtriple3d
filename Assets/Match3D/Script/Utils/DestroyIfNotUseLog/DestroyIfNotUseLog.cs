using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyIfNotUseLog : MonoBehaviour {
	void Start() {
#if !USE_LOG
		Destroy(gameObject);
#endif//USE_LOG
	}
}
