#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Test tool, Development build only: jump to a level by pushing a file before launching the app.
///   adb shell "echo 499 > /sdcard/Android/data/[package]/files/debug_level.txt"
/// The number is the level index (0-based, level 500 = 499). The file is deleted after use.
/// Also refills lives (energy) so many levels can be tested in a row.
/// </summary>
public static class DebugLevelJump {
	const string FILE_NAME = "debug_level.txt";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Register() {
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
		TryJump();
	}

	static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		TryJump();
	}

	static void TryJump() {
		var path = Path.Combine(Application.persistentDataPath, FILE_NAME);
		if(!File.Exists(path))
			return;
		if(!int.TryParse(File.ReadAllText(path).Trim(), out var levelIndex))
			return;
		var inventories = Resources.FindObjectsOfTypeAll<UserLevelScoreInventory>();
		if(inventories.Length == 0)
			return;
		File.Delete(path);
		foreach(var inventory in inventories) {
			inventory.Data.reachedLevel = Mathf.Max(0, levelIndex);
			inventory.SaveToDisk();
		}
		foreach(var energy in Resources.FindObjectsOfTypeAll<UserEnergyInventory>()) {
			for(int n = 0; n < energy.MaxEnergy && !energy.IsFull(); n++)
				energy.IncreaseOneEnergyAndSaveCurrentTime();
		}
		Debug.Log($"[DebugLevelJump] reachedLevel = {levelIndex}");
	}
}
#endif
