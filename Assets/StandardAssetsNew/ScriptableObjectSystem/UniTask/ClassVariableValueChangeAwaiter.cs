using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> We want to await OnValueChange of a ClassVariable, then create this Utils class to create a UniTask func for that purpose.
/// Read the main page of Unitask https://github.com/Cysharp/UniTask, find "UniTaskCompletionSource", to understand how we can use UniTaskCompletionSource to create a custom async wait for gameEvent </summary>
public class ClassVariableValueChangeAwaiter<T> where T : class {
	///<summary> Auto un-register any callback in destructor </summary>
	~ClassVariableValueChangeAwaiter() {
		ClearExistCallback();
	}

	public UniTask Start(ClassVariable<T> variable) {
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
	ClassVariable<T> _variableToListen = null;
	UniTaskCompletionSource _completeSource = null;
	void OnValueChanged(T value) {
		// unsubcribe for event, and also call TrySetResult to let the asyncTask to complete
		ClearExistCallback();
		_completeSource.TrySetResult();
	}

	void ClearExistCallback() {
		if(_variableToListen != null) {
			_variableToListen.OnValueChangedHandler -= OnValueChanged;
		}
	}
}
