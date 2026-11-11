using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Use in Undo booster, put a random toy to box for tutorial
/// </summary>
[CreateAssetMenu(fileName = "PutRandomToyToBoxTutorialAction", menuName = "Tutorial/Action/PutRandomToyToBoxTutorialAction")]
public class PutRandomToyToBoxTutorialAction : TutorialActionBase {
	#region Init, config
	[Header("Data")]
	[SerializeField] LevelToyStatistics toyStats;

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
		// random pick a toy, remove it out of the toyStats
		var pickToy = toyStats.GetRandomDecoyToy();
		// put it to ToyBoxGroup
		var toyBoxGroup = SimpleDI.Instance.Get<ToyBoxGroup>();
		if(toyBoxGroup == null) {
			return false;
		}
		toyBoxGroup.TakeToyToBox(pickToy);
		// wait a bit to make sure the toy land at box before return true
		await UniTask.Delay(1000, cancellationToken: _disposeToken.Token);
		return true;
	}

	public override void Dispose() {
		ClearRunningTasks();
	}
	#endregion//Public


	#region Private
	UniTaskTokenHelper _disposeToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disposeToken.ClearRunningTasks();
	}
	#endregion//Private
}
