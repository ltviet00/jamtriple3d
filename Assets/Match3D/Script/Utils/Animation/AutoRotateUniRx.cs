using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class AutoRotateUniRx : MonoBehaviour {
	public Vector3 rotateSpeed;

	void OnEnable() {
		ClearCoroutine();
		_corountine = Observable.EveryUpdate().Subscribe(_ => WorkEveryFrame());
	}

	void OnDisable() {
		ClearCoroutine();
	}

	IDisposable _corountine;
	void ClearCoroutine() {
		_corountine?.Dispose();
		_corountine = null;
	}

	void WorkEveryFrame() {
		var rotate = transform.localEulerAngles;
		rotate += (Time.deltaTime * rotateSpeed);
		transform.localEulerAngles = rotate;
	}
}
