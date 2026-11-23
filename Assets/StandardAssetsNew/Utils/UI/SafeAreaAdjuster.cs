using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///<summary> Attach to a RectTransform, NOTES: let the RectTransform to fully stretch (anchor min = (0,0), anchorMax = (1,1),
/// and all distance on 4 direction are 0 (top = bottom = right = left = 0).
///This component will adjust the anchorMin, max of the Rect transform to let it fit the Screen.safeArea on the mobile device
///that has notch or not full screen</summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdjuster : MonoBehaviour {
	#region Init, config
	[Header("Target Rect")]
	[SerializeField] RectTransform targetRectTrans;

	[Header("Ignore")]
	[SerializeField] bool ignoreTop = false;
	[SerializeField] bool ignoreBottom = false;
	[SerializeField] bool ignoreLeft = false;
	[SerializeField] bool ignoreRight = false;

	[Header("Ads")]
	[Tooltip("Keep BannerSpace.BottomPixels free at the bottom (gameplay screens with an anchored banner)")]
	[SerializeField] bool reserveBottomForBanner = false;
	[Tooltip("Free space (canvas units) the layout already has under its bottom content (e.g. Footer offset). " +
		"Only the part of the banner taller than this pushes the content up")]
	[SerializeField] float bannerFreeBottomUnits = 0f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		if(targetRectTrans == null) {
			targetRectTrans = GetComponent<RectTransform>();
		}
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void Awake() {
		if(isFailedConfig)
			return;
		// safeArea is a Rect that already ignore top&bottom avoid-display-parts on mobile device
		// so let simply take the bottom-left point as anchorMin, and top-right point as anchorMax
		var safeArea = Screen.safeArea;
		// check ignore option here too
		safeArea.xMin = ignoreLeft ? 0f : safeArea.xMin;
		safeArea.xMax = ignoreRight ? Screen.width : safeArea.xMax;
		safeArea.yMin = ignoreBottom ? 0f : safeArea.yMin;
		safeArea.yMax = ignoreTop ? Screen.height : safeArea.yMax;
		if(reserveBottomForBanner && BannerSpace.BottomPixels > 0f) {
			// anchored banner sits right above the bottom safe inset; reuse the free space the layout already has
			var push = Mathf.Max(0f, BannerSpace.BottomPixels - bannerFreeBottomUnits * GetCanvasScale());
			safeArea.yMin = Mathf.Min(safeArea.yMin + push, safeArea.yMax);
		}

		var anchorMin = safeArea.position;
		var anchorMax = anchorMin + safeArea.size;
		// to asign to the anchorMin/Max of the RectTransform, we need to normalized them, so devide by Screen size
		anchorMin.x /= Screen.width;
		anchorMax.x /= Screen.width;
		anchorMin.y /= Screen.height;
		anchorMax.y /= Screen.height;
		// assign back to the RectTrans
		targetRectTrans.anchorMin = anchorMin;
		targetRectTrans.anchorMax = anchorMax;
	}

	///<summary> Canvas units -> screen pixels. Computed from the CanvasScaler settings, because
	/// Canvas.scaleFactor may not be updated yet during Awake </summary>
	float GetCanvasScale() {
		var scaler = GetComponentInParent<CanvasScaler>();
		if(scaler == null) {
			var canvas = GetComponentInParent<Canvas>();
			return canvas != null ? canvas.scaleFactor : 1f;
		}
		if(scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
			return scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize ? scaler.scaleFactor : 1f;
		var rx = Screen.width / scaler.referenceResolution.x;
		var ry = Screen.height / scaler.referenceResolution.y;
		switch(scaler.screenMatchMode) {
			case CanvasScaler.ScreenMatchMode.Expand:
				return Mathf.Min(rx, ry);
			case CanvasScaler.ScreenMatchMode.Shrink:
				return Mathf.Max(rx, ry);
			default:
				return Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(rx, 2f), Mathf.Log(ry, 2f), scaler.matchWidthOrHeight));
		}
	}
	#endregion// Init config
}
