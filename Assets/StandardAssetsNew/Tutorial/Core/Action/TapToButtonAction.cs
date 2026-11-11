using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wait for user to tap to define Button
/// </summary>
[CreateAssetMenu(fileName = "TapToButtonAction", menuName = "Tutorial/Action/TapToButtonAction")]
public class TapToButtonAction : TutorialActionBase {
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
		var objectAtPath = TutorialUtils.FindObjectByFullPathAndInTutorialCanvas(objectPath);
		if(objectAtPath == null) {
			return false;
		}
		var buttonComponent = objectAtPath.GetComponent<Button>();
		if(buttonComponent == null) {
			Log.Warning(false, objectAtPath, $"Not found Button component for object [{objectPath}]");
			return false;
		}
		// wait for button click
		var buttonClickAwaiter = new ButtonClickAwaiter();
		await buttonClickAwaiter.Start(buttonComponent);
		return true;
	}
	#endregion//Public
}
