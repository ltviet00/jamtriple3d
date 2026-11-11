using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Define task (in SO), include many actions
/// </summary>
[CreateAssetMenu(fileName = "TutorialTask", menuName = "Tutorial/TutorialTask")]
public class TutorialTask : ScriptableObject {
	#region Init, config
	[Header("Config")]
	public string id;
	public StringVariable location;
	public int priority;

	[Header("Conditino & Action list")]
	public List<TutorialConditionBase> conditions;
	public List<TutorialActionBase> actions;

	[Header("Validation")]
	public bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> Check if this task passed all conditions, ready to be execute </summary>
	public bool IsPassAllConditions() {
		if(isFailedConfig)
			return false;
		for(int n = 0; n < conditions.Count; n++) {
			// return if failed at any condition
			if(!conditions[n].IsPass())
				return false;
		}
		// reach here mean no failed condition
		return true;
	}

	///<summary> Run the Action list, one by one. Return true if execute success all actions </summary>
	public async UniTask<bool> Run() {
		// start action list
		for(int n = 0; n < actions.Count; n++) {
			var action = actions[n];
			// set ref for action, some action need the parent ref (eg: UnHighlight object, need this to retrieve the ActionList)
			action.parentTask = this;
			var result = await action.Execute();
			// dispose action in any case
			CheckDisposeAction(action);
			if(!result) {
				Log.Info(this, $"Task [{name}], failed at action [{action.name}]");
				return false;
			}
		}

		// case: exit game, close editor during the task, TutorialCanvas got destroy
		if(!TutorialCanvas.HasInstance()) {
			return false;
		}
		// done all task. Extra: done all task, then disable tutorial canvasn as well
		var tutorialCanvas = TutorialCanvas.Instance;
		tutorialCanvas.SetActiveRender(false);
		return true;
	}
	#endregion//Public


	#region Private
	void CheckDisposeAction(TutorialActionBase action) {
		// auto dispose
		if(action.disposeOnFinish) {
			action.Dispose();
		}
		// check dispose previous task
		if(action.disposePreviousActions) {
			for(int n = 0; n < actions.Count; n++) {
				var act = actions[n];
				// reach target, then break
				if(act == action) {
					break;
				}
				act.Dispose();
			}
		}
	}
	#endregion//Private


#if UNITY_EDITOR
	[Button]
	void LoadAllActionAndConditionInfolder() {
		actions.Clear();
		conditions.Clear();
		var myPath = AssetDatabase.GetAssetPath(this);
		var folderPath = myPath.Substring(0, myPath.LastIndexOf('/'));

		//------- get all Action in folder
		var allActionDataGUIDInFolder = AssetDatabase.FindAssets("t:TutorialActionBase", new string[] { folderPath });
		if(allActionDataGUIDInFolder.Length == 0) {
			Log.Error(false, this, $"Found no TutorialActionBase in path [{folderPath}]");
			return;
		}
		// from guid back to Actino, retrieve ToyView component
		for(int n = 0; n < allActionDataGUIDInFolder.Length; n++) {
			var guid = allActionDataGUIDInFolder[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var actionData = AssetDatabase.LoadAssetAtPath<TutorialActionBase>(toPath);
			actions.Add(actionData);
		}

		//------- get all Condtion in folder
		var allConditionDataGUIDInFolder = AssetDatabase.FindAssets("t:TutorialConditionBase", new string[] { folderPath });
		if(allActionDataGUIDInFolder.Length == 0) {
			// no need warning, conditions are ok to empty
			//Log.Error(false, this, $"Found no TutorialActionBase in path [{folderPath}]");
			return;
		}
		// from guid back to Action, retrieve ToyView component
		for(int n = 0; n < allConditionDataGUIDInFolder.Length; n++) {
			var guid = allConditionDataGUIDInFolder[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var conditionData = AssetDatabase.LoadAssetAtPath<TutorialConditionBase>(toPath);
			conditions.Add(conditionData);
		}
		// dirty so it can be save
		EditorUtility.SetDirty(this);
	}
#endif//UNITY_EDITOR
}
