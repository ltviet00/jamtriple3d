using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	#endregion// Init config
}
