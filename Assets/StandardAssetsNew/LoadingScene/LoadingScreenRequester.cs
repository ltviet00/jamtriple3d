using UnityEngine;
using System.Collections;
using UniRx;
using System;
using UnityEngine.SceneManagement;


/// <summary>
/// Config loading parameters (time, mode, scene...). have a interface with LoadNextScene()
/// function, the other component can invoke it via UnityEvent (remove coupling)
/// </summary>
public class LoadingScreenRequester : MonoBehaviour {
	#region Init, config
	[SerializeField] string nextSceneName;

	void OnValidate() {
		Log.Warning(!string.IsNullOrEmpty(nextSceneName), this, "Missing nextSceneName");
	}

	void Start() {
		// Use Application.CanStreamedLevelBeLoaded instead of SceneManager.GetSceneByName
		// dont know why the result from SceneManager cant help
		bool sceneValid = Application.CanStreamedLevelBeLoaded(nextSceneName);
		Log.Error(sceneValid, this, "Not found scene with name [{0}]", nextSceneName);
	}
	#endregion// Init, config


	#region Public - receive command from UI
	///<summay> Call this to start the Loading process</summay>
	public void In_Process() {
		Log.Info(this, $"In_Process, scene [{nextSceneName}], time [{Time.time}]");
		Process(nextSceneName);
	}

	///<summary> Loading next sceen with a delay </summary>>
	public void In_ProcessWithDelay(float delay) {
		Log.Info(this, $"In_ProcessWithDelay [{delay}], scene [{nextSceneName}], time [{Time.time}]");
		Observable.Timer(TimeSpan.FromSeconds(delay)).Subscribe(_ => Process(nextSceneName));
	}

	public void In_LoadCustomScene(string sceneName) {
		Log.Info(this, $"In_LoadCustomScene, scene [{sceneName}], time [{Time.time}]");
		Process(sceneName);
	}

	public void In_LoadCustomSceneWithDelay(string sceneName, float delay) {
		Log.Info(this, $"In_LoadCustomSceneWithDelay, delay [{delay}], scene [{sceneName}], time [{Time.time}]");
		nextSceneName = sceneName;
		Observable.Timer(TimeSpan.FromSeconds(delay)).Subscribe(_ => Process(nextSceneName));
	}

	public void In_ReloadCurrentSceneWithDelay(float delay) {
		nextSceneName = SceneManager.GetActiveScene().name;
		Log.Info(this, $"In_ReloadCurrentSceneWithDelay, delay [{delay}], scene [{nextSceneName}], time [{Time.time}]");
		Observable.Timer(TimeSpan.FromSeconds(delay)).Subscribe(_ => Process(nextSceneName));
	}
	#endregion// Public - receive command


	#region Private
	void Process(string nextScene) {
		LoadingScreen.Instance.Load(nextScene);
	}
	#endregion//Private
}