using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Turn some common events (Start, Awake, OnEnable, OnDisable ...) into UnityEvent
/// </summary>
public class CommonEventRaiser : MonoBehaviour {
	public UnityEvent OnAwakeHandler;
	public UnityEvent OnStartHandler;
	public UnityEvent OnEnableHandler;
	public UnityEvent OnDisableHandler;

	void Awake() {
		OnAwakeHandler.Invoke();
	}

	void Start() {
		OnStartHandler.Invoke();
	}

	void OnEnable() {
		OnEnableHandler.Invoke();
	}

	void OnDisable() {
		OnDisableHandler.Invoke();
	}
}
