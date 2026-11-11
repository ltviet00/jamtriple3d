using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyIfNotUseCheat : MonoBehaviour {
	void Start() {
#if !CHEAT
		Destroy(gameObject);
#endif//USE_LOG
	}
}
