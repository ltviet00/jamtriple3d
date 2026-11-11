using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Show target toy cards on header UI
/// </summary>
public class TargetUIBar : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Transform rootItemTrans;
	[SerializeField] GameObject itemPrefab;

	[Header("Config")]
	[SerializeField] float itemDistance = 270f;
	[SerializeField] float farLeftItemPosition = -450f;
	[SerializeField] AnimationConfig animConfig;

	[Header("ShareData")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] ToyThumbnailStorage thumbnailStorage;
	[SerializeField] IntegerVariable maxConcurrentTargetCfg;

	[Header("Event in")]
	[SerializeField] GameEvent finishSpawningToyEvent;

	[Header("Event out")]
	[SerializeField] GameEvent targetClearEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
		RegisterToyStatsEvent();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
		UnRegisterToyStatsEvent();
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Called from Mainflow, when finsh toy spawning, get data from LevelToyStats to generate target bar </summary>
	public async UniTask Init(CancellationToken cancelToken) {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
		_cardStack.Clear();
		_activeCards.Clear();
		_toDestroyCards.Clear();
		_queuedDestroyCards.Clear();
		var targetMap = toyStats.TargetMap;
		var beginPostition = GetAnchorPositionForCardAtIndex(0);
		// spawn card, add to stack first
		foreach(var target in targetMap) {
			// retrieve data for item
			var rootPrefab = target.Key;
			var toyList = target.Value;
			var thumbnail = thumbnailStorage.GetThumbnail(rootPrefab);
			// spawn item, save to list
			var item = Instantiate(itemPrefab, rootItemTrans).GetComponent<TargetItemUI>();
			_cardStack.Push(item);
			item.SetupContent(rootPrefab, thumbnail, toyList);
			// let them default face down first, place at first card position
			item.FaceDown();
			(item.transform as RectTransform).anchoredPosition = beginPostition;
		}

		// move from stack to activeList. First update to stats
		while(_cardStack.Count > 0 && _activeCards.Count < maxConcurrentTargetCfg.Value) {
			_activeCards.Add(_cardStack.Pop());
		}
		UpdateAllowTargetToToyStats();

		// animation : span
		for(int n = _activeCards.Count - 1; n >= 0; n--) {
			var item = _activeCards[n];
			var pos = GetAnchorPositionForCardAtIndex(n);
			item.MoveToTarget(pos);
			// wait a bit between item
			await UniTask.Delay(TimeSpan.FromSeconds(animConfig.delayMoveBetweencard), cancellationToken: cancelToken);
		}
		// animation : flip
		for(int n = _activeCards.Count - 1; n >= 0; n--) {
			var item = _activeCards[n];
			item.PlayFaceUpAnimation();
			// wait a bit between item
			await UniTask.Delay(TimeSpan.FromSeconds(animConfig.delayFlipBetweencard), cancellationToken: cancelToken);
		}
	}
	#endregion//Public


	#region Events - Target changes
	void RegisterToyStatsEvent() {
		toyStats.OnTargetRemainChangedHandler += OnTargeRemainChanged;
	}

	void UnRegisterToyStatsEvent() {
		toyStats.OnTargetRemainChangedHandler -= OnTargeRemainChanged;
	}

	void OnTargeRemainChanged(int totalRemain) {
		TargetItemUI toDestroyCard = null;
		// all item list got ref to the list they're tracking, just call update
		for(int n = 0; n < _activeCards.Count; n++) {
			var item = _activeCards[n];
			if(item == null)
				continue;
			item.UpdateRemainTextFollowTrackingList();
			if(item.ToyRemain == 0) {
				toDestroyCard = item;
			}
		}
		// check collapse item that finish, and expand item (if stack not empty)
		if(toDestroyCard != null) {
			//DestroyCardAndArrangeRemainList(toDestroyCard).Forget();
			if(_queuedDestroyCards.Add(toDestroyCard)) {
				_toDestroyCards.Enqueue(toDestroyCard);
			}
		}
	}
	#endregion//Events - Target changes


	#region Private
	///<summary> All cards here, if we got card over the max capacity, then this stack will hold some. And expand when user finish any cards </summary>
	Stack<TargetItemUI> _cardStack = new Stack<TargetItemUI>();
	///<summary> Current displaying cards </summary>
	List<TargetItemUI> _activeCards = new List<TargetItemUI>();
	///<summary> for race condition fixing. If we are showing animation  DestroyCard, and user quickly drag more toy, it might cause error.
	///So add the destroy cards to the list, the async function will work on this</summary>
	Queue<TargetItemUI> _toDestroyCards = new Queue<TargetItemUI>();
	HashSet<TargetItemUI> _queuedDestroyCards = new HashSet<TargetItemUI>();
	UniTask _destroyCardAndArrangeUITask;

	void Update() {
		// when card to destroy > 0 && no DestroyCard task are running ,then run new task. This to fix race condition
		if(_toDestroyCards.Count > 0 && _destroyCardAndArrangeUITask.Status == UniTaskStatus.Succeeded) {
			var card = _toDestroyCards.Dequeue();
			_destroyCardAndArrangeUITask = DestroyCardAndArrangeRemainList(card);
		}
	}

	///<summary> If stack not empty, then we keep 1 facedown card on left, then increase distance for other card. Else, stack empty? then no facedown card </summary>
	Vector2 GetAnchorPositionForCardAtIndex(int index) {
		var result = Vector2.zero;
		if(_cardStack.Count > 0) {
			index++;
		}
		result.x = farLeftItemPosition + itemDistance * index;
		return result;
	}

	///<summary> Only allow toy in activeCard list to be picked. Update to the ToyStats </summary>
	void UpdateAllowTargetToToyStats() {
		var toySet = new HashSet<GameObject>();
		for(int n = 0; n < _activeCards.Count; n++) {
			if(_activeCards[n] != null && _activeCards[n].RootToyPrefab != null) {
				toySet.Add(_activeCards[n].RootToyPrefab);
			}
		}
		toyStats.SetAllowTargetToyMap(toySet);
	}

	async UniTask DestroyCardAndArrangeRemainList(TargetItemUI toDestroy) {
		try {
			if(toDestroy == null)
				return;
			// find index of destroy Card in the active list
			var indexOfDestroyCard = -1;
			for(int n = 0; n < _activeCards.Count; n++) {
				if(_activeCards[n] == toDestroy) {
					indexOfDestroyCard = n;
					break;
				}
			}
			if(indexOfDestroyCard < 0)
				return;
			// destroy it and wait for animation
			toDestroy.DisappearAndDestroy();
			await UniTask.Delay(TimeSpan.FromSeconds(toDestroy.AnimConfig.DestroyDuration), cancellationToken: _disableToken.Token);
			// if stack has more than 1, then pick one and fill to the missing place
			if(_cardStack.Count > 1) {
				var replacement = _cardStack.Pop();
				_activeCards[indexOfDestroyCard] = replacement;
				var positionForReplacement = GetAnchorPositionForCardAtIndex(indexOfDestroyCard);
				replacement.MoveToTargetAndFaceUp(positionForReplacement);
				// update for stats
				UpdateAllowTargetToToyStats();
				return;
			}
			// stack only got 1 cards left. Then face up that card ,add that card to list, and shift remain cards (from the destroy one) to the left 1 index
			if(_cardStack.Count == 1) {
				// shift remain cards to left
				for(int n = indexOfDestroyCard + 1; n < _activeCards.Count; n++) {
					var card = _activeCards[n];
					if(card == null)
						continue;
					var posForCard = GetAnchorPositionForCardAtIndex(n - 1);
					card.MoveToTarget(posForCard);
					// assign index of it in activeCard
					_activeCards[n - 1] = card;
				}
				// faceup the last card in stack, let it the fist one in active list
				var lastCard = _cardStack.Pop();
				lastCard.PlayFaceUpAnimation();
				// add last Card to active list. And reorder the list
				_activeCards[_activeCards.Count - 1] = lastCard;
				_activeCards.BringLastToFirst();
				// update for stats
				UpdateAllowTargetToToyStats();
				return;
			}
			// no card remain in stack, just these cards only, then shift cards back
			// call removeAt to let the list to reduce count, arrange array (
			_activeCards.RemoveAt(indexOfDestroyCard);
			// after removeAtIndex, cards on right side of destroyed one already at right index in array, just need to move them in visual
			for(int n = indexOfDestroyCard; n < _activeCards.Count; n++) {
				var card = _activeCards[n];
				if(card == null)
					continue;
				var posForCard = GetAnchorPositionForCardAtIndex(n);
				card.MoveToTarget(posForCard);
			}
			// update for stats
			UpdateAllowTargetToToyStats();
		}
		catch(OperationCanceledException) {
		}
		finally {
			_queuedDestroyCards.Remove(toDestroy);
		}
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
		_queuedDestroyCards.Clear();
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	public class AnimationConfig {
		public float delayFlipBetweencard;
		public float delayMoveBetweencard;
	}
	#endregion//Classes
}
