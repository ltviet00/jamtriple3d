using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Listen to changes of FloatVariable then convert it (C# action) to UnityEvent
/// </summary>
public class FloatVariableListener : MonoBehaviour {
	#region Config
	[Header("Select register/unregister type")]
	[SerializeField] RegisterType registerType = RegisterType.OnEnable;
	[SerializeField] UnRegisterType unRegisterType = UnRegisterType.OnDisable;

	[Header("FloatVarialbe to subscribe")]
	[SerializeField] FloatVariable variable;
	public UnityEventFloat listener;

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
		listener.Invoke(variable.Value);
	}
	#endregion//Subcribe

	#region Classes
	[System.Serializable]
	public class UnityEventFloat : UnityEvent<float> {
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
