#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Test tool, Development build only: jump to a level by pushing a file before launching the app.
///   adb shell "echo 499 > /sdcard/Android/data/[package]/files/debug_level.txt"
/// The number is the level index (0-based, level 500 = 499). The file is deleted after use.
/// Also refills lives (energy) so many levels can be tested in a row.
/// Optional: mark tutorial tasks as done (one task id per line) to test a later tutorial directly:
///   adb shell "echo Task1_UsingMagnet > /sdcard/Android/data/[package]/files/debug_tutorial_done.txt"
/// </summary>
public static class DebugLevelJump {
	const string FILE_NAME = "debug_level.txt";
	const string TUTORIAL_FILE_NAME = "debug_tutorial_done.txt";

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
		TryMarkTutorialsDone();
		TryGiveUnlimitEnergy();
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

	///<summary> Name part of a toy group to force for every level (file kept), e.g. "Cartoon". Empty = random group as usual
	///   adb shell "echo Cartoon > /sdcard/Android/data/[package]/files/debug_toy_group.txt" </summary>
	public static string ForcedToyGroup() {
		var path = Path.Combine(Application.persistentDataPath, "debug_toy_group.txt");
		return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
	}

	///<summary> Give unlimited lives for N minutes (file deleted after use)
	///   adb shell "echo 60 > /sdcard/Android/data/[package]/files/debug_unlimit_energy.txt" </summary>
	static void TryGiveUnlimitEnergy() {
		var path = Path.Combine(Application.persistentDataPath, "debug_unlimit_energy.txt");
		if(!File.Exists(path) || !int.TryParse(File.ReadAllText(path).Trim(), out var minutes))
			return;
		var inventories = Resources.FindObjectsOfTypeAll<UserUnlimitEnergyInventory>();
		if(inventories.Length == 0)
			return;
		File.Delete(path);
		foreach(var inventory in inventories)
			inventory.ExpandUnlimitDurationByMinutes(minutes);
		Debug.Log($"[DebugLevelJump] unlimited energy +{minutes}m");
	}

	static void TryMarkTutorialsDone() {
		var path = Path.Combine(Application.persistentDataPath, TUTORIAL_FILE_NAME);
		if(!File.Exists(path))
			return;
		var tutorials = Resources.FindObjectsOfTypeAll<UserTutorialData>();
		if(tutorials.Length == 0)
			return;
		var ids = File.ReadAllLines(path);
		File.Delete(path);
		foreach(var tutorial in tutorials) {
			foreach(var line in ids) {
				var id = line.Trim();
				if(id.Length > 0 && !tutorial.IsTaskDone(id))
					tutorial.AddToDoneTask(id);
			}
		}
		Debug.Log($"[DebugLevelJump] tutorial done: {string.Join(", ", ids)}");
	}
}
#endif
