using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Show the hand on the same position to target object.
/// Config offset, animation prefab ....
/// </summary>
[CreateAssetMenu(fileName = "ShowHandOnUIObjectAction", menuName = "Tutorial/Action/ShowHandOnUIObjectAction")]
public class ShowHandOnUIObjectAction : TutorialActionBase {
	#region Init, config
	[Header("Data")]
	[SerializeField] string path;
	[SerializeField] Vector2 offset;
	[SerializeField] GameObject handPrefab;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region override
	///<summary> Ref to the hand object, will destroy in Dispose </summary>
	GameObject _handObject;

	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		var target = TutorialUtils.FindObjectByFullPathAndInTutorialCanvas(path);
		if(target == null) {
			return false;
		}
		// spawn hand object, put it on the canvas
		var rootHighlight = TutorialCanvas.Instance.RootHighlightItem;
		_handObject = Instantiate(handPrefab, rootHighlight);
		// position for it
		var targetWorldPos = target.position;
		targetWorldPos.z = rootHighlight.position.z;
		_handObject.transform.position = targetWorldPos;
		// offset
		var toRectTrans = _handObject.transform as RectTransform;
		var anchorPos = toRectTrans.anchoredPosition;
		var finalPos = anchorPos += offset;
		toRectTrans.anchoredPosition = finalPos;
		return true;
	}

	public override void Dispose() {
		if(_handObject != null) {
			Destroy(_handObject);
		}
	}
	#endregion//override
}
