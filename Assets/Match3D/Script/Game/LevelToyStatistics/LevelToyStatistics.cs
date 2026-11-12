using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Contains statistics about toy on screen : toy remain, types of toy ... 
/// will be filledby ToySpawner, got cleared when level start/end.
/// </summary>
[CreateAssetMenu(fileName = "LevelToyStatistics", menuName = "Match3D/LevelToyStatistics")]
public class LevelToyStatistics : ScriptableObject {
	#region Init, config
	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public - setup
	///<summary> Key = prefab, value = list toy alive on screen. Why list instead of Hashet? cause we need to take out later when use booster </summary>
	Dictionary<GameObject, List<ToyView>> _decoyMap = new Dictionary<GameObject, List<ToyView>>();
	///<summary> Key = prefab, value = list toy alive on screen. Why list instead of Hashet? cause we need to take out later when use booster</summary>
	Dictionary<GameObject, List<ToyView>> _targetMap = new Dictionary<GameObject, List<ToyView>>();
	///<summary> Key = prefab, value = list toy alive on screen. Why list instead of Hashet? cause we need to take out later when use booster </summary>
	Dictionary<GameObject, List<ToyView>> _supportMap = new Dictionary<GameObject, List<ToyView>>();
	///<summary> Key = prefab, value = list toy alive on screen. Why list instead of Hashet? cause we need to take out later when use booster </summary>
	Dictionary<GameObject, List<ToyView>> _eventMap = new Dictionary<GameObject, List<ToyView>>();

	public Dictionary<GameObject, List<ToyView>> DecoyMap => _decoyMap;
	public Dictionary<GameObject, List<ToyView>> TargetMap => _targetMap;
	public Dictionary<GameObject, List<ToyView>> SupportMap => _supportMap;
	public Dictionary<GameObject, List<ToyView>> EventMap => _eventMap;

	///<summary> All toy map (decoy, target, support ..). Currently used by Fan booster (add force on all toys) </summary>
	public List<Dictionary<GameObject, List<ToyView>>> _allMaps = new List<Dictionary<GameObject, List<ToyView>>>();
	///<summary> All toy map (decoy, target, support ..). Currently used by Fan booster (add force on all toys) </summary>
	public List<Dictionary<GameObject, List<ToyView>>> AllMaps => _allMaps;

	///<summary> Target bar has maximum cards display pertime, this is map of ROOT PREFAB of displaying cards, allow pick these targets only. </summary>
	HashSet<GameObject> _allowTargetMap = new HashSet<GameObject>();
	///<summary> Target bar has maximum cards display pertime, this is map of ROOT PREFAB of displaying cards, allow pick these targets only. </summary>
	public HashSet<GameObject> AllowTargetMap => _allowTargetMap;

	public void ClearAllStats() {
		_decoyMap.Clear();
		_targetMap.Clear();
		_supportMap.Clear();
		_eventMap.Clear();
		// remain stats
		_remainTargetcount = 0;
	}

	///<summary> Call from Toy spawner when finish spawn toy on screen (TODO: update supportMap later) </summary>
	public void SetupStats(Dictionary<GameObject, List<ToyView>> decoyMap, Dictionary<GameObject, List<ToyView>> targetMap) {
		// simply save refs
		ClearAllStats();
		_decoyMap = decoyMap;
		_targetMap = targetMap;
		// first stats
		_remainTargetcount = GetToyCountInMap(_targetMap);
		_remainDecoyCount = GetToyCountInMap(_decoyMap);
		// create the allmap
		_allMaps.Add(decoyMap);
		_allMaps.Add(targetMap);
	}

	///<summary> Target bar has maximum cards display pertime, this is map of displaying card, allow pick these targets only </summary>
	public void SetAllowTargetToyMap(HashSet<GameObject> allowTargetMap) {
		_allowTargetMap = allowTargetMap;
	}

	///<summary> Call from Spawner, if level got SupportToy, then save to here </summary>
	public void SetSupportToyMap(Dictionary<GameObject, List<ToyView>> supportMap) {
		_supportMap = supportMap;
	}

