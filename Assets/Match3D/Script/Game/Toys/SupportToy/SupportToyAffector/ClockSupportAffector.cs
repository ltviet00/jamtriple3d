using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger affect for ClockSupportToy
/// </summary>
public class ClockSupportAffector : SupportToyAffectorController.BaseSupportToyAffector {
	#region Init, config
	[Header("Config")]
	[SerializeField] SupportToyID targetSupportID;
	[SerializeField] IntegerVariable timeAddMorePerItemVar;
	[SerializeField] float increaseEffectDuration = 1f;

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
		_remainToProcess ++;
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
		var delayToIncreaseOneSecond = increaseEffectDuration / (float)timeAddMorePerItemVar.Value;
		var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
		while(_remainToProcess > 0) {
			// reduce the remain
			_remainToProcess--;
			for(int n = 0; n < timeAddMorePerItemVar.Value; n++) {
				await UniTask.Delay(TimeSpan.FromSeconds(delayToIncreaseOneSecond), cancellationToken: _disableToken.Token);
				levelTimer.IncreaseRemainTimeAndUpdateShareVar(1);
			}
		}
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
