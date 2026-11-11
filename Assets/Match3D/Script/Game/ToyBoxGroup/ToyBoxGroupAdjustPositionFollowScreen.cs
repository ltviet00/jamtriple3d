using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Adjust main camera view size and position of toyBoxGroup to make sure the toyBox within the display area </summary>
public class ToyBoxGroupAdjustPositionFollowScreen : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] CameraVariable mainCamVar;
	[SerializeField] TransformVariable footerUiLeftBottomVar;
	[SerializeField] TransformVariable footerUiRightBottomVar;

	[Header("Anchor ref")]
	[SerializeField] Transform mainTransform;
	[SerializeField] Transform anchorLeftTop;
	[SerializeField] Transform anchorRightBottom;

	[Header("Config margin")]
	[SerializeField] float marginFromScreenSide;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Call from main flow to adjust camera and toybox position. Async cause  </summary>
	public async UniTask AdjustCameraViewAndToyBoxPosition() {
		if(isFailedConfig)
			return;
		// let alway make toybox at center at camera (same Xpos)
		var cam = mainCamVar.Value;
		var toyBoxPos = mainTransform.position;
		toyBoxPos.x = cam.transform.position.x;
		mainTransform.position = toyBoxPos;

		// check if cam's view already cover toybox
		var camViewWidth = GetCameraViewWidth();
		var minCamViewRequirement = GetMinCameraOthorSizeToFitToyBox();
		if(camViewWidth < minCamViewRequirement) {
			AdjustCameraOrthorSizeToFitWidth(minCamViewRequirement);
			// wait 7 frames for UI to change after resize camera's view.
			// Dont know why need exactly 7 frame to wait for canvas to finish adjust position of anchor point. Maybe relate to the hirearchy of canvas
			//for(int n = 0; n < 7; n++) {
			//	await UniTask.Yield(cancellationToken: _cancelToken.Token);
			//}
			await UniTask.Delay(300, cancellationToken: _cancelToken.Token);
		}

		// camera view ok now, now adjust Ypos of the toybox to equal to the footer.
		// distance from mainTransform to UIFooter = distance from it to anchor point
		AdjustToyBoxPosMarginToFooterUI();
	}
	#endregion//Public


	#region Private
	float GetCameraViewWidth() {
		var cam = mainCamVar.Value;
		if(cam == null) {
			Log.Error(false, this, $"Missing main camera");
			return 0f;
		}
		return cam.orthographicSize * cam.aspect * 2f;
	}

	float GetFooterDisplayWidth() {
		if(footerUiLeftBottomVar.Value == null || footerUiRightBottomVar.Value == null) {
			Log.Error(false, this, $"NUll footer UI left/right");
			return 0;
		}
		var result = footerUiRightBottomVar.Value.position.x - footerUiLeftBottomVar.Value.position.x;
		return Mathf.Abs(result);
	}

	float GetMinCameraOthorSizeToFitToyBox() {
		//var footerDisplayWidth = gettoybox();
		var toyBoxDisplayWidth = GetToyBoxViewSizeWidth();
		return toyBoxDisplayWidth + 2f * marginFromScreenSide;
	}

	void AdjustCameraOrthorSizeToFitWidth(float width) {
		var cam = mainCamVar.Value;
		if(cam == null) {
			Log.Error(false, this, $"Missing main camera");
			return;
		}
		var targetOrthorSize = (width / cam.aspect) * 0.5f;
		cam.orthographicSize = targetOrthorSize;
	}

	void AdjustToyBoxPosMarginToFooterUI() {
		var distanceFromCenterToAnchor = mainTransform.position.y - anchorRightBottom.position.y;
		var targetYpos = footerUiRightBottomVar.Value.position.y + distanceFromCenterToAnchor;

		var resultPos = mainTransform.position;
		resultPos.y = targetYpos;
		mainTransform.position = resultPos;
	}

	float GetToyBoxViewSizeWidth() {
		var anchorDistance = Mathf.Abs(anchorRightBottom.position.x - anchorLeftTop.position.x);
		return anchorDistance;
	}

	UniTaskTokenHelper _cancelToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_cancelToken.ClearRunningTasks();
	}
	#endregion//Private
}
