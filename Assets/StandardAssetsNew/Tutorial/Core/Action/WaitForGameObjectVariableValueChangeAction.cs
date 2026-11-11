using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wait for the value of a GameObjectVariable's value to be changed (eg: user touch hit a toy, the touchToyVar changed ..)
/// </summary>
[CreateAssetMenu(fileName = "WaitForGameObjectVariableValueChangeAction", menuName = "Tutorial/Action/WaitForGameObjectVariableValueChangeAction")]
public class WaitForGameObjectVariableValueChangeAction : TutorialActionBase {
	#region Init, config
	[Header("Config")]
	[SerializeField] GameObjectVariable variable;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	ClassVariableValueChangeAwaiter<GameObject> _awaiter;

	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		// dispose to clear everything first
		if(_awaiter != null) {
			_awaiter.Cancel();
		}
		_awaiter = new ClassVariableValueChangeAwaiter<GameObject>();
		await _awaiter.Start(variable);
		return true;
	}

	public override void Dispose() {
		if(_awaiter != null) {
			_awaiter.Cancel();
			_awaiter = null;
		}
	}
	#endregion//Public
}
