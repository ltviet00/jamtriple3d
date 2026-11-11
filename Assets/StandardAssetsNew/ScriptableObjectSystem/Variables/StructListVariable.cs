using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
// the developerDescription in private will thrown a warning. We dont need that,
// we just need a description to read in Inspector, and dont want any class access it.
// So disable this warning
#pragma warning disable 0414
public class StructListVariable<T> : ScriptableObject where T : struct {
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
	}
	#endregion//Events


	#region Public
	///<summary> Get the list, NOTES: modify the list via this won't raise the OnLIstChanged event </summary>
	public List<T> Value => _list;

	///<summary> Override operator to access the list </summary>
	public T this[int index] => _list[index];
	public void Add(T item) {
		if(protectedType == ProtectType.ReadOnly) {
			Log.Warning(false, this, "Project level = ReadOnly, cannot modify");
			return;
		}
		_list.Add(item);
		// event at last
		RaiseOnListChangedHandler();
	}

	public void Remove(T item) {
		if(protectedType == ProtectType.ReadOnly) {
			Log.Warning(false, this, "Project level = ReadOnly, cannot modify");
			return;
		}
		_list.Remove(item);
		// event at last
		RaiseOnListChangedHandler();
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

	///<summary> Simply replace list </summary>
	public void ReplaceList(List<T> newList) {
		_list = newList;
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
		_list.Clear();
	}

	public int Count {
		get {
			return _list.Count;
		}
	}
	#endregion//Public


	#region Classes
	public enum ProtectType {
		FreeToEditInEditorAndRuntime,
		ReadOnly,
	}
	#endregion//Classes
}
#pragma warning restore 0414