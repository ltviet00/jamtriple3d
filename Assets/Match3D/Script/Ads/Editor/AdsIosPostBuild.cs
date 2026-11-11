#if UNITY_EDITOR && UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class AdsIosPostBuild : IPostprocessBuildWithReport {
	const string TrackingDescription =
		"Your data helps us deliver more relevant ads and keep the game free.";

	public int callbackOrder => 100;

	public void OnPostprocessBuild(BuildReport report) {
		if(report.summary.platform != BuildTarget.iOS) {
			return;
		}

		ConfigureIos(report.summary.outputPath);
	}

	static void ConfigureIos(string buildPath) {
		var plistPath = Path.Combine(buildPath, "Info.plist");
		var plist = File.ReadAllText(plistPath)
			.TrimStart('\uFEFF')
			.Replace("\"[]>", "\">");
		var escapedDescription = EscapePlistString(TrackingDescription);
		const string key = "<key>NSUserTrackingUsageDescription</key>";

		if(plist.Contains(key)) {
			File.WriteAllText(plistPath, plist);
			return;
		}

		var rootDictEnd = plist.LastIndexOf("</dict>", StringComparison.Ordinal);
		if(rootDictEnd < 0) {
			throw new InvalidDataException("Info.plist does not contain a closing root dict.");
		}

		var entry =
			"\t<key>NSUserTrackingUsageDescription</key>\n" +
			"\t<string>" + escapedDescription + "</string>\n";
		plist = plist.Insert(rootDictEnd, entry);
		File.WriteAllText(plistPath, plist);
	}

	static string EscapePlistString(string value) {
		return value
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&apos;");
	}
}
#endif
