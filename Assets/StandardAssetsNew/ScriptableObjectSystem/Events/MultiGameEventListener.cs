using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Same usage with GameEventListener, but we can config many events instead of just one
/// </summary>
public class MultiGameEventListener : MonoBehaviour {
	#region Init, config
	[Header("Select register/unregister type")]
	[SerializeField] RegisterType registerType = RegisterType.OnStart;
	[SerializeField] UnRegisterType unRegisterType = UnRegisterType.OnDestroy;

	[Header("Config all listener")]
	[SerializeField] ListenerConfig[] listeners;

	void OnValidate() {
		if(listeners == null)
			return;
		for(int n = 0; n < listeners.Length; n++) {
			if(listeners[n].IsFailedConfig())
				Log.Warning(false, this, "Missing GameEvent at index [{0}]", n);
		}
	}

	void Start() {
		/// register event on Start, not Awake
		/// Awake is often use for some high priority init on other objects
		if(registerType == RegisterType.OnStart) {
			SubcribeAllEvents();
		}
	}

	void OnEnable() {
		if(registerType == RegisterType.OnEnable) {
			SubcribeAllEvents();
		}
	}

	void OnDisable() {
		if(unRegisterType == UnRegisterType.OnDisable) {
			UnsubcribeAllEvents();
		}
	}

	void OnDestroy() {
		if(unRegisterType == UnRegisterType.OnDestroy) {
			UnsubcribeAllEvents();
		}
	}


	void SubcribeAllEvents() {
		for(int n = 0; n < listeners.Length; n++) {
			listeners[n].SubcribeGameEvent();
		}
	}

	void UnsubcribeAllEvents() {
		for(int n = 0; n < listeners.Length; n++) {
			listeners[n].UnsubcribeGameEvent();
		}
	}
	#endregion// Init, config

	#region Classes
	[System.Serializable]
	class ListenerConfig {
		public GameEvent evt;
		public UnityEvent listener;

		///<summary> Use for checking </summary>
		public bool IsFailedConfig() {
			return evt == null;
		}

		public void SubcribeGameEvent() {
			evt.Subscribe(OnNotify);
		}

		public void UnsubcribeGameEvent() {
			evt.Unsubscribe(OnNotify);
		}

		public void OnNotify() {
			listener.Invoke();
		}
	}

	enum RegisterType {
		OnStart,
		OnEnable,
	}

	enum UnRegisterType {
		OnDisable,
		OnDestroy,
	}
	#endregion// classes
}
