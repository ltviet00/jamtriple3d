using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Simulate the toys of random levels and report how big each toy is on screen after ToySpawner.NormalizeToySizes.
/// Trigger: create "Temp/toy_size_audit" (optional content: level count), result in Temp/toy_size_audit_result.txt.
/// </summary>
[InitializeOnLoad]
public static class ToySizeAudit {
	const string TRIGGER = "Temp/toy_size_audit", RESULT = "Temp/toy_size_audit_result.txt";
	const float TARGET = 1.56f, LONG_RATIO = 1.9f, FLAG = 1.25f;
	static double _next;

	static ToySizeAudit() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;
			_next = EditorApplication.timeSinceStartup + 1;
			if(!File.Exists(TRIGGER))
				return;
			int.TryParse(File.ReadAllText(TRIGGER).Trim(), out var count);
			File.Delete(TRIGGER);
			File.WriteAllText(RESULT, Run(count > 0 ? count : 30));
		};
	}

	class Shown {
		public string name;
		public float silhouette, boxFace, longest, thick, fill;
		public bool inTable, longCapped;
	}

	static readonly Dictionary<GameObject, Shown> _cache = new Dictionary<GameObject, Shown>();

	static Shown Measure(GameObject prefab, ToyVisualSizeTable table) {
		if(_cache.TryGetValue(prefab, out var s))
			return s;
		var size = ToyVisualSizeTool.RuntimeSortedSize(prefab);
		s = new Shown { name = prefab.name };
		if(size.x > 0.01f) {
			var boxFace = Mathf.Sqrt(size.x * size.y);
			var face = boxFace;
			var visual = boxFace * 0.82f;
			if(table != null && table.TryGet(prefab, out var e)) {
				s.inTable = true;
				s.fill = e.fill;
				visual = e.visualSize * Mathf.Abs(prefab.transform.lossyScale.x);
				face = boxFace * Mathf.Clamp(Mathf.Sqrt(e.fill / table.referenceFill), 0.75f, 1.25f);
				visual = e.fill * boxFace;
			}
			var scale = TARGET / face;
			var cap = TARGET * LONG_RATIO / size.x;
			if(cap < scale) {
				scale = cap;
				s.longCapped = true;
			}
			s.silhouette = visual * scale;
			s.boxFace = boxFace * scale;
			s.longest = size.x * scale;
			s.thick = size.z * scale;
		}
		_cache[prefab] = s;
		return s;
	}

	static float Median(IEnumerable<float> values) {
		var list = values.OrderBy(v => v).ToList();
		return list.Count == 0 ? 0f : list[list.Count / 2];
	}

	public static string Run(int levelCount) {
		_cache.Clear();
		var table = AssetDatabase.LoadAssetAtPath<ToyVisualSizeTable>("Assets/Match3D/Resources/ToyVisualSizeTable.asset");
		table?.ClearCache();
		var storage = AssetDatabase.FindAssets("t:ToyStorage").Select(g => AssetDatabase.LoadAssetAtPath<ToyStorage>(AssetDatabase.GUIDToAssetPath(g))).First();
		var levels = AssetDatabase.FindAssets("t:LevelDataStorage").Select(g => AssetDatabase.LoadAssetAtPath<LevelDataStorage>(AssetDatabase.GUIDToAssetPath(g))).First();
		var rnd = new System.Random(20261113);
		Random.InitState(20261113);
		var sb = new StringBuilder();
		var flaggedCount = new Dictionary<string, int>();
		var flaggedInfo = new Dictionary<string, string>();
		var levelIndices = Enumerable.Range(0, levels.LevelCount).OrderBy(_ => rnd.Next()).Take(levelCount).OrderBy(i => i).ToList();
		foreach(var index in levelIndices) {
			var data = levels.GetLevelData(index);
			var toys = storage.SelectToyPrefabsForLevel(data).Select(t => t.gameObject).ToList();
			var groupNames = string.Join("+", storage.AllToyGroup.Where(g => g != null && toys.Any(t => g.toyList.Any(x => x != null && x.gameObject == t))).Select(g => $"{g.name.Replace("Group_", "")}:{toys.Count(t => g.toyList.Any(x => x != null && x.gameObject == t))}"));
			foreach(var support in data.supportToyList) {
				var def = storage.SupportToyList.FirstOrDefault(d => d != null && d.id == support.id);
				if(def != null && def.toyView != null)
					toys.Add(def.toyView.gameObject);
			}
			var shown = toys.Select(t => Measure(t, table)).Where(s => s.longest > 0f).ToList();
			var medBox = Median(shown.Select(s => s.boxFace));
			var medLong = Median(shown.Select(s => s.longest));
			var medSil = Median(shown.Select(s => s.silhouette));
			sb.AppendLine($"Level {index + 1} [{groupNames}] {shown.Count} kinds, median box {medBox:F2} long {medLong:F2} silhouette {medSil:F2}");
			foreach(var s in shown) {
				var rBox = s.boxFace / medBox;
				var rLong = s.longest / medLong;
				var rSil = s.silhouette / medSil;
				var big = rBox > FLAG || rLong > FLAG || rSil > FLAG;
				var small = rBox < 1f / FLAG && rLong < 1f / FLAG;
				if(!big && !small)
					continue;
				var line = $"{(big ? "BIG  " : "small")} {s.name,-34} box x{rBox:F2} long x{rLong:F2} silh x{rSil:F2} fill {s.fill:F2}{(s.inTable ? "" : " NO-TABLE")}{(s.longCapped ? " long-capped" : "")}";
				sb.AppendLine("   " + line);
				if(big) {
					flaggedCount[s.name] = flaggedCount.TryGetValue(s.name, out var c) ? c + 1 : 1;
					flaggedInfo[s.name] = line;
				}
			}
		}
		var header = new StringBuilder();
		header.AppendLine($"== {levelIndices.Count} random levels, toys flagged BIG (>{FLAG}x level median), most often first ==");
		foreach(var kv in flaggedCount.OrderByDescending(k => k.Value))
			header.AppendLine($"{kv.Value,2} levels: {flaggedInfo[kv.Key]}");
		// all toys, biggest box first
		header.AppendLine("\n== all toys (top 25 by box face, top 15 by silhouette) ==");
		var all = storage.AllToyGroup.Where(g => g != null).SelectMany(g => g.toyList).Where(t => t != null).Select(t => t.gameObject).Distinct().Select(t => Measure(t, table)).Where(s => s.longest > 0f).ToList();
		header.AppendLine($"median box {Median(all.Select(s => s.boxFace)):F2} long {Median(all.Select(s => s.longest)):F2} silhouette {Median(all.Select(s => s.silhouette)):F2} thick {Median(all.Select(s => s.thick)):F2}");
		foreach(var s in all.OrderByDescending(s => s.boxFace).Take(25))
			header.AppendLine($"  box {s.boxFace:F2} long {s.longest:F2} silh {s.silhouette:F2} thick {s.thick:F2} fill {s.fill:F2} {s.name}");
		header.AppendLine("--");
		foreach(var s in all.OrderByDescending(s => s.silhouette).Take(15))
			header.AppendLine($"  silh {s.silhouette:F2} box {s.boxFace:F2} long {s.longest:F2} thick {s.thick:F2} fill {s.fill:F2} {s.name}");
		header.AppendLine("--thickest");
		foreach(var s in all.OrderByDescending(s => s.thick).Take(15))
			header.AppendLine($"  thick {s.thick:F2} box {s.boxFace:F2} silh {s.silhouette:F2} {s.name}");
		return header + "\n" + sb;
	}
}
