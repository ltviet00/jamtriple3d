using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Spawn combo star vfx when user match success
/// </summary>
public class ComboStarVfxSpawner : MonoBehaviour {
	#region Init, config
	[Header("ShareData - in")]
	[SerializeField] IntegerVariable comboCountVar;
	[SerializeField] TransformVariable toyCatcherPostionVar;

	[Header("Config")]
	[SerializeField] ComboStarVfx comboVfxPrefab;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		comboCountVar.OnValueChangedHandler += OnComboChanged;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		comboCountVar.OnValueChangedHandler -= OnComboChanged;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	void OnComboChanged() {
		if(comboCountVar.Value <= 0)
			return;
		// simple spawn the vfx, moving part will be taken care by ComboStarVfx
		for(int n = 0; n < comboCountVar.Value; n++) {
			var vfx = ObjectPool.SpawnObject(comboVfxPrefab.gameObject);
			vfx.transform.position = toyCatcherPostionVar.Value.position;
			var script = vfx.GetComponent<ComboStarVfx>();
			//script.RandomCurveAndStartMovingAsync().AttachExternalCancellation(_token.Token);
			script.RandomCurveAndStartMovingAsync().Forget();
		}
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