	///<summary> Call from Spawner, if level got WeeklyToy, then save to here </summary>
	public void SetWeeklyEventToyMap(Dictionary<GameObject, List<ToyView>> eventToyMap) {
		_eventMap = eventToyMap;
	}

	///<summary> Enable, disable physics for all toy in all map : disable rigidbody so they will freeze, disable collider so we can't touch them </summary>
	public void SetActivePhysicsForAllToy(bool active) {
		SetActivePhysicsForAllToyInMap(_decoyMap, active);
		SetActivePhysicsForAllToyInMap(_targetMap, active);
		SetActivePhysicsForAllToyInMap(_supportMap, active);
		SetActivePhysicsForAllToyInMap(_eventMap, active);
	}
	#endregion//Public - Setup


	#region Public - event, stats
	[Header("Stats")]
	[SerializeField] int _remainTargetcount;
	[SerializeField] int _remainDecoyCount;

	///<summary> Remain target toy count </summary>
	public int RemainTargetcount => _remainTargetcount;
	///<summary> Raised when target toy changed </summary>
	public event Action<int> OnTargetRemainChangedHandler;

	void DeductAndUpdateTargetRemainStats() {
		_remainTargetcount--;
		OnTargetRemainChangedHandler?.Invoke(_remainTargetcount);
	}

	void IncreaseAndUpdateTargeRemainStats() {
		_remainTargetcount++;
		OnTargetRemainChangedHandler?.Invoke(_remainTargetcount);
	}

	void DeductAndUpdateDecoyRemainStats() {
		_remainDecoyCount--;
	}

	void IncreaseAndUpdateDecoyRemainStats() {
		_remainDecoyCount++;
	}
	#endregion//Public - event, stats


	#region Public - checking, remove toy
	///<summary> Pickable mean it's target toy or Support, or it NOT a Decoy toy.
	/// UPDATE condition : targetToy but contains in allowTargetMap only.
	/// Update: support toy or event toys are also pickable</summary>
	public bool IsPickable(ToyView toy) {
		if(IsTargetToy(toy)) {
			return AllowTargetMap.Contains(toy.rootPrefab);
		}
		if(IsSupportToy(toy) || IsWeeklyEventToy(toy)) {
			return true;
		}
		return !IsDecoyToy(toy);
	}

	///<summary> Check if this toy (rootPrefab) is contained in any active targetMap (live on scene), then check if it belong the the active List </summary>
	public bool IsTargetToy(ToyView toy) {
		return ContainInMap(toy, _targetMap);
	}

	///<summary> Check if this toy (rootPrefab) is contained in any active targetMap (live on scene), then check if it belong the the active List </summary>
	public bool IsDecoyToy(ToyView toy) {
		return ContainInMap(toy, _decoyMap);
	}

	///<summary> Check if this toy (rootPrefab) is contained in any active targetMap (live on scene), then check if it belong the the active List </summary>
	public bool IsSupportToy(ToyView toy) {
		return ContainInMap(toy, _supportMap);
	}

	///<summary> Check if this toy (rootPrefab) is contained in any active targetMap (live on scene), then check if it belong the the active List </summary>
	public bool IsWeeklyEventToy(ToyView toy) {
		return ContainInMap(toy, _eventMap);
	}

	///<summary> Call this when user catch a toy, it will check which map this toy belong to (decoy, target, support ...), then remove it out of the map </summary>
	public void RemoveToy(ToyView toy) {
		// find the map contain toy
		var containMap = FindMapThatContainToy(toy, out bool isThisTargetToy, out bool isThisDecoyToy);
		// remove it out of the map
		if(containMap != null) {
			RemoveFromMap(toy, containMap);
			// check update stats after removing item
			if(isThisTargetToy) {
				DeductAndUpdateTargetRemainStats();
			}
			if(isThisDecoyToy) {
				DeductAndUpdateDecoyRemainStats();
			}
			return;
		}
		Log.Warning(false, this, $"Not found any map contains toy [{toy.name}] to remove");
	}

