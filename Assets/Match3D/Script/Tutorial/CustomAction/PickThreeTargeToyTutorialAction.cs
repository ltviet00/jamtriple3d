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
		// spawn the Hand object
		_handInstance = Instantiate(instructionHandPrefab);
		// pick random 3 target toys for tutorial/
		var toysForTut = toyStats.GetRandomListOfTargetToy(3);
		// disable physics for others, keep physics for the one we want user to touch on
		toyStats.SetActivePhysicsForAllToy(false);

		var awaiter = new ClassVariableValueChangeAwaiter<ToyView>();
		for(int n = 0; n < toysForTut.Count; n++) {
			// keep physics for the select toy only
			var toy = toysForTut[n];
			toy.SetActivePhysics(true);
			// show the hand at select toy, 
			var posForHand = toy.transform.position;
			posForHand.z -= offsetHandToTarget;
			_handInstance.transform.position = posForHand;
			// wait for user to touch on it
			await awaiter.Start(userPickToyVar);
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
