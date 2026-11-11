using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// New script for Triple match game. Handle input and pull toy to ToyBox position.
/// - listen to userPickToy -> check bring that toy to toyGroup
/// - play fly animation for picked toy to toyGroup
/// - animation merging toy. Shift toy when need
/// - event out when ToyBoxFull
/// </summary>
public class ToyBoxGroup : MonoBehaviour {
	#region Init, config
	[Header("ShareData - in")]
	[SerializeField] ToyViewVariable userPickToyVar;

	[Header("ShareData - out")]
	//[SerializeField] Vector3Variable positionToSpawnSuccessMatchingVfxVar;
	[SerializeField] ToyViewClassListVariable toyInBoxesListVar;
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Event out")]
	[SerializeField] GameEvent onToyBoxFullEvent;
	[SerializeField] GameEvent successMatchToyEvent;
	[SerializeField] GameEvent captureSpecialToyEvent;

	[Header("Input Command")]
	[SerializeField] GameEvent disableInputCmd;
	[SerializeField] GameEvent enableInputCmd;

	[Header("Refs, config - toyBox center")]
	[SerializeField] List<Transform> toyBoxTransformList;
	[SerializeField] float offsetFromToyToBox = 1f;

	[Header("Config")]
	[SerializeField] AnimationConfig animConfig;