	///<summary> Check rootPrefab to see which map it belong to (decoy, target, support...) then add to that map. Use this when user undo or clearBar, need to add toy back to active maps </summary>
	public void AddToy(ToyView toy) {
		// find the map contain toy
		var containMap = FindMapForToy(toy, out bool isThisTargetToy, out bool isThisDecoyToy);
		// remove it out of the map
		if(containMap != null) {
			AddToMap(toy, containMap);
			// check update stats after removing item
			if(isThisTargetToy) {
				IncreaseAndUpdateTargeRemainStats();
			}
			if(isThisDecoyToy) {
				IncreaseAndUpdateDecoyRemainStats();
			}
			return;
		}
		Log.Warning(false, this, $"Not found any map contains toy [{toy.name}] to add to");
	}
	#endregion//Public - checking, remove toy


	#region Public - random
	///<summary> Random one toy in the target map. Currently use for the lightbulb booster </summary>
	public ToyView GetRandomTargetToy() {
		if(isFailedConfig)
			return null;
		// random index
		var randomMapIndex = Random.Range(0, _allowTargetMap.Count);
		GameObject selectPrefab = null;
		var counter = 0;
		foreach(var prefab in _allowTargetMap) {
			if(counter == randomMapIndex) {
				selectPrefab = prefab;
				break;
			}
			counter++;
		}
		// retrieve the list, random in the toyList
		var toyList = _targetMap[selectPrefab];
		var randomIndex = Random.Range(0, toyList.Count);
		return toyList[randomIndex];
	}

	///<summary> Random pick a list of target toy with target amount. Currently use for MagnifyGlass booster </summary>
	public List<ToyView> GetRandomListOfTargetToy(int amount) {
		if(amount > _remainTargetcount) {
			Log.Warning(false, this, $"Request amount [{amount}] > remain Target [{_remainTargetcount}]. capped at remain");
			amount = _remainTargetcount;
		}
		var result = new List<ToyView>(amount);
		// random index list
		var setToList = MathUtils.HashsetToList(_allowTargetMap);
		MathUtils.ShuffleList(setToList);
		// to simply, let just take out enough amount from first list, if not enough, then move to next list
		var remain = amount;
		for(int n = 0; n < setToList.Count; n++) {
			var toyList = _targetMap[setToList[n]];
			var takeOutAmount = remain > toyList.Count ? toyList.Count : remain;
			remain -= takeOutAmount;

			var takeOutList = MathUtils.CreateSubListWithRandomElement(toyList, takeOutAmount);
			result.AddRange(takeOutList);
		}
		return result;
	}

	public List<ToyView> GetRandomListOfDecoyToy(int amount) {
		if(amount > _remainDecoyCount) {
			Log.Warning(false, this, $"Request amount [{amount}] > remain decoy [{_remainDecoyCount}]. capped at remain");
			amount = _remainDecoyCount;
		}
		var result = new List<ToyView>(amount);
		// simply take out from the map until we got enough toys
		var remainToTake = amount;
		foreach(var item in _decoyMap) {
			var list = item.Value;
			// just this list is enough for the request
			if(list.Count >= remainToTake) {
				for(int n = 0; n < remainToTake; n++) {
					result.Add(list[n]);
				}
				remainToTake = 0;
				break;
			}
			// oh ,this list not enough, take it all
			else {
				result.AddRange(list);
				remainToTake -= list.Count;
			}
		}
		if(remainToTake > 0) {
			Log.Warning(false, this, $"Not enough decoy toy to get [{amount}]");
		}
		return result;
	}

	public ToyView GetRandomDecoyToy() {
		if(_remainDecoyCount == 0) {
			Log.Warning(false, this, $"No remain decoy toy to pick");
			return null;
		}
		var randomToyList = MathUtils.PickRandomMapValue(_decoyMap);
		return randomToyList.PickRandom();
	}

	public bool HasWeeklyEventToy => HasAnyToy(_eventMap);
	public bool HasSupportToy => HasAnyToy(_supportMap);

	static bool HasAnyToy(Dictionary<GameObject, List<ToyView>> map) {
		if(map == null)
			return false;
		foreach(var list in map.Values) {
			if(list != null && list.Count > 0)
				return true;
		}
		return false;
	}

