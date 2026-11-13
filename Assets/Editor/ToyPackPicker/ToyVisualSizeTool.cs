using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Measure the visible size of every toy (silhouette area on its biggest face, by rasterizing the mesh triangles)
/// and save it to Resources/ToyVisualSizeTable, used by ToySpawner to make all toys look the same size.
/// Run again after adding toys: menu Tools/Toy Visual Size - Measure all toys, or create "Temp/toy_visual_size".
/// </summary>
[InitializeOnLoad]
public static class ToyVisualSizeTool {
	const string TRIGGER = "Temp/toy_visual_size", RESULT = "Temp/toy_visual_size_result.txt";
	const string TABLE_PATH = "Assets/Match3D/Resources/ToyVisualSizeTable.asset";
	const int GRID = 128;
	static double _next;

	static ToyVisualSizeTool() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _next || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;
			_next = EditorApplication.timeSinceStartup + 1;
			if(!File.Exists(TRIGGER))
				return;
			File.Delete(TRIGGER);
			File.WriteAllText(RESULT, MeasureAll());
		};
	}

	[MenuItem("Tools/Toy Visual Size - Measure all toys")]
	static void Menu() {
		Debug.Log(MeasureAll());
	}

	public static string MeasureAll() {
		var prefabs = new HashSet<GameObject>();
		foreach(var guid in AssetDatabase.FindAssets("t:ToyStorage")) {
			var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(AssetDatabase.GUIDToAssetPath(guid));
			foreach(var group in storage.AllToyGroup.Where(g => g != null))
				foreach(var toy in group.toyList.Where(t => t != null))
					prefabs.Add(toy.gameObject);
			foreach(var s in storage.SupportToyList.Where(s => s != null && s.toyView != null))
				prefabs.Add(s.toyView.gameObject);
			foreach(var w in storage.WeeklyToyList.Where(w => w != null && w.toyView != null))
				prefabs.Add(w.toyView.gameObject);
		}
		var table = AssetDatabase.LoadAssetAtPath<ToyVisualSizeTable>(TABLE_PATH);
		if(table == null) {
			table = ScriptableObject.CreateInstance<ToyVisualSizeTable>();
			AssetDatabase.CreateAsset(table, TABLE_PATH);
		}
		table.entries.Clear();
		var sb = new StringBuilder();
		foreach(var prefab in prefabs.OrderBy(p => AssetDatabase.GetAssetPath(p))) {
			if(!Measure(prefab, out var visual, out var boxFace))
				continue;
			var fill = visual / boxFace;
			table.entries.Add(new ToyVisualSizeTable.Entry { prefab = prefab, visualSize = visual, fill = fill });
			var sorted = RuntimeSortedSize(prefab);
			var shown = sorted.x > 0f ? visual * Mathf.Abs(prefab.transform.lossyScale.x) * Mathf.Min(1f / Mathf.Sqrt(sorted.x * sorted.y), 1.9f / sorted.x) : 0f;
			sb.AppendLine($"{fill:F2} shownOld={shown:F2} longest={sorted.x * Mathf.Min(1f / Mathf.Sqrt(sorted.x * sorted.y), 1.9f / sorted.x):F2} {AssetDatabase.GetAssetPath(prefab)}");
		}
		var fills = table.entries.Select(e => e.fill).OrderBy(f => f).ToList();
		table.referenceFill = fills.Count > 0 ? fills[fills.Count / 2] : 0.8f;
		table.ClearCache();
		EditorUtility.SetDirty(table);
		AssetDatabase.SaveAssets();
		return $"measured {table.entries.Count} toys, reference fill {table.referenceFill:F2}\n" + sb;
	}

	///<summary> Silhouette of the toy seen along the shortest side of its (vertex) bounds, root units </summary>
	static bool Measure(GameObject prefab, out float visualSize, out float boxFace) {
		visualSize = boxFace = 0f;
		var root = prefab.transform;
		var tris = new List<Vector3>();
		foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true)) {
			var mesh = filter.sharedMesh;
			if(mesh == null)
				continue;
			var renderer = filter.GetComponent<MeshRenderer>();
			if(renderer == null || !renderer.enabled)
				continue;
			var verts = mesh.vertices;
			var indices = mesh.triangles;
			var toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
			for(int n = 0; n < indices.Length; n++)
				tris.Add(toRoot.MultiplyPoint3x4(verts[indices[n]]));
		}
		if(tris.Count < 3)
			return false;
		var bounds = new Bounds(tris[0], Vector3.zero);
		foreach(var p in tris)
			bounds.Encapsulate(p);
		var size = bounds.size;
		// biggest face box (shortest axis dropped)
		var shortest = size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
		boxFace = Mathf.Sqrt(size[shortest == 0 ? 1 : 0] * size[shortest == 2 ? 1 : 2]);
		// silhouette seen along each axis, keep the biggest: a thick toy (umbrella, ring) can land on any side,
		// measuring only its thin side view made it grow far too big when it showed the full view
		for(int drop = 0; drop < 3; drop++) {
			int a = drop == 0 ? 1 : 0, b = drop == 2 ? 1 : 2;
			var min = new Vector2(bounds.min[a], bounds.min[b]);
			var extent = Mathf.Max(size[a], size[b]);
			if(extent <= 0f)
				continue;
			var cell = extent / GRID;
			var filled = new bool[GRID, GRID];
			for(int n = 0; n + 2 < tris.Count; n += 3) {
				var p0 = (new Vector2(tris[n][a], tris[n][b]) - min) / cell;
				var p1 = (new Vector2(tris[n + 1][a], tris[n + 1][b]) - min) / cell;
				var p2 = (new Vector2(tris[n + 2][a], tris[n + 2][b]) - min) / cell;
				FillTriangle(filled, p0, p1, p2);
			}
			var count = 0;
			foreach(var f in filled)
				if(f)
					count++;
			visualSize = Mathf.Max(visualSize, Mathf.Sqrt(count * cell * cell));
		}
		return visualSize > 0f && boxFace > 0f;
	}

	static void FillTriangle(bool[,] grid, Vector2 p0, Vector2 p1, Vector2 p2) {
		var area = Cross(p1 - p0, p2 - p0);
		if(Mathf.Abs(area) < 1e-6f)
			return;
		int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))), 0, GRID - 1);
		int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))), 0, GRID - 1);
		int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y))), 0, GRID - 1);
		int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))), 0, GRID - 1);
		for(int x = x0; x <= x1; x++) {
			for(int y = y0; y <= y1; y++) {
				if(grid[x, y])
					continue;
				var c = new Vector2(x + 0.5f, y + 0.5f);
				var w0 = Cross(p1 - p0, c - p0);
				var w1 = Cross(p2 - p1, c - p1);
				var w2 = Cross(p0 - p2, c - p2);
				if((w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0))
					grid[x, y] = true;
			}
		}
	}

	///<summary> Same as ToySpawner.MeasureSortedSize (box of the biggest mesh, in its space) </summary>
	internal static Vector3 RuntimeSortedSize(GameObject prefab) {
		Transform space = prefab.transform;
		var biggest = 0f;
		foreach(var f in prefab.GetComponentsInChildren<MeshFilter>()) {
			if(f.sharedMesh == null)
				continue;
			var sz = Vector3.Scale(f.sharedMesh.bounds.size, f.transform.lossyScale);
			var v = Mathf.Abs(sz.x * sz.y) + Mathf.Abs(sz.y * sz.z) + Mathf.Abs(sz.x * sz.z);
			if(v > biggest) { biggest = v; space = f.transform; }
		}
		var has = false;
		var b = new Bounds();
		foreach(var f in prefab.GetComponentsInChildren<MeshFilter>()) {
			if(f.sharedMesh == null)
				continue;
			var mb = f.sharedMesh.bounds;
			for(int i = 0; i < 8; i++) {
				var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				var p = space.InverseTransformPoint(f.transform.TransformPoint(c));
				if(!has) { b = new Bounds(p, Vector3.zero); has = true; } else b.Encapsulate(p);
			}
		}
		if(!has)
			return Vector3.zero;
		var ls = space.lossyScale;
		var d = new[] { b.size.x * Mathf.Abs(ls.x), b.size.y * Mathf.Abs(ls.y), b.size.z * Mathf.Abs(ls.z) };
		System.Array.Sort(d);
		return new Vector3(d[2], d[1], d[0]);
	}

	static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
}
