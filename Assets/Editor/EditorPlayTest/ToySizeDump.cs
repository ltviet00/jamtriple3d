using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary> Dev tool: create "Temp/toy_size_dump" (content = group name part) -> writes mesh sizes of the group toys to "Temp/toy_size_dump_result.txt" </summary>
[InitializeOnLoad]
public static class ToySizeDump {
	const string TRIGGER = "Temp/toy_size_dump", RESULT = "Temp/toy_size_dump_result.txt";
	const string FIT_TRIGGER = "Temp/toy_fit_colliders", FIT_RESULT = "Temp/toy_fit_colliders_result.txt";
	static double _next;
	const string CENTER_TRIGGER = "Temp/toy_center_pivot", CENTER_RESULT = "Temp/toy_center_pivot_result.txt";

	///<summary> New pack toys keep the pack pivot (bottom / end of the item), so they spin around it in the slots.
	/// Move the meshes so the collider center (= meshes center) is the root pivot </summary>
	static string CenterNewToyPivots() {
		var sb = new StringBuilder();
		foreach(var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Match3D/Model/NewToys" })) {
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var go = PrefabUtility.LoadPrefabContents(path);
			var box = go.GetComponent<BoxCollider>();
			var view = go.GetComponent<ToyView>();
			if(box != null && view != null && box.center.magnitude > box.size.magnitude * 0.02f) {
				var center = box.center;
				// meshes on the root itself: move them to a child first
				var rootFilter = go.GetComponent<MeshFilter>();
				var rootRenderer = go.GetComponent<MeshRenderer>();
				if(rootFilter != null && rootRenderer != null) {
					var model = new GameObject("Model");
					model.layer = go.layer;
					model.transform.SetParent(go.transform, false);
					model.AddComponent<MeshFilter>().sharedMesh = rootFilter.sharedMesh;
					var renderer = model.AddComponent<MeshRenderer>();
					EditorUtility.CopySerialized(rootRenderer, renderer);
					if(view.mainRenderer == rootRenderer)
						view.mainRenderer = renderer;
					Object.DestroyImmediate(rootRenderer);
					Object.DestroyImmediate(rootFilter);
				}
				foreach(Transform child in go.transform)
					child.localPosition -= center;
				box.center = Vector3.zero;
				PrefabUtility.SaveAsPrefabAsset(go, path);
				sb.AppendLine($"{path} moved {center}");
			}
			PrefabUtility.UnloadPrefabContents(go);
		}
		AssetDatabase.SaveAssets();
		return sb.Length == 0 ? "nothing to center" : sb.ToString();
	}
	const string BAKE_TRIGGER = "Temp/toy_bake_rotated", BAKE_SCAN_TRIGGER = "Temp/toy_bake_scan", BAKE_RESULT = "Temp/toy_bake_rotated_result.txt";
	const string BAKED_FOLDER = "Assets/Match3D/Model/BakedToyMeshes";

