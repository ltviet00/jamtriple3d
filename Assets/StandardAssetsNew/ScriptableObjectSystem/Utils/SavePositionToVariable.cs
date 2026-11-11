using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Save world position to target variable
/// </summary>
public class SavePositionToVariable : MonoBehaviour {
	#region Init, config
	[Header("Target variable")]
	[SerializeField] Vector3Variable saveToVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		saveToVar.Value = transform.position;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		saveToVar.Value = Vector3.zero;
	}
	#endregion// Init config
}
