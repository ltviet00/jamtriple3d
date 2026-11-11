using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Currently used for VFX of LevelChest and StarChest. We need dot to follow fillEffect.
/// Simple create a parent object, a dot (will rotate circle with radius) will be children, with an offset to parent (radius)
/// Rotate parent to let child moving on circle path.
/// By default we disable the dot when finish
/// </summary>
public class DotFollowImageCircleFill : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] GameObject rootOfMovingDot;

	[Header("Config")]
	[SerializeField] Vector2 customRotateRangeOnZcoord = new Vector2(0, 1f);
	//[SerializeField] bool isClockwise = true;
	[SerializeField] float delayAtLastBeforeDisable = 0.2f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// disable by default
		rootOfMovingDot.SetActive(false);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	public async UniTask PlayEffect(float fromPercent, float toPercent, float duration) {
		if(isFailedConfig)
			return;
		// active moving dot first
		rootOfMovingDot.SetActive(true);

		var fromZ = Mathf.Lerp(customRotateRangeOnZcoord.x, customRotateRangeOnZcoord.y, fromPercent);
		var toZ = Mathf.Lerp(customRotateRangeOnZcoord.x, customRotateRangeOnZcoord.y, toPercent);
		var rotate = rootOfMovingDot.transform.localEulerAngles;

		var timer = 0f;
		while(timer < duration) {
			await UniTask.Yield(cancellationToken: _disableToken.Token);
			timer += Time.deltaTime;
			var ratio = timer / duration;
			var targetZ = Mathf.Lerp(fromZ, toZ, ratio);
			rotate.z = targetZ;
			rootOfMovingDot.transform.localEulerAngles = rotate;
		}
		// finish, let it hit target
		rotate.z = toZ;
		rootOfMovingDot.transform.localEulerAngles = rotate;
		// delay a bit before deactive
		await UniTask.Delay(TimeSpan.FromSeconds(delayAtLastBeforeDisable), cancellationToken: _disableToken.Token);
		rootOfMovingDot.SetActive(false);
	}
	#endregion//Public


	#region Private
	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
