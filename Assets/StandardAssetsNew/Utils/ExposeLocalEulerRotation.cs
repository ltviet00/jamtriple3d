using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Just a test component to see value in the Unity rotation field of Transform component.
/// </summary>
public class ExposeLocalEulerRotation : MonoBehaviour {
	[SerializeField] Vector3 localEulerAngleToApplyTo;
	void OnValidate() {
		transform.localEulerAngles = localEulerAngleToApplyTo;
	}
}
