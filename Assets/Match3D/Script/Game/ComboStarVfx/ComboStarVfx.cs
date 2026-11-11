using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// The star VFX spawn by Spawner, and head to the Star on the header of UI
/// </summary>
public class ComboStarVfx : MonoBehaviour {
	#region Init, config
	[Header("ShareData - in")]
	[SerializeField] TransformVariable starUIPositionVar;

	[Header("ShareData - out")]
	[SerializeField] IntegerVariable userStarCollectedVar;

	[Header("Config")]
	[SerializeField] Vector2Variable flyTimeDurationRangeVar;
	[SerializeField] Vector2Variable spawnCircleRadiusRangeVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	public async UniTask RandomCurveAndStartMovingAsync() {
		// create bezier curve
		var randomCirclePos = transform.position + (Vector3)(Random.insideUnitCircle * spawnCircleRadiusRangeVar.RandomBetweenXY());
		var bezierCurve = new QuadraticBezierCurve(transform.position, randomCirclePos, starUIPositionVar.Value.position);
		var randomDuration = flyTimeDurationRangeVar.RandomBetweenXY();
		var timer = 0f;
		while(timer < randomDuration) {
			await UniTask.Yield(cancellationToken: _token.Token);
			timer += Time.deltaTime;
			var ratio = timer / randomDuration;
			var pos = bezierCurve.GetPointInCurve(ratio);
			transform.position = pos;
		}
		// reached target. Disable it, to back to pool
		gameObject.SetActive(false);
		// increase user star count
		userStarCollectedVar.Value++;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
