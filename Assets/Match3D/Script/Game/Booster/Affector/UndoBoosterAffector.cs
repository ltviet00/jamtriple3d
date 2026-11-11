using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handle affect for Undo booster
/// </summary>
public class UndoBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onUserRequestUndoEvent;

	[Header("Share Data")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] ToyViewClassListVariable toyInBoxesListVar;
	[SerializeField] TransformVariable grabToyElevationVar;

	[Header("Config")]
	[SerializeField] Vector2 throwBackVeloRange;
	[SerializeField] FloatVariable scaleForToyInsideBoxVar;
	[SerializeField] float delayBeforeReactiveColliderForToy = 1f;

	[Header("UserBoosterData")]
	[SerializeField] UserBoosterInventory boosterInventory;
	[SerializeField] BoosterID undoBoosterID;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onUserRequestUndoEvent.Subscribe(OnRequestUndo);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onUserRequestUndoEvent.Unsubscribe(OnRequestUndo);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	void OnRequestUndo() {
		ExecuteUndoTask().Forget();
	}

	async UniTask ExecuteUndoTask() {
		var toyBoxGroup = SimpleDI.Instance.Get<ToyBoxGroup>();
		if(toyBoxGroup == null) {
			Log.Warning(false, this, $"No ToyBoxGroup to execute Undo booster");
			return;
		}
		// make sure we not execute Undo during the moving toy process
		while(toyBoxGroup.IsRunningMoveToyTask()) {
			await UniTask.Yield(cancellationToken: _disableToken.Token);
		}

		// check if any toy on ToyBox.
		var toyToUndo = toyInBoxesListVar.GetTheOutsiteRightValidToyView(out int boxIndex);
		if(toyToUndo == null) {
			// if not? then increase the booster count, because we deduct it via UI Button
			boosterInventory.AddBooster(undoBoosterID, 1);
			return;
		}

		// then bring it back to field, add to ToyStats
		// let them same height to the elevator, so they won't collide with other toy when be throwned back
		var pos = toyToUndo.transform.position;
		pos.z = grabToyElevationVar.Value.position.z;
		toyToUndo.transform.position = pos;
		// add velocity to let the fly back. Remember to active physics, cause they got disable in ToyBoxGroup
		toyToUndo.SetActivePhysics(true);
		var veloForToy = new Vector3(0, RandomThrowVelo(), 0);
		toyToUndo.mainRigid.velocity = veloForToy;
		// remember to disable auto rotate component - which was added when toy pull to ToyBox
		toyToUndo.DisableAutoRotate();
		// scale them up (cause they got scale down in the ToyBoxGroup, now divide for the scale value to get the scale value.
		var targetScale = toyToUndo.transform.localScale / scaleForToyInsideBoxVar.Value;
		LeanTween.scale(toyToUndo.gameObject, targetScale, 0.5f).setEase(LeanTweenType.linear);
		// add toy back to the active toy list
		toyStats.AddToy(toyToUndo);

		// mark toy in boxList as null
		toyInBoxesListVar.Value[boxIndex] = null;

		// note: cause toy in the box was disable collider, so they won't collide with wall (wall often cover the toybox).
		// So if we throw the toy back like above, they will collide with the bottom wall, so, disable the collider a bit then re-enable them later
		toyToUndo.SetActiveCollider(false);
		// wait a bit to make sure they 
		await UniTask.Delay((int)(delayBeforeReactiveColliderForToy * 1000), cancellationToken: _disableToken.Token);
		toyToUndo.SetActiveCollider(true);
	}

	float RandomThrowVelo() {
		return Random.Range(throwBackVeloRange.x, throwBackVeloRange.y);
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
