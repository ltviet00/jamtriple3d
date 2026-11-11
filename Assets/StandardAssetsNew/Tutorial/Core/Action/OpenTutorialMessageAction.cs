using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn Tutorial message popup on TutorialCanvas, config the text
/// </summary>
[CreateAssetMenu(fileName = "OpenTutorialMessageAction", menuName = "Tutorial/Action/OpenTutorialMessageAction")]
public class OpenTutorialMessageAction : TutorialActionBase {
	#region Init, config
	[Header("Config, prefab")]
	[SerializeField] string message;
	[SerializeField] GameObject popupPrefab;
	[SerializeField] Vector2 popupAnchorPosition;

	[Header("Active black screen")]
	[SerializeField] bool activeBlackCoverScreen = true;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> Refs to the current message popup </summary>
	TutorialMessagePopup _messagePopup;

	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		// simple spawn popup on Tutorial canvas, config content and position
		if(!TutorialCanvas.HasInstance()) {
			Log.Warning(false, this, $"TutorialCanvas not exist !!");
			return false;
		}
		// remember to active tutorial canvas.
		var tutorialCanvas = TutorialCanvas.Instance;
		tutorialCanvas.SetActiveRender(true);
		// active black screen? for some tut we not cover screen, let user input on that
		tutorialCanvas.BlackCoverImage.enabled = activeBlackCoverScreen;

		var popup = Instantiate(popupPrefab, tutorialCanvas.RootHighlightItem);
		_messagePopup = popup.GetComponent<TutorialMessagePopup>();
		if(_messagePopup == null) {
			Log.Error(false, popupPrefab, $"Not found TutorialMessagePopup component in prefab [{popupPrefab.name}]");
			return false;
		}
		// message, position
		_messagePopup.SetMessage(message);
		var rectTrans = _messagePopup.transform as RectTransform;
		rectTrans.anchoredPosition = popupAnchorPosition;
		return true;
	}

	public override void Dispose() {
		if(_messagePopup != null) {
			Destroy(_messagePopup.gameObject);
			_messagePopup = null;
		}
	}
	#endregion//Public
}
