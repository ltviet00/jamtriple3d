using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Listen to event when user revive when toybox full
/// </summary>
public class ToyBoxClearAffector : MonoBehaviour {
	#region Init, config
	//[Header("Event in")]
	//[SerializeField] GameEvent userClearBoxesEvent;

	[Header("ShareData")]
	[SerializeField] ToyViewClassListVariable toyInBoxesListVar;
	//[SerializeField] ToyViewClassListVariable activeToyListVar;
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] TransformVariable grabToyElevationVar;

	[Header("Config")]
	[SerializeField] FloatVariable scaleForToyInsideBoxVar;
	[SerializeField] Vector2 throwBackVeloRange;
	[SerializeField] float delayBeforeReactiveColliderForToy = 1f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		//userClearBoxesEvent.Subscribe(OnUserClearBox);
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		//userClearBoxesEvent.Unsubscribe(OnUserClearBox);
		ClearRunningTasks();
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> Clear toyBar , call this when user revive by coin or watch ads ... </summary>
	public void ClearBox() {
		ClearRunningTasks();
		ClearBoxesAndPushToyBackToGround().Forget();
	}
	#endregion//Public


	#region Private
	async UniTask ClearBoxesAndPushToyBackToGround() {
		// wait a bit, cause user often ClearBox when use coin or watch ads on BoxFull popup, wait sometime for the popup to fadeout
		await UniTask.Delay(300, cancellationToken:_cancelToken.Token);
		// throw toy
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			var toy = toyInBoxesListVar[n];
			// let them same height to the elevator, so they won't collide with other toy when be throwned back
			var pos = toy.transform.position;
			pos.z = grabToyElevationVar.Value.position.z;
			toy.transform.position = pos;
			// add velocity to let the fly back. Remember to active physics, cause they got disable in ToyBoxGroup
			toy.SetActivePhysics(true);
			var veloForToy = new Vector3(0, RandomThrowVelo(), 0);
			toy.mainRigid.velocity = veloForToy;
			// remember to disable auto rotate component - which was added when toy pull to ToyBox
			toy.DisableAutoRotate();
			// scale them up (cause they got scale down in the ToyBoxGroup, now divide for the scale value to get the scale value.
			var targetScale = toy.transform.localScale / scaleForToyInsideBoxVar.Value;
			LeanTween.scale(toy.gameObject, targetScale, 0.5f).setEase(LeanTweenType.linear);
			// add toy back to the active toy list
			toyStats.AddToy(toy);
		}
		// note: cause toy in the box was disable collider, so they won't collide with wall (wall often cover the toybox).
		// So if we throw the toy back like above, they will collide with the bottom wall, so, disable the collider a bit then re-enable them later
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			var toy = toyInBoxesListVar[n];
			toy.SetActiveCollider(false);
		}
		// wait a bit to make sure they 
		await UniTask.Delay((int)(delayBeforeReactiveColliderForToy * 1000), cancellationToken: _cancelToken.Token);
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			var toy = toyInBoxesListVar[n];
			toy.SetActiveCollider(true);
		}
		// final, clear the toyInBoxList (reset to null, not clear). Read more in toyBoxGroup
		var toyInBoxCapacity = toyInBoxesListVar.Value.Capacity;
		toyInBoxesListVar.Clear();
		for(int n = 0; n < toyInBoxCapacity; n++) {
			toyInBoxesListVar.Value.Add(null);
		}
	}

	float RandomThrowVelo() {
		return Random.Range(throwBackVeloRange.x, throwBackVeloRange.y);
	}

	UniTaskTokenHelper _cancelToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_cancelToken.ClearRunningTasks();
	}
	#endregion//Private
}
