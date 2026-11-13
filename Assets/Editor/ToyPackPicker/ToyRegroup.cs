using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot: the 4 groups made per pack (Cartoon 50, Drink 12, Objects 12, Christmas 8) were unbalanced.
/// Regroup them by theme into 3 groups of similar size: CartoonFont (letters, numbers), FoodDrink, Stuff (things + Christmas).
/// Create "Temp/toy_regroup" (Editor polls it) or use the menu.
/// </summary>
[InitializeOnLoad]
public static class ToyRegroup {
	const string TRIGGER = "Temp/toy_regroup", RESULT = "Temp/toy_regroup_result.txt";
	const string GROUP_FOLDER = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/ToyGroups";
	const string ID_FOLDER = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/GroupID";
	static readonly string[] FOOD = { "Bottle_", "Can_", "Carton_", "ChickenLeg", "Ham", "Milk", "Pizza", "bowl", "frying_pan", "bottle" };
	static double _next;

	static ToyRegroup() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;
			_next = EditorApplication.timeSinceStartup + 1;
			if(!File.Exists(TRIGGER))
				return;
			File.Delete(TRIGGER);
			File.WriteAllText(RESULT, Regroup());
		};
	}

	[MenuItem("Tools/Toy Pack Picker - Regroup new toys")]
	static void Menu() {
		Debug.Log(Regroup());
	}

	static string Regroup() {
		var sb = new StringBuilder();
		var cartoon = Load<ToyPrefabGroup>($"{GROUP_FOLDER}/Group_Cartoon.asset");
		var drink = Load<ToyPrefabGroup>($"{GROUP_FOLDER}/Group_Drink.asset");
		var christmas = Load<ToyPrefabGroup>($"{GROUP_FOLDER}/Group_Christmas.asset");
		var objects = Load<ToyPrefabGroup>($"{GROUP_FOLDER}/Group_Objects.asset");
		if(cartoon == null || drink == null || christmas == null || objects == null)
			return "groups not found (already regrouped?)";
		var all = new List<ToyView>();
		foreach(var group in new[] { cartoon, drink, christmas, objects })
			all.AddRange(group.toyList.Where(t => t != null));

		var letters = new List<ToyView>();
		var food = new List<ToyView>();
		var stuff = new List<ToyView>();
		foreach(var toy in all) {
			var name = toy.gameObject.name;
			if(name.StartsWith("CartoonFont_"))
				letters.Add(toy);
			else if(FOOD.Any(f => name.StartsWith(f)))
				food.Add(toy);
			else
				stuff.Add(toy);
		}
		Apply(cartoon, letters, "CartoonFont", sb);
		Apply(drink, food, "FoodDrink", sb);
		Apply(christmas, stuff, "Stuff", sb);

		// Objects group: all its toys moved, remove it
		var storagePath = AssetDatabase.FindAssets("t:ToyStorage").Select(AssetDatabase.GUIDToAssetPath).First();
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(storagePath);
		var so = new SerializedObject(storage);
		var list = so.FindProperty("allToyGroup");
		for(int n = list.arraySize - 1; n >= 0; n--) {
			if(list.GetArrayElementAtIndex(n).objectReferenceValue == objects) {
				list.GetArrayElementAtIndex(n).objectReferenceValue = null;
				list.DeleteArrayElementAtIndex(n);
			}
		}
		so.ApplyModifiedPropertiesWithoutUndo();
		var objectsID = objects.groupID;
		AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(objects));
		if(objectsID != null)
			AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(objectsID));
		sb.AppendLine("removed Group_Objects");
		AssetDatabase.SaveAssets();
		return sb.ToString();
	}

	static void Apply(ToyPrefabGroup group, List<ToyView> toys, string name, StringBuilder sb) {
		group.groupName = name;
		group.toyList = toys;
		if(group.groupID != null) {
			group.groupID.groupName = name;
			EditorUtility.SetDirty(group.groupID);
			AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(group.groupID), "ToyGroupID_" + name);
		}
		foreach(var toy in toys) {
			toy.groupID = group.groupID;
			EditorUtility.SetDirty(toy);
		}
		EditorUtility.SetDirty(group);
		AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(group), "Group_" + name);
		sb.AppendLine($"Group_{name}: {toys.Count} -> {string.Join(", ", toys.Select(t => t.gameObject.name))}");
	}

	static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
}
