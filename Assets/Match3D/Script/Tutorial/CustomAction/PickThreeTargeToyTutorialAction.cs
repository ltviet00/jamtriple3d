using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For ingame tut, show a hand, instruction for how to drag the toy to Toycatcher
/// </summary>
[CreateAssetMenu(fileName = "PickThreeTargeToyTutorialAction", menuName = "Tutorial/Action/PickThreeTargeToyTutorialAction")]
public class PickThreeTargeToyTutorialAction : TutorialActionBase {
	#region Init, config
	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] ToyViewVariable userPickToyVar;

	[Header("Config, prefab")]
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
		// toys start as a pile that collapses, wait until it is still, otherwise the hand points to a moving toy
		await TutorialToyHelper.WaitForToysToSettle(toyStats);
		// pick 3 target toys that are really visible (on top of the pile)
		var toysForTut = toyStats.GetVisibleTopTargetToys(3, Camera.main);
		if(toysForTut.Count == 0)
			toysForTut = toyStats.GetRandomListOfTargetToy(3);
		if(toysForTut.Count == 0)
			return true;
		// spawn the Hand object
		_handInstance = Instantiate(instructionHandPrefab);
		// freeze everything and disable touch, only the selected toy can be touched
		toyStats.SetActivePhysicsForAllToy(false);

		var awaiter = new ClassVariableValueChangeAwaiter<ToyView>();
		for(int n = 0; n < toysForTut.Count; n++) {
			var toy = toysForTut[n];
			if(toy == null)
				continue;
			// collider on (touchable) but still frozen: the toy under it has no collider now, it would fall through
			TutorialToyHelper.MakeTouchableButFrozen(toy);
			// hand follows the toy until the user touches it
			var waitTouch = awaiter.Start(userPickToyVar);
			while(!waitTouch.Status.IsCompleted()) {
				if(_handInstance != null && toy != null)
					_handInstance.transform.position = TutorialToyHelper.HandPositionFor(toy, offsetHandToTarget);
				await UniTask.Yield();
			}
		}
		// finish all toy, delete hand, release physics for all toys
		Destroy(_handInstance);
		toyStats.SetActivePhysicsForAllToy(true);
		return true;
	}
	#endregion//Private


	#region Private
	UniTaskTokenHelper _disposeToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disposeToken.ClearRunningTasks();
	}
	#endregion//Private
}
