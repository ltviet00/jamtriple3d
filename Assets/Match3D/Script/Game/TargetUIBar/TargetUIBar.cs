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

	[Header("Box sort mode")]
	[Tooltip("ON: each card is a box receiving [toyPerBox] toys of the same type, matching toys fly straight into the box, others wait in the ToyBoxGroup slots. OFF: classic triple-match target cards")]
	[SerializeField] bool boxSortMode = true;
	[SerializeField] int toyPerBox = 3;
	[Tooltip("Box sort mode: number of boxes open at the same time (reference video: 2 open + 1 face down waiting). 0 = use maxConcurrentTargetCfg")]
	[SerializeField] int openBoxCount = 2;
	[Tooltip("Box sort mode: distance between 2 crates (crates are bigger than cards)")]
	[SerializeField] float boxItemDistance = 320f;
	[Tooltip("Box sort mode: Extra Box booster opens the waiting crate, at most this number at the same time. The row goes back to normal when a box completes")]
	[SerializeField] int maxExtraOpenBoxes = 1;

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
	///<summary> Box sort mode is active </summary>
	public bool IsBoxSortMode => boxSortMode && !isFailedConfig;
	///<summary> Box sort mode: raised when the open boxes changed (a box closed, new box opened) </summary>
	public event Action OnActiveBoxesChanged;

	///<summary> Box sort mode: find an open box that can receive this toy type, reserve 1 slot and return it. Null if no box accept </summary>
	public TargetItemUI ReserveBoxForToy(GameObject rootPrefab) {
		if(!IsBoxSortMode || rootPrefab == null)
			return null;
		for(int n = 0; n < _activeCards.Count; n++) {
			var card = _activeCards[n];
			if(card == null || _queuedDestroyCards.Contains(card))
				continue;
			if(card.CanReserveToy(rootPrefab)) {
				card.ReserveToy();
				return card;
			}
		}
		return null;
	}

	///<summary> Box sort mode: return first open box which still has free slot (for magnet booster) </summary>
	public TargetItemUI GetFirstOpenBoxHasFreeSlot() {
		if(!IsBoxSortMode)
			return null;
		for(int n = 0; n < _activeCards.Count; n++) {
			var card = _activeCards[n];
			if(card != null && !_queuedDestroyCards.Contains(card) && card.BoxFreeSlot > 0)
				return card;
		}
		return null;
	}

	///<summary> Box sort mode: call when a toy reached the box. Return true if the box just got completed </summary>
	public bool NotifyToyDeliveredToBox(TargetItemUI box) {
		if(!IsBoxSortMode || box == null)
			return false;
		box.DeliverToy();
		if(box.IsBoxComplete) {
			if(_queuedDestroyCards.Add(box)) {
				_toDestroyCards.Enqueue(box);
			}
			return true;
		}
		return false;
	}

	///<summary> Box sort mode, Extra Box booster: open the waiting crate as one more open box. False if nothing to open (refund booster) </summary>
	public bool TryOpenExtraBox() {
		if(!IsBoxSortMode || openBoxCount <= 0 || _extraOpenBoxes >= maxExtraOpenBoxes || _cardStack.Count == 0)
			return false;
		_extraOpenBoxes++;
		var entering = PopNextCard();
		_activeCards.Insert(0, entering);
		// all open crates are lit, no waiting crate shown until a box completes
		SetWaitingCratesVisible(false);
		for(int n = 0; n < _activeCards.Count; n++) {
			var card = _activeCards[n];
			if(card == null || _queuedDestroyCards.Contains(card))
				continue;
			var pos = GetAnchorPositionForCardAtIndex(n);
			if(card == entering)
				card.MoveToTargetAndFaceUp(pos);
			else
				card.MoveToTarget(pos);
		}
		UpdateAllowTargetToToyStats();
		return true;
	}

	///<summary> Called from Mainflow, when finsh toy spawning, get data from LevelToyStats to generate target bar </summary>
	public async UniTask Init(CancellationToken cancelToken) {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
		_cardStack.Clear();
		_activeCards.Clear();
		_toDestroyCards.Clear();
		_queuedDestroyCards.Clear();
		_extraOpenBoxes = 0;
		var targetMap = toyStats.TargetMap;
		var beginPostition = GetAnchorPositionForCardAtIndex(0);
		// box sort mode: split each toy type into boxes
		if(IsBoxSortMode) {
			SpawnBoxCards(targetMap, beginPostition);
		}
		// spawn card, add to stack first
		else foreach(var target in targetMap) {
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
		while(_cardStack.Count > 0 && _activeCards.Count < MaxActiveCards) {
			_activeCards.Add(PopNextCard());
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
		// box sort mode: box progress is updated by NotifyToyDeliveredToBox(), not by the remain toy list
		if(IsBoxSortMode)
			return;
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
		if(ShowWaitingSlot) {
			index++;
		}
		// box sort mode: few boxes, center the row (open boxes + 1 face down slot)
		var isCrateRow = IsBoxSortMode && openBoxCount > 0;
		var distance = isCrateRow ? boxItemDistance : itemDistance;
		var left = farLeftItemPosition;
		if(isCrateRow) {
			// stack not empty (or init): open crates + 1 face down crate. Else only the remain open crates, centered
			var slotCount = ShowWaitingSlot || _activeCards.Count == 0 ? MaxActiveCards + 1 : _activeCards.Count;
			left = -distance * (slotCount - 1) * 0.5f;
		}
		result.x = left + distance * index;
		return result;
	}

	///<summary> Only allow toy in activeCard list to be picked. Update to the ToyStats </summary>
	void UpdateAllowTargetToToyStats() {
		var toySet = new HashSet<GameObject>();
		// box sort mode: every target toy is pickable, toy not match any open box will wait in the slots
		if(IsBoxSortMode) {
			foreach(var target in toyStats.TargetMap) {
				toySet.Add(target.Key);
			}
			toyStats.SetAllowTargetToyMap(toySet);
			OnActiveBoxesChanged?.Invoke();
			return;
		}
		for(int n = 0; n < _activeCards.Count; n++) {
			if(_activeCards[n] != null && _activeCards[n].RootToyPrefab != null) {
				toySet.Add(_activeCards[n].RootToyPrefab);
			}
		}
		toyStats.SetAllowTargetToyMap(toySet);
	}

	async UniTask DestroyCardAndArrangeRemainList(TargetItemUI toDestroy) {
		if(IsBoxSortMode && openBoxCount > 0) {
			await DestroyCrateAndShiftConveyor(toDestroy);
			return;
		}
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
				var replacement = PopNextCard();
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
				RealignCratesWhenStackEmpty();
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
			RealignCratesWhenStackEmpty();
			// update for stats
			UpdateAllowTargetToToyStats();
		}
		catch(OperationCanceledException) {
		}
		finally {
			_queuedDestroyCards.Remove(toDestroy);
		}
	}

	///<summary> Box sort mode (reference video): crates stand on a conveyor, waiting crate on the left.
	/// A crate completed -> it flies away, every crate on its left shift 1 place to the right, the waiting crate opens, next waiting crate shows up on the left </summary>
	async UniTask DestroyCrateAndShiftConveyor(TargetItemUI toDestroy) {
		try {
			if(toDestroy == null)
				return;
			var index = _activeCards.IndexOf(toDestroy);
			if(index < 0)
				return;
			toDestroy.DisappearAndDestroy();
			await UniTask.Delay(TimeSpan.FromSeconds(toDestroy.AnimConfig.DestroyDuration), cancellationToken: _disableToken.Token);
			// index may change during the animation (Extra Box booster insert a crate)
			index = _activeCards.IndexOf(toDestroy);
			if(index < 0)
				return;
			_activeCards.RemoveAt(index);
			// waiting crate enter the conveyor at the leftmost open place
			TargetItemUI entering = null;
			// Extra Box booster was used: back to normal (2 open + waiting crate), no crate enters this time
			if(_extraOpenBoxes > 0) {
				_extraOpenBoxes--;
				if(_extraOpenBoxes == 0)
					SetWaitingCratesVisible(true);
			}
			else if(_cardStack.Count > 0) {
				entering = PopNextCard();
				_activeCards.Insert(0, entering);
			}
			// shift all crates to their place (crates right of the completed one keep their place)
			for(int n = 0; n < _activeCards.Count; n++) {
				var card = _activeCards[n];
				if(card == null)
					continue;
				var pos = GetAnchorPositionForCardAtIndex(n);
				if(card == entering)
					card.MoveToTargetAndFaceUp(pos);
				else
					card.MoveToTarget(pos);
			}
			UpdateAllowTargetToToyStats();
		}
		catch(OperationCanceledException) {
		}
		finally {
			_queuedDestroyCards.Remove(toDestroy);
		}
	}

	int _extraOpenBoxes;

	///<summary> Box sort mode: hide / show the face down waiting crates (hidden while the Extra Box booster is active) </summary>
	void SetWaitingCratesVisible(bool visible) {
		foreach(var card in _cardStack) {
			if(card == null)
				continue;
			var group = card.GetComponent<CanvasGroup>();
			if(group == null)
				group = card.gameObject.AddComponent<CanvasGroup>();
			group.alpha = visible ? 1f : 0f;
		}
	}

	///<summary> Box sort mode: no face down crate left, move all open crates to the centered positions </summary>
	void RealignCratesWhenStackEmpty() {
		if(!IsBoxSortMode || openBoxCount <= 0 || ShowWaitingSlot)
			return;
		for(int n = 0; n < _activeCards.Count; n++) {
			if(_activeCards[n] != null && !_queuedDestroyCards.Contains(_activeCards[n]))
				_activeCards[n].MoveToTarget(GetAnchorPositionForCardAtIndex(n));
		}
	}

	///<summary> Face down waiting crate keeps 1 place on the left (not while Extra Box booster is active) </summary>
	bool ShowWaitingSlot => _cardStack.Count > 0 && _extraOpenBoxes == 0;

	int MaxActiveCards => IsBoxSortMode && openBoxCount > 0 ? openBoxCount + _extraOpenBoxes : maxConcurrentTargetCfg.Value;

	///<summary> Pop next card to open. Box sort mode: prefer a box whose toy type is not open yet, so 2 open boxes never ask for the same toy </summary>
	TargetItemUI PopNextCard() {
		var picked = PickNextCardFromStack();
		// box sort mode: waiting crates stand at the same place and show their content, keep the real next crate on top
		if(IsBoxSortMode && _cardStack.Count > 0 && _cardStack.Peek() != null)
			_cardStack.Peek().transform.SetAsLastSibling();
		if(IsBoxSortMode && picked != null)
			picked.transform.SetAsLastSibling();
		return picked;
	}

	TargetItemUI PickNextCardFromStack() {
		if(!IsBoxSortMode || _cardStack.Count <= 1)
			return _cardStack.Pop();
		var openTypes = new HashSet<GameObject>();
		for(int n = 0; n < _activeCards.Count; n++) {
			var card = _activeCards[n];
			if(card != null && !_queuedDestroyCards.Contains(card) && card.RootToyPrefab != null)
				openTypes.Add(card.RootToyPrefab);
		}
		var top = _cardStack.Peek();
		if(top == null || !openTypes.Contains(top.RootToyPrefab))
			return _cardStack.Pop();
		// enumerate stack from top to bottom
		var list = new List<TargetItemUI>(_cardStack);
		var pickIndex = list.FindIndex(c => c != null && !openTypes.Contains(c.RootToyPrefab));
		if(pickIndex < 0)
			return _cardStack.Pop();
		var picked = list[pickIndex];
		list.RemoveAt(pickIndex);
		_cardStack.Clear();
		for(int n = list.Count - 1; n >= 0; n--)
			_cardStack.Push(list[n]);
		return picked;
	}

	///<summary> Box sort mode: create box cards. Each toy type split into boxes of [toyPerBox], shuffle so the same type not always next to each other </summary>
	void SpawnBoxCards(Dictionary<GameObject, List<ToyView>> targetMap, Vector2 beginPostition) {
		var boxes = new List<KeyValuePair<GameObject, int>>();
		var perBox = Mathf.Max(1, toyPerBox);
		foreach(var target in targetMap) {
			var remain = target.Value.Count;
			while(remain > 0) {
				var capacity = Mathf.Min(perBox, remain);
				boxes.Add(new KeyValuePair<GameObject, int>(target.Key, capacity));
				remain -= capacity;
			}
		}
		MathUtils.ShuffleList(boxes);
		// avoid 2 same boxes standing next to each other if possible
		for(int n = 1; n < boxes.Count; n++) {
			if(boxes[n].Key != boxes[n - 1].Key)
				continue;
			for(int k = n + 1; k < boxes.Count; k++) {
				if(boxes[k].Key != boxes[n - 1].Key) {
					var tmp = boxes[n];
					boxes[n] = boxes[k];
					boxes[k] = tmp;
					break;
				}
			}
		}
		// stack is LIFO, push reverse to open boxes from the first one
		for(int n = boxes.Count - 1; n >= 0; n--) {
			var rootPrefab = boxes[n].Key;
			var thumbnail = thumbnailStorage.GetThumbnail(rootPrefab);
			var item = Instantiate(itemPrefab, rootItemTrans).GetComponent<TargetItemUI>();
			_cardStack.Push(item);
			item.SetupAsBox(rootPrefab, thumbnail, boxes[n].Value);
			item.FaceDown();
			(item.transform as RectTransform).anchoredPosition = beginPostition;
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
