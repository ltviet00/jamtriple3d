using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

/// <summary>
/// Run the flow at the very first time level load: spawn toy, check booster...
/// </summary>
public class InGameMainFlow : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent loadingScreenFinishEvent;

	[Header("Event out")]
	[SerializeField] GameEvent readyForUserToInputEvent;

	[Header("Command")]
	[SerializeField] GameEvent disableInputCmd;
	[SerializeField] GameEvent enableInputCmd;

	[Header("Trigger tutorial location")]
	[SerializeField] UnityEvent triggerTutorialLocationRequest;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		loadingScreenFinishEvent.Subscribe(OnLoadingScreenFinish);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		loadingScreenFinishEvent.Unsubscribe(OnLoadingScreenFinish);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Event in
	void OnLoadingScreenFinish() {
		//RunFirstFlowAsync().AttachExternalCancellation(_disableToken.Token);
		RunFirstFlowAsync().Forget();
	}
	#endregion//Event int


	#region Priate
	async UniTask RunFirstFlowAsync() {
		// dependencies
		var toySpawnerRef = SimpleDI.Instance.Get<ToySpawner>();
		var targetUIBar = SimpleDI.Instance.Get<TargetUIBar>();
		var toyBoxPositionAdjustor = SimpleDI.Instance.Get<ToyBoxGroupAdjustPositionFollowScreen>();
		var wallAdjusttor = SimpleDI.Instance.Get<AdjustWallFollowVisibleAreaAndCamera>();

		// disable input
		disableInputCmd.Raise();
		// adjust toyboxGroup, wall. Wall need to adjust after the toyBoxPosition (cause camera change, UI change ...)
		await toyBoxPositionAdjustor.AdjustCameraViewAndToyBoxPosition();
		wallAdjusttor.AdjustWallPositionFollowVieaArea();

		// spawn toys first
		await toySpawnerRef.SpawnToysAsync(_disableToken.Token);
		// show up level target
		await UniTask.Delay(1000, cancellationToken: _disableToken.Token);
		await targetUIBar.Init(_disableToken.Token);

		// check active pre-game boosters
		await ProcessPreBoosterAsync();

		// wait a bit more (currently, to let toys not fall into Toy Catcher
		await UniTask.Delay(1000, cancellationToken: _disableToken.Token);
		// enable input at last
		enableInputCmd.Raise();
		// raise event to let other module to work
		readyForUserToInputEvent.Raise();

		// last, trigger tutorial location
		triggerTutorialLocationRequest.Invoke();
	}

	async UniTask ProcessPreBoosterAsync() {
		var extraTimeboosterAffector = SimpleDI.Instance.Get<PreGameExtraTimeBoosterAffector>();
		//var fanBoosterButton = SimpleDI.Instance.Get<FanBoosterUIButton>();
		var thunderAffector = SimpleDI.Instance.Get<PreGameLightingStormBoosterAffector>();

		//await fanBoosterButton.ProcessPreFanBooster();
		await extraTimeboosterAffector.ProcessAffecting();
		await thunderAffector.ProcessAffecting();
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
