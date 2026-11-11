using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreGameExtraTimeBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] BooleanVariable activeExtraTimeBoosterVar;
	[SerializeField] IntegerVariable amountTimeToAddMoreVar;
	[SerializeField] IntegerVariable levelTimeVar;
	[SerializeField] TransformVariable headerClockIconPostionVar;

	[Header("Config")]
	[SerializeField] GameObject extraTimePrefab;
	[SerializeField] float zoomDuration = 1f;
	[SerializeField] Vector3 scaleWhenReachUI;
	[SerializeField] float flyToHeaderDuration = 1f;
	[SerializeField] float increaseTimeDuration = 1f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> Get call from mainFlow </summary>
	public async UniTask ProcessAffecting() {
		// no active booster? do nothing
		if(!activeExtraTimeBoosterVar.Value) {
			return;
		}
		// spawn extraTime
		var extraTimeVfx = Instantiate(extraTimePrefab);
		var centerPos = transform.position;
		extraTimeVfx.transform.position = centerPos;
		extraTimeVfx.transform.localScale = Vector3.zero;
		// zoom effect
		LeanTween.scale(extraTimeVfx, Vector3.one, zoomDuration).setEaseOutElastic();
		await UniTask.Delay(TimeSpan.FromSeconds(zoomDuration * 2f));
		// move to header, also scale down
		LeanTween.move(extraTimeVfx, headerClockIconPostionVar.Value.position, flyToHeaderDuration).setEaseOutQuint();
		LeanTween.scale(extraTimeVfx, scaleWhenReachUI, flyToHeaderDuration);
		await UniTask.Delay(TimeSpan.FromSeconds(flyToHeaderDuration));
		Destroy(extraTimeVfx);

		// increase time second text
		var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
		var startFrom = levelTimeVar.Value;
		var endAt = startFrom + amountTimeToAddMoreVar.Value;

		var timer = 0f;
		while(timer < increaseTimeDuration) {
			await UniTask.Yield();
			timer += Time.deltaTime;
			var ratio = timer / increaseTimeDuration;
			var secondAmountToDisplay = (int)Mathf.Lerp(startFrom, endAt, ratio);
			levelTimer.SetRemainTimeAndUpdateShareVar(secondAmountToDisplay);
		}

		// finish job? don't forget to reset the magnet activate status
		activeExtraTimeBoosterVar.Value = false;
		// also set level time at target time, so other modules (for now the CountDownClock) will use it correctly
		levelTimer.SetRemainTimeAndUpdateShareVar(endAt);
	}
	#endregion//Public
}
