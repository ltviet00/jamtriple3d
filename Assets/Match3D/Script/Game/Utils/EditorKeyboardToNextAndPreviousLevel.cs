using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Editor code only, for next, previous level, use for Editor only </summary>
public class EditorKeyboardToNextAndPreviousLevel : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent loadingSceneFinishEvent;

	[Header("Event out, shareData")]
	[SerializeField] GameEvent gameEndEvent;
	[SerializeField] IntegerVariable selectedLevelIndexVar;
	[SerializeField] LevelDataStorage levelStorage;

	[Header("Refs")]
	[SerializeField] LoadingScreenRequester sceneLoader;

	[Header("Config")]
	[SerializeField] KeyCode nextLevelKey;
	[SerializeField] KeyCode previousLevelKey;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void Start() {
		// default disable the Update() until loading finish
		enabled = false;
		loadingSceneFinishEvent.Subscribe(OnLoadingScreneFinish);
	}

	void OnLoadingScreneFinish() {
		enabled = true;
		loadingSceneFinishEvent.Unsubscribe(OnLoadingScreneFinish);
	}
	#endregion// Init config


#if UNITY_EDITOR
	void Update() {
		if(Input.GetKeyDown(nextLevelKey)) {
			selectedLevelIndexVar.Value++;
			ReloadGame();
		}
		else if(Input.GetKeyDown(previousLevelKey)) {
			selectedLevelIndexVar.Value--;
			ReloadGame();
		}
	}

	void ReloadGame() {
		// disable is, prevent spam on keyboard
		enabled = false;
		// raise event to stop all module
		gameEndEvent.Raise();
		// clamp selected level index
		var levelIndex = selectedLevelIndexVar.Value;
		levelIndex = Mathf.Clamp(levelIndex, 0, levelStorage.GetMaxLevel() - 1);
		selectedLevelIndexVar.Value = levelIndex;
		sceneLoader.In_Process();
	}
#endif
}
