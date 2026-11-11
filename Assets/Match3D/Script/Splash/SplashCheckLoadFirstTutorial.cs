using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach at Splash scene ,check user data to decide load the first tutorial level data or go to home scene
/// </summary>
public class SplashCheckLoadFirstTutorial : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onLoadingScreenFinishEvent;

	[Header("Event out")]
	[SerializeField] UnityEvent loadGameSceneRequest;
	[SerializeField] UnityEvent loadHomeSceneRequest;

	[Header("Data")]
	[SerializeField] UserLevelScoreInventory levelInventory;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onLoadingScreenFinishEvent.Subscribe(OnLoadingScreenFinish);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onLoadingScreenFinishEvent.Unsubscribe(OnLoadingScreenFinish);
	}
	#endregion// Init config


	#region Private
	void OnLoadingScreenFinish() {
		// if user at level 0, mean first launch, then load the game scene, tutorial will trigger in that
		if(levelInventory.GetCurrentReachLevel() == 0) {
			loadGameSceneRequest.Invoke();
		}
		else {
			loadHomeSceneRequest.Invoke();
		}
	}
	#endregion//Private
}