	///<summary> Toy meshes authored diagonally (mesh bounds much bigger than the real shape, guitar): sizing and collider use the bounds,
	/// so the toy got too small. Bake the mesh again with the vertices in the toy space: tight bounds </summary>
	static string BakeRotatedMeshes(bool apply) {
		var sb = new StringBuilder();
		var done = new System.Collections.Generic.HashSet<string>();
		foreach(var guid in AssetDatabase.FindAssets("t:ToyStorage")) {
			var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(AssetDatabase.GUIDToAssetPath(guid));
			foreach(var group in Groups(storage)) {
				if(group == null)
					continue;
				foreach(var toy in group.toyList) {
					if(toy == null)
						continue;
					var path = AssetDatabase.GetAssetPath(toy.gameObject);
					if(!done.Add(path))
						continue;
					var go = PrefabUtility.LoadPrefabContents(path);
					var root = go.transform;
					var boxBounds = new Bounds(); var vertBounds = new Bounds(); var hasBox = false; var hasVert = false;
					var filters = go.GetComponentsInChildren<MeshFilter>(true);
					foreach(var f in filters) {
						var mesh = f.sharedMesh;
						if(mesh == null)
							continue;
						var mb = mesh.bounds;
						for(int i = 0; i < 8; i++) {
							var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
							var p = root.InverseTransformPoint(f.transform.TransformPoint(c));
							if(!hasBox) { boxBounds = new Bounds(p, Vector3.zero); hasBox = true; } else boxBounds.Encapsulate(p);
						}
						foreach(var v in mesh.vertices) {
							var p = root.InverseTransformPoint(f.transform.TransformPoint(v));
							if(!hasVert) { vertBounds = new Bounds(p, Vector3.zero); hasVert = true; } else vertBounds.Encapsulate(p);
						}
					}
					if(!hasBox || !hasVert) {
						PrefabUtility.UnloadPrefabContents(go);
						continue;
					}
					var boxFace = FaceArea(boxBounds.size);
					var vertFace = FaceArea(vertBounds.size);
					if(boxFace > vertFace * 1.3f) {
						sb.AppendLine($"{path}: box face={boxFace:F2} real face={vertFace:F2} box={boxBounds.size} real={vertBounds.size}");
						if(apply) {
							if(!AssetDatabase.IsValidFolder(BAKED_FOLDER))
								AssetDatabase.CreateFolder("Assets/Match3D/Model", "BakedToyMeshes");
							foreach(var f in filters) {
								if(f.sharedMesh == null || f.transform == root)
									continue;
								var t = f.transform;
								var rot = t.localRotation;
								var scale = t.localScale;
								var mesh = Object.Instantiate(f.sharedMesh);
								var verts = mesh.vertices;
								for(int n = 0; n < verts.Length; n++)
									verts[n] = rot * Vector3.Scale(scale, verts[n]);
								var normals = mesh.normals;
								for(int n = 0; n < normals.Length; n++)
									normals[n] = (rot * normals[n]).normalized;
								var tangents = mesh.tangents;
								for(int n = 0; n < tangents.Length; n++) {
									var tv = rot * new Vector3(tangents[n].x, tangents[n].y, tangents[n].z);
									tangents[n] = new Vector4(tv.x, tv.y, tv.z, tangents[n].w);
								}
								mesh.vertices = verts;
								mesh.normals = normals;
								mesh.tangents = tangents;
								mesh.RecalculateBounds();
								var meshPath = $"{BAKED_FOLDER}/{go.name}_{t.name}.asset";
								AssetDatabase.CreateAsset(mesh, meshPath);
								f.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
								t.localRotation = Quaternion.identity;
								t.localScale = Vector3.one;
							}
							var box = go.GetComponent<BoxCollider>();
							if(box != null) {
								box.center = vertBounds.center;
								box.size = vertBounds.size;
							}
							PrefabUtility.SaveAsPrefabAsset(go, path);
							sb.AppendLine("   baked");
						}
					}
					PrefabUtility.UnloadPrefabContents(go);
				}
			}
		}
		AssetDatabase.SaveAssets();
		return sb.Length == 0 ? "nothing found" : sb.ToString();
	}

	static float FaceArea(Vector3 size) {
		var d = new[] { size.x, size.y, size.z };
		System.Array.Sort(d);
		return d[2] * d[1];
	}

