using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeCursorForRecording : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	[SerializeField] bool dontDestroy = false;
	[SerializeField] Vector3 marginToCamera;

	[Header("Config - custom camera")]
	[SerializeField] bool useCustomCamera = false;
	[SerializeField] Camera customCamera;

	[Header("Validation")]
	[SerializeField] string[] ignoreList;
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields(ignoreList);
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}

	void Start() {
		if(dontDestroy) {
			DontDestroyOnLoad(this.gameObject);
		}
		// call cache one to cache the main camera
		var mainCam = MainCam;
	}
	#endregion// Init config


	#region Private
	Camera _cacheMainCam;
	Camera MainCam {
		get {
			if(_cacheMainCam == null) {
				_cacheMainCam = Camera.main;
			}
			return _cacheMainCam;
		}
	}

	void Update() {
		var mousePosOnScreen = Input.mousePosition;
		var toWorldPos = GetCamToFollow().ScreenToWorldPoint(mousePosOnScreen);
		// add a margin so it will infront of the camera
		toWorldPos += marginToCamera;
		transform.position = toWorldPos;
	}

	Camera GetCamToFollow() {
		if(useCustomCamera && customCamera != null) {
			return customCamera;
		}
		else {
			return MainCam;
		}
	}
	#endregion//Private
}
