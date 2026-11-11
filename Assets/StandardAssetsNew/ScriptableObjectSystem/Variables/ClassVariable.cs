using UnityEngine;
using System.Collections;
using System;

// the developerDescription in private will thrown a warning. We dont need that,
// we just need a description to read in Inspector, and dont want any class access it.
// So disable this warning
#pragma warning disable 0414
/// <summary>
/// Base class for those variable of NOT struct(int, float, bool ...) but Classes (string, user's define class) 
/// Already implement some basic things like Value (get, set), OnValueChanged event .....
/// </summary>
public abstract class ClassVariable<T> : ScriptableObject where T : class {
	#region Declare
	/// If we keep those var UNITY_EDITOR, and they are public or SerializeField, then Unity will serialize them
	/// to the asset file in project, their size will increase (size fro the string descs). But when we build to platform
	/// those var will be stripped, but the total size will be keep the same, then on device, we'll got the exception :
	/// "A script behaviour (script unknown or not yet loaded) has a different serialization layout when loading. 
	/// (Read 48 bytes but expected 88 bytes), Did you #ifdef UNITY_EDITOR a section of your serialized properties in any of your scripts?"
	/// So, we cannot use those UNITY_EDITOR things in ScriptableObject, let keep those var serialized in to asset,
	/// cost us some more memory, butits just a minor problem
	[TextArea(2, 10)] [SerializeField] string developerDescription = "";
//#if UNITY_EDITOR
	[SerializeField] bool activeLogOnValueChanged = false;
	//#endif
	/// <summary> Disable this, warning message will fire when we assign null var to this.
	/// and the current value will be kept</summary>
	[SerializeField] bool allowNullAssign = true;
	/// <summary> The main variable for accessing in runtime </summary>
	[SerializeField] protected T _value;
	/// <summary> If protectType = ResetToInitAtEnable, then the _value will alway be reset to this value </summary>
	[SerializeField] protected T _initValue;
	/// <summary> Some variable we only want to use as constant, then set to Readonly. Read other mode to understand the meaning </summary>
	[SerializeField] protected ProtectType protectedType;
	/// <summary> Class variable's value is alway the reference type. By default, we reset to null at Disable(). But in somecase, we need to keep those reference (or value for StringVariable), then cahnge it to KeepTheValue </summary>
	[SerializeField] protected BehaviorOnDisable behaviorOnDisable = BehaviorOnDisable.ResetReferenceToNull;

	public T Value {
		get {
			return _value;
		}
		set {
			// check null for class type (not like struct - never null)
			if(value == null && !allowNullAssign) {
				Log.Error(false, this, "Not allow assign null value to this variable [{0}]", name);
				return;
			}
			if(protectedType == ProtectType.ReadOnly) {
				Log.Warning(false, this, "This variable is set to ReadOnly");
				return;
			}
			_value = value;
			NotifyValueChanged();
		}
	}
	#endregion//Declare


	#region Public
	public bool IsValueNull() {
		return _value == null;
	}

	public bool IsValueNotNull() {
		return _value != null;
	}
	#endregion//Public


	#region Events
	/// <summary> Raise when the value was changed </summary>
	public event Action<T> OnValueChangedHandler;
	///// <summary> Raise when the value was changed, but without parameter </summary>
	//public event Action OnValueChangedWithoutParameterHandler;

	void NotifyValueChanged() {
#if UNITY_EDITOR
		if(activeLogOnValueChanged) {
			Log.Info(this, "[{0}] value changed : [{1}]", name, _value);
		}
#endif//UNITY_EDITOR
		OnValueChangedHandler?.Invoke(_value);
		//OnValueChangedWithoutParameterHandler?.Invoke();
	}
	#endregion//Events


	#region Private
	void OnEnable() {
		if(protectedType == ProtectType.ResetToInitValueAtEnable) {
			_value = _initValue;
		}
	}

	void OnDisable() {
		if(behaviorOnDisable == BehaviorOnDisable.KeepTheReferenceValue)
			return;
		_value = null;
	}
	#endregion//Private


	#region Classes
	public enum ProtectType {
		FreeToEditInEditorAndRuntime,
		ResetToInitValueAtEnable,
		ReadOnly,
	}

	public enum BehaviorOnDisable {
		ResetReferenceToNull,
		KeepTheReferenceValue,
	}
	#endregion//Classes
}
#pragma warning restore 0414