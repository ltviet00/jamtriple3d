using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using System;

/// <summary>
/// Handle tutorial, show hand, disable input, let user follow instruction
/// </summary>
public class TutorialHandler : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] ToyViewClassListVariable activeToysListVar;
	[SerializeField] GameObjectVariable hitObjectVar;
	[SerializeField] TransformVariable toyCatcherPositionVar;

	[Header("Event in")]
	[SerializeField] GameEvent userMatchSuccessEvent;

	[Header("Command")]
	[SerializeField] GameEvent disableInputCmd;
	[SerializeField] GameEvent enableInputCmd;

	[Header("Event out")]
	[SerializeField] GameEvent finishSpawnToyEvent;
	[SerializeField] GameEvent readyForUserToInputEvent;
	[SerializeField] GameEvent gameWinEvent;
	[SerializeField] UnityEvent openNextSceneCmd;

	[Header("Config")]
	[SerializeField] ToyAndRootPrefab[] toyAndRootList;
	[SerializeField] float movingHandDuration = 0.5f;

	[Header("Refs")]
	[SerializeField] GameObject handeObject;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
		// set rootPrefab for toys, so they can be match
		SetRootPrefabForToysOnScene();
		//Process().AttachExternalCancellation(_token.Token);
		Process().Forget();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	async UniTask Process() {
		// disable hand first
		handeObject.SetActive(false);
		// wait one frame to let other modules to init. then disable input
		await UniTask.Yield();
		await UniTask.Yield();
		disableInputCmd.Raise();
		// wait a bit then raise event finishSpawn, currently, module toyElevator need this to
		await UniTask.Delay(1000, cancellationToken: _token.Token);
		finishSpawnToyEvent.Raise();
		readyForUserToInputEvent.Raise();
		// ready for input now
		enableInputCmd.Raise();
		// first pick toy, and loop until user finish all toys
		PickPairToyShowInstructionAndWaitForUserToInput();
	}

	void PickPairToyShowInstructionAndWaitForUserToInput() {
		// active a select pair, disable others, wait for user to follow instruction
		var toyMap = activeToysListVar.ActiveToyMap;
		var isPickedPair = false;
		foreach(var item in toyMap) {
			var toyList = item.Value;
			if(!isPickedPair) {
				isPickedPair = true;
				SetActiveInputForToyList(toyList, true);
				//ShowInstructionForToyPair(toyList).AttachExternalCancellation(_token.Token);
				ShowInstructionForToyPair(toyList).Forget();
			}
			else {
				SetActiveInputForToyList(toyList, false);
			}
		}
	}

	void SetRootPrefabForToysOnScene() {
		for(int n = 0; n < toyAndRootList.Length; n++) {
			var item = toyAndRootList[n];
			var rootPrefab = item.rootPrefab;
			for(int m = 0; m < item.toys.Length; m++) {
				item.toys[m].rootPrefab = rootPrefab;
			}
		}
	}

	void SetActiveInputForToyList(List<ToyView> toyList, bool active) {
		for(int n = 0; n < toyList.Count; n++) {
			var toy = toyList[n];
			toy.mainCollider.enabled = active;
			toy.mainRigid.isKinematic = !active;
		}
	}

	async UniTask ShowInstructionForToyPair(List<ToyView> toyList) {
		// when user press on select toy, stop instruction
		hitObjectVar.OnValueChangedHandler += OnUserPressOnToy;
		handeObject.SetActive(true);
		while(true) {
			var firstToy = toyList[0];
			LetHandMoveFromToyToCatcherPosition(firstToy);
			await UniTask.Delay(TimeSpan.FromSeconds(movingHandDuration + 0.5f), cancellationToken:_token.Token);
			var secondToy = toyList[1];
			LetHandMoveFromToyToCatcherPosition(secondToy);
			await UniTask.Delay(TimeSpan.FromSeconds(movingHandDuration + 0.5f), cancellationToken: _token.Token);
		}
	}

	void OnUserPressOnToy(GameObject hit) {
		// stop the instruction, wait for user match success
		ClearRunningTasks();
		hitObjectVar.OnValueChangedHandler -= OnUserPressOnToy;
		userMatchSuccessEvent.Subscribe(OnUserMatchSuccess);
		// disable the hand
		handeObject.SetActive(false);
	}

	void OnUserMatchSuccess() {
		userMatchSuccessEvent.Unsubscribe(OnUserMatchSuccess);
		// check if all toys are clear
		if(activeToysListVar.Count == 0) {
			gameWinEvent.Raise();
			openNextSceneCmd.Invoke();
		}
		// not finish? open next instruction
		else {
			PickPairToyShowInstructionAndWaitForUserToInput();
		}
	}

	void LetHandMoveFromToyToCatcherPosition(ToyView toy) {
		// keep the hand Z pos to highe than toy
		var posForHand = toy.transform.position;
		posForHand.z = handeObject.transform.position.z;
		handeObject.transform.position = posForHand;
		// move
		var targetPos = toyCatcherPositionVar.Value.position;
		targetPos.z = posForHand.z;
		LeanTween.move(handeObject, targetPos, movingHandDuration).setEase(LeanTweenType.easeInOutQuart);
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private


	#region Classes
	///<summary> Normal level, toySpawner will spawn toy and assign rootPrefab for them, but tutorial we have to do this manually. So need a class to store the rootPrefab ref and toys on scene that same root. </summary>
	[System.Serializable]
	class ToyAndRootPrefab {
		public GameObject rootPrefab;
		public ToyView[] toys;
	}
	#endregion//Classes
}
