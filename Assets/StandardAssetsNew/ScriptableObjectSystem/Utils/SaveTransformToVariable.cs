using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple save the transform ref to the TransformVariable at OnEnable() and reset to null at OnDisable
/// </summary>
public class SaveTransformToVariable : MonoBehaviour {
	[SerializeField] TransformVariable saveToVar;

	void OnValidate() {
		Log.ValidateNotNull(saveToVar, this, "saveToVar");
	}

	bool IsFailedConfig() {
		return saveToVar == null;
	}

	void OnEnable() {
		if(IsFailedConfig())
			return;
		ActiveWorking();
	}

	void OnDisable() {
		if(IsFailedConfig())
			return;
		DeactiveWorking();
	}

	void ActiveWorking() {
		saveToVar.Value = transform;
	}

	void DeactiveWorking() {
		// carefully check, in some case more than one "saver" write to same var, so check if the current
		// pointer in variable is ours, then reset, if not, ignore
		if(saveToVar.Value != null && saveToVar.Value == transform) {
			saveToVar.Value = null;
		}
	}
}
