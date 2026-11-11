using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Retrieve the previous highlight action, and revert it, use as RemoveHighlight
/// </summary>
[CreateAssetMenu(fileName = "RemovePreviousHighlightAction", menuName = "Tutorial/Action/RemovePreviousHighlightAction")]
public class RemovePreviousHighlightAction : TutorialActionBase {
	#region Init, config
	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public override async UniTask<bool> Execute() {
		var actionList = parentTask.actions;
		bool foundAnyAction = false;
		// find previous highligh action 
		for(int n = 0; n < actionList.Count; n++) {
			var action = actionList[n];
			// reach itself?
			if(action == this) {
				Log.Warning(false, this, $"Task [{parentTask.name}], not found previous Highlight action to undo");
				return false;
			}
			// look for highligh action only
			if(action is HighlightObjectAction highlightAction) {
				highlightAction.RemoveCurrentHighlightObject();
				foundAnyAction = true;
				// not return, we might got more than one highligh action
			}
		}
		return foundAnyAction;
	}
	#endregion//Public
}
