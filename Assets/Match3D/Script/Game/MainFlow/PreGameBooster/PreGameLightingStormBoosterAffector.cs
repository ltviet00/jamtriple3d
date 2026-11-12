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

		// box sort mode has no decoy toy: strike sets of 3 toys, they fly into the open boxes
		var boxGroup = SimpleDI.Instance.Get<ToyBoxGroup>();
		if(boxGroup != null && boxGroup.IsInBoxSortMode) {
			await StrikeBoxSetsAsync(boxGroup, Mathf.Max(1, Mathf.CeilToInt(destroyToyAmountVar.Value / 3f)));
			return;
		}

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
	const float WAIT_NEXT_BOX_SECONDS = 3f;

	///<summary> Fill [setCount] boxes, one by one. Only a few boxes are open at a time, wait for the next box to open when needed </summary>
	async UniTask StrikeBoxSetsAsync(ToyBoxGroup boxGroup, int setCount) {
		var strikeDelay = delayBetweenThunderAndDestroy + delayBetweenThunder;
		for(int n = 0; n < setCount; n++) {
			var waitUntil = Time.time + WAIT_NEXT_BOX_SECONDS;
			while(!boxGroup.TryThunderFillOpenBox(SpawnThunderVfx, strikeDelay)) {
				if(Time.time > waitUntil)
					return;
				await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: _disableToken.Token);
			}
			await UniTask.Delay(TimeSpan.FromSeconds(strikeDelay * 3f), cancellationToken: _disableToken.Token);
		}
	}

	void SpawnThunderVfx(ToyView toy) {
		var thunderVfx = ObjectPool.SpawnObject(thunderStrikeVfxPrefab);
		thunderVfx.transform.position = toy.transform.position;
		thunderAudioCmd.Execute();
	}

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
