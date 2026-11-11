using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Check click enough to open Cheat poup </summary>
public class CheatActivator : MonoBehaviour {
	[SerializeField] int clickCountToOpenCheat = 3;
	[SerializeField] UnityEvent onEnableEvent;
	[SerializeField] UnityEvent requestCheatPopupEvent;

	void OnEnable() {
		onEnableEvent.Invoke();
	}

	int _clickCount = 0;
	public void In_OnPress() {
		_clickCount++;
		if(_clickCount >= clickCountToOpenCheat) {
			_clickCount = 0;
			requestCheatPopupEvent.Invoke();
		}
	}
}