	///<summary> Toy prefabs still using the default 1x1x1 BoxCollider: fit the collider to the meshes (root space) </summary>
	static string FitDefaultColliders() {
		var sb = new StringBuilder();
		foreach(var guid in AssetDatabase.FindAssets("t:ToyStorage")) {
			var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(AssetDatabase.GUIDToAssetPath(guid));
			foreach(var group in Groups(storage)) {
				if(group == null)
					continue;
				foreach(var toy in group.toyList) {
					if(toy == null)
						continue;
					var path = AssetDatabase.GetAssetPath(toy.gameObject);
					var go = PrefabUtility.LoadPrefabContents(path);
					var box = go.GetComponent<BoxCollider>();
					if(box != null && box.size == Vector3.one && box.center == Vector3.zero) {
						var root = go.transform;
						var b = new Bounds();
						var has = false;
						foreach(var f in go.GetComponentsInChildren<MeshFilter>()) {
							if(f.sharedMesh == null)
								continue;
							var mb = f.sharedMesh.bounds;
							for(int i = 0; i < 8; i++) {
								var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
								var p = root.InverseTransformPoint(f.transform.TransformPoint(c));
								if(!has) { b = new Bounds(p, Vector3.zero); has = true; } else b.Encapsulate(p);
							}
						}
						if(has) {
							box.center = b.center;
							box.size = b.size;
							PrefabUtility.SaveAsPrefabAsset(go, path);
							sb.AppendLine($"{path} -> center={b.center} size={b.size}");
						}
					}
					PrefabUtility.UnloadPrefabContents(go);
				}
			}
		}
		AssetDatabase.SaveAssets();
		return sb.Length == 0 ? "nothing to fit" : sb.ToString();
	}
	static ToySizeDump() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;
			_next = EditorApplication.timeSinceStartup + 1;
			if(File.Exists(BAKE_TRIGGER)) {
				File.Delete(BAKE_TRIGGER);
				File.WriteAllText(BAKE_RESULT, BakeRotatedMeshes(true));
			}
			if(File.Exists(BAKE_SCAN_TRIGGER)) {
				File.Delete(BAKE_SCAN_TRIGGER);
				File.WriteAllText(BAKE_RESULT, BakeRotatedMeshes(false));
			}
			if(File.Exists(CENTER_TRIGGER)) {
				File.Delete(CENTER_TRIGGER);
				File.WriteAllText(CENTER_RESULT, CenterNewToyPivots());
			}
			if(File.Exists(FIT_TRIGGER)) {
				File.Delete(FIT_TRIGGER);
				File.WriteAllText(FIT_RESULT, FitDefaultColliders());
			}
			if(!File.Exists(TRIGGER))
				return;
			var key = File.ReadAllText(TRIGGER).Trim();
			File.Delete(TRIGGER);
			var sb = new StringBuilder();
			foreach(var guid in AssetDatabase.FindAssets("t:ToyStorage")) {
				var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(AssetDatabase.GUIDToAssetPath(guid));
				foreach(var group in Groups(storage)) {
					if(group == null || !group.name.Contains(key))
						continue;
					sb.AppendLine("== " + group.name);
					foreach(var toy in group.toyList) {
						if(toy == null)
							continue;
						sb.AppendLine(Dump(toy.gameObject));
					}
				}
			}
			File.WriteAllText(RESULT, sb.ToString());
		};
	}

	static System.Collections.Generic.IEnumerable<ToyPrefabGroup> Groups(ToyStorage storage) {
		var list = new SerializedObject(storage).FindProperty("allToyGroup");
		for(int n = 0; n < list.arraySize; n++)
			yield return list.GetArrayElementAtIndex(n).objectReferenceValue as ToyPrefabGroup;
	}

	static string Dump(GameObject go) {
		var sb = new StringBuilder(go.name + " scale=" + go.transform.localScale);
		var root = go.transform;
		var b = new Bounds(); var has = false;
		foreach(var f in go.GetComponentsInChildren<MeshFilter>(true)) {
			if(f.sharedMesh == null)
				continue;
			var mb = f.sharedMesh.bounds;
			sb.Append($"\n   mesh {f.name} active={f.gameObject.activeInHierarchy} rend={(f.GetComponent<Renderer>() ? f.GetComponent<Renderer>().enabled : false)} bounds={mb.size}");
			for(int i = 0; i < 8; i++) {
				var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				var p = root.InverseTransformPoint(f.transform.TransformPoint(c));
				if(!has) { b = new Bounds(p, Vector3.zero); has = true; } else b.Encapsulate(p);
			}
		}
		var s = Vector3.Scale(b.size, go.transform.lossyScale);
		sb.Append($"\n   size={s} sorted area face={Mathf.Sqrt(Mathf.Max(s.x * s.y, Mathf.Max(s.x * s.z, s.y * s.z))):F3}");
		return sb.ToString();
	}
}
