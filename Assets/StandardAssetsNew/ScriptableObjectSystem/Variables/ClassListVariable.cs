using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
// the developerDescription in private will thrown a warning. We dont need that,
// we just need a description to read in Inspector, and dont want any class access it.
// So disable this warning
#pragma warning disable 0414


public class ClassListVariable<T> : ScriptableObject where T : class {
	#region Declare
	/// If we keep those var UNITY_EDITOR, and they are public or SerializeField, then Unity will serialize them
	/// to the asset file in project, their size will increase (size fro the string descs). But when we build to platform
	/// those var will be stripped, but the total size will be keep the same, then on device, we'll got the exception :
	/// "A script behaviour (script unknown or not yet loaded) has a different serialization layout when loading.
	/// (Read 48 bytes but expected 88 bytes), Did you #ifdef UNITY_EDITOR a section of your serialized properties in any of your scripts?"
	/// So, we cannot use those UNITY_EDITOR things in ScriptableObject, let keep those var serialized in to asset,
	/// cost us some more memory, butits just a minor problem
	//#if UNITY_EDITOR
	[TextArea(2, 10)] [SerializeField] string developerDescription = "";

	///<summary> Config the protect level </summary>
	[SerializeField] protected ProtectType protectedType;
	///<summary> The list of T type, access in runtime </summary>
	[SerializeField] protected List<T> _list = new List<T>();
	#endregion//Declare


	#region Events
	///<summary> Invoke when number of list was changed </summary>
	public event Action OnListChangedHandler;
	void RaiseOnListChangedHandler() {
		OnListChangedHandler?.Invoke();
		// raise virtual function
		OnListChanged();
	}
	#endregion//Events


	#region Public
	///<summary> Get the list, NOTES: modify the list via this won't raise the OnLIstChanged event </summary>
	public List<T> Value => _list;

	///<summary> Override operator to access the list </summary>
	public T this[int index] => _list[index];

	///<summary> Add item to list, ignore null and raise event listChanged </summary>
	public void Add(T item) {
		if(protectedType == ProtectType.ReadOnly) {
			Log.Warning(false, this, "Project level = ReadOnly, cannot modify");
			return;
		}
		// okay, add
		if(item != null) {
			_list.Add(item);
			// call virtual func
			OnAddItem(item);
			// event at last
			RaiseOnListChangedHandler();
		}
		else {
			Log.Warning(false, this, "Add a null item to list, ignore");
		}
	}

	///<summary> Remove item from list, ignore null and raise event listChanged </summary>
	public void Remove(T item) {
		if(protectedType == ProtectType.ReadOnly) {
			Log.Warning(false, this, "Project level = ReadOnly, cannot modify");
			return;
		}
		if(item != null) {
			_list.Remove(item);
			// call virtual func
			OnRemoveItem(item);
			// event at last
			RaiseOnListChangedHandler();
		}
		else {
			Log.Warning(false, this, "Remove a null item");
		}
	}

	public T Get(int index) {
		return _list[index];
	}

	public void Set(int index, T item) {
		if(protectedType == ProtectType.ReadOnly) {
			Log.Warning(false, this, "Project level = ReadOnly, cannot modify");
			return;
		}
		_list[index] = item;
		// event at last
		RaiseOnListChangedHandler();
	}

	public bool Contain(T item) {
		return _list.Contains(item);
	}

	public void Clear() {
		if(protectedType == ProtectType.ReadOnly) {
			Log.Warning(false, this, "Project level = ReadOnly, cannot modify");
			return;
		}
		// clear and notify
		_list.Clear();
		// call virtual func
		OnClearList();
		RaiseOnListChangedHandler();
	}

	public int Count => _list.Count;
	public bool IsEmpty() => _list.Count == 0;

	/// <summary> The last one on the list. Should check empty first </summary>
	public T GetTheLastOne() {
		if(IsEmpty())
			return null;
		return _list[_list.Count - 1];
	}

	/// <summary> The first one on the list. Should check empty first </summary>
	public T GetTheFirstOne() {
		if(IsEmpty())
			return null;
		return _list[0];
	}

	/// <summary> Copy the input list to us </summary>
	public void Copy(List<T> source) {
		// clear first
		_list.Clear();
		_list = new List<T>(source.Count);
		for(int n = 0, count = source.Count; n < count; n++) {
			_list[n] = source[n];
		}
	}

	///<summary> Simply replace list </summary>
	public void ReplaceList(List<T> newList) {
		_list = newList;
		RaiseOnListChangedHandler();
	}

	///<summary> Make a clone of the list </summary>
	public List<T> Clone() {
		var result = new List<T>(_list.Count);
		for(int n = 0; n < _list.Count; n++) {
			result.Add(_list[n]);
		}
		return result;
	}

	///<summary> Cause this is class-list, mean item in array can be null. This will iterate throught the list and count not-null item.
	///So don't use it more frequently</summary>
	public int CountNotNullItems() {
		var result = 0;
		for(int n = 0; n < _list.Count; n++) {
			if(_list[n] != null)
				result++;
		}
		return result;
	}
	#endregion//Public


	#region Virtual functions
	///<summary> Raise when list changed </summary>
	protected virtual void OnListChanged() {
	}

	///<summary> Raise when add new item </summary>
	protected virtual void OnAddItem(T item) {
	}

	///<summary> Raise when add remove item </summary>
	protected virtual void OnRemoveItem(T item) {
	}

	///<summary> Raise when Clear </summary>
	protected virtual void OnClearList() {
	}
	#endregion//Virtual functions


	#region Classes
	public enum ProtectType {
		FreeToEditInEditorAndRuntime,
		ReadOnly,
	}
	#endregion//Classes
}
#pragma warning restore 0414