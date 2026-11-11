using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;
using System;

public class MagnifyGlassBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onUserRequestMagnifyGlassEvent;

	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Config")]
	[SerializeField] IntegerVariable hintToyAmountCfg;
	[SerializeField] GameObject hintVfxAtTargetToyPrefab;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onUserRequestMagnifyGlassEvent.Subscribe(OnRequestLightBulb);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onUserRequestMagnifyGlassEvent.Unsubscribe(OnRequestLightBulb);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	void OnRequestLightBulb() {
		//ProcessAffect().AttachExternalCancellation(_token.Token);
		//var randomToy = toyStats.GetRandomTargetToy();
		var randomToyList = toyStats.GetRandomListOfTargetToy(hintToyAmountCfg.Value);
		for(int n = 0; n < randomToyList.Count; n++) {
			var toy = randomToyList[n];
			var vfx = ObjectPool.SpawnObject(hintVfxAtTargetToyPrefab);
			var vfxScript = vfx.GetComponent<VfxFollowToy>();
			if(vfxScript == null) {
				Log.Error(false, hintVfxAtTargetToyPrefab, $"Not found VfxFollowToy component at prefab [{hintVfxAtTargetToyPrefab.name}]");
				return;
			}
			vfxScript.FollowToy(toy);
		}
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
