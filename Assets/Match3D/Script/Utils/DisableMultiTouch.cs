using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableMultiTouch : MonoBehaviour {
	void Start() {
		Input.multiTouchEnabled = false;
	}
}