	[Header("VFX")]
	[SerializeField] GameObject vfxNormalToyExplodePrefab;
	[SerializeField] GameObject vfxSupportToyExplodePrefab;
	[SerializeField] GameObject vfxWeeklyToyExplodePrefab;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
		userPickToyVar.OnValueChangedHandler += OnUserPickToy;
		// create an empty list of toyView
		toyInBoxesListVar.Clear();
		toyInBoxesListVar.Value.Capacity = Capacity;
		for(int n = 0; n < Capacity; n++) {
			toyInBoxesListVar.Value.Add(null);
		}
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
		userPickToyVar.OnValueChangedHandler -= OnUserPickToy;
		ClearRunningTasks();
		toyInBoxesListVar.Clear();
	}
	#endregion// Init config


	#region Public
	///<summary> Capacity of the toyBoxGroup, currently it's the length of toyBoxTransformList </summary>
	public int Capacity => toyBoxTransformList.Count;
	///<summary> Raise when insert a toy to group (and not make any triple). currently use for lowSpaceNotification </summary>
	public event Action OnInsertToyToBoxHandler;

	///<summary> Move toy to Box take time, check this to know if we're doing such a task, to now affect to the toyList </summary>
	public bool IsRunningMoveToyTask() {
		if(isFailedConfig)
			return false;
		return _moveToyTask.Status == UniTaskStatus.Pending;
	}

	public Transform GetBoxTransformAtIndex(int index) {
		if(isFailedConfig)
			return null;
		// check clamp the index
		if(index < 0 || index >= Capacity) {
			index = Mathf.Clamp(index, 0, Capacity);
			Log.Warning(false, this, $"Request boxIndex out of range [{index}], clamped");
		}
		return toyBoxTransformList[index];
	}

	///<summary> Force move the select toy to box. Currently use for Undo booster tut </summary>
	public void TakeToyToBox(ToyView toy) {
		if(isFailedConfig)
			return;
		OnUserPickToy(toy);
	}
	#endregion//Public


	#region Event in
	void OnUserPickToy(ToyView toy) {
		_moveToyTask = MoveToyAndCheckMatchTripleOrSpecialToy(toy);
	}
	#endregion//Event in


	#region Private
	///<summary> Cache current moveToyTask to know if we're running a move toy task or not  </summary>
	UniTask _moveToyTask;

	async UniTask MoveToyAndCheckMatchTripleOrSpecialToy(ToyView toy) {
		// check full toy. Do nothing
		if(IsAllBoxesAreFulled()) {
			Log.Warning(false, this, $"All box are full, ignore this pick toy");
			return;
		}
		//TODO: check special toy : support toy, weekly toy, to just move and destroy them
		if(CheckHandleIfUserCatchSpecialToy(toy)) {
			return;
		}

		// first pick toy
		if(IsAllBoxesAreEmpty()) {
			await PullToyToBoxIndex(toy, 0);
			// Update UX: we enableInpt after pull the toy to box, before do checking match triple
			//enableInputCmd.Raise();
			// all box are empty, then no need to check matching
			//await CheckTripleMatchingAndCheckRaiseToyBoxFullEvent().AttachExternalCancellation(_cancelToken.Token);
			return;
		}
		// not the first one? check if we got same toys on bar
		var nearestSameToyBoxIndex = FindLastBoxIndexHasSameToy(toy);
		if(nearestSameToyBoxIndex >= 0) {
			await PullAndInsertToyToBoxBar(toy, nearestSameToyBoxIndex);
			// Update UX: we enableInpt after pull the toy to box, before do checking match triple
			//enableInputCmd.Raise();
			await CheckTripleMatchingAndCheckRaiseToyBoxFullEvent();
			return;
		}
		// no same toy on any box? then pull it to the last empty box
		var firstEmptyBoxIndex = GetFirstEmptyBoxIndex();
		if(firstEmptyBoxIndex >= 0) {
			await PullToyToBoxIndex(toy, firstEmptyBoxIndex);
			// Update UX: we enableInpt after pull the toy to box, before do checking match triple
			//enableInputCmd.Raise();
			await CheckTripleMatchingAndCheckRaiseToyBoxFullEvent();
			return;
		}
		// ouch, not handle this case
		Log.Warning(false, this, $"Not handle this pick toy case yet");
		enableInputCmd.Raise();
		// not full yet, pull toy to box
		//PullToyToBoxAndCheckTripleMatch(toy).AttachExternalCancellation(_cancelToken.Token);
	}

	async UniTask PullToyToBoxIndex(ToyView toy, int boxIndex) {
		if(toyInBoxesListVar[boxIndex] != null) {
			Log.Error(false, this, $"Notice: boxIndex has toy, override?");
			return;
		}
		// disable input first, don't let user do anything when full
		disableInputCmd.Raise();
		// disable physics of the toy
		toy.SetActivePhysics(false);
		// asign toy to box before animation
		toyInBoxesListVar.Value[boxIndex] = toy;
		// also remove the toy from active list
		toyStats.RemoveToy(toy);
		// pull toy to box (also rotate to idle rotation),
		MoveToyToBoxScaleDownAndRotateToDefault(toy, boxIndex);
		await UniTask.Delay(TimeSpan.FromSeconds(animConfig.moveToyToBoxDuration), cancellationToken: _cancelToken.Token);
		// and release input.OnInsertToyToBoxHandler?.Invoke();
		enableInputCmd.Raise();
		OnInsertToyToBoxHandler?.Invoke();
	}

	async UniTask PullAndInsertToyToBoxBar(ToyView toy, int boxIndexHasSameToy) {
		// disable input first, don't let user do anything when full
		disableInputCmd.Raise();
		// disable physics of the toy
		toy.SetActivePhysics(false);
		// we will pull toy to aside the box, check if if empty or not
		var nextToBoxIndex = boxIndexHasSameToy + 1;
		if(nextToBoxIndex >= Capacity) {
			Log.Error(false, this, $"boxHasSameToyIndex = [{boxIndexHasSameToy}], lead to out of range");
			return;
		}
		// shift toy on the right side to 1 cell
		for(int n = Capacity - 1; n >= nextToBoxIndex; n--) {
			var checkToy = toyInBoxesListVar[n];
			if(checkToy != null) {
				// NOTES: should clear exist animations on the toy, we got the bug that 2 toy overlap, may because the "shift toy to left" function
				LeanTween.cancel(checkToy.gameObject);
				LeanTween.move(checkToy.gameObject, GetPositionOfToyBoxIndex(n + 1), animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
				toyInBoxesListVar.Value[n + 1] = checkToy;
			}
		}
		// pull toy to box (also rotate to idle rotation), assign to array and wait for animation
		MoveToyToBoxScaleDownAndRotateToDefault(toy, nextToBoxIndex);
		toyInBoxesListVar.Value[nextToBoxIndex] = toy;
		// also remove the toy from active list
		toyStats.RemoveToy(toy);
		await UniTask.Delay(TimeSpan.FromSeconds(animConfig.moveToyToBoxDuration), cancellationToken: _cancelToken.Token);
		// and release input.
		enableInputCmd.Raise();
	}

	///<summary> Check tripple matching, also check if toybox was fulled </summary>
	async UniTask CheckTripleMatchingAndCheckRaiseToyBoxFullEvent() {
		var successMatching = await CheckMatchTripleToyAndDoMerging();
		// check if all toyBox are full each time we pull toy to box to raise event. If user not make a match, then check if toybox was full
		if(!successMatching) {
			CheckRaiseFullToyboxEvent();
			// not matching mean insert new toy, then raise evet
			OnInsertToyToBoxHandler?.Invoke();
		}
	}

	///<summary> Check matching toy, move matched toy together, boom affect. Return true if find a match, false if not </summary>
	async UniTask<bool> CheckMatchTripleToyAndDoMerging() {
		// check from left to right, got enough 3 toys in a row, then merge and explode the
		var sameToyList = new List<ToyView>();
		// if we got a match3, we need to shift all the toy on right side to first pos, save to here
		var boxIndexOfFirstToyInMatchThree = 0;
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			var toy = toyInBoxesListVar[n];
			// meet empty box, then break
			if(toy == null) {
				return false;
			}
			// toy list empty? then just add to it and continue
			if(sameToyList.Count == 0) {
				sameToyList.Add(toy);
				boxIndexOfFirstToyInMatchThree = n;
				continue;
			}
			// toy list got 1 (or some) toy in it, check it it same to the first one, then add to the list
			if(sameToyList[0].rootPrefab == toy.rootPrefab) {
				sameToyList.Add(toy);
				// full 3 toy? break,
				if(sameToyList.Count == 3) {
					break;
				}
				continue;
			}
			// not same to exist toy in list? then clear list, also add the different one to the list
			else {
				sameToyList.Clear();
				sameToyList.Add(toy);
				boxIndexOfFirstToyInMatchThree = n;
			}
		}
		// check enough triple toys and do merging
		if(sameToyList.Count == 3) {
			// clear toyList from toybox, the only waiting for being destroyed
			for(int n = boxIndexOfFirstToyInMatchThree; n < boxIndexOfFirstToyInMatchThree + 3; n++) {
				toyInBoxesListVar.Value[n] = null;
			}
			// UPDATE: we updat the toyInBox list right here, before do animatiing.
			// Cause we will enable input after the toy reach the toybox, so update index logic first, before do animating
			var indexOfFirstRemainToy = boxIndexOfFirstToyInMatchThree + 3;
			var numberOfRemainToy = 0;
			// re-asign them to the main list
			if(indexOfFirstRemainToy < Capacity) {
				for(int n = indexOfFirstRemainToy; n < Capacity; n++) {
					var toy = toyInBoxesListVar.Value[n];
					// check null to break
					if(toy == null) {
						break;
					}
					// re-asign in box
					toyInBoxesListVar.Value[n] = null;
					toyInBoxesListVar.Value[n - 3] = toy;
					// increase number of remain toy
					numberOfRemainToy++;
				}
			}
			// after above step, the remain toy still visible on the group (not move their model yet)
			// but the index of them in indexList was changed. So mark the indexOfFirstRemainToy, for doing animating step later
			indexOfFirstRemainToy = boxIndexOfFirstToyInMatchThree;

			// FIrst animating: move the 1st and 3rd of matching triple to the center
			var centerToyPos = sameToyList[1].transform.position;
			LeanTween.move(sameToyList[0].gameObject, centerToyPos, animConfig.mergeToyDuration).setEase(animConfig.mergeTweenType);
			LeanTween.move(sameToyList[2].gameObject, centerToyPos, animConfig.mergeToyDuration).setEase(animConfig.mergeTweenType);
			//// save to shareVar for the VFX module
			//positionToSpawnSuccessMatchingVfxVar.Value = centerToyPos;

			// wait for the animation then destroy these toys
			await UniTask.Delay(TimeSpan.FromSeconds(animConfig.mergeToyDuration), cancellationToken: _cancelToken.Token);
			for(int n = 0; n < sameToyList.Count; n++) {
				Destroy(sameToyList[n].gameObject);
			}
			// shift the remain toys on the right to the left
			//if(indexOfFirstRemainToy <= capacity) {
			for(int n = indexOfFirstRemainToy; n < indexOfFirstRemainToy + numberOfRemainToy; n++) {
				var toy = toyInBoxesListVar.Value[n];
				// check null to break
				//if(toy == null) {
				//	break;
				//}
				// now the moving step. Cause we already re-assign index for toy, so no need to "-3" here anymore
				//var targetBoxPos = GetPositionOfToyBoxIndex(n - 3);
				var targetBoxPos = GetPositionOfToyBoxIndex(n);
				LeanTween.move(toy.gameObject, targetBoxPos, animConfig.mergeToyDuration).setEase(animConfig.moveTweenType);
			}
			//}
			// VFX ? then event
			var vfx = ObjectPool.SpawnObject(vfxNormalToyExplodePrefab);
			vfx.transform.position = centerToyPos;
			successMatchToyEvent.Raise();
			// result, ofcourse true
			return true;
		}
		// reach here mean not found a match
		return false;
	}
	#endregion//Private


	#region Handle special toy
	///<summary> Special toy = support toys or event toys. Return true if this toy is special </summary>
	bool CheckHandleIfUserCatchSpecialToy(ToyView toy) {
		// support toy
		var supportMarker = toy.GetComponent<SupportToyMarker>();
		if(supportMarker) {
			PullSupportToyAsync(toy, supportMarker.supportToyID).Forget();
			return true;
		}
		// event toy
		var weeklyToyMarker = toy.GetComponent<WeeklyToyMarker>();
		if(weeklyToyMarker) {
			PullWeeklyToyAsync(toy, weeklyToyMarker.weeklyToyID).Forget();
			return true;
		}
		// reach here mean not special toy
		return false;
	}

	async UniTask PullSupportToyAsync(ToyView toy, SupportToyID supportToyID) {
		// just need the position to pull toy to, no replace to the toyList array
		var firstEmptyBoxIndex = GetFirstEmptyBoxIndex();
		var targetScale = toy.transform.localScale * animConfig.scaleForToyInsideBoxVar.Value;
		// disable physics of the toy, remove from stats
		toy.SetActivePhysics(false);
		toyStats.RemoveToy(toy);
		// move toy to box, wait for duration and destroy
		LeanTween.move(toy.gameObject, GetPositionOfToyBoxIndex(firstEmptyBoxIndex), animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
		LeanTween.scale(toy.gameObject, targetScale, animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
		await UniTask.Delay(TimeSpan.FromSeconds(animConfig.moveToyToBoxDuration), cancellationToken: _cancelToken.Token);
		// explode, VFX
		Destroy(toy.gameObject);
		var vfx = ObjectPool.SpawnObject(vfxSupportToyExplodePrefab);
		vfx.transform.position = toy.transform.position;
		// trigger affect
		var supportToyAffectController = SimpleDI.Instance.Get<SupportToyAffectorController>();
		supportToyAffectController.TriggerAffectForSupportToy(supportToyID);
		// event
		captureSpecialToyEvent.Raise();
	}

	async UniTask PullWeeklyToyAsync(ToyView toy, WeeklyToyID weeklyToyID) {
		// just need the position to pull toy to, no replace to the toyList array
		var firstEmptyBoxIndex = GetFirstEmptyBoxIndex();
		var targetScale = toy.transform.localScale * animConfig.scaleForToyInsideBoxVar.Value;
		// disable physics of the toy, remove from stats
		toy.SetActivePhysics(false);
		toyStats.RemoveToy(toy);
		// move toy to box, wait for duration and destroy
		LeanTween.move(toy.gameObject, GetPositionOfToyBoxIndex(firstEmptyBoxIndex), animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
		LeanTween.scale(toy.gameObject, targetScale, animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
		await UniTask.Delay(TimeSpan.FromSeconds(animConfig.moveToyToBoxDuration), cancellationToken: _cancelToken.Token);
		// explode, VFX
		Destroy(toy.gameObject);
		var vfx = ObjectPool.SpawnObject(vfxWeeklyToyExplodePrefab);
		vfx.transform.position = toy.transform.position;
		// trigger affect
		var weeklyAffector = SimpleDI.Instance.Get<WeeklyToyAffectorController>();
		weeklyAffector.HandleCatchingWeeklyEventToy(toy, weeklyToyID);
		// event
		captureSpecialToyEvent.Raise();
	}
	#endregion//Handle support toy


	#region Utils
	///<summary> Find the empty position in toyInBoxes </summary>
	int GetFirstEmptyBoxIndex() {
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			if(toyInBoxesListVar.Value[n] == null)
				return n;
		}
		Log.Warning(false, this, $"Not found any null box");
		return -1;
	}

	///<summary> We alway shift toy to the left side of toyGroup, so first box empty mean the whole bar was empty </summary>
	bool IsAllBoxesAreEmpty() {
		return toyInBoxesListVar.Value[0] == null;
	}

	bool IsAllBoxesAreFulled() {
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			if(toyInBoxesListVar.Value[n] == null)
				return false;
		}
		return true;
	}

	///<summary> calculate position relate to the outside left box. Note: index start from 0 </summary>
	Vector3 GetPositionOfToyBoxIndex(int boxIndex) {
		if(boxIndex < 0 || boxIndex >= toyBoxTransformList.Count) {
			Log.Warning(false, this, $"GetPositionOfToyBoxIndex(). out of range [{boxIndex}], clamp");
			boxIndex = Mathf.Clamp(boxIndex, 0, toyBoxTransformList.Count - 1);
		}
		// add offset to the toy, Z coord
		var result = toyBoxTransformList[boxIndex].position;
		result.z -= offsetFromToyToBox;
		return result;
	}

	int GetEmptyBoxRemainCount() {
		var result = 0;
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			if(toyInBoxesListVar.Value[n] == null)
				result++;
		}
		return result;
	}

	int FindLastBoxIndexHasSameToy(ToyView toy) {
		List<int> sameToyBoxIndexList = new List<int>();
		for(int n = 0; n < toyInBoxesListVar.Count; n++) {
			var check = toyInBoxesListVar.Value[n];
			if(check != null && check != toy && check.rootPrefab == toy.rootPrefab)
				sameToyBoxIndexList.Add(n);
		}
		if(sameToyBoxIndexList.Count > 0) {
			return sameToyBoxIndexList[sameToyBoxIndexList.Count - 1];
		}
		// reach here mean not found
		return -1;
	}

	bool IsBoxNotNullAndSameToy(int boxIndex, ToyView toy) {
		if(toyInBoxesListVar.Value[boxIndex] == null) {
			return false;
		}
		return toyInBoxesListVar.Value[boxIndex].rootPrefab == toy.rootPrefab;
	}

	void MoveToyToBoxScaleDownAndRotateToDefault(ToyView toy, int boxIndex) {
		var targetScale = toy.transform.localScale * animConfig.scaleForToyInsideBoxVar.Value;
		LeanTween.move(toy.gameObject, GetPositionOfToyBoxIndex(boxIndex), animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
		LeanTween.scale(toy.gameObject, targetScale, animConfig.moveToyToBoxDuration).setEase(animConfig.moveTweenType);
		LeanTween.rotate(toy.gameObject, animConfig.toyRotationWhenIdle, animConfig.moveToyToBoxDuration);
		toy.AutoRotate(animConfig.rotateInBoxSpeed);
	}

	void CheckRaiseFullToyboxEvent() {
		if(IsAllBoxesAreFulled())
			onToyBoxFullEvent.Raise();
	}
	#endregion//Utils


	#region Cancel token
	UniTaskTokenHelper _cancelToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_cancelToken.ClearRunningTasks();
	}
	#endregion//Cancel token


	#region Classes
	[System.Serializable]
	class AnimationConfig {
		[Header("Move")]
		public float moveToyToBoxDuration = 0.2f;
		public LeanTweenType moveTweenType;

		[Header("Merge")]
		public float mergeToyDuration = 0.2f;
		public LeanTweenType mergeTweenType;

		[Header("Scale, rotate")]
		public Vector3 toyRotationWhenIdle;
		public FloatVariable scaleForToyInsideBoxVar;
		public Vector3 rotateInBoxSpeed;
	}
	#endregion//Classes
}
