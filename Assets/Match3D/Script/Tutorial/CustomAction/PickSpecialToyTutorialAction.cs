using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pick special toy : weekly toy, support toy ...
/// </summary>
[CreateAssetMenu(fileName = "PickSpecialToyTutorialAction", menuName = "Tutorial/Action/PickSpecialToyTutorialAction")]
public class PickSpecialToyTutorialAction : TutorialActionBase {
	#region Init, config
	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] ToyViewVariable userPickToyVar;

	[Header("Config, prefab")]
	[SerializeField] SpecialToy specialToyType;
	[SerializeField] GameObject instructionHandPrefab;
	[SerializeField] float offsetHandToTarget = 3f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> the hand object </summary>
	GameObject _handInstance;

	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		var result = await PickToyInstructionAsync();
		return result;
	}

	public override void Dispose() {
		ClearRunningTasks();
		if(_handInstance != null) {
			Destroy(_handInstance);
		}
	}
	#endregion//Public


	#region Private
	async UniTask<bool> PickToyInstructionAsync() {
		// level has no special toy (support / weekly): nothing to teach, skip (and no hand left on screen)
		var hasToy = specialToyType == SpecialToy.Support ? toyStats.HasSupportToy
			: specialToyType == SpecialToy.WeeklyEvent && toyStats.HasWeeklyEventToy;
		if(!hasToy)
			return true;
		await TutorialToyHelper.WaitForToysToSettle(toyStats);
		// pick special toy follow setting
		var toyForTut = PickSpecialToy();
		if(toyForTut == null)
			return true;
		// spawn the Hand object
		_handInstance = Instantiate(instructionHandPrefab);
		// freeze everything and disable touch, only the selected toy can be touched
		toyStats.SetActivePhysicsForAllToy(false);
		TutorialToyHelper.MakeTouchableButFrozen(toyForTut);

		// hand follows the toy until the user touches it
		var awaiter = new ClassVariableValueChangeAwaiter<ToyView>();
		var waitTouch = awaiter.Start(userPickToyVar);
		while(!waitTouch.Status.IsCompleted()) {
			if(_handInstance != null && toyForTut != null)
				_handInstance.transform.position = TutorialToyHelper.HandPositionFor(toyForTut, offsetHandToTarget);
			await UniTask.Yield();
		}

		// finish all toy, delete hand, release physics for all toys
		Destroy(_handInstance);
		toyStats.SetActivePhysicsForAllToy(true);
		return true;
	}

	ToyView PickSpecialToy() {
		switch(specialToyType) {
			case SpecialToy.WeeklyEvent:
				return toyStats.GetRandomWeeklyEventToy();
			case SpecialToy.Support:
				return toyStats.GetRandomSupportToy();
			default:
				Log.Warning(false, this, $"Not handle for [{specialToyType}]");
				return null;
		}
	}
	#endregion//Private


	#region Private
	UniTaskTokenHelper _disposeToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disposeToken.ClearRunningTasks();
	}
	#endregion//Private


	#region Classes
	enum SpecialToy {
		WeeklyEvent, Support
	}
	#endregion//Classes
}
