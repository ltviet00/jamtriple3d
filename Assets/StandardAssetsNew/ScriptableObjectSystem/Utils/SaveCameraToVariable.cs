using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveCameraToVariable : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Camera targetCamera;
	[SerializeField] CameraVariable cameraVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
#if UNITY_EDITOR
		if(targetCamera == null) {
			targetCamera = GetComponent<Camera>();
			if(targetCamera != null) {
				UnityEditor.EditorUtility.SetDirty(this);
			}
		}
#endif
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		cameraVar.Value = targetCamera;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		// carefully check reset if value is same our
		if(cameraVar.Value != null && cameraVar.Value == targetCamera) {
			cameraVar.Value = null;
		}
	}
#endregion// Init config
}
