using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Function for LightBulb booster 
/// </summary>
public class LightBulbBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onUserRequestLightBulbEvent;

	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Config")]
	[SerializeField] GameObject hintVfxAtTargetToyPrefab;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onUserRequestLightBulbEvent.Subscribe(OnRequestLightBulb);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onUserRequestLightBulbEvent.Unsubscribe(OnRequestLightBulb);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	void OnRequestLightBulb() {
		//ProcessAffect().AttachExternalCancellation(_token.Token);
		var randomToy = toyStats.GetRandomTargetToy();
		var vfx = ObjectPool.SpawnObject(hintVfxAtTargetToyPrefab);
		var vfxScript = vfx.GetComponent<VfxFollowToy>();
		if(vfxScript == null) {
			Log.Error(false, hintVfxAtTargetToyPrefab, $"Not found VfxFollowToy component at prefab [{hintVfxAtTargetToyPrefab.name}]");
			return;
		}
		vfxScript.FollowToy(randomToy);
	}

	//async UniTask ProcessAffect() {
	//	// take toy, disable physics
	//	var pairToTakeOut = RandomPickToyPair();
	//	for(int n = 0; n < pairToTakeOut.Count; n++) {
	//		var toy = pairToTakeOut[n];
	//		Destroy(toy.mainCollider);
	//		Destroy(toy.mainRigid);
	//		// same heigh to elevation
	//		var pos = toy.transform.position;
	//		pos.z = grabToyElevationVar.Value.position.z;
	//		toy.transform.position = pos;
	//	}
	//	// spawn the halo at toy to let user see picked toy
	//	var haloVfxList = new List<GameObject>();
	//	for(int n = 0; n < pairToTakeOut.Count; n++) {
	//		var halo = ObjectPool.SpawnObject(haloAtPickedToyVfxPrefab);
	//		haloVfxList.Add(halo);
	//		var targetToy = pairToTakeOut[n];
	//		halo.transform.position = targetToy.transform.position;
	//	}
	//	// wait a bit
	//	await UniTask.Delay(TimeSpan.FromSeconds(holdToyBeforePullDuration)).AttachExternalCancellation(_token.Token);
	//	// disable halo here, we pull them from pool, so just need to disable
	//	for(int n = 0; n < haloVfxList.Count; n++) {
	//		var halo = haloVfxList[n];
	//		halo.gameObject.SetActive(false);
	//	}
	//	// pull them to toy catcher
	//	var catcherPos = toyCatcherPositionVar.Value.position;
	//	catcherPos.z = grabToyElevationVar.Value.position.z;
	//	for(int n = 0; n < pairToTakeOut.Count; n++) {
	//		var toy = pairToTakeOut[n];
	//		LeanTween.move(toy.gameObject, catcherPos, pullToyToCatcherDuration);
	//	}
	//	// wait for pulling effect
	//	await UniTask.Delay(TimeSpan.FromSeconds(pullToyToCatcherDuration)).AttachExternalCancellation(_token.Token);
	//	// then destroy toy, and raise event
	//	for(int n = 0; n < pairToTakeOut.Count; n++) {
	//		Destroy(pairToTakeOut[n].gameObject);
	//	}
	//	successMatchToyPairEvent.Raise();
	//}

	//List<ToyView> RandomPickToyPair() {
	//	var result = new List<ToyView>();
	//	var randomIndex = Random.Range(0, allToyListVar.ActiveToyMap.Count);
	//	var counter = 0;
	//	foreach(var item in allToyListVar.ActiveToyMap) {
	//		if(counter == randomIndex) {
	//			// let take first 2
	//			var list = item.Value;
	//			result.Add(list[0]);
	//			result.Add(list[1]);
	//		}
	//		counter++;
	//	}
	//	return result;
	//}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