	///<summary> Fastest speed of all toys still on the field (used to wait for the start pile to settle) </summary>
	public float GetMaxToySpeed() {
		var max = 0f;
		max = Mathf.Max(max, GetMaxSpeedInMap(_targetMap));
		max = Mathf.Max(max, GetMaxSpeedInMap(_decoyMap));
		max = Mathf.Max(max, GetMaxSpeedInMap(_supportMap));
		max = Mathf.Max(max, GetMaxSpeedInMap(_eventMap));
		return max;
	}

	static float GetMaxSpeedInMap(Dictionary<GameObject, List<ToyView>> map) {
		var max = 0f;
		if(map == null)
			return max;
		foreach(var list in map.Values) {
			foreach(var toy in list) {
				if(toy == null || toy.mainRigid == null || toy.mainRigid.isKinematic)
					continue;
				max = Mathf.Max(max, toy.mainRigid.velocity.magnitude);
			}
		}
		return max;
	}

	///<summary> Target toys the user can really see (nothing on top of them), top of the pile first.
	/// Pickable toys (match an open box) come first </summary>
	public List<ToyView> GetVisibleTopTargetToys(int amount, Camera camera) {
		var candidates = new List<ToyView>();
		foreach(var list in _targetMap.Values) {
			foreach(var toy in list) {
				if(toy != null && toy.mainCollider != null && IsVisibleFromCamera(toy, camera))
					candidates.Add(toy);
			}
		}
		// pickable first, then closest to the camera (gravity is +z, smaller z = on top)
		candidates.Sort((a, b) => {
			var pickA = IsPickable(a) ? 0 : 1;
			var pickB = IsPickable(b) ? 0 : 1;
			if(pickA != pickB)
				return pickA.CompareTo(pickB);
			return a.transform.position.z.CompareTo(b.transform.position.z);
		});
		if(candidates.Count > amount)
			candidates.RemoveRange(amount, candidates.Count - amount);
		return candidates;
	}

