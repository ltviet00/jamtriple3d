using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Behavior for the CatchToy beahavior area on the scene.
/// </summary>
public class CatchToyPlaceholder : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent readyForUserToInputEvent;

	[Header("Event out")]
	[SerializeField] GameEvent successCaptureToyEvent;
	[SerializeField] GameEvent failedCaptureEvent;

	[Header("ShareData in")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] TransformVariable toyPlaceholderOnUIVar;
	[SerializeField] FloatVariable pushAwayToyVeloVar;
	[SerializeField] ToyViewVariable grabbingToyVar;

	[Header("Config - Toy layer, throw toy")]
	[SerializeField] LayerMaskVariable layerForToyInNormalVar;
	[SerializeField] float offsetBeforeThrowingToy = 1f;

	[Header("Config - VFX")]
	[SerializeField] GameObject explodeToyVfxPrefab;
	[SerializeField] GameObject throwToyAwayVfxPrefab;
	[SerializeField] Vector3 offsetForExplodeVfx;

	[Header("Just release toy")]
	[SerializeField] GameEvent requestCheckReleaseToyCmd;
	[SerializeField] ToyViewVariable justReleaseToyVar;

	[Header("Config - animation")]
	[SerializeField] FloatVariable pullTookToyToIdlePositionDurationVar;

	//[Header("Event - out")]
	//[SerializeField] GameEvent catcherTakeToyEvent;

	[Header("Refs")]
	[SerializeField] Transform leftDoorTrans;
	[SerializeField] Transform rightDoorTrans;
	[SerializeField] Collider triggerCollider;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		//ResetShareDataOut();
		// disable trigger collider by default, we not function it yet until ready event was fired
		triggerCollider.enabled = false;
		readyForUserToInputEvent.Subscribe(OnUserReadyForInput);
		requestCheckReleaseToyCmd.Subscribe(OnRequestCheckReleaseToy);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		//ResetShareDataOut();
		readyForUserToInputEvent.Unsubscribe(OnUserReadyForInput);
		requestCheckReleaseToyCmd.Unsubscribe(OnRequestCheckReleaseToy);
		ClearRunningTasks();
	}

	void Start() {
		if(isFailedConfig)
			return;
		SyncPositionWithPlaceHolderOnUI();
	}
	#endregion// Init config


	#region Events, Command
	void OnUserReadyForInput() {
		// eanble trigger collider, ready for the OnTriggerEnter() to function
		triggerCollider.enabled = true;
	}

	void OnRequestCheckReleaseToy() {
		if(justReleaseToyVar.Value == null) {
			Log.Error(false, this, $"Null data in JustReleaseToyVar but got command request check");
			return;
		}
		// check if the just release toy is inside out collider
		// https://stackoverflow.com/questions/53453014/collider-istouching-for-3d-colliders
		var toy = justReleaseToyVar.Value;
		var toyBound = toy.mainCollider.bounds;
		if(triggerCollider.bounds.Intersects(toyBound)) {
			//Log.Info("Just release toy inside area, check take it");
			// let count this as a enter collider
			OnTriggerEnter(toy.mainCollider);
		}
		else {
			//Log.Info("Just release toy not collide, ignore");
		}
		// reset var anyway
		justReleaseToyVar.Value = null;
	}
	#endregion//Events. Command


	#region Collision event
	///<summary> Use for inside collider, when toy drag hit the inner zone </summary>
	void OnTriggerEnter(Collider other) {
		if(isFailedConfig)
			return;
		// get toyview component
		var toy = other.GetComponent<ToyView>();
		if(toy == null) {
			return;
		}
		// user still grabbing toy, then ignore
		if(toy == grabbingToyVar.Value) {
			Log.Info(this, "Still grabbing toy, no take");
			return;
		}
		// check if the toy is pickable to capture, or throw away
		if(toyStats.IsPickable(toy)) {
			// remove it out of stats
			toyStats.RemoveToy(toy);
			// chekc if it a special toy, trigger other affect
			CheckIfUserCatchSpecialToy(toy);
			// pull toy to center + VFX
			PlayCaptureToyEffect(toy).Forget();
			// raise event
			successCaptureToyEvent.Raise();
		}
		else {
			// throw toy and raise event
			ThrowToyAway(toy);
			failedCaptureEvent.Raise();
		}
	}
	#endregion//Collision event


	#region Private
	void SyncPositionWithPlaceHolderOnUI() {
		var placeholderUI = toyPlaceholderOnUIVar.Value;
		if(placeholderUI == null) {
			Log.Error(false, this, $"Null palceHolder pointer, no sync position");
			return;
		}
		// sync X,Y only
		var placeHolderPos = placeholderUI.transform.position;
		var targetPos = transform.position;
		targetPos.x = placeHolderPos.x;
		targetPos.y = placeHolderPos.y;
		transform.position = targetPos;
	}

	///<summary> Special toy = support toys or event toys </summary>
	void CheckIfUserCatchSpecialToy(ToyView toy) {
		// support toy
		var supportMarker = toy.GetComponent<SupportToyMarker>();
		if(supportMarker) {
			var supportToyAffectController = SimpleDI.Instance.Get<SupportToyAffectorController>();
			supportToyAffectController.TriggerAffectForSupportToy(supportMarker.supportToyID);
			return;
		}
		// event toy
		var weeklyToyMarker = toy.GetComponent<WeeklyToyMarker>();
		if(weeklyToyMarker) {
			var weeklyAffector = SimpleDI.Instance.Get<WeeklyToyAffectorController>();
			weeklyAffector.HandleCatchingWeeklyEventToy(toy, weeklyToyMarker.weeklyToyID);
			return;
		}
	}
	#endregion//Private


	#region Private - Check, Get
	void ThrowToyAway(ToyView toy) {
		// add velo, on XY only
		var direction = toy.transform.position - transform.position;
		direction.z = 0f;
		var velo = direction.normalized * pushAwayToyVeloVar.Value;
		// carefully change toy position, add offset to the catcher, so it won't behind the catcher
		var newPosForToy = toy.transform.position;
		newPosForToy.y = transform.position.y + offsetBeforeThrowingToy;
		toy.transform.position = newPosForToy;
		// NOTES: there a bug where toy drag to behind the catcher pos, adding velo will thrown toy out of view,
		// so abs to make sure Y-velo alway (+)
		velo.y = Mathf.Abs(velo.y);
		toy.mainRigid.velocity = velo;
		// carefull change layer
		toy.gameObject.layer = GameObjectUtils.FromLayerMaskToLayerIndexForGameObject(layerForToyInNormalVar.Value);
		// Vfx, offset it a bit
		var failVfx = ObjectPool.SpawnObject(throwToyAwayVfxPrefab);
		var posForVfx = transform.position;
		posForVfx += offsetForExplodeVfx;
		failVfx.transform.position = posForVfx;
	}
	#endregion//Private - check, get


	#region Private - animation
	async UniTask PlayCaptureToyEffect(ToyView toy) {
		// pull toy and destroy
		PullToyAndDestroy(toy).Forget();
		// open-close door effect
		PlayDoorOpenCloseEffect().Forget();
		// delay a bit then play VFX (TODO: will updat VFX later depend on toy type)
		await UniTask.Delay(TimeSpan.FromSeconds(pullTookToyToIdlePositionDurationVar.Value * 0.5f), cancellationToken: _disableToken.Token);
		// take exlode vfxPrefab from toyview
		var vfxPrefab = toy.explodeVfxVar.Value;
		if(vfxPrefab == null) {
			Log.Warning(false, this, $"Null explodeVfx for toy [{toy.name}], use default");
			vfxPrefab = explodeToyVfxPrefab;
		}
		// a little offset for VFX,
		var vfxPos = transform.position;
		vfxPos += offsetForExplodeVfx;
		var explodeVfx = ObjectPool.SpawnObject(vfxPrefab);
		explodeVfx.transform.position = vfxPos;
	}

	async UniTask PlayDoorOpenCloseEffect() {
		// left door, rotate -90 on Y coord, and right door rotate +90
		// Update, by defaut, they all at 0,0,0 (setup in inspector for that)
		//var leftDefaultRotate = leftDoorTrans.localEulerAngles;
		//var rightDefaultRotate = rightDoorTrans.localEulerAngles;
		var leftDefaultRotate = Vector3.zero;
		var rightDefaultRotate = Vector3.zero;

		// open
		var rotateForLeftDoor = leftDefaultRotate;
		rotateForLeftDoor.y -= 90f;
		LeanTween.rotateLocal(leftDoorTrans.gameObject, rotateForLeftDoor, pullTookToyToIdlePositionDurationVar.Value * 0.5f);
		var rotateForRightDoor = leftDefaultRotate;
		rotateForRightDoor.y += 90f;
		LeanTween.rotateLocal(rightDoorTrans.gameObject, rotateForRightDoor, pullTookToyToIdlePositionDurationVar.Value * 0.5f);

		// delay abit for the effect
		await UniTask.Delay(TimeSpan.FromSeconds(pullTookToyToIdlePositionDurationVar.Value * 0.5f), cancellationToken: _disableToken.Token);

		// close
		rotateForLeftDoor.y -= 90f;
		LeanTween.rotateLocal(leftDoorTrans.gameObject, leftDefaultRotate, pullTookToyToIdlePositionDurationVar.Value * 0.5f);
		rotateForRightDoor.y += 90f;
		LeanTween.rotateLocal(rightDoorTrans.gameObject, rightDefaultRotate, pullTookToyToIdlePositionDurationVar.Value * 0.5f);
	}

	async UniTask PullToyAndDestroy(ToyView toy) {
		// disable physics, so no more collide event
		toy.SetActivePhysics(false);
		// pull it to center, also scale down
		var targetPos = transform.position;
		targetPos.z = toy.transform.position.z;
		LeanTween.move(toy.gameObject, targetPos, pullTookToyToIdlePositionDurationVar.Value);
		LeanTween.scale(toy.gameObject, Vector3.zero, pullTookToyToIdlePositionDurationVar.Value);
		// wait abit and destroy
		await UniTask.Delay(TimeSpan.FromSeconds(pullTookToyToIdlePositionDurationVar.Value), cancellationToken: _disableToken.Token);
		Destroy(toy.gameObject);
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private - animation
}
