using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger affect for ThunderSupport
/// </summary>
public class ThunderSupportAffector : SupportToyAffectorController.BaseSupportToyAffector {
	#region Init, config
	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Config")]
	[SerializeField] SupportToyID targetSupportID;
	[SerializeField] IntegerVariable destroyToyPerItemVar;
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

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Interface
	public override bool IsSameTargetSupportToyID(SupportToyID id) {
		return targetSupportID == id;
	}

	public override void TriggerAffect() {
		if(isFailedConfig)
			return;
		// destroy toy queue
		_remainToProcess += destroyToyPerItemVar.Value;
		// if we're running, then do nothing, if not, then start new one
		if(_runningTask.Status.IsCompleted()) {
			//_runningTask = Process().AttachExternalCancellation(_disableToken.Token);
			_runningTask = Process();
		}
	}
	#endregion//Interface


	#region Private
	///<summary> Cause user can catch thunders quick, the previous affect has not done yet, but new toy was catch.
	/// So let just save them to an amount, the Process function will execute them .</summary>
	int _remainToProcess = 0;
	UniTask _runningTask;

	async UniTask Process() {
		while(_remainToProcess > 0) {
			// reduce the remain
			_remainToProcess--;
			// destroy toys, take random, also remove it out of stats
			var toy = toyStats.GetRandomDecoyToy();
			toyStats.RemoveToy(toy);
			// delay a bit for effect
			await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenThunder), cancellationToken: _disableToken.Token);
			SpawnThunderAndDestroyToy(toy).Forget();
		}
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
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
