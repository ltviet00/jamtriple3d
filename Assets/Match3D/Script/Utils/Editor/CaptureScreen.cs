using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class CaptureScreen : EditorWindow {
	//void Update() {
	//	if(Input.GetKeyDown(KeyCode.Space)) {
	//		TakeScreenShot();
	//	}
	//}

	[MenuItem("Utilities/TakeScreenShot")]
	static void ObjectSearchReferenceInScene() {
		// some infomation for screenshot
		var activeScene = EditorSceneManager.GetActiveScene();
		var time = (int)EditorApplication.timeSinceStartup;
		// capture
		var savePath = $"{activeScene.name}_{time}.png";
		ScreenCapture.CaptureScreenshot(savePath);
		Log.Info($"Take screenshot [{savePath}] done");
	}
}
#endif//UNIYT_EDITOR