	static bool IsVisibleFromCamera(ToyView toy, Camera camera) {
		if(camera == null)
			return true;
		var center = toy.mainCollider.bounds.center;
		var screenPos = camera.WorldToScreenPoint(center);
		if(screenPos.x < 0f || screenPos.y < 0f || screenPos.x > Screen.width || screenPos.y > Screen.height)
			return false;
		var ray = camera.ScreenPointToRay(screenPos);
		if(!Physics.Raycast(ray, out var hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
			return false;
		return hit.collider == toy.mainCollider || hit.collider.GetComponentInParent<ToyView>() == toy;
	}

	public ToyView GetRandomWeeklyEventToy() {
		var randomToyList = MathUtils.PickRandomMapValue(_eventMap);
		return randomToyList.PickRandom();
	}

	public ToyView GetRandomSupportToy() {
		var randomToyList = MathUtils.PickRandomMapValue(_supportMap);
		return randomToyList.PickRandom();
	}
	#endregion//Public - random


	#region Public - Get pack of Toy and same rootPrefab
	///<summary> Let say we need 3 banana toys, or 6 chicken toys. Return null if can't get enough toy </summary>
	public List<ToyView> GetListOfRandomTargetToyWhichSameRootPrefab(int amount) {
		var enoughToyLists = new List<List<ToyView>>();
		foreach(var list in _targetMap.Values) {
			if(list.Count >= amount)
				enoughToyLists.Add(list);
		}
		// no list remaim has enough toy to get, then return null
		if(enoughToyLists.Count == 0) {
			return null;
		}
		// random pick a list
		var selectList = enoughToyLists.PickRandom();
		var result = MathUtils.CreateListOfItemsFromTheEnd(selectList, amount);
		return result;
	}
	#endregion//Public - Get pack of Toy and same rootPrefab


	#region Private - check/add/remove from/to maps
	///<summary> Base on rootPrefab, check if any map contains it then check if the map contains any list that holding this toy </summary>
	Dictionary<GameObject, List<ToyView>> FindMapThatContainToy(ToyView toy, out bool isThisTargetToy, out bool isThisDecoyToy) {
		Dictionary<GameObject, List<ToyView>> containMap = null;
		isThisDecoyToy = false;
		isThisTargetToy = false;

		if(IsTargetToy(toy)) {
			containMap = _targetMap;
			isThisTargetToy = true;
		}
		else if(IsSupportToy(toy)) {
			containMap = _supportMap;
		}
		else if(IsWeeklyEventToy(toy)) {
			containMap = _eventMap;
		}
		else if(IsDecoyToy(toy)) {
			containMap = _decoyMap;
			isThisDecoyToy = true;
		}
		return containMap;
	}

	///<summary> Base on rootPrefab, check if any map contains this rootPrefab. Use this when we need to add new toy to existing map (when user undo or clearBar).
	///Return null if not found</summary>
	Dictionary<GameObject, List<ToyView>> FindMapForToy(ToyView toy, out bool isThisTargetToy, out bool isThisDecoyToy) {
		Dictionary<GameObject, List<ToyView>> containMap = null;
		isThisDecoyToy = false;
		isThisTargetToy = false;
		var rootPrefab = toy.rootPrefab;

		if(_targetMap.ContainsKey(rootPrefab)) {
			containMap = _targetMap;
			isThisTargetToy = true;
		}
		else if(_supportMap.ContainsKey(rootPrefab)) {
			containMap = _supportMap;
		}
		else if(_eventMap.ContainsKey(rootPrefab)) {
			containMap = _eventMap;
		}
		else if(_decoyMap.ContainsKey(rootPrefab)) {
			containMap = _decoyMap;
			isThisDecoyToy = true;
		}
		return containMap;
	}


	///<summary> Check if rootPrefab is contained in Map, and it belong the the active List </summary>
	bool ContainInMap(ToyView toy, Dictionary<GameObject, List<ToyView>> map) {
		if(!map.TryGetValue(toy.rootPrefab, out var list)) {
			//Log.Info($"Target map not contain prefab [{toy.rootPrefab.name}]");
			return false;
		}
		return list.Contains(toy);
	}

	void RemoveFromMap(ToyView toy, Dictionary<GameObject, List<ToyView>> map) {
		if(!map.TryGetValue(toy.rootPrefab, out var list)) {
			Log.Info($"RemoveFromMap(), Target map not contain prefab [{toy.rootPrefab.name}]");
			return;
		}
		list.RemoveElementWithoutShifting(toy);
	}

	///<summary> Add toy to target map, return true if success (no duplicate .. or any issues) </summary>
	bool AddToMap(ToyView toy, Dictionary<GameObject, List<ToyView>> map) {
		if(!map.TryGetValue(toy.rootPrefab, out var list)) {
			Log.Info($"AddToMap(), Target map not contain prefab [{toy.rootPrefab.name}]");
			return false;
		}
		// one more check for adding case, in case duplicated
		if(list.Contains(toy)) {
			Log.Warning(false, $"AddTomap, but toy [{toy.name}] already contains in target list");
			return false;
		}
		list.Add(toy);
		return true;
	}

	int GetToyCountInMap(Dictionary<GameObject, List<ToyView>> map) {
		var result = 0;
		foreach(var item in map) {
			result += item.Value.Count;
		}
		return result;
	}
	#endregion//Private - check/add/remove from/to maps


	#region Utils
	void SetActivePhysicsForAllToyInMap(Dictionary<GameObject, List<ToyView>> map, bool active) {
		foreach(var list in map.Values) {
			for(int n = 0; n < list.Count; n++) {
				list[n].SetActivePhysics(active);	
			}
		}
	}
	#endregion//Utils


	#region Classes
	//[System.Serializable]
	//public class Test {
	//	List<int> value;
	//}
	//[Header("TestMap")]
	//[SerializeField] Dictionary<int, string> heeh;
	//[SerializeField] IntStringMap intStringMap;
	//[SerializeField] ToyViewIntMap toyIntMap;
	//[SerializeField] ToyViewListToyViewMap toyToyListMap;

	//[System.Serializable]
	//public class IntStringMap : UnitySerializedDictionary<int, string> { }

	//[System.Serializable]
	//public class ToyViewIntMap : UnitySerializedDictionary<ToyView, int> { }

	//[System.Serializable]
	//public class ToyViewListToyViewMap : UnitySerializedDictionary<ToyView, List<ToyView>> { }
	#endregion//Classes
}
