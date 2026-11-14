using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Move new toys (Model/NewToys/&lt;pack&gt;) into theme groups by their pack and name: Kitchen, Sport, Vehicle, Toy.
/// Toys of other packs stay where they are. Safe to run again after adding toys.
/// Menu: Tools/Toy Pack Picker - Sort new toys by theme, or create "Temp/toy_theme_sort".
/// </summary>
[InitializeOnLoad]
public static class ToyThemeSort {
	const string TRIGGER = "Temp/toy_theme_sort", RESULT = "Temp/toy_theme_sort_result.txt";
	const string NEW_TOYS = "Assets/Match3D/Model/NewToys/";
	static double _next;

	static ToyThemeSort() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;
			_next = EditorApplication.timeSinceStartup + 1;
			if(!File.Exists(TRIGGER))
				return;
			File.Delete(TRIGGER);
			File.WriteAllText(RESULT, Sort());
		};
	}

	[MenuItem("Tools/Toy Pack Picker - Sort new toys by theme")]
	static void Menu() => Debug.Log(Sort());

	///<summary> Theme group for a toy, null = keep its group </summary>
	static string ThemeOf(string pack, string name) {
		switch(pack) {
			case "PropKitchen":
				return "Kitchen";
			case "PropSport":
			case "PropCamp":
				return "Sport";
			case "Vehicles":
			case "City":
				return "Vehicle";
			case "Weapons":
				return name.StartsWith("Tennis_Ball") ? "Sport" : "Toy";
			case "Furniture":
				return name.StartsWith("Toy_") ? "Toy" : null;
			case "ToysPack":
				if(name.StartsWith("Ball_") || name.StartsWith("Basketball_") || name.StartsWith("Boomerang"))
					return "Sport";
				if(name.StartsWith("Car_") || name.StartsWith("Tricycle") || name.StartsWith("Rocket") || name.StartsWith("WoodenTrain"))
					return "Vehicle";
				return "Toy";
		}
		return null;
	}

	public static string Sort() {
		var sb = new StringBuilder();
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(ToyPackData.TOY_STORAGE_PATH);
		var moves = new List<(ToyView toy, ToyPrefabGroup from, string to)>();
		foreach(var group in storage.AllToyGroup.Where(g => g != null).ToList()) {
			foreach(var toy in group.toyList.Where(t => t != null)) {
				var path = AssetDatabase.GetAssetPath(toy.gameObject);
				if(!path.StartsWith(NEW_TOYS))
					continue;
				var pack = path.Substring(NEW_TOYS.Length).Split('/')[0];
				var theme = ThemeOf(pack, toy.gameObject.name);
				if(theme != null && group.name != "Group_" + theme)
					moves.Add((toy, group, theme));
			}
		}
		foreach(var (toy, from, to) in moves) {
			var target = ToyPackImporter.GetOrCreateGroup(to, storage);
			from.toyList.Remove(toy);
			if(!target.toyList.Contains(toy))
				target.toyList.Add(toy);
			toy.groupID = target.groupID;
			EditorUtility.SetDirty(toy);
			EditorUtility.SetDirty(from);
			EditorUtility.SetDirty(target);
		}
		foreach(var g in moves.GroupBy(m => m.to))
			sb.AppendLine($"-> {g.Key}: {string.Join(", ", g.Select(m => m.toy.gameObject.name))}");
		AssetDatabase.SaveAssets();
		sb.AppendLine("\nGroups now:");
		foreach(var group in storage.AllToyGroup.Where(g => g != null))
			sb.AppendLine($"  {group.name}: {group.toyList.Count(t => t != null)}");
		return $"moved {moves.Count} toys\n" + sb;
	}
}
