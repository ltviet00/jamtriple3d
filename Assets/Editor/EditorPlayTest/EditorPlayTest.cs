using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dev tool: run a small scripted test in the Editor Play mode, driven by a file (so tools outside Unity can run it).
/// Create "Temp/editor_play_test" -> enter Play mode, press the home LEVEL button, take a Game view screenshot
/// and dump the state of the level popup booster buttons + cameras to "Temp/editor_play_test_result.txt", then stop.
/// </summary>
[InitializeOnLoad]
public static class EditorPlayTest {
	const string TRIGGER = "Temp/editor_play_test";
	const string RESULT = "Temp/editor_play_test_result.txt";
	const string SHOT = "Temp/editor_play_test_shot.png";
	const string RUNNING_KEY = "EditorPlayTest.running";
	const string STEP_KEY = "EditorPlayTest.step";
	const float CLICK_LEVEL_AT = 10f, DUMP_AT = 15f, STOP_AT = 18f;

	static double _nextCheck;
	static double _playStart = -1;

	static EditorPlayTest() {
		EditorApplication.update += Update;
		EditorApplication.playModeStateChanged += state => {
			if(state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RUNNING_KEY, false))
				_playStart = EditorApplication.timeSinceStartup;
			if(state == PlayModeStateChange.EnteredEditMode)
				SessionState.SetBool(RUNNING_KEY, false);
		};
	}

	static void Update() {
		if(!EditorApplication.isPlaying) {
			if(EditorApplication.timeSinceStartup < _nextCheck)
				return;
			_nextCheck = EditorApplication.timeSinceStartup + 1;
			if(!File.Exists(TRIGGER) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			File.Delete(TRIGGER);
			if(File.Exists(RESULT))
				File.Delete(RESULT);
			if(File.Exists(SHOT))
				File.Delete(SHOT);
			SessionState.SetBool(RUNNING_KEY, true);
			SessionState.SetInt(STEP_KEY, 0);
			EditorApplication.isPlaying = true;
			return;
		}
		if(!SessionState.GetBool(RUNNING_KEY, false))
			return;
		if(_playStart < 0)
			_playStart = EditorApplication.timeSinceStartup;
		var t = EditorApplication.timeSinceStartup - _playStart;
		var step = SessionState.GetInt(STEP_KEY, 0);
		if(step == 0 && t > CLICK_LEVEL_AT) {
			SessionState.SetInt(STEP_KEY, 1);
			Append("click: " + ClickHomeLevelButton());
		}
		else if(step == 1 && t > DUMP_AT) {
			SessionState.SetInt(STEP_KEY, 2);
			ScreenCapture.CaptureScreenshot(SHOT);
			Append(DumpState());
		}
		else if(step == 2 && t > STOP_AT) {
			SessionState.SetInt(STEP_KEY, 3);
			Append("done");
			EditorApplication.isPlaying = false;
		}
	}

	static void Append(string text) {
		File.AppendAllText(RESULT, text + "\n");
	}

	///<summary> Home "LEVEL xx" button: a Button whose label starts with LEVEL, outside the level popup </summary>
	static string ClickHomeLevelButton() {
		foreach(var button in Object.FindObjectsOfType<Button>()) {
			if(!button.isActiveAndEnabled || PathOf(button.transform).Contains("LevelPageInfo"))
				continue;
			var label = button.GetComponentsInChildren<Component>(true)
				.Select(c => c.GetType().GetProperty("text")?.GetValue(c) as string)
				.FirstOrDefault(s => !string.IsNullOrEmpty(s) && System.Text.RegularExpressions.Regex.IsMatch(s.Trim().ToUpper(), @"^LEVEL\s*\d+$"));
			if(label == null)
				continue;
			button.onClick.Invoke();
			return $"{PathOf(button.transform)} [{label}]";
		}
		return "NOT FOUND";
	}

	static string DumpState() {
		var sb = new StringBuilder();
		sb.AppendLine("== booster buttons");
		foreach(var rect in Resources.FindObjectsOfTypeAll<RectTransform>()) {
			if(!rect.gameObject.scene.IsValid() || !rect.name.StartsWith("PreboosterBtn_"))
				continue;
			var canvas = rect.GetComponentInParent<Canvas>(true);
			sb.AppendLine($"{PathOf(rect)} activeSelf={rect.gameObject.activeSelf} inHierarchy={rect.gameObject.activeInHierarchy}");
			sb.AppendLine($"   pos={rect.position} anchored={rect.anchoredPosition} localScale={rect.localScale} lossy={rect.lossyScale} size={rect.rect.size}");
			if(canvas != null) {
				var root = canvas.rootCanvas;
				sb.AppendLine($"   canvas={PathOf(canvas.transform)} enabled={canvas.enabled} root={root.name} mode={root.renderMode} cam={(root.worldCamera ? root.worldCamera.name : "null")} sortOrder={root.sortingOrder} layer={LayerMask.LayerToName(rect.gameObject.layer)}");
			}
			var alpha = 1f;
			foreach(var group in rect.GetComponentsInParent<CanvasGroup>(true))
				alpha *= group.alpha;
			sb.AppendLine($"   canvasGroupAlpha={alpha}");
			foreach(var image in rect.GetComponentsInChildren<Image>(true))
				sb.AppendLine($"   img {image.name} active={image.gameObject.activeInHierarchy} enabled={image.enabled} sprite={(image.sprite ? image.sprite.name : "null")} a={image.color.a}");
		}
		sb.AppendLine("== cameras");
		foreach(var cam in Resources.FindObjectsOfTypeAll<Camera>()) {
			if(!cam.gameObject.scene.IsValid())
				continue;
			sb.AppendLine($"{PathOf(cam.transform)} enabled={cam.enabled} active={cam.gameObject.activeInHierarchy} tag={cam.tag} ortho={cam.orthographic} size={cam.orthographicSize} depth={cam.depth} mask={cam.cullingMask} pos={cam.transform.position} display={cam.targetDisplay}");
		}
		sb.AppendLine($"Camera.main={(Camera.main ? PathOf(Camera.main.transform) : "null")}  screen={Screen.width}x{Screen.height}");
		return sb.ToString();
	}

	static string PathOf(Transform t) {
		var parts = new List<string>();
		for(; t != null; t = t.parent)
			parts.Insert(0, t.name);
		return string.Join("/", parts);
	}
}
