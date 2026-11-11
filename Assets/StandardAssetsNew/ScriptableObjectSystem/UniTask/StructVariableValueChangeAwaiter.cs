using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> We want to await OnValueChange of a ClassVariable, then create this Utils class to create a UniTask func for that purpose.
/// Read the main page of Unitask https://github.com/Cysharp/UniTask, find "UniTaskCompletionSource", to understand how we can use UniTaskCompletionSource to create a custom async wait for gameEvent </summary>
public class StructVariableValueChangeAwaiter<T> where T : struct {
	///<summary> Auto un-register any callback in destructor </summary>
	~StructVariableValueChangeAwaiter() {
		ClearExistCallback();
	}

	public UniTask Start(StructVariable<T> variable) {
		ClearExistCallback();
		_completeSource = new UniTaskCompletionSource();
		//_eventToListen = variable.OnValueChangedHandler;
		//_eventToListen.Subscribe(OnEventRaise);
		_variableToListen = variable;
		_variableToListen.OnValueChangedHandler += OnValueChanged;
		return _completeSource.Task;
	}

	///<summary> Call this to interrupt </summary>
	public void Cancel() {
		ClearExistCallback();
	}

	//GameEvent _eventToListen = null;
	StructVariable<T> _variableToListen = null;
	UniTaskCompletionSource _completeSource = null;
	void OnValueChanged() {
		// unsubcribe for event, and also call TrySetResult to let the asyncTask to complete
		ClearExistCallback();
		_completeSource.TrySetResult();
	}

	void ClearExistCallback() {
		_variableToListen.OnValueChangedHandler -= OnValueChanged;
	}
}
