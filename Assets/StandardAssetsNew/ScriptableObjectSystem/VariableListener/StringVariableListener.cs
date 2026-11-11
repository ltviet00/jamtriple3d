using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Listen to changes of StringVariable then convert it (C# action) to UnityEvent
/// </summary>
public class StringVariableListener : MonoBehaviour {
	#region Config
	[Header("Select register/unregister type")]
	[SerializeField] RegisterType registerType = RegisterType.OnEnable;
	[SerializeField] UnRegisterType unRegisterType = UnRegisterType.OnDisable;
	///<summary> Sometime we need to raise ValueChanged event at Subscribe time (Start, Enable)</summary>
	[SerializeField] bool raiseValueChangedAtSubscribe = false;

	[Header("StringVarialbe to subscribe")]
	[SerializeField] StringVariable variable;
	[SerializeField] UnityEventString listener;

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
		if(raiseValueChangedAtSubscribe)
			OnValueChanged(variable.Value);
	}

	void UnsubcribeVariable() {
		variable.OnValueChangedHandler -= OnValueChanged;
	}

	void OnValueChanged(string newValue) {
		listener.Invoke(newValue);
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
