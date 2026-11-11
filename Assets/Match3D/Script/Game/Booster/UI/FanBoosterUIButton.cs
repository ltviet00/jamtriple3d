using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle UI and function for the fan booster button on Booster bar.
/// UPDATE: deprecated function for the Find3D-Triple version
/// </summary>
public class FanBoosterUIButton : MonoBehaviour {
	#region Init, config
	[Header("User")]
	[SerializeField] UserBoosterInventory userBoosterInventory;
	[SerializeField] IntegerVariable userReachedLevelVar;

	[Header("ShareData, config")]
	[SerializeField] LevelToyStatistics toyStats;
	[SerializeField] IntegerVariable levelToUnlockVar;

	[SerializeField] FloatVariable fanCoolDownDurationCfg;
	[SerializeField] FloatVariable customTimeScaleVar;

	[Header("ShareData - out")]
	[SerializeField] FloatVariable currentCooldownTimerVar;

	[Header("Prebooster fan")]
	[SerializeField] BooleanVariable activePreFanBoosterVar;
	[SerializeField] float preFanFillUpDuration = 0.5f;

	[Header("Config - push toy")]
	[SerializeField] Vector2Variable pushToyVelocityVar;
	[SerializeField] Vector2Variable pushToyXrandomVar;
	[SerializeField] Vector2Variable pushToyYrandomVar;

	[Header("Refs - locked")]
	[SerializeField] GameObject rootLockedContent;
	[SerializeField] TMP_Text levelToUnlockText;

	[Header("Refs - unlocked")]
	[SerializeField] GameObject rootUnlockedContent;
	[SerializeField] Button mainButton;
	[SerializeField] Image fillProgressImage;
	[SerializeField] Image fanIconImage;

	[Header("Config - fan sprite")]
	[SerializeField] Sprite fanReadySprite;
	[SerializeField] Sprite fanCoolingSprite;

	[Header("Config - fan rotate")]
	[SerializeField] float fanRotatingDuration = 2f;
	[SerializeField] int fanRotatingCircleCount = 5;
	[SerializeField] Easing.TweenType fanRotateTween;

	[Header("Config - vfx")]
	[SerializeField] GameObject fanReadyVfxPrefab;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
		// by default, not ready at first run
		ActiveContentBaseOnUseLevel();
		SetReadyState(false);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI invoke
	///<summary> Config the Fan button to invoke this </summary>
	public void In_OnClick() {
		if(isFailedConfig)
			return;
		// affect : rotate fan + push toy
		FanRotatingEffect().Forget();
		AddVelocityOnAllToys();
		// start cooling process ,vfx at last
		CoolDownProcessAsync(fanCoolDownDurationCfg.Value).Forget();
	}
	#endregion//UI invoke


	#region Public - from mainflow
	///<summary> called from mainflow to process pre-fan booster </summary>
	public async UniTask ProcessPreFanBooster() {
		if(isFailedConfig)
			return;
		// no pre-fan, normal case, do first cooling down
		if(!activePreFanBoosterVar.Value) {
			SetReadyState(false);
			CoolDownProcessAsync(fanCoolDownDurationCfg.Value).Forget();
			return;
		}
		// got pre-fan, play quick cooldown fill up , and reset the pre-fan value
		activePreFanBoosterVar.Value = false;
		await CoolDownProcessAsync(preFanFillUpDuration);
	}
	#endregion//Public - from mainflow


	#region Private
	void ActiveContentBaseOnUseLevel() {
		// active root content
		var locked = userReachedLevelVar.Value < levelToUnlockVar.Value;
		rootLockedContent.SetActive(locked);
		rootUnlockedContent.SetActive(!locked);
		// other content
		if(locked) {
			// +1 cause level start from 0
			const string FORMAT = "LVL {0}";
			var text = string.Format(FORMAT, levelToUnlockVar.Value + 1);
			levelToUnlockText.text = text;
		}
	}

	void AddVelocityOnAllToys() {
		var allToyMap = toyStats.AllMaps;
		for(int n = 0; n < allToyMap.Count; n++) {
			var map = allToyMap[n];
			foreach(var list in map.Values) {
				for(int m = 0; m < list.Count; m++) {
					var toy = list[m];
					var velo = Vector3.zero;
					// we changed gravity coordinate to Z, and -Z mean push toy up
					velo.z = -1f * pushToyVelocityVar.RandomBetweenXY();
					// random x, y
					velo.x = pushToyXrandomVar.RandomBetweenXY();
					velo.y = pushToyYrandomVar.RandomBetweenXY();
					toy.mainRigid.velocity = velo;
				}
			}
		}
	}

	async UniTask CoolDownProcessAsync(float duration) {
		// start cooling. Disable button
		// not change fan sprite, the FanRotatingEffect will do that
		mainButton.interactable = false;
		fillProgressImage.fillAmount = 0f;
		fillProgressImage.enabled = true;

		// cooling down, upate fill bar
		currentCooldownTimerVar.Value = 0f;
		while(currentCooldownTimerVar.Value < duration) {
			currentCooldownTimerVar.Value += Time.deltaTime * customTimeScaleVar.Value;
			var ratio = currentCooldownTimerVar.Value / duration;
			fillProgressImage.fillAmount = ratio;
			await UniTask.Yield(_disableToken.Token);
		}

		// finish cooling, update sprite, VFX. disable filling image
		SetReadyState(true);
		var vfx = ObjectPool.SpawnObject(fanReadyVfxPrefab);
		vfx.transform.position = fanIconImage.transform.position;
		fillProgressImage.enabled = false;
	}

	async UniTask FanRotatingEffect() {
		var targetRotateZ= (float)fanRotatingCircleCount * 360f;
		var timer = 0f;

		while(timer < fanRotatingDuration) {
			timer += Time.deltaTime * customTimeScaleVar.Value;
			var ratio = timer / fanRotatingDuration;
			var tweenRotateZ = Easing.EaseFloat(0f, targetRotateZ, ratio, fanRotateTween);
			var rotate = new Vector3(0, 0, tweenRotateZ);
			fanIconImage.transform.localEulerAngles = rotate;
			await UniTask.Yield(_disableToken.Token);
		}
		// finish. change sprite fan
		fanIconImage.sprite = fanCoolingSprite;
		fanIconImage.transform.localEulerAngles = Vector3.zero;
	}

	void SetReadyState(bool ready) {
		mainButton.interactable = ready;
		fanIconImage.sprite = ready ? fanReadySprite : fanCoolingSprite;
		fillProgressImage.fillAmount = ready ? 1f : 0f;
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
