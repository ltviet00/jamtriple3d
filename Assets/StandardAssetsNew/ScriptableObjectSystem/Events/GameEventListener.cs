using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// "Convert" GameEvent (use C# action) to UnityEvent, so use can use it as a component,
/// attach to gameobject, config Unityevent to do some behavior when the event was raised
/// </summary>
public class GameEventListener : MonoBehaviour {
	#region Init, config
	[Header("Select register/unregister type")]
	[SerializeField] RegisterType registerType = RegisterType.OnEnable;
	[SerializeField] UnRegisterType unRegisterType = UnRegisterType.OnDisable;

	[Tooltip("Event to register with.")]
	public GameEvent Event;

	[Tooltip("Response to invoke when Event is raised.")]
	public UnityEvent Response;

	void OnValidate() {
		Log.ValidateNotNull(Event, this, "Event");
	}

	bool IsFailedConfig() {
		return Event == null;
	}

	void Start() {
		if(IsFailedConfig())
			return;
		if(registerType == RegisterType.OnStart) {
			SubcribeEvents();
		}
	}

	void OnEnable() {
		if(IsFailedConfig())
			return;
		if(registerType == RegisterType.OnEnable) {
			SubcribeEvents();
		}
	}

	void OnDisable() {
		if(IsFailedConfig())
			return;
		if(unRegisterType == UnRegisterType.OnDisable) {
			UnsubcribeEvents();
		}
	}

	void OnDestroy() {
		if(IsFailedConfig())
			return;
		if(unRegisterType == UnRegisterType.OnDestroy) {
			UnsubcribeEvents();
		}
	}
	#endregion//Config

	#region Events
	private void SubcribeEvents() {
		Event.Subscribe(OnEventRaised);
	}

	private void UnsubcribeEvents() {
		Event.Unsubscribe(OnEventRaised);
	}

	public void OnEventRaised() {
		Response.Invoke();
	}
	#endregion//Events

	#region Classes
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