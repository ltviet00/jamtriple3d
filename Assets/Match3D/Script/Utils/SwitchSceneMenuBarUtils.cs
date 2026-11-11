#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SwitchSceneMenuBarUtils {
	#region Constant
	const string HOME_SCENE = "Assets/Match3D/Scene/Home.unity";
	const string SPLASH_SCENE = "Assets/Match3D/Scene/Splash.unity";
	const string GAME_SCENE = "Assets/Match3D/Scene/Game.unity";
	const string TUTORIAL_MATCHING_SCENE = "Assets/Match3D/Scene/Tutorial.unity";
	#endregion// Constant


	#region Public statics
	// click command-0 to go to the prelaunch scene and then play
	[MenuItem("Utilities/Play from Splash Scene %0")]
	public static void PlayFromPrelaunchScene() {
		UnloadAllScenes();
		OpenScene(SPLASH_SCENE, OpenSceneMode.Additive);
		PlayCurrentScene();
	}

	[MenuItem("Utilities/Go to Splash")]
	public static void GotoSplashScene() {
		OpenScene(SPLASH_SCENE);
	}

	[MenuItem("Utilities/Go to Home")]
	public static void GotoMainMenuScene() {
		OpenScene(HOME_SCENE);
	}

	[MenuItem("Utilities/Go to Game")]
	public static void GotoGameScene() {
		OpenScene(GAME_SCENE);
	}

	[MenuItem("Utilities/Go to Tutorial Matching")]
	public static void GotoTutorialMatching() {
		OpenScene(TUTORIAL_MATCHING_SCENE);
	}
	#endregion// Public statics


	#region Private statics
	static void OpenScene(string sceneName, OpenSceneMode openSceneMode = OpenSceneMode.Single) {
		if(EditorApplication.isPlaying == true) {
			EditorApplication.isPlaying = false;
			return;
		}
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene(sceneName);
	}

	static void PlayCurrentScene() {
		if(!EditorApplication.isPlaying)
			EditorApplication.isPlaying = true;
	}

	static void UnloadAllScenes() {
		for(int n = 0; n < EditorSceneManager.sceneCount; n++) {
			var scene = EditorSceneManager.GetSceneAt(n);
			EditorSceneManager.CloseScene(scene, false);
		}
	}
	#endregion//Private statics
}
#endif//UNITY_EDITOR