using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main class for Tutorial
/// - Load available tasks, check remain tasks
/// - Save to local/cloud the current process
/// - Get trigger location ..
/// </summary>
public class TutorialManager : MonoSingleton<TutorialManager> {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserTutorialData userData;
	[SerializeField] TutorialTaskStorage taskStorage;

	[Header("Debug")]
	[SerializeField] bool enableLogProgress;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	protected override void DoOnStart() {
		base.DoOnStart();
		LoadAndSetupRemainTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Trigger location, it will check running task to trigger tasks </summary>
	public void TriggerLocation(StringVariable location) {
		if(isFailedConfig)
			return;
		LogProgress($"Tutorial, Got trigger location [{location}]");
		// no remain task, then ignore
		if(_remainTask.Count == 0) {
			LogProgress($"Tutorial, No remain task");
			return;
		}
		// only let one task running at a time. simple ignore, no need warning
		if(IsRunningAnyTasks()) {
			LogProgress($"Tutorial, task running [{_currentTask.name}], ignore TriggerLocation");
			return;
		}
		// no task running, check if any remainTask that will got trigger by this location
		var tasksAtLocation = GetListOfRemainTaskAtLocation(location);
		if(tasksAtLocation.Count == 0) {
			LogProgress($"Tutorial, No remain task at location [{location}]");
			return;
		}
		// filter tasks that pass their conditions
		var readyTasks = FilterTasksThatPassedTheirConditions(tasksAtLocation);
		if(readyTasks.Count == 0) {
			LogProgress($"Tutorial, got [{tasksAtLocation.Count}] but non are ready (passed conditions)");
			return;
		}
		// more than one task at same location? sort and take the highest priority one
		readyTasks.Sort((a, b) => b.priority - a.priority);
		var taskToRun = readyTasks[0];
		RunTask(taskToRun).Forget();
	}

	public bool IsRunningAnyTasks() {
		return _currentTask != null;
	}

	///<summary> Get current execute task, return null if not run any task </summary>
	public TutorialTask GetCurrentRunningTasks() {
		return _currentTask;
	}
	#endregion//Public


	#region Private
	TutorialTask _currentTask = null;
	List<TutorialTask> _remainTask = new List<TutorialTask>();

	void LoadAndSetupRemainTasks() {
		_remainTask.Clear();
		var tasks = taskStorage.tasks;
		for(int n = 0; n < tasks.Count; n++) {
			var task = tasks[n];
			if(!userData.IsTaskDone(task.id))
				_remainTask.Add(task);
		}
	}

	async UniTask RunTask(TutorialTask task) {
		LogProgress($"Tutorial, Run task [{task.name}]");
		// wait for task to done
		_currentTask = task;
		var result = await task.Run();
		if(result) {
			// task done, remove from remainTask, save to disk
			userData.AddToDoneTask(task.id);
			_remainTask.Remove(task);
			_currentTask = null;
			// load next task?
			LogProgress($"Tutorial, Done task [{task.name}]");
		}
		else {
			// task fail? log?
		}
	}

	List<TutorialTask> GetListOfRemainTaskAtLocation(StringVariable location) {
		var result = new List<TutorialTask>();
		for(int n = 0; n < _remainTask.Count; n++) {
			var task = _remainTask[n];
			if(task.location == location)
				result.Add(task);
		}
		return result;
	}

	List<TutorialTask> FilterTasksThatPassedTheirConditions(List<TutorialTask> tasks) {
		var result = new List<TutorialTask>();
		for(int n = 0; n < tasks.Count; n++) {
			var task = tasks[n];
			if(task.IsPassAllConditions())
				result.Add(task);
		}
		return result;
	}
	#endregion//Private


	#region Progress logging
	void LogProgress(string message) {
		if(enableLogProgress)
			Log.Info(this, message);
	}
	#endregion//Progress logging
}
