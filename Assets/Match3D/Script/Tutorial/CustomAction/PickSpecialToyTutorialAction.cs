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
		// spawn the Hand object
		_handInstance = Instantiate(instructionHandPrefab);
		// pick special toy follow setting
		var toysForTut = PickSpecialToy();
		// disable physics for others, keep physics for the one we want user to touch on
		toyStats.SetActivePhysicsForAllToy(false);

		var awaiter = new ClassVariableValueChangeAwaiter<ToyView>();
		toysForTut.SetActivePhysics(true);
		// show the hand at select toy, 
		var posForHand = toysForTut.transform.position;
		posForHand.z -= offsetHandToTarget;
		_handInstance.transform.position = posForHand;
		// wait for user to touch on it
		await awaiter.Start(userPickToyVar);

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
