using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToyViewClassListVariable", menuName = "Match3D/Toys/ToyViewClassListVariable")]
public class ToyViewClassListVariable : ClassListVariable<ToyView> {
	#region Active toy map
	///<summary> Track active toys by prefab, use for hint booster later </summary>
	Dictionary<GameObject, List<ToyView>> _activetoysMap = new Dictionary<GameObject, List<ToyView>>();
	///<summary> Track active toys by prefab, use for hint booster later </summary>
	public Dictionary<GameObject, List<ToyView>> ActiveToyMap => _activetoysMap;

	protected override void OnAddItem(ToyView item) {
		if(_activetoysMap.ContainsKey(item.rootPrefab)) {
			_activetoysMap[item.rootPrefab].Add(item);
		}
		else {
			var list = new List<ToyView>();
			list.Add(item);
			_activetoysMap.Add(item.rootPrefab, list);
		}
	}

	protected override void OnRemoveItem(ToyView item) {
		if(_activetoysMap.ContainsKey(item.rootPrefab)) {
			var list = _activetoysMap[item.rootPrefab];
			list.Remove(item);
			// one more step, if list down to zero, then also remove the key-pair
			if(list.Count == 0) {
				list = null;
				_activetoysMap.Remove(item.rootPrefab);
			}
		}
		else {
			Log.Warning(false, this, $"Not contains ToyView [{item.name}] in dictionary, can't remove");
		}
	}

	protected override void OnClearList() {
		_activetoysMap.Clear();
	}
	#endregion//Active toy map


	#region Public
	///<summary> Default ToyBoxGroup will fill the list will all null pointer, then fill ref of Pulled toy to it.
	///So get the outsite right also mean the last toy pull to this. Wiill be null if no toy in the list </summary>
	public ToyView GetTheOutsiteRightValidToyView(out int boxIndex) {
		boxIndex = 0;
		for(int n = _list.Count - 1; n >= 0; n--) {
			var item = _list[n];
			if(item != null) {
				boxIndex = n;
				return item;
			}
		}
		return null;
	}

	///<summary> First index of null item. Return 0 if not found (or first item is also null) </summary>
	public int GetFirstEmptyItemIndexFromLeft() {
		for(int n = 0; n < _list.Count; n++) {
			if(_list[n] == null)
				return n;
		}
		// not found, let just return the first one
		return 0;
	}
	#endregion//Public
}
