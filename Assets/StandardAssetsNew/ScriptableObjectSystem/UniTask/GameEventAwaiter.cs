using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
//using Cysharp.Threading.Tasks.Linq;
using System;
using System.Threading;

///<summary> Read the main page of Unitask https://github.com/Cysharp/UniTask, find "UniTaskCompletionSource", to understand how we can use UniTaskCompletionSource to create a custom async wait for gameEvent </summary>
public class GameEventAwaiter {
	///<summary> Auto un-register any callback in destructor </summary>
	~GameEventAwaiter() {
		ClearExistCallback();
	}

	public UniTask Start(GameEvent gameEvent) {
		ClearExistCallback();
		_completeSource = new UniTaskCompletionSource();
		_eventToListen = gameEvent;
		_eventToListen.Subscribe(OnEventRaise);
		return _completeSource.Task;
	}

	///<summary> Call this to interrupt </summary>
	public void Cancel() {
		ClearExistCallback();
	}

	GameEvent _eventToListen = null;
	UniTaskCompletionSource _completeSource = null;
	void OnEventRaise() {
		// unsubcribe for event, and also call TrySetResult to let the asyncTask to complete
		ClearExistCallback();
		_completeSource.TrySetResult();
	}

	void ClearExistCallback() {
		_eventToListen?.Unsubscribe(OnEventRaise);
	}
}
