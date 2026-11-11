using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Same with IntergerListener, but it will create a string with pre-config format, have this Integer value in that.
/// Then output a UnityEventString, use that to config UI or other things.
/// </summary>
public class IntegerVariableToStringEvent : MonoBehaviour {
	#region Config
	[Header("Select register/unregister type")]
	[SerializeField] RegisterType registerType = RegisterType.OnEnable;
	[SerializeField] UnRegisterType unRegisterType = UnRegisterType.OnDisable;

	[Header("IntegerVarialbe to subscribe")]
	[SerializeField] IntegerVariable variable;
	public UnityEventString listener;

	[Header("Other config")]
	[SerializeField] string stringFormat = "{0}";

	void OnValidate() {
		Log.ValidateNotNull(variable, this, "variable");
		Log.ValidateNotNull(listener, this, "listener");
	}

	bool IsFailedConfig() {
		return variable == null
			|| listener == null;
	}

	void Start() {
		if(IsFailedConfig())
			return;
		if(registerType == RegisterType.OnStart) {
			SubcribeVariable();
		}
	}

	void OnEnable() {
		if(IsFailedConfig())
			return;
		if(registerType == RegisterType.OnEnable) {
			SubcribeVariable();
		}
	}

	void OnDisable() {
		if(IsFailedConfig())
			return;
		if(unRegisterType == UnRegisterType.OnDisable) {
			UnsubcribeVariable();
		}
	}

	void OnDestroy() {
		if(IsFailedConfig())
			return;
		if(unRegisterType == UnRegisterType.OnDestroy) {
			UnsubcribeVariable();
		}
	}
	#endregion// Config

	#region Subcribe
	void SubcribeVariable() {
		variable.OnValueChangedHandler += OnValueChanged;
	}

	void UnsubcribeVariable() {
		variable.OnValueChangedHandler -= OnValueChanged;
	}

	void OnValueChanged() {
		var text = string.Format(stringFormat, variable.Value);
		listener.Invoke(text);
	}
	#endregion//Subcribe

	#region Classes
	[System.Serializable]
	public class UnityEventString : UnityEvent<string> {
	}

	enum RegisterType {
		OnStart,
		OnEnable,
	}

	enum UnRegisterType {
		OnDisable,
		OnDestroy,
	}
	#endregion//Classes
}
