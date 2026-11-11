using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For ingame tut, show a hand, instruction for how to drag the Weekly Toy to Toycatcher
/// </summary>
[CreateAssetMenu(fileName = "DragSupportToyToCatcherTutorialAction", menuName = "Tutorial/Action/DragSupportToyToCatcherTutorialAction")]
public class DragSupportToyToCatcherTutorialAction : TutorialActionBase {
	#region Init, config
	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] ToyStorage toyStorage;
	[SerializeField] TransformVariable toyCatcherTransVar;
	[SerializeField] SupportToyID supportToyID;

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
		// find the support toy first, if not, return false for this action
		var target = PickClosestToyToTheCatcher();
		if(target == null) {
			return false;
		}
		LoopHandMoving(target).Forget();
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
	async UniTask LoopHandMoving(GameObject target) {
		// spawn the Hand object
		_handInstance = Instantiate(instructionHandPrefab);
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
		// get prefab to lookup in ToyStats
		var toyPrefab = toyStorage.GetSupportToyWithID(supportToyID);
		var supportToy = toyStats.SupportMap;
		if(!supportToy.TryGetValue(toyPrefab.gameObject, out var thunderToyList)) {
			Log.Warning(false, this, $"Cannot get Thunder supportTOy list");
			return null;
		}

		var toyCatcherPos = toyCatcherTransVar.Value.position;
		var sqrClosestDistance = 9999999f;
		GameObject closetToy = null;
		for(int n = 0; n < thunderToyList.Count; n++) {
			var toy = thunderToyList[n];
			var sqrDistanceToCatcher = Vector3.SqrMagnitude(toy.transform.position - toyCatcherPos);
			if(sqrDistanceToCatcher < sqrClosestDistance) {
				sqrClosestDistance = sqrDistanceToCatcher;
				closetToy = toy.gameObject;
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