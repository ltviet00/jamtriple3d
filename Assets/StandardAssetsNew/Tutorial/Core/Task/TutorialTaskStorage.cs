using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SO, contain list to all task (in SO as well) assets, used for querying later
/// </summary>
[CreateAssetMenu(fileName = "TutorialTaskStorage", menuName = "Tutorial/TutorialTaskStorage")]
public class TutorialTaskStorage : ScriptableObject {
	#region Init, config
	[Header("Data")]
	public List<TutorialTask> tasks;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> Find task by ID. Notes: should check ValidateTask to make sure no duplicate ID task. This function won't check duplicate again </summary>
	public TutorialTask GetTaskByID(string id) {
		for(int n = 0; n < tasks.Count; n++) {
			var task = tasks[n];
			if(task.id == id)
				return task;
		}
		Log.Error(false, this, $"Not found task with ID [{id}]");
		return null;
	}
	#endregion//Public


#if UNITY_EDITOR
	[Button]
	void LoadAllTaskInSubfolder() {
		tasks.Clear();
		// get all LevelData in the toy folder
		var myPath = AssetDatabase.GetAssetPath(this);
		var folderPath = myPath.Substring(0, myPath.LastIndexOf('/'));
		var allLevelDataGUIDInFolder = AssetDatabase.FindAssets("t:TutorialTask", new string[] { folderPath });
		if(allLevelDataGUIDInFolder.Length == 0) {
			Log.Error(false, this, $"Found no TutorialTask in path [{folderPath}]");
			return;
		}
		// from guid back to LevelData, retrieve ToyView component
		for(int n = 0; n < allLevelDataGUIDInFolder.Length; n++) {
			var guid = allLevelDataGUIDInFolder[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var taskData = AssetDatabase.LoadAssetAtPath<TutorialTask>(toPath);
			tasks.Add(taskData);
		}
		// dirty so it can be save
		EditorUtility.SetDirty(this);
		// validate after load task
		ValidateTaskList();
	}

	[Button]
	void ValidateTaskList() {
		ValidateNullID();
		ValidateEmptyActionsList();
		ValidateDuplicateID();
	}
#endif//UNITY_EDITOR



	#region Validate tasks
	void ValidateNullID() {
		Log.Info("------ Validate null ID");
		for(int n = 0; n < tasks.Count; n++) {
			var task = tasks[n];
			if(string.IsNullOrEmpty(task.id)) {
				Log.Error(false, task, $"Null id of tasks [{task.name}]");
			}
		}
	}

	void ValidateEmptyActionsList() {
		Log.Info("------ Validate empty action list");
		for(int n = 0; n < tasks.Count; n++) {
			var task = tasks[n];
			if(task.actions.Count == 0) {
				Log.Warning(false, task, $"Tasks [{task.name}] has empty action list");
			}
		}
	}

	void ValidateDuplicateID() {
		Log.Info("------ Validate duplicated ID");
		for(int n = 0; n < tasks.Count; n++) {
			var task = tasks[n];
			for(int m = n + 1; m < tasks.Count; m++) {
				var nextTask = tasks[m];
				if(task.id == nextTask.id) {
					Log.Warning(false, this, $"Task [{task.name}] and [{nextTask.name}] are same ID [{task.id}]");
				}
			}
		}
	}
	#endregion//Validate tasks
}
