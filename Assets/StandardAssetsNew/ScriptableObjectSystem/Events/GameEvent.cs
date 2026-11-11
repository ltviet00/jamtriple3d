// ----------------------------------------------------------------------------
// Unite 2017 - Game Architecture with Scriptable Objects
// 
// Author: Ryan Hipple
// Date:   10/04/17
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Create this ScriptableObject in Asset, objects that need this event will reference to it, then register event.
/// And "somewhere" from project, call Raise, and all listener will be invoked
/// </summary>
[CreateAssetMenu(menuName = "SO architect/GameEvent")]
public class GameEvent : ScriptableObject {
	// the developerDescription in private will thrown a warning. We dont need that,
	// we just need a description to read in Inspector, and dont want any class access it.
	// So disable this warning
#pragma warning disable 0414

	/// If we keep those var UNITY_EDITOR, and they are public or SerializeField, then Unity will serialize them
	/// to the asset file in project, their size will increase (size fro the string descs). But when we build to platform
	/// those var will be stripped, but the total size will be keep the same, then on device, we'll got the exception :
	/// "A script behaviour (script unknown or not yet loaded) has a different serialization layout when loading. 
	/// (Read 48 bytes but expected 88 bytes), Did you #ifdef UNITY_EDITOR a section of your serialized properties in any of your scripts?"
	/// So, we cannot use those UNITY_EDITOR things in ScriptableObject, let keep those var serialized in to asset,
	/// cost us some more memory, butits just a minor problem
	//#if UNITY_EDITOR
	[TextArea(2, 10)] [SerializeField] string developerDescription = "";
	[SerializeField] bool logWhenGetRaised = false;

	/// <summary> All listener </summary>
	event Action _listeners;
	///<summary> Listeners got invoke only once and auto unregister listen </summary>
	event Action _oneTimeLiteners;

	public void Raise() {
#if UNITY_EDITOR
		if(logWhenGetRaised) {
			Log.Info(this, $"Event [{name}] raised");
		}
#endif//UNITY_EDITOR
		if(_listeners != null) {
			_listeners.Invoke();
		}
		// onetime only listeners. Clear after invoking
		if(_oneTimeLiteners != null) {
			_oneTimeLiteners.Invoke();
			_oneTimeLiteners = null;
		}
	}

	public void Subscribe(Action callback) {
		//if(_listeners != null)
		_listeners += callback;
	}

	public void Unsubscribe(Action callback) {
		if(_listeners != null)
			_listeners -= callback;
	}

	public void SubscribleOneTimeOnly(Action callback) {
		_oneTimeLiteners += callback;
	}

	void OnDisable() {
		// clear all references
		_listeners = null;
		_oneTimeLiteners = null;
	}
}
