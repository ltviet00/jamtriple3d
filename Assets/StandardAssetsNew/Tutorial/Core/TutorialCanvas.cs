using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Work with Tutorial manager. The highest priority canvas, to show tutorial instruction ...
/// </summary>
public class TutorialCanvas : MonoSingleton<TutorialCanvas> {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Camera renderCamera;
	[SerializeField] Canvas mainCanvas;
	[SerializeField] Image blackCoverImage;

	[Header("Refs - for action")]
	///<summary> All hight light item will be place under this </summary>
	[SerializeField] Transform rootHighlightItems;
	///<summary> Enable + listen to this button to execute skip function </summary>
	[SerializeField] NoGraphicsButton skipButton;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	protected override void DoOnStart() {
		base.DoOnStart();
		// default disable the canvas
		SetActiveRender(false);
		// disable skip button
		skipButton.gameObject.SetActive(false);
	}
	#endregion// Init config


	#region Public
	///<summary> All hight light item will be place under this </summary>
	public Transform RootHighlightItem => rootHighlightItems;
	///<summary> Enable + listen to this button to execute skip function </summary>
	public NoGraphicsButton SkipButton => skipButton;
	/////<summary> Some action need to control this blackCover image </summary>
	public Image BlackCoverImage => blackCoverImage;

	public void SetActiveRender(bool active) {
		renderCamera.enabled = active;
		mainCanvas.enabled = active;
		if(active) { AddRenderCemeraToStackOfCurrentBaseCamera(); }
		else { RemoveRenderCameraFromCurrentStackOnBaseCamera(); }
	}

	///<summary> Find object with same name on the rootHightlight item.
	/// When we highligh object, its parent change (to rootHightligh), so path change. Use this to try find object in TutorialCanvas.
	/// Return null if not found </summary>
	public Transform FindHighlightObject(string name) {
		var childCount = rootHighlightItems.childCount;
		for(int n = 0; n < childCount; n++) {
			var child = rootHighlightItems.GetChild(n);
			if(child.name == name)
				return child;
		}
		// reach here mean not found
		return null;
	}

	///<summary> RectTransform in other canvas will has different setting (anchor, anchorPosition ...), to let them got same 
	/// relative position on screen when move to Tutorial canvas, we need some furthet setup </summary>
	public void BringObjectToCanvasAndKeepRelativePostion(RectTransform rectTrans) {
		if(isFailedConfig)
			return;
		// algorithm is : find the relative position of the rect to the camera that render its canvas
		// then bring it to the tutorial canvas, keep the same relative distance to the tut canvas camera

		// look up for the canvas
		var canvas = rectTrans.GetComponentInParent<Canvas>();
		var canvasCamera = canvas.worldCamera;
		if(canvasCamera == null) {
			Log.Warning(false, this, $"Work best with ScreenSpace - camera canvas only. Now just change parent for object, might got some offset");
			rectTrans.SetParent(rootHighlightItems, false);
			return;
		}
		// calculate relative position to the its camera (in world pos)
		var offsetToCamera = canvasCamera.transform.position - rectTrans.position;
		// camera of it canvas and tut canvas got different, do a scale (will right if both camera are Othorgraphic)
		var ratio = renderCamera.orthographicSize / canvasCamera.orthographicSize;
		var newPos = renderCamera.transform.position - offsetToCamera * ratio;

		// keep the screen position (x, y) but put it on the tutorial canvas plane:
		// the kept camera distance (10) equals the tutorial camera far clip (10), so the object was clipped (invisible) by float error
		newPos.z = rootHighlightItems.position.z;

		// move it to tutorial canvas
		rectTrans.SetParent(rootHighlightItems, false);
		rectTrans.position = newPos;
	}
	#endregion//Public



	#region Utils - URP camera
	void AddRenderCemeraToStackOfCurrentBaseCamera() {
		// let this camera overlay to current mainCamera
		var data = renderCamera.GetUniversalAdditionalCameraData();
		data.renderType = CameraRenderType.Overlay;
		// add to mainCamera
		var mainCam = Camera.main;
		var mainCamData = mainCam.GetUniversalAdditionalCameraData();
		if(mainCamData == null) {
			Log.Warning(false, this, $"Not found UniversalAdditionalCameraData data in MainCamera");
			return;
		}
		// check exist
		var camStack = mainCamData.cameraStack;
		if(camStack.Contains(renderCamera)) {
			Log.Warning(false, this, $"Already contain renderCamera in baseStack list");
			return;
		}
		camStack.Add(renderCamera);
	}

	void RemoveRenderCameraFromCurrentStackOnBaseCamera() {
		var mainCam = Camera.main;
		// when scene destroy, then we got null pointer here
		if(mainCam == null) {
			return;
		}

		var mainCamData = mainCam.GetUniversalAdditionalCameraData();
		if(mainCamData == null) {
			Log.Warning(false, this, $"Not found UniversalAdditionalCameraData data in MainCamera");
			return;
		}
		// check exist
		var camStack = mainCamData.cameraStack;
		if(!camStack.Contains(renderCamera)) {
			Log.Warning(false, this, $"Not contain renderCamera in baseStack list to remove");
			return;
		}
		camStack.Remove(renderCamera);
	}
	#endregion//Utils - URP camera
}
