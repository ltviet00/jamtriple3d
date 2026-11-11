using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Apply new value to a float variable
/// </summary>
[CreateAssetMenu(fileName = "ChangeFloatVariableValueAction", menuName = "Tutorial/Action/ChangeFloatVariableValueAction")]
public class ChangeFloatVariableValueAction : TutorialActionBase {
	#region Init, config
	[Header("Config")]
	[SerializeField] FloatVariable targetVar;
	[SerializeField] float value;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		targetVar.Value = value;
		return true;
	}
	#endregion//Public
}
