using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Execute logic and VFX for Magnet booster
/// </summary>
public class MagnetBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onUserRequestMagnetEvent;

	[Header("Event - out")]
	[SerializeField] GameEvent successMatchToyEvent;

	[Header("Data, ShareData")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] TransformVariable sceneGrabToyElevationVar;
	[SerializeField] ToyViewClassListVariable toyInBoxesListVar;
	//[SerializeField] TransformVariable toyCatcherTransVar;

	[Header("UserBoosterData")]
	[SerializeField] UserBoosterInventory boosterInventory;
	[SerializeField] BoosterID mangetBoosterID;

	[Header("Config")]
	[SerializeField] FloatVariable scaleForToyInsideBoxVar;
	[SerializeField] GameObject destroyToyVfxPrefab;
	[SerializeField] GameObject hintToyVfxPrefab;
	[SerializeField] float idleToyDuration = 0.5f;
	[SerializeField] float pullingToyDuration = 0.5f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onUserRequestMagnetEvent.Subscribe(OnRequestMagnet);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onUserRequestMagnetEvent.Unsubscribe(OnRequestMagnet);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	void OnRequestMagnet() {
		PullToyToCatcherAndDestroy().Forget();
	}

	async UniTask PullToyToCatcherAndDestroy() {
		var toyBoxGroup = SimpleDI.Instance.Get<ToyBoxGroup>();
		if(toyBoxGroup == null) {
			Log.Warning(false, this, $"No ToyBoxGroup to execute Undo booster");
			return;
		}

		// box sort mode: magnet fills the open box (from waiting slots first, then from the field)
		var targetBar = FindObjectOfType<TargetUIBar>();
		if(targetBar != null && targetBar.IsBoxSortMode) {
			if(!toyBoxGroup.TryMagnetFillOpenBox()) {
				boosterInventory.AddBooster(mangetBoosterID, 1);
			}
			return;
		}

		// NOTES: need to dividable by 3
		const int AMOUNT_TO_DESTROY = 3;
		var toDestroyList = toyStats.GetListOfRandomTargetToyWhichSameRootPrefab(AMOUNT_TO_DESTROY);
		if(toDestroyList == null) {
			// return the booster to user inventory (cause we deduct at boosterUI button). nothing to execute now
			boosterInventory.AddBooster(mangetBoosterID, 1);
			return;
		}

		// start pull toy to box and destroy process
		var hintList = new List<GameObject>();

		for(int n = 0; n < toDestroyList.Count; n++) {
			var toy = toDestroyList[n];
			// put select toy to high position. Add hint vfx, remove physics
			toy.mainCollider.enabled = false;
			toy.mainRigid.isKinematic = true;

			var posForToy = toy.transform.position;
			posForToy.z = sceneGrabToyElevationVar.Value.position.z;
			toy.transform.position = posForToy;

			var hint = ObjectPool.SpawnObject(hintToyVfxPrefab);
			var posForHint = posForToy;
			posForHint.z -= 1f;
			hint.transform.position = posForHint;
			hintList.Add(hint);
		}

		// destroy hint (disable => back to pool) after idle duration
		await UniTask.Delay(TimeSpan.FromSeconds(idleToyDuration), cancellationToken: _token.Token);
		for(int n = 0; n < hintList.Count; n++) {
			hintList[n].gameObject.SetActive(false);
		}

		// pulling toy to catcher
		var targetPosForToy = GetPositionToPullToyTo();
		for(int n = 0; n < toDestroyList.Count; n++) {
			var toy = toDestroyList[n];
			var scale = toy.transform.localScale * scaleForToyInsideBoxVar.Value;
			// move and scale
			LeanTween.move(toy.gameObject, targetPosForToy, pullingToyDuration);
			LeanTween.scale(toy.gameObject, scale, pullingToyDuration);
		}
		await UniTask.Delay(TimeSpan.FromSeconds(pullingToyDuration), cancellationToken: _token.Token);

		// destroy it from scene and stats
		for(int n = 0; n < toDestroyList.Count; n++) {
			var toy = toDestroyList[n];
			toyStats.RemoveToy(toy);
			Destroy(toy.gameObject);
		}

		// destroy VFX
		var destroyVfx = ObjectPool.SpawnObject(destroyToyVfxPrefab);
		destroyVfx.transform.position = targetPosForToy;

		// event
		successMatchToyEvent.Raise();
	}

	Vector3 GetPositionToPullToyTo() {
		var toyBoxGroup = SimpleDI.Instance.Get<ToyBoxGroup>();
		// find the first empty cell from right side
		var firstEmptyBoxIndex = toyInBoxesListVar.GetFirstEmptyItemIndexFromLeft();
		return toyBoxGroup.GetBoxTransformAtIndex(firstEmptyBoxIndex).position;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
