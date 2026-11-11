using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreGameLightingStormBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Data, config")]
	[SerializeField] BooleanVariable activeThunderBoosterVar;
	[SerializeField] IntegerVariable destroyToyAmountVar;
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Config - show up icon")]
	[SerializeField] GameObject thunderIconPrefab;
	[SerializeField] float zoomDuration = 1f;

	[Header("Config")]
	[SerializeField] float delayBetweenThunderAndDestroy = 0.3f;
	[SerializeField] float delayBetweenThunder = 0.2f;

	[Header("Config - VFX")]
	[SerializeField] GameObject thunderStrikeVfxPrefab;
	[SerializeField] GameObject destroyToyVfxPrefab;

	[Header("Audio")]
	[SerializeField] AudioCommand thunderAudioCmd;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Get call from mainFlow </summary>
	public async UniTask ProcessAffecting() {
		// no active booster? do nothing
		if(!activeThunderBoosterVar.Value) {
			return;
		}
		// destroy toy, disable active thunderVar
		activeThunderBoosterVar.Value = false;

		// spawn icon Thunder
		var thunderIconVfx = Instantiate(thunderIconPrefab);
		var centerPos = transform.position;
		thunderIconVfx.transform.position = centerPos;
		thunderIconVfx.transform.localScale = Vector3.zero;
		// zoom out-in effect
		LeanTween.scale(thunderIconVfx, Vector3.one, zoomDuration).setEaseOutElastic();
		await UniTask.Delay(TimeSpan.FromSeconds(zoomDuration + 0.5f));
		LeanTween.scale(thunderIconVfx, Vector3.zero, zoomDuration).setEaseInElastic();
		await UniTask.Delay(TimeSpan.FromSeconds(zoomDuration));
		Destroy(thunderIconVfx);

		// destroy toys
		var toDestroyList = toyStats.GetRandomListOfDecoyToy(destroyToyAmountVar.Value);
		// disable physics for all target toys first
		for(int n = 0; n < toDestroyList.Count; n++) {
			var toy = toDestroyList[n];
			toy.mainCollider.enabled = false;
			toy.mainRigid.isKinematic = true;
		}
		// destroy them
		for(int n = 0; n < toDestroyList.Count; n++) {
			var toy = toDestroyList[n];
			SpawnThunderAndDestroyToy(toy).Forget();
			await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenThunder), cancellationToken: _disableToken.Token);
		}
	}
	#endregion//Public


	#region Private
	async UniTask SpawnThunderAndDestroyToy(ToyView toy) {
		// thunder strike at toy, then delay a bit
		var posForVfx = toy.transform.position;
		var thunderVfx = ObjectPool.SpawnObject(thunderStrikeVfxPrefab);
		thunderVfx.transform.position = posForVfx;
		// audio
		thunderAudioCmd.Execute();
		await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenThunderAndDestroy), cancellationToken: _disableToken.Token);
		// destroy toy, spawn VFX, remove it out of toyStats
		Destroy(toy.gameObject);
		// a bit offse from explode vfx to let it higher than the toy
		var explodeVfx = ObjectPool.SpawnObject(destroyToyVfxPrefab);
		posForVfx.z -= 1.5f;
		explodeVfx.transform.position = posForVfx;
		toyStats.RemoveToy(toy);
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
