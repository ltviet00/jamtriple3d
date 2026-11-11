using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// When we grab a toy, we bring that toy (higher) than other toys (in this game, Zcoord, so lower Z pos)
/// But not too high from all toys, so let find the highest one, then add a bit more, and use that at toy
/// elevation.
/// </summary>
public class GrabToyElevator : MonoBehaviour {
	#region Init, config
	[Header("ShareData - in")]
	//[SerializeField] ToyViewClassListVariable activeToysListVar;
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] ToyViewVariable grabbingToyVar;
	[SerializeField] GameEvent toyFinishSpawningEvent;

	[Header("ShareData - out")]
	[SerializeField] TransformVariable saveToTransformVar;

	[Header("Config")]
	///<summary> no need to check every frame </summary>
	[SerializeField] float checkingInterval = 5f;
	[SerializeField] float marginFromTheHighestElevation = 3f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		toyFinishSpawningEvent.Subscribe(OnToyFinishSpawning);
		saveToTransformVar.Value = transform;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
		if(saveToTransformVar.Value != null && saveToTransformVar.Value == transform) {
			saveToTransformVar.Value = null;
		}
	}
	#endregion// Init config


	#region Private
	void OnToyFinishSpawning() {
		// we need to listen once
		toyFinishSpawningEvent.Unsubscribe(OnToyFinishSpawning);
		IntervalCheckToyElevationAsync().Forget();
	}

	async UniTask IntervalCheckToyElevationAsync() {
		// let wait a bit at first, and update one then inter check later
		UpdateHighestToyElevation();
		await UniTask.Delay(1000, cancellationToken:_token.Token);
		// loop forever, will cancel when disable. Notes: dont forget to use CancellationToken
		while(true) {
			await UniTask.Delay(TimeSpan.FromSeconds(checkingInterval), cancellationToken: _token.Token);
			UpdateHighestToyElevation();
		}
	}

	void UpdateHighestToyElevation() {
		// highest toy = lowest Z
		var lowestZ = 0f;
		// find lowest toy in groups of toy
		var lowestDecoyZ = GetHighestPositionOfToyMap(toyStats.DecoyMap);
		if(lowestDecoyZ < lowestZ) {
			lowestZ = lowestDecoyZ;
		}
		var lowestTargetZ = GetHighestPositionOfToyMap(toyStats.TargetMap);
		if(lowestTargetZ < lowestZ) {
			lowestZ = lowestTargetZ;
		}
		var lowestSupportZ = GetHighestPositionOfToyMap(toyStats.SupportMap);
		if(lowestSupportZ < lowestZ) {
			lowestZ = lowestSupportZ;
		}

		// set positior it. input handler modules will follow it transform to get Z pos
		var pos = transform.position;
		pos.z = lowestZ - marginFromTheHighestElevation;
		transform.position = pos;
	}

	float GetHighestPositionOfToyMap(Dictionary<GameObject, List<ToyView>> map) {
		// highest toy = lowest Z
		var result = 0f;
		foreach(var toyList in map.Values) {
			var lowestZ = GetHighestPositionOfToyList(toyList);
			if(lowestZ < result) {
				result = lowestZ;
			}
		}
		return result;
	}

	float GetHighestPositionOfToyList(List<ToyView> toyList) {
		// highest toy = lowest Z
		var lowestZ = 0f;
		for(int n = 0; n < toyList.Count; n++) {
			var toy = toyList[n];
			// check if user is grabbing a toy, then we won't update position, if not, the grabbing toy
			// is the higest one, and we will keep continue increase our position
			if(grabbingToyVar.Value != null && toy == grabbingToyVar.Value) {
				continue;
			}
			var toyPosZ = toy.transform.position.z;
			if(toyPosZ < lowestZ) {
				lowestZ = toyPosZ;
			}
		}
		return lowestZ;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
