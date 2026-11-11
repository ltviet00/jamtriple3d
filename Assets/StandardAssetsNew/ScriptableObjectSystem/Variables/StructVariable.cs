using UnityEngine;
using System.Collections;
using System;

// the developerDescription in private will thrown a warning. We dont need that,
// we just need a description to read in Inspector, and dont want any class access it.
// So disable this warning
#pragma warning disable 0414
/// <summary>
/// Base class for FloatVariable, IntegerVariable ... struct variable (vector, float, interger .v.v..).
/// Already implement some basic things like Value (get, set), OnValueChanged event .....
/// </summary>
public abstract class StructVariable<T> : ScriptableObject where T : struct {
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
	[SerializeField] bool activeLogOnValueChanged = false;
//#endif
	/// <summary> The main variable for accessing in runtime </summary>
	[SerializeField] protected T _value;
	[SerializeField] protected ProtectType protectedType;

	public T Value {
		get {
			return _value;
		}
		set {
			if(protectedType == ProtectType.ReadOnly) {
				Log.Warning(false, this, "This variable is set to ReadOnly");
				return;
			}
			var isValueChanged = IsNewValueDifferentFromCurrentValue(value);
			if(isValueChanged) {
				var previous = _value;
				_value = value;
				NotifyValueChanged(previous, value);
			}
		}
	}

	/// <summary> If protectType = ResetToInitAtEnable, then the _value will alway be reset to this value </summary>
	[SerializeField] protected T _initValue;

	/// <summary> Inherit class should implement this function, will be call each time new value set to variable.
	/// The OnValueChangedHandler only be raised when new value is different. Default is </summary>
	protected virtual bool IsNewValueDifferentFromCurrentValue(T newValue) {
		return true;
	}
	#endregion//Declare


	#region Events
	/// <summary> Raise when the value was changed </summary>
	public event Action OnValueChangedHandler;
	/// <summary> Raise when the value was changed, has previous/current value in parameters </summary>
	public event Action<T, T> OnValueChangedWithParaHandler;

	void NotifyValueChanged(T previous, T current) {
#if UNITY_EDITOR
		if(activeLogOnValueChanged) {
			Log.Info(this, "[{0}] value changed : {1}", name, _value);
		}
#endif//UNITY_EDITOR
		// raise 2 events
		OnValueChangedHandler?.Invoke();
		OnValueChangedWithParaHandler?.Invoke(previous, current);
	}
	#endregion//Events


	#region Private
	void OnEnable() {
		if(protectedType == ProtectType.ResetToInitValueAtEnable) {
			_value = _initValue;
		}
	}
	#endregion//Private


	#region Classes
	public enum ProtectType {
		FreeToEditInEditorAndRuntime,
		ResetToInitValueAtEnable,
		ReadOnly,
	}
	#endregion//Classes
}
#pragma warning restore 0414