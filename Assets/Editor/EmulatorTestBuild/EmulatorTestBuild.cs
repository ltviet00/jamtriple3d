#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Dev tool: build a debug APK (ARM64 only, debug keystore) for testing on the Android emulator.
/// - Menu: Tools/Emulator Test/Build APK
/// - Or create the file "Temp/emulator_build_trigger" while the Editor is open, result is written to "Temp/emulator_build_result.txt"
/// Output: Builds/Emulator/BoxJam_test.apk
/// </summary>
[InitializeOnLoad]
public static class EmulatorTestBuild {
	const string TriggerPath = "Temp/emulator_build_trigger";
	const string ResultPath = "Temp/emulator_build_result.txt";
	const string OutputPath = "Builds/Emulator/BoxJam_test.apk";
	static double _nextCheck;

	static EmulatorTestBuild() {
		EditorApplication.update += Poll;
	}

	static void Poll() {
		if(EditorApplication.timeSinceStartup < _nextCheck)
			return;
		_nextCheck = EditorApplication.timeSinceStartup + 2;
		if(!File.Exists(TriggerPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			return;
		File.Delete(TriggerPath);
		Build();
	}

	[MenuItem("Tools/Emulator Test/Build APK")]
	public static void Build() {
		var arch = PlayerSettings.Android.targetArchitectures;
		var customKeystore = PlayerSettings.Android.useCustomKeystore;
		string result;
		try {
			File.WriteAllText(ResultPath, "BUILDING " + DateTime.Now.ToString("HH:mm:ss"));
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.Android.useCustomKeystore = false;
			EditorUserBuildSettings.buildAppBundle = false;
			Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
			var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
			var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
				scenes = scenes,
				locationPathName = OutputPath,
				target = BuildTarget.Android,
				options = BuildOptions.Development,
			});
			result = $"{report.summary.result} {report.summary.totalTime} errors={report.summary.totalErrors}";
			if(report.summary.result != BuildResult.Succeeded) {
				foreach(var step in report.steps)
					foreach(var msg in step.messages)
						if(msg.type == LogType.Error || msg.type == LogType.Exception)
							result += "\n" + msg.content;
			}
		}
		catch(Exception e) {
			result = "Exception " + e;
		}
		finally {
			PlayerSettings.Android.targetArchitectures = arch;
			PlayerSettings.Android.useCustomKeystore = customKeystore;
			// build saved the temp settings to ProjectSettings.asset, save again with the original values
			AssetDatabase.SaveAssets();
		}
		File.WriteAllText(ResultPath, result);
		Debug.Log("[EmulatorTestBuild] " + result);
	}
}
#endif
