using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Highlight button and wait for user to click on it.
/// Funfact: when we highlight button only, it will change the button path.
/// and if then we use TapTobutton action, the path was changed, can't find button anymore
/// </summary>
[CreateAssetMenu(fileName = "HighlightAndTapToButtonAction", menuName = "Tutorial/Action/HighlightAndTapToButtonAction")]
public class HighlightAndTapToButtonAction : TutorialActionBase {
	#region Init, config
	[Header("Config")]
	[SerializeField] string objectPath;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		var objectAtPath = TutorialUtils.FindObjectByFullPathAndInTutorialCanvas(objectPath);
		if(objectAtPath == null) {
			return false;
		}
		var buttonComponent = objectAtPath.GetComponent<Button>();
		if(buttonComponent == null) {
			Log.Warning(false, objectAtPath, $"Not found Button component for object [{objectPath}]");
			return false;
		}

		//back up
		var backupTransform = new HighlightObjectAction.BackupTransformnInfo();
		backupTransform.Backup(objectAtPath);
		// highlight button by bring it to Tutorial canvas call to TutorialCanvas 
		var tutorialCanvas = TutorialCanvas.Instance;
		tutorialCanvas.SetActiveRender(true);
		var rectTrans = objectAtPath as RectTransform;
		tutorialCanvas.BringObjectToCanvasAndKeepRelativePostion(rectTrans);

		// wait for button click
		var buttonClickAwaiter = new ButtonClickAwaiter();
		await buttonClickAwaiter.Start(buttonComponent);
		// user click to button, bring it back. deactive the tut canvas
		backupTransform.Restore();
		tutorialCanvas.SetActiveRender(false);

		return true;
	}
	#endregion//Public
}
