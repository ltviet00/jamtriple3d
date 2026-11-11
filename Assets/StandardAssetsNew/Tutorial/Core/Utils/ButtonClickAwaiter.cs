using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

///<summary> Read the main page of Unitask https://github.com/Cysharp/UniTask, find "UniTaskCompletionSource", to understand how we can use UniTaskCompletionSource to create a custom async wait for gameEvent </summary>
public class ButtonClickAwaiter {
	///<summary> Auto un-register any callback in destructor </summary>
	~ButtonClickAwaiter() {
		ClearExistCallback();
	}

	public UniTask Start(Button button) {
		ClearExistCallback();
		_completeSource = new UniTaskCompletionSource();
		_targetButton = button;
		_targetButton.onClick.AddListener(OnEventRaise);
		return _completeSource.Task;
	}

	///<summary> Call this to interrupt </summary>
	public void Cancel() {
		ClearExistCallback();
	}

	Button _targetButton = null;
	UniTaskCompletionSource _completeSource = null;
	void OnEventRaise() {
		// unsubcribe for event, and also call TrySetResult to let the asyncTask to complete
		ClearExistCallback();
		_completeSource.TrySetResult();
	}

	void ClearExistCallback() {
		_targetButton?.onClick.RemoveListener(OnEventRaise);
	}
}
