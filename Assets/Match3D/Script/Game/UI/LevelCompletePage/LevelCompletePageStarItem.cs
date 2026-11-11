using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to Star item in LevelComplete page, simply ref to star image, to play VFX later
/// </summary>
public class LevelCompletePageStarItem : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] Image starBgImage;
	[SerializeField] Image starFillImage;
	[SerializeField] UIElement starFillElement;

	[Header("VFX")]
	[SerializeField] GameObject starExplodeVfxPrefab;
	[SerializeField] float delayShowVfx = 0.2f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// by default disable the StarFill, will enable manually from LevelCompletePage
		starFillImage.enabled = false;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Public
	///<summary> Play animation show up star </summary>
	public void ShowUp() {
		if(isFailedConfig)
			return;
		ShowUpAndVFX().Forget();
	}
	#endregion//Public


	#region Private
	async UniTask ShowUpAndVFX() {
		// show star animation first
		starFillImage.enabled = true;
		starFillElement.Show();
		// wait a little bit (star scale down ..) then explode VFX
		await UniTask.Delay(TimeSpan.FromSeconds(delayShowVfx));
		var vfx = ObjectPool.SpawnObject(starExplodeVfxPrefab);
		vfx.transform.position = transform.position;
	}
	#endregion//Private
}
