using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The touch outline (QuickOutline) reads mesh vertices/normals at runtime, so every toy mesh must be Read/Write enabled.
/// Turn it on for the model files of all toys in the toy groups (and support toys).
/// Menu: Tools/Toy Pack Picker - Enable Read-Write on toy meshes. Trigger file: Temp/toy_mesh_readable
/// </summary>
[InitializeOnLoad]
public static class ToyMeshReadableFix {
	const string TRIGGER = "Temp/toy_mesh_readable", RESULT = "Temp/toy_mesh_readable_result.txt";
	static double _next;

	static ToyMeshReadableFix() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;
			_next = EditorApplication.timeSinceStartup + 1;
			if(!File.Exists(TRIGGER))
				return;
			File.Delete(TRIGGER);
			File.WriteAllText(RESULT, Run());
		};
	}

	[MenuItem("Tools/Toy Pack Picker - Enable Read-Write on toy meshes")]
	static void Menu() => Debug.Log(Run());

	public static string Run() {
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(ToyPackData.TOY_STORAGE_PATH);
		var prefabs = storage.AllToyGroup.Where(g => g != null).SelectMany(g => g.toyList).Where(t => t != null).Select(t => t.gameObject).ToList();
		prefabs.AddRange(storage.SupportToyList.Where(s => s != null && s.toyView != null).Select(s => s.toyView.gameObject));
		var modelPaths = new HashSet<string>();
		foreach(var prefab in prefabs.Distinct())
			foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
				if(filter.sharedMesh != null && !filter.sharedMesh.isReadable)
					modelPaths.Add(AssetDatabase.GetAssetPath(filter.sharedMesh));
		var fixedPaths = new List<string>();
		var other = new List<string>();
		AssetDatabase.StartAssetEditing();
		try {
			foreach(var path in modelPaths) {
				if(AssetImporter.GetAtPath(path) is ModelImporter importer) {
					importer.isReadable = true;
					importer.SaveAndReimport();
					fixedPaths.Add(path);
				}
				else
					other.Add(path);
			}
		}
		finally {
			AssetDatabase.StopAssetEditing();
		}
		return $"[ToyMeshReadableFix] {prefabs.Count} toys, enabled Read/Write on {fixedPaths.Count} model files\n" + string.Join("\n", fixedPaths)
			+ (other.Count > 0 ? "\nnot a model importer (check by hand):\n" + string.Join("\n", other) : "");
	}
}
