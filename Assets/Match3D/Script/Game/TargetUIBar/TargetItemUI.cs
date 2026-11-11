using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle display card for target toy on UI
/// </summary>
public class TargetItemUI : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] TMP_Text remainItemText;
	[SerializeField] Image backgroundImage;
	[SerializeField] Image thumbnailImage;
	[SerializeField] Image finishCheckerImage;

	[Header("Config")]
	[SerializeField] Sprite backfaceSprite;
	[SerializeField] Sprite frontFaceSprite;

	[Header("Config - anim")]
	[SerializeField] AnimationConfig animConfig;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// by default, disable the finish checker. Active it later 
		finishCheckerImage.enabled = false;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}

	void OnDestroy() {
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Got animation Config</summary>
	public AnimationConfig AnimConfig => animConfig;

	///<summary> Call from TargetUIBar, to setup content at very first game loaded </summary>
	public void SetupContent(GameObject rootPrefab, Sprite thumbnailSprite, List<ToyView> targetToyList) {
		if(isFailedConfig || !IsUsable())
			return;
		_rootPrefab = rootPrefab;
		_targetToyList = targetToyList;
		thumbnailImage.sprite = thumbnailSprite;
		remainItemText.text = targetToyList.Count.ToString();
		// should change it name to easier follow on Editor
		this.name = rootPrefab.name;
	}

	///<summary> Active backface without animation </summary>
	public void FaceDown() {
		if(isFailedConfig || !IsUsable())
			return;
		// disable content
		remainItemText.enabled = false;
		thumbnailImage.enabled = false;
		// show backface
		backgroundImage.sprite = backfaceSprite;
	}

	///<summary> Active backface without animation </summary>
	public void FaceUp() {
		if(isFailedConfig || !IsUsable())
			return;
		// disable content
		remainItemText.enabled = true;
		thumbnailImage.enabled = true;
		// show backface
		backgroundImage.sprite = frontFaceSprite;
	}

	public void MoveToTarget(Vector2 target) {
		if(isFailedConfig || !IsUsable())
			return;
		MoveAsync(target, _disableToken.Token).Forget();
	}

	public void PlayFaceUpAnimation() {
		if(isFailedConfig || !IsUsable())
			return;
		FlipAsync(_disableToken.Token).Forget();
	}

	public bool IsSameRootPrefab(GameObject prefab) {
		if(isFailedConfig)
			return false;
		return prefab == _rootPrefab;
	}

	public void UpdateRemainTextFollowTrackingList() {
		if(isFailedConfig || !IsUsable() || _targetToyList == null)
			return;
		remainItemText.text = _targetToyList.Count.ToString();
		// if remain toy down to zero, then disable counting text, active finsh checker
		if(_targetToyList.Count == 0) {
			remainItemText.enabled = false;
			finishCheckerImage.enabled = true;
		}
	}

	#region Public - box sort mode
	///<summary> Box capacity. 0 mean this card is NOT a box (classic target card mode) </summary>
	int _boxCapacity;
	///<summary> Toys already assigned to this box (flying or delivered). Prevent over-filling when many toys fly in at the same time </summary>
	int _boxReserved;
	///<summary> Toys reached the box </summary>
	int _boxDelivered;

	///<summary> True if this card works as a box: receive [capacity] toys of the same type then close </summary>
	public bool IsBoxMode => _boxCapacity > 0;
	///<summary> Box got enough toys </summary>
	public bool IsBoxComplete => IsBoxMode && _boxDelivered >= _boxCapacity;
	///<summary> Number of toys this box still can reserve </summary>
	public int BoxFreeSlot => IsBoxMode ? Mathf.Max(0, _boxCapacity - _boxReserved) : 0;

	///<summary> Call from TargetUIBar (box sort mode). This card will be a box which receive [capacity] toys of rootPrefab </summary>
	public void SetupAsBox(GameObject rootPrefab, Sprite thumbnailSprite, int capacity) {
		if(isFailedConfig || !IsUsable())
			return;
		_rootPrefab = rootPrefab;
		_targetToyList = null;
		_boxCapacity = Mathf.Max(1, capacity);
		_boxReserved = 0;
		_boxDelivered = 0;
		thumbnailImage.sprite = thumbnailSprite;
		remainItemText.text = _boxCapacity.ToString();
		this.name = $"Box_{rootPrefab.name}";
	}

	///<summary> Check if this box can receive a toy of [prefab] </summary>
	public bool CanReserveToy(GameObject prefab) {
		if(isFailedConfig || !IsUsable() || !IsBoxMode)
			return false;
		return prefab == _rootPrefab && _boxReserved < _boxCapacity;
	}

	///<summary> Mark 1 slot of this box is taken by a flying toy </summary>
	public void ReserveToy() {
		if(!IsBoxMode)
			return;
		_boxReserved = Mathf.Min(_boxReserved + 1, _boxCapacity);
	}

	///<summary> Call when the flying toy reach the box. Update remain text, show checker when complete </summary>
	public void DeliverToy() {
		if(!IsBoxMode)
			return;
		_boxDelivered = Mathf.Min(_boxDelivered + 1, _boxCapacity);
		if(!IsUsable())
			return;
		var remain = _boxCapacity - _boxDelivered;
		remainItemText.text = remain.ToString();
		// small punch to feel the toy dropped in
		LeanTween.cancel(gameObject);
		transform.localScale = Vector3.one;
		LeanTween.scale(gameObject, Vector3.one * 1.15f, 0.08f).setLoopPingPong(1);
		if(remain == 0) {
			remainItemText.enabled = false;
			finishCheckerImage.enabled = true;
		}
	}
	#endregion//Public - box sort mode

	public void MoveToTargetAndFaceUp(Vector2 targetAnchorPosition) {
		if(isFailedConfig || !IsUsable())
			return;
		MoveToTargetAndFaceUpAsync(targetAnchorPosition).Forget();
	}

	///<summary> Call from TargetBar when this card got ToyRemain = 0, scale down and destroy </summary>
	public void DisappearAndDestroy() {
		if(!this)
			return;
		ClearRunningTasks();
		PlayScaleDownAndAutoDestroy(_disableToken.Token).Forget();
	}
	#endregion//Public


	#region Private
	///<summary> Root prefab of the toy it display for. Be set in SetupContent() </summary>
	GameObject _rootPrefab;
	///<summary> Root prefab of the toy it display for. Be set in SetupContent() </summary>
	public GameObject RootToyPrefab => _rootPrefab;

	///<summary> The ToyList that this item is tracking. Be set in SetuPContent() </summary>
	List<ToyView> _targetToyList;
	///<summary> Remain toy count </summary>
	public int ToyRemain => _targetToyList != null ? _targetToyList.Count : 0;

	RectTransform _cacheRectTrans = null;
	RectTransform CacheRectTrans {
		get {
			if(!this)
				return null;
			if(_cacheRectTrans == null) {
				_cacheRectTrans = transform as RectTransform;
				if(_cacheRectTrans == null) {
					Log.Error(false, this, $"TargetItemUI only work with RechTransform");
				}
			}
			return _cacheRectTrans;
		}
	}

	///<summary> Start with face down, do animation flip then faceUp </summary>
	async UniTask FlipAsync(CancellationToken cancelToken) {
		try {
			if(!IsUsable())
				return;
			var timer = 0f;
			FaceDown();
			// flip, scale X to zero first
			var scale = transform.localScale;
			while(timer < animConfig.flipTime) {
				if(!IsUsable())
					return;
				timer += Time.deltaTime;
				scale.x = Easing.Tween(1f, 0f, animConfig.tweenType, timer / animConfig.flipTime);
				transform.localScale = scale;
				await UniTask.Yield(cancellationToken: cancelToken);
			}
			if(!IsUsable())
				return;
			// now active content, then scale up
			timer = 0f;
			FaceUp();
			while(timer < animConfig.flipTime) {
				if(!IsUsable())
					return;
				timer += Time.deltaTime;
				scale.x = Easing.Tween(0f, 1f, animConfig.tweenType, timer / animConfig.flipTime);
				transform.localScale = scale;
				await UniTask.Yield(cancellationToken: cancelToken);
			}
			if(!IsUsable())
				return;
			// finish
			scale.x = 1f;
			transform.localScale = scale;
		}
		catch(OperationCanceledException) {
		}
	}

	///<summary> Move to target position using default animation config </summary>
	async UniTask MoveAsync(Vector2 targetAnchorPosition, CancellationToken cancelToken) {
		try {
			if(!IsUsable() || CacheRectTrans == null)
				return;
			var timer = 0f;
			var from = CacheRectTrans.anchoredPosition;

			while(timer < animConfig.movingTime) {
				if(!IsUsable() || CacheRectTrans == null)
					return;
				timer += Time.deltaTime;
				var ratio = timer / animConfig.movingTime;
				var pos = Easing.Tween(from, targetAnchorPosition, animConfig.tweenType, ratio);
				CacheRectTrans.anchoredPosition = pos;
				await UniTask.Yield(cancellationToken: cancelToken);
			}
			if(!IsUsable() || CacheRectTrans == null)
				return;
			// finish
			CacheRectTrans.anchoredPosition = targetAnchorPosition;
		}
		catch(OperationCanceledException) {
		}
	}

	async UniTask PlayScaleDownAndAutoDestroy(CancellationToken cancelToken) {
		try {
			if(!this)
				return;
			// don't know why we got nullReferenceException in this func. so let just cache ref to gameobject for carefully
			var objectRef = gameObject;
			// delay a bit to let user saw the 0 count text
			await UniTask.Delay(TimeSpan.FromSeconds(animConfig.delayBeforeScaleDown), cancellationToken: cancelToken);
			if(objectRef == null)
				return;
			LeanTween.cancel(objectRef);
			LeanTween.scale(objectRef, Vector3.zero, animConfig.scaleDownTime);
			await UniTask.Delay(TimeSpan.FromSeconds(animConfig.scaleDownTime), cancellationToken: cancelToken);
			// then carefully check null here
			if(objectRef != null)
				Destroy(objectRef);
		}
		catch(OperationCanceledException) {
		}
		catch(Exception e) {
			Log.Warning(false, $"Exception in PlayScaleDownAndAutoDestroy(). [{e.Message}]");
		}
	}

	async UniTask MoveToTargetAndFaceUpAsync(Vector2 targetAnchorPosition) {
		try {
			await MoveAsync(targetAnchorPosition, _disableToken.Token);
			if(IsUsable()) {
				FlipAsync(_disableToken.Token).Forget();
			}
		}
		catch(OperationCanceledException) {
		}
	}

	bool IsUsable() {
		return this != null
			&& gameObject != null
			&& remainItemText != null
			&& backgroundImage != null
			&& thumbnailImage != null
			&& finishCheckerImage != null;
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	public class AnimationConfig {
		public float flipTime = 0.15f;
		public float movingTime = 0.3f;
		public float delayBeforeScaleDown = 0.2f;
		public float scaleDownTime = 0.15f;
		public Easing.TweenType tweenType;

		public float DestroyDuration => delayBeforeScaleDown + scaleDownTime;
	}
	#endregion//Classes
}
