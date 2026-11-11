using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For ingame tut, show a hand, instruction for how to drag the Weekly Toy to Toycatcher
/// </summary>
[CreateAssetMenu(fileName = "DragWeeklyToyToCatcherTutorialAction", menuName = "Tutorial/Action/DragWeeklyToyToCatcherTutorialAction")]
public class DragWeeklyToyToCatcherTutorialAction : TutorialActionBase {
	#region Init, config
	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] TransformVariable toyCatcherTransVar;

	[Header("Config, prefab")]
	[SerializeField] GameObject instructionHandPrefab;
	[SerializeField] float offsetHandToTarget = 3f;
	[SerializeField] float moveHandToToyCatcherDuration = 1f;

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
		LoopHandMoving().Forget();
		return true;
	}

	public override void Dispose() {
		ClearRunningTasks();
		if(_handInstance != null) {
			Destroy(_handInstance);
		}
	}
	#endregion//Public


	#region Private
	async UniTask LoopHandMoving() {
		// spawn the Hand object
		_handInstance = Instantiate(instructionHandPrefab);
		var target = PickClosestToyToTheCatcher();
		var toyCatcherPos = toyCatcherTransVar.Value.position;
		toyCatcherPos.z -= offsetHandToTarget;

		// looping show the instruction. Already dispose via ClearRunningTasks()
		while(true) {
			var targetPos = target.transform.position;
			targetPos.z -= offsetHandToTarget;
			_handInstance.transform.position = targetPos;
			LeanTween.move(_handInstance, toyCatcherPos, moveHandToToyCatcherDuration).setEase(LeanTweenType.easeInQuart);
			await UniTask.Delay(TimeSpan.FromSeconds(moveHandToToyCatcherDuration), cancellationToken: _disposeToken.Token);
			// wait a little more before next loop
			await UniTask.Delay(TimeSpan.FromSeconds(moveHandToToyCatcherDuration * 0.5f), cancellationToken: _disposeToken.Token);
		}
	}

	GameObject PickClosestToyToTheCatcher() {
		var eventToys = toyStats.EventMap;
		var toyCatcherPos = toyCatcherTransVar.Value.position;
		var sqrClosestDistance = 9999999f;
		GameObject closetToy = null;

		foreach(var keyPair in eventToys) {
			// we often has only one weekly toy, let make foreach for general purpose
			var toyList = keyPair.Value;
			for(int n = 0; n < toyList.Count; n++) {
				var toy = toyList[n];
				var sqrDistanceToCatcher = Vector3.SqrMagnitude(toy.transform.position - toyCatcherPos);
				if(sqrDistanceToCatcher < sqrClosestDistance) {
					sqrClosestDistance = sqrDistanceToCatcher;
					closetToy = toy.gameObject;
				}
			}
		}
		return closetToy;
	}
	#endregion//Private


	#region Private
	UniTaskTokenHelper _disposeToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disposeToken.ClearRunningTasks();
	}
	#endregion//Private
}