using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Pick toys from newly imported 3D packs (with a picture of each one) and add them to the game.
/// Only lists the new packs, and hides toys already used in gameplay.
/// Menu: Tools/Toy Pack Picker
/// </summary>
[InitializeOnLoad]
public class ToyPackPickerWindow : EditorWindow {
	#region Init, config
	const string OPEN_ONCE_FILE = "Tools/ToyPackPicker/open_once";
	const string RUN_IMPORT_FILE = "Tools/ToyPackPicker/run_import";
	const string REDO_IMPORT_FILE = "Tools/ToyPackPicker/redo_import";
	static double _nextOpenCheck;

	///<summary> Open the window by itself once, when the file "open_once" exists (lets tools outside Unity open it) </summary>
	static ToyPackPickerWindow() {
		EditorApplication.update += () => {
			if(EditorApplication.timeSinceStartup < _nextOpenCheck)
				return;
			_nextOpenCheck = EditorApplication.timeSinceStartup + 1.0;
			if(File.Exists(REDO_IMPORT_FILE) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) {
				File.Delete(REDO_IMPORT_FILE);
				ToyPackImporter.DeleteEverythingAdded();
				File.WriteAllText(RUN_IMPORT_FILE, "");
			}
			if(File.Exists(RUN_IMPORT_FILE) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) {
				File.Delete(RUN_IMPORT_FILE);
				ToyPackImporter.AddSelectionToGame();
				foreach(var window in Resources.FindObjectsOfTypeAll<ToyPackPickerWindow>())
					window.Rescan();
			}
			if(!File.Exists(OPEN_ONCE_FILE))
				return;
			File.Delete(OPEN_ONCE_FILE);
			Open();
		};
	}

	[MenuItem("Tools/Toy Pack Picker")]
	[MenuItem("Window/Toy Pack Picker")]
	static void Open() {
		GetWindow<ToyPackPickerWindow>("Toy Pack Picker").Show();
	}

	List<ToyPackData.Item> _items = new List<ToyPackData.Item>();
	ToyPackData.Selection _selection;
	readonly Dictionary<string, Texture2D> _previews = new Dictionary<string, Texture2D>();
	readonly Queue<string> _previewQueue = new Queue<string>();
	readonly HashSet<string> _queued = new HashSet<string>();
	ToyThumbnailShooter _shooter;
	string[] _groupOptions;
	int _packTab;
	string _search = "";
	bool _onlySelected;
	float _cellSize = 110f;
	Vector2 _scroll;
	const float PREVIEW_WIDTH = 300f;
	string _focusedPath;
	Texture2D _bigPreview;
	string _bigPreviewKey;
	float _yaw, _pitch = 19f, _zoom = 1f;

	void OnEnable() {
		Rescan();
		EditorApplication.update += RenderSomePreviews;
	}

	void OnDisable() {
		EditorApplication.update -= RenderSomePreviews;
		if(_bigPreview != null)
			DestroyImmediate(_bigPreview);
		_shooter?.Dispose();
		_shooter = null;
		foreach(var tex in _previews.Values)
			if(tex != null)
				DestroyImmediate(tex);
		_previews.Clear();
	}

	void Rescan() {
		_items = ToyPackData.ScanNewItems();
		_selection = ToyPackData.LoadSelection();
		// toys already added (or removed) are not listed: drop them from the selection too
		var listed = new HashSet<string>(_items.Select(i => i.path));
		_selection.items.RemoveAll(i => !listed.Contains(i.path));
		_groupOptions = ToyPackData.GroupOptions(_selection);
	}
	#endregion// Init config


	#region GUI
	void OnGUI() {
		var packs = new List<string> { $"Tất cả ({_items.Count})" };
		foreach(var pack in ToyPackData.PackFolders)
			packs.Add($"{ToyPackData.ShortPackName(pack)} ({_items.Count(i => i.pack == pack)})");

		using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
			// many packs: a dropdown instead of tabs
			GUILayout.Label("Pack:", GUILayout.Width(36));
			_packTab = EditorGUILayout.Popup(_packTab, packs.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(260));
			GUILayout.FlexibleSpace();
		}
		using(new EditorGUILayout.HorizontalScope()) {
			GUILayout.Label("Tìm:", GUILayout.Width(30));
			_search = EditorGUILayout.TextField(_search, GUILayout.Width(180));
			_onlySelected = GUILayout.Toggle(_onlySelected, "Chỉ món đã chọn", GUILayout.Width(120));
			GUILayout.Label("Cỡ ảnh", GUILayout.Width(45));
			_cellSize = GUILayout.HorizontalSlider(_cellSize, 70f, 200f, GUILayout.Width(100));
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Quét lại", GUILayout.Width(70)))
				Rescan();
		}

		var visible = VisibleItems();
		DrawGroupBar(visible);
		using(new EditorGUILayout.HorizontalScope()) {
			using(new EditorGUILayout.VerticalScope())
				DrawGrid(visible);
			DrawPreviewPanel();
		}
		DrawBottomBar();
	}

	List<ToyPackData.Item> VisibleItems() {
		IEnumerable<ToyPackData.Item> list = _items;
		if(_packTab > 0)
			list = list.Where(i => i.pack == ToyPackData.PackFolders[_packTab - 1]);
		if(!string.IsNullOrEmpty(_search))
			list = list.Where(i => i.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);
		if(_onlySelected)
			list = list.Where(i => _selection.Has(i.path));
		return list.ToList();
	}

	///<summary> Group that newly ticked toys go into, per pack </summary>
	void DrawGroupBar(List<ToyPackData.Item> visible) {
		using(new EditorGUILayout.HorizontalScope()) {
			if(_packTab > 0) {
				var pack = ToyPackData.PackFolders[_packTab - 1];
				var current = _selection.DefaultGroupFor(pack);
				var index = Mathf.Max(0, Array.IndexOf(_groupOptions, current));
				GUILayout.Label("Món chọn mới vào nhóm:", GUILayout.Width(150));
				var newIndex = EditorGUILayout.Popup(index, _groupOptions, GUILayout.Width(150));
				if(newIndex != index)
					_selection.SetDefaultGroup(pack, _groupOptions[newIndex]);
				if(GUILayout.Button("Đổi nhóm cho món đã chọn ở tab này", GUILayout.Width(230))) {
					foreach(var item in visible)
						if(_selection.Has(item.path))
							_selection.Add(item.path, _groupOptions[newIndex]);
				}
			}
			else {
				GUILayout.Label("Chọn tab một pack để đặt nhóm (Drink, Christmas...) cho món của pack đó.", EditorStyles.miniLabel);
			}
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Chọn hết", GUILayout.Width(70)))
				foreach(var item in visible)
					_selection.Add(item.path, _selection.DefaultGroupFor(item.pack));
			if(GUILayout.Button("Bỏ hết", GUILayout.Width(60)))
				foreach(var item in visible)
					_selection.Remove(item.path);
		}
	}

	void DrawGrid(List<ToyPackData.Item> visible) {
		var width = position.width - PREVIEW_WIDTH - 30f;
		var columns = Mathf.Max(1, Mathf.FloorToInt(width / (_cellSize + 6f)));
		var rows = Mathf.CeilToInt(visible.Count / (float)columns);
		var cellHeight = _cellSize + 34f;

		_scroll = EditorGUILayout.BeginScrollView(_scroll);
		var area = GUILayoutUtility.GetRect(width, rows * cellHeight);
		var viewTop = _scroll.y;
		var viewBottom = _scroll.y + position.height;
		for(int n = 0; n < visible.Count; n++) {
			var rect = new Rect(area.x + (n % columns) * (_cellSize + 6f), area.y + (n / columns) * cellHeight, _cellSize, cellHeight - 4f);
			// only draw (and render previews for) cells on screen
			if(rect.yMax < viewTop - cellHeight || rect.y > viewBottom)
				continue;
			DrawCell(rect, visible[n]);
		}
		EditorGUILayout.EndScrollView();
	}

	void DrawCell(Rect rect, ToyPackData.Item item) {
		var selected = _selection.Has(item.path);
		var imageRect = new Rect(rect.x, rect.y, _cellSize, _cellSize);
		EditorGUI.DrawRect(rect, selected ? new Color(0.2f, 0.55f, 0.25f, 0.9f) : new Color(0f, 0f, 0f, 0.18f));
		if(item.path == _focusedPath)
			EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3, rect.width, 3), new Color(0.3f, 0.6f, 1f));
		EditorGUI.DrawRect(new Rect(imageRect.x + 3, imageRect.y + 3, imageRect.width - 6, imageRect.height - 6), new Color(0.82f, 0.86f, 0.92f));

		if(_previews.TryGetValue(item.path, out var tex) && tex != null)
			GUI.DrawTexture(imageRect, tex, ScaleMode.ScaleToFit);
		else {
			GUI.Label(imageRect, "...", EditorStyles.centeredGreyMiniLabel);
			if(_queued.Add(item.path))
				_previewQueue.Enqueue(item.path);
		}
		if(selected)
			GUI.Label(new Rect(rect.x + 4, rect.y + 2, 30, 20), "✔", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = Color.white } });

		var nameStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
		GUI.Label(new Rect(rect.x, rect.y + _cellSize, rect.width, 15), item.name, nameStyle);
		if(selected)
			GUI.Label(new Rect(rect.x, rect.y + _cellSize + 14, rect.width, 15), "→ " + _selection.GroupOf(item.path), nameStyle);

		var tickRect = new Rect(imageRect.xMax - 24, imageRect.y + 3, 21, 21);
		GUI.Label(tickRect, selected ? "☑" : "☐", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17, alignment = TextAnchor.MiddleCenter });
		var e = Event.current;
		if(e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) {
			if(e.button == 0) {
				// tick box, or a second click on the toy being previewed: select / unselect. Otherwise just preview it
				if(tickRect.Contains(e.mousePosition) || _focusedPath == item.path) {
					if(selected)
						_selection.Remove(item.path);
					else
						_selection.Add(item.path, _selection.DefaultGroupFor(item.pack));
				}
				_focusedPath = item.path;
			}
			else if(e.button == 1) {
				EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(item.path));
			}
			e.Use();
			Repaint();
		}
	}

	void DrawBottomBar() {
		using(new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
			var counts = _selection.items.GroupBy(i => i.group).Select(g => $"{g.Key}: {g.Count()}");
			GUILayout.Label($"Đã chọn {_selection.items.Count} món   ({string.Join(", ", counts)})");
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Lưu lựa chọn", GUILayout.Width(110), GUILayout.Height(26))) {
				ToyPackData.SaveSelection(_selection);
				ShowNotification(new GUIContent("Đã lưu"));
			}
			GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
			if(GUILayout.Button("Thêm vào game", GUILayout.Width(130), GUILayout.Height(26))) {
				ToyPackData.SaveSelection(_selection);
				if(EditorUtility.DisplayDialog("Thêm vào game", $"Tạo prefab, ảnh thumbnail và thêm {_selection.items.Count} món vào các nhóm đồ chơi?", "Thêm", "Huỷ")) {
					ToyPackImporter.AddSelectionToGame();
					Rescan();
				}
			}
			GUI.backgroundColor = Color.white;
		}
		GUILayout.Label("Click: xem 3D.  Click lần nữa hoặc ô ☐: chọn / bỏ chọn.  Click phải: tìm prefab trong Project.", EditorStyles.miniLabel);
	}

	///<summary> Rotatable 3D view of the clicked toy </summary>
	void DrawPreviewPanel() {
		using(new EditorGUILayout.VerticalScope(GUILayout.Width(PREVIEW_WIDTH))) {
			var prefab = string.IsNullOrEmpty(_focusedPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(_focusedPath);
			if(prefab == null) {
				GUILayout.Label("Bấm vào một món để xem 3D.\nKéo chuột trong khung để xoay.", EditorStyles.wordWrappedLabel);
				return;
			}
			GUILayout.Label(prefab.name, EditorStyles.boldLabel);
			// rendered with the same URP converted materials as the thumbnails (the pack materials are not URP: pink in Unity's preview)
			var rect = GUILayoutUtility.GetRect(PREVIEW_WIDTH - 10, PREVIEW_WIDTH - 10);
			var e = Event.current;
			if(e.type == EventType.MouseDrag && rect.Contains(e.mousePosition)) {
				_yaw -= e.delta.x * 0.6f;
				_pitch = Mathf.Clamp(_pitch + e.delta.y * 0.6f, -89f, 89f);
				e.Use();
			}
			else if(e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition)) {
				_zoom = Mathf.Clamp(_zoom * (e.delta.y > 0 ? 0.9f : 1.1f), 0.5f, 3f);
				e.Use();
			}
			else if(e.type == EventType.MouseDown && e.clickCount == 2 && rect.Contains(e.mousePosition)) {
				_yaw = 0f;
				_pitch = 19f;
				_zoom = 1f;
				e.Use();
			}
			if(e.type == EventType.Repaint) {
				var key = $"{_focusedPath}|{_yaw:F0}|{_pitch:F0}|{_zoom:F2}";
				if(_bigPreview == null || _bigPreviewKey != key) {
					if(_bigPreview != null)
						DestroyImmediate(_bigPreview);
					_shooter ??= new ToyThumbnailShooter();
					_bigPreview = _shooter.Shoot(prefab, 512, true, ToyPackData.NeedsTurnAround(_focusedPath), _yaw, _pitch, _zoom);
					_bigPreviewKey = key;
				}
				EditorGUI.DrawRect(rect, new Color(0.82f, 0.86f, 0.92f));
				GUI.DrawTexture(rect, _bigPreview, ScaleMode.ScaleToFit);
			}
			GUILayout.Label("Kéo chuột để xoay, cuộn để zoom, double-click để đặt lại", EditorStyles.centeredGreyMiniLabel);
			var selected = _selection.Has(_focusedPath);
			var item = _items.FirstOrDefault(i => i.path == _focusedPath);
			GUI.backgroundColor = selected ? new Color(1f, 0.6f, 0.6f) : new Color(0.5f, 1f, 0.5f);
			if(item != null && GUILayout.Button(selected ? "Bỏ chọn món này" : "Chọn món này", GUILayout.Height(30))) {
				if(selected)
					_selection.Remove(item.path);
				else
					_selection.Add(item.path, _selection.DefaultGroupFor(item.pack));
			}
			GUI.backgroundColor = Color.white;
			if(selected)
				GUILayout.Label("→ nhóm " + _selection.GroupOf(_focusedPath), EditorStyles.miniLabel);
		}
	}
	#endregion//GUI


	#region Previews
	void RenderSomePreviews() {
		if(_previewQueue.Count == 0)
			return;
		_shooter ??= new ToyThumbnailShooter();
		var start = DateTime.Now;
		while(_previewQueue.Count > 0 && (DateTime.Now - start).TotalMilliseconds < 60) {
			var path = _previewQueue.Dequeue();
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			_previews[path] = prefab != null ? _shooter.Shoot(prefab, 160, true, ToyPackData.NeedsTurnAround(path)) : null;
		}
		Repaint();
	}
	#endregion//Previews
}


/// <summary>
/// Scan new packs, save/load the selection (Tools/ToyPackPicker/selection.json)
/// </summary>
public static class ToyPackData {
	///<summary> The packs that were just imported. Toys from older packs are already in the game </summary>
	public static readonly string[] PackFolders = {
		"Assets/Beverage_Pack_Low_Poly_Cartoon_Items",
		"Assets/Low Poly Christmas Pack Free",
		"Assets/Low-Poly_Objects_Pack",
		"Assets/Cartoon Items Pack Vol1",
		"Assets/3D Props Ultimate Pack/3D Prop Kitchen",
		"Assets/3D Props Ultimate Pack/3D Prop Camp",
		"Assets/3D Props Ultimate Pack/3d Prop Art",
		"Assets/3D Props Ultimate Pack/3D Prop Sport",
		"Assets/ToysPack",
		"Assets/ithappy/Cute_Furniture_Free",
		"Assets/ithappy/Funny_Weapons_Free",
		"Assets/ithappy/Cartoon_City_Free",
		"Assets/ithappy/Creative_Characters_FREE",
		"Assets/makemakex/Low_Poly_Vehicles_Pack",
		"Assets/Gley/DeliveryVehiclesPack",
	};
	///<summary> Short name of a pack: tab name and the folder under Model/NewToys </summary>
	static readonly Dictionary<string, string> SHORT_NAMES = new Dictionary<string, string> {
		{ "3D Prop Kitchen", "PropKitchen" },
		{ "3D Prop Camp", "PropCamp" },
		{ "3d Prop Art", "PropArt" },
		{ "3D Prop Sport", "PropSport" },
		{ "ToysPack", "ToysPack" },
		{ "Cute_Furniture_Free", "Furniture" },
		{ "Funny_Weapons_Free", "Weapons" },
		{ "Cartoon_City_Free", "City" },
		{ "Creative_Characters_FREE", "Characters" },
		{ "Low_Poly_Vehicles_Pack", "Vehicles" },
		{ "DeliveryVehiclesPack", "Delivery" },
	};
	static readonly string[] SKIP_PATH_PARTS = { "/Built-in/", "/Colliders/", "/Scenes/", "/Demo/", "/Baked/", "Render_Pipeline_Versions", "Render_Pipeline_Convert", "/Demo_Profiles/" };
	static readonly string[] SKIP_NAMES = { "plane", "floor", "ground", "camera", "light", "scene" };
	public const string SELECTION_FILE = "Tools/ToyPackPicker/selection.json";
	public const string REJECTED_LOG = "Tools/ToyPackPicker/rejected.txt";
	public const string TOY_STORAGE_PATH = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/ToyStorage.asset";
	public const string GROUP_FOLDER = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/ToyGroups";

	[Serializable]
	public class Item {
		public string path;
		public string pack;
		public string name;
	}

	[Serializable]
	public class Choice {
		public string path;
		public string group;
	}

	[Serializable]
	public class PackGroup {
		public string pack;
		public string group;
	}

	[Serializable]
	public class Selection {
		public List<Choice> items = new List<Choice>();
		public List<PackGroup> packGroups = new List<PackGroup>();

		public bool Has(string path) => items.Any(i => i.path == path);
		public string GroupOf(string path) => items.FirstOrDefault(i => i.path == path)?.group;
		public void Remove(string path) => items.RemoveAll(i => i.path == path);
		public void Add(string path, string group) {
			Remove(path);
			items.Add(new Choice { path = path, group = group });
		}
		public string DefaultGroupFor(string pack) {
			var found = packGroups.FirstOrDefault(p => p.pack == pack);
			return found != null ? found.group : SuggestedGroup(pack);
		}
		public void SetDefaultGroup(string pack, string group) {
			packGroups.RemoveAll(p => p.pack == pack);
			packGroups.Add(new PackGroup { pack = pack, group = group });
		}
	}

	///<summary> Packs modeled with the front facing +z. The game camera looks along +z, so these must turn 180 degrees to show their front </summary>
	static readonly string[] FRONT_FACES_PLUS_Z = { "Low Poly Christmas Pack Free", "Cartoon Items Pack Vol1" };
	public static bool NeedsTurnAround(string assetPath) {
		return FRONT_FACES_PLUS_Z.Any(pack => assetPath.Contains("/" + pack + "/"));
	}

	public static string ShortPackName(string pack) {
		if(SHORT_NAMES.TryGetValue(Path.GetFileName(pack), out var shortName))
			return shortName;
		if(pack.Contains("Beverage"))
			return "Beverage";
		if(pack.Contains("Christmas"))
			return "Christmas";
		if(pack.Contains("Objects"))
			return "Objects";
		if(pack.Contains("Cartoon"))
			return "Cartoon";
		return Path.GetFileName(pack);
	}

	static string SuggestedGroup(string pack) {
		if(SHORT_NAMES.ContainsKey(Path.GetFileName(pack)))
			return "Stuff";
		if(pack.Contains("Beverage"))
			return "FoodDrink";
		if(pack.Contains("Cartoon"))
			return "CartoonFont";
		return "Stuff";
	}

	///<summary> Existing groups (Fruit, Kitchen...) plus the new ones (FoodDrink, Stuff, CartoonFont) </summary>
	public static string[] GroupOptions(Selection selection) {
		var names = new List<string>();
		foreach(var guid in AssetDatabase.FindAssets("t:ToyPrefabGroup", new[] { GROUP_FOLDER })) {
			var name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
			names.Add(name.StartsWith("Group_") ? name.Substring(6) : name);
		}
		foreach(var extra in new[] { "FoodDrink", "Stuff", "CartoonFont" })
			if(!names.Contains(extra))
				names.Add(extra);
		return names.ToArray();
	}

	///<summary> Every prefab of the new packs, minus toys already used in gameplay </summary>
	public static List<Item> ScanNewItems() {
		var used = UsedSourcePrefabPaths();
		var result = new List<Item>();
		foreach(var pack in PackFolders) {
			if(!AssetDatabase.IsValidFolder(pack))
				continue;
			foreach(var guid in AssetDatabase.FindAssets("t:Prefab", new[] { pack })) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if(SKIP_PATH_PARTS.Any(part => path.Contains(part)))
					continue;
				var name = Path.GetFileNameWithoutExtension(path);
				var lower = name.ToLowerInvariant();
				if(SKIP_NAMES.Any(skip => lower.Contains(skip)))
					continue;
				if(used.Contains(path))
					continue;
				result.Add(new Item { path = path, pack = pack, name = name });
			}
		}
		result.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));
		return result;
	}

	///<summary> Source prefabs that already became a toy (recorded by the importer), and prefabs directly in a toy group </summary>
	static HashSet<string> UsedSourcePrefabPaths() {
		var used = new HashSet<string>(ToyPackImporter.LoadAddedLog());
		// toys removed with Toy Remover don't come back
		if(File.Exists(REJECTED_LOG))
			used.UnionWith(File.ReadAllLines(REJECTED_LOG).Where(l => l.Length > 0));
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(TOY_STORAGE_PATH);
		if(storage != null) {
			foreach(var group in storage.AllToyGroup) {
				if(group == null)
					continue;
				foreach(var toy in group.toyList) {
					if(toy == null)
						continue;
					used.Add(AssetDatabase.GetAssetPath(toy.gameObject));
					var source = PrefabUtility.GetCorrespondingObjectFromSource(toy.gameObject);
					if(source != null)
						used.Add(AssetDatabase.GetAssetPath(source));
				}
			}
		}
		return used;
	}

	public static Selection LoadSelection() {
		try {
			if(File.Exists(SELECTION_FILE))
				return JsonUtility.FromJson<Selection>(File.ReadAllText(SELECTION_FILE)) ?? new Selection();
		}
		catch(Exception e) {
			Debug.LogWarning($"[ToyPackPicker] cannot read {SELECTION_FILE}: {e.Message}");
		}
		return new Selection();
	}

	public static void SaveSelection(Selection selection) {
		Directory.CreateDirectory(Path.GetDirectoryName(SELECTION_FILE));
		File.WriteAllText(SELECTION_FILE, JsonUtility.ToJson(selection, true));
	}
}


/// <summary>
/// Turn chosen pack prefabs into game toys: prefab with Rigidbody + BoxCollider + ToyView (copied from an existing toy),
/// materials converted to URP, thumbnail sprite for the box UI, added to a toy group in ToyStorage.
/// Batch: -executeMethod ToyPackImporter.AddSelectionToGame
/// </summary>
public static class ToyPackImporter {
	const string OUTPUT_ROOT = "Assets/Match3D/Model/NewToys";
	const string THUMBNAIL_FOLDER = "Assets/Match3D/Sprite/ToyThumbnail";
	const string THUMBNAIL_STORAGE_PATH = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/ToyThumbnailStorage.asset";
	const string GROUP_ID_FOLDER = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/GroupID";
	const string ADDED_LOG = "Tools/ToyPackPicker/added.txt";
	///<summary> orange_prefab: reference for Rigidbody, layer, explode VFX </summary>
	const string REFERENCE_TOY_GUID = "c752a9e45c06a2d468229b771b5c4061";
	const int THUMBNAIL_SIZE = 200;

	public static List<string> LoadAddedLog() {
		return File.Exists(ADDED_LOG) ? File.ReadAllLines(ADDED_LOG).Where(l => l.Length > 0).ToList() : new List<string>();
	}

	///<summary> Undo every toy this tool added: prefabs, converted materials, thumbnails, group entries, the added log </summary>
	public static void DeleteEverythingAdded() {
		if(AssetDatabase.IsValidFolder(OUTPUT_ROOT))
			AssetDatabase.DeleteAsset(OUTPUT_ROOT);
		foreach(var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { THUMBNAIL_FOLDER })) {
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if(path.EndsWith("_new_thumbnail.png") || Path.GetFileName(path).StartsWith("NewToys_"))
				AssetDatabase.DeleteAsset(path);
		}
		RemoveMissingReferences();
		if(File.Exists(ADDED_LOG))
			File.Delete(ADDED_LOG);
		Debug.Log("[ToyPackImporter] deleted all added toys");
	}

	///<summary> Drop entries of deleted toys from the groups and the thumbnail map </summary>
	public static void RemoveMissingReferences() {
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(ToyPackData.TOY_STORAGE_PATH);
		foreach(var group in storage.AllToyGroup) {
			if(group != null && group.toyList.RemoveAll(t => t == null) > 0)
				EditorUtility.SetDirty(group);
		}
		var thumbnails = AssetDatabase.LoadAssetAtPath<ToyThumbnailStorage>(THUMBNAIL_STORAGE_PATH);
		var deadKeys = thumbnails.thumbnailMap.Where(pair => pair.Key == null || pair.Value == null).Select(pair => pair.Key).ToList();
		foreach(var key in deadKeys)
			if(key != null)
				thumbnails.thumbnailMap.Remove(key);
		// keys of deleted prefabs are "fake null" objects, rebuild the map without them
		var alive = thumbnails.thumbnailMap.Where(pair => pair.Key != null && pair.Value != null).ToList();
		thumbnails.thumbnailMap.Clear();
		foreach(var pair in alive)
			thumbnails.thumbnailMap[pair.Key] = pair.Value;
		EditorUtility.SetDirty(thumbnails);
		AssetDatabase.SaveAssets();
	}

	public static void AddSelectionToGame() {
		var selection = ToyPackData.LoadSelection();
		if(selection.items.Count == 0) {
			Debug.LogWarning("[ToyPackImporter] nothing selected");
			return;
		}
		var reference = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(REFERENCE_TOY_GUID))?.GetComponent<ToyView>();
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(ToyPackData.TOY_STORAGE_PATH);
		var thumbnails = AssetDatabase.LoadAssetAtPath<ToyThumbnailStorage>(THUMBNAIL_STORAGE_PATH);
		if(reference == null || storage == null || thumbnails == null) {
			Debug.LogError("[ToyPackImporter] missing reference toy / ToyStorage / ToyThumbnailStorage");
			return;
		}

		var added = LoadAddedLog();
		var materialCache = new Dictionary<Material, Material>();
		var scene = EditorSceneManager.NewPreviewScene();
		var shooter = new ToyThumbnailShooter();
		var report = new List<string>();
		try {
			for(int n = 0; n < selection.items.Count; n++) {
				var choice = selection.items[n];
				EditorUtility.DisplayProgressBar("Thêm đồ chơi vào game", choice.path, n / (float)selection.items.Count);
				var source = AssetDatabase.LoadAssetAtPath<GameObject>(choice.path);
				if(source == null) {
					report.Add($"SKIP (not found) {choice.path}");
					continue;
				}
				var group = GetOrCreateGroup(choice.group, storage);
				var toy = CreateToyPrefab(source, choice.path, group.groupID, reference, scene, materialCache);
				if(toy == null) {
					report.Add($"SKIP (no mesh) {choice.path}");
					continue;
				}
				if(!group.toyList.Contains(toy))
					group.toyList.Add(toy);
				EditorUtility.SetDirty(group);

				thumbnails.thumbnailMap[toy.gameObject] = SaveThumbnail(shooter, toy.gameObject);
				if(!added.Contains(choice.path))
					added.Add(choice.path);
				report.Add($"OK {group.name} <- {AssetDatabase.GetAssetPath(toy)}");
			}
		}
		finally {
			EditorUtility.ClearProgressBar();
			shooter.Dispose();
			EditorSceneManager.ClosePreviewScene(scene);
		}

		EditorUtility.SetDirty(thumbnails);
		EditorUtility.SetDirty(storage);
		AssetDatabase.SaveAssets();
		Directory.CreateDirectory(Path.GetDirectoryName(ADDED_LOG));
		File.WriteAllLines(ADDED_LOG, added);
		// the chosen toys are in the game now, clear the list for the next pick
		var leftover = new ToyPackData.Selection { packGroups = selection.packGroups };
		ToyPackData.SaveSelection(leftover);

		foreach(var group in storage.AllToyGroup)
			if(group != null && group.toyList.Count <= 12)
				report.Add($"NOTE group {group.name} has {group.toyList.Count} toys: only used by levels with fewer kinds (needs > typeUsed)");
		Debug.Log("[ToyPackImporter] done\n" + string.Join("\n", report));
		File.WriteAllLines("Tools/ToyPackPicker/last_import.txt", report);
	}

	public static ToyPrefabGroup GetOrCreateGroup(string groupName, ToyStorage storage) {
		var groupPath = $"{ToyPackData.GROUP_FOLDER}/Group_{groupName}.asset";
		var group = AssetDatabase.LoadAssetAtPath<ToyPrefabGroup>(groupPath);
		if(group == null) {
			var idPath = $"{GROUP_ID_FOLDER}/ToyGroupID_{groupName}.asset";
			var id = AssetDatabase.LoadAssetAtPath<ToyGroupID>(idPath);
			if(id == null) {
				id = ScriptableObject.CreateInstance<ToyGroupID>();
				id.groupName = groupName;
				AssetDatabase.CreateAsset(id, idPath);
			}
			group = ScriptableObject.CreateInstance<ToyPrefabGroup>();
			group.groupName = groupName;
			group.groupID = id;
			group.toyList = new List<ToyView>();
			AssetDatabase.CreateAsset(group, groupPath);
		}
		if(!storage.AllToyGroup.Contains(group)) {
			var so = new SerializedObject(storage);
			var list = so.FindProperty("allToyGroup");
			list.arraySize++;
			list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = group;
			so.ApplyModifiedPropertiesWithoutUndo();
		}
		return group;
	}

	static ToyView CreateToyPrefab(GameObject source, string sourcePath, ToyGroupID groupID, ToyView reference, Scene scene, Dictionary<Material, Material> materialCache) {
		var pack = ToyPackData.PackFolders.FirstOrDefault(p => sourcePath.StartsWith(p)) ?? "Other";
		var folder = $"{OUTPUT_ROOT}/{ToyPackData.ShortPackName(pack)}";
		EnsureFolder(folder);
		EnsureFolder(folder + "/Materials");

		var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
		if(PrefabUtility.IsPartOfPrefabInstance(instance))
			PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
		var toyName = ToyName(source.name);
		instance.name = toyName;
		instance.transform.position = Vector3.zero;
		if(ToyPackData.NeedsTurnAround(sourcePath))
			instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * instance.transform.rotation;

		// only keep meshes: drop old physics, scripts, lights, LODs from the pack
		foreach(var c in instance.GetComponentsInChildren<Component>(true)) {
			if(c == null || c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer)
				continue;
			Object.DestroyImmediate(c);
		}
		foreach(var t in instance.GetComponentsInChildren<Transform>(true))
			GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

		var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
		if(renderers.Length == 0) {
			Object.DestroyImmediate(instance);
			return null;
		}
		foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true)) {
			var mats = renderer.sharedMaterials;
			for(int m = 0; m < mats.Length; m++)
				mats[m] = SaveConvertedMaterial(mats[m], folder + "/Materials", materialCache);
			renderer.sharedMaterials = mats;
		}
		foreach(var t in instance.GetComponentsInChildren<Transform>(true))
			t.gameObject.layer = reference.gameObject.layer;

		// pivot at the meshes center (pack pivots are often at the bottom / end of the item: toys spin around it in the slots)
		instance = WrapWithCenteredRoot(instance, toyName);

		// physics like existing toys
		var rigid = instance.AddComponent<Rigidbody>();
		EditorUtility.CopySerialized(reference.mainRigid, rigid);
		var box = instance.AddComponent<BoxCollider>();
		var localBounds = LocalBounds(instance.transform);
		box.center = localBounds.center;
		box.size = Vector3.Max(localBounds.size, Vector3.one * 0.01f);

		var view = instance.AddComponent<ToyView>();
		view.groupID = groupID;
		view.mainRigid = rigid;
		view.mainCollider = box;
		view.mainRenderer = renderers[0];
		view.rootPrefab = instance;
		view.explodeVfxVar = reference.explodeVfxVar;

		var prefabPath = $"{folder}/{toyName}.prefab";
		var saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
		Object.DestroyImmediate(instance);
		return saved != null ? saved.GetComponent<ToyView>() : null;
	}

	///<summary> Put the model under a new root placed at the meshes center </summary>
	static GameObject WrapWithCenteredRoot(GameObject model, string toyName) {
		var root = new GameObject(toyName);
		root.layer = model.layer;
		SceneManager.MoveGameObjectToScene(root, model.scene);
		model.name = "Model";
		model.transform.SetParent(root.transform, true);
		var center = LocalBounds(root.transform).center;
		model.transform.localPosition -= center;
		return root;
	}

	///<summary> "044 Xmas_Tree" -> "Xmas_Tree_044" (number kept: the pack has several "Santa_claus") </summary>
	static string ToyName(string name) {
		var match = System.Text.RegularExpressions.Regex.Match(name, @"^(\d+)\s*(.+)$");
		var result = match.Success ? $"{match.Groups[2].Value}_{match.Groups[1].Value}" : name;
		return result.Replace(' ', '_');
	}

	///<summary> Bounds of all meshes, in root local space (for the BoxCollider on root) </summary>
	static Bounds LocalBounds(Transform root) {
		var has = false;
		var bounds = new Bounds();
		foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)) {
			if(filter.sharedMesh == null)
				continue;
			var mb = filter.sharedMesh.bounds;
			for(int i = 0; i < 8; i++) {
				var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				var p = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
				if(!has) {
					bounds = new Bounds(p, Vector3.zero);
					has = true;
				}
				else
					bounds.Encapsulate(p);
			}
		}
		return bounds;
	}

	static Material SaveConvertedMaterial(Material source, string folder, Dictionary<Material, Material> cache) {
		if(source == null || ToyThumbnailShooter.IsUrp(source))
			return source;
		if(cache.TryGetValue(source, out var done))
			return done;
		var path = $"{folder}/{source.name.Replace('/', '_')}_URP.mat";
		var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
		if(existing == null) {
			existing = ToyThumbnailShooter.ConvertToUrp(source);
			AssetDatabase.CreateAsset(existing, path);
		}
		cache[source] = existing;
		return existing;
	}

	static Sprite SaveThumbnail(ToyThumbnailShooter shooter, GameObject toyPrefab) {
		var tex = shooter.Shoot(toyPrefab, THUMBNAIL_SIZE, false);
		var packFolder = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(toyPrefab)));
		var path = $"{THUMBNAIL_FOLDER}/NewToys_{packFolder}_{toyPrefab.name}_thumbnail.png";
		File.WriteAllBytes(path, tex.EncodeToPNG());
		Object.DestroyImmediate(tex);
		AssetDatabase.ImportAsset(path);
		var importer = (TextureImporter)AssetImporter.GetAtPath(path);
		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Single;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.SaveAndReimport();
		return AssetDatabase.LoadAssetAtPath<Sprite>(path);
	}

	static void EnsureFolder(string path) {
		if(AssetDatabase.IsValidFolder(path))
			return;
		var parent = Path.GetDirectoryName(path).Replace('\\', '/');
		EnsureFolder(parent);
		AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
	}

	///<summary> Batch helper: render every new pack item into one contact sheet PNG (for checking how they look) </summary>
	public static void RenderContactSheet() {
		var items = ToyPackData.ScanNewItems();
		const int CELL = 128, COLUMNS = 20;
		var rows = Mathf.CeilToInt(items.Count / (float)COLUMNS);
		var sheet = new Texture2D(CELL * COLUMNS, CELL * rows, TextureFormat.RGBA32, false);
		var fill = Enumerable.Repeat(new Color32(214, 220, 234, 255), sheet.width * sheet.height).ToArray();
		sheet.SetPixels32(fill);
		var shooter = new ToyThumbnailShooter();
		for(int n = 0; n < items.Count; n++) {
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(items[n].path);
			var tex = shooter.Shoot(prefab, CELL, true, ToyPackData.NeedsTurnAround(items[n].path));
			var x = (n % COLUMNS) * CELL;
			var y = (rows - 1 - n / COLUMNS) * CELL;
			var px = tex.GetPixels();
			var bg = sheet.GetPixels(x, y, CELL, CELL);
			for(int i = 0; i < px.Length; i++)
				bg[i] = Color.Lerp(bg[i], px[i], px[i].a);
			sheet.SetPixels(x, y, CELL, CELL, bg);
			Object.DestroyImmediate(tex);
		}
		shooter.Dispose();
		Directory.CreateDirectory("Builds/ToyPackPicker");
		File.WriteAllBytes("Builds/ToyPackPicker/contact_sheet.png", sheet.EncodeToPNG());
		File.WriteAllLines("Builds/ToyPackPicker/contact_sheet.txt", items.Select((it, i) => $"{i}\t{it.path}"));
		Debug.Log($"[ToyPackPicker] contact sheet: {items.Count} items");
	}
}


/// <summary>
/// Renders a prefab to a transparent texture in a private preview scene (does not touch the open scene).
/// Non-URP materials (pack made for Built-in) are swapped for URP Simple Lit copies, like the game uses.
/// </summary>
public class ToyThumbnailShooter : IDisposable {
	Scene _scene;
	Camera _camera;
	readonly Dictionary<Material, Material> _tempMaterials = new Dictionary<Material, Material>();
	static Material _referenceMaterial;

	public ToyThumbnailShooter() {
		_scene = EditorSceneManager.NewPreviewScene();
		var camGo = new GameObject("ThumbCam");
		SceneManager.MoveGameObjectToScene(camGo, _scene);
		_camera = camGo.AddComponent<Camera>();
		_camera.scene = _scene;
		_camera.enabled = false;
		_camera.clearFlags = CameraClearFlags.SolidColor;
		_camera.backgroundColor = new Color(0, 0, 0, 0);
		_camera.fieldOfView = 25f;
		_camera.nearClipPlane = 0.01f;
		_camera.farClipPlane = 1000f;
		AddLight(new Vector3(40, -30, 0), 1.15f);
		AddLight(new Vector3(-20, 150, 0), 0.55f);
	}

	void AddLight(Vector3 euler, float intensity) {
		var go = new GameObject("ThumbLight");
		SceneManager.MoveGameObjectToScene(go, _scene);
		go.transform.rotation = Quaternion.Euler(euler);
		var light = go.AddComponent<Light>();
		light.type = LightType.Directional;
		light.intensity = intensity;
		light.shadows = LightShadows.None;
	}

	public Texture2D Shoot(GameObject prefab, int size, bool convertMaterials, bool turnAround = false) {
		return Shoot(prefab, size, convertMaterials, turnAround, 0f, Mathf.Atan(0.35f) * Mathf.Rad2Deg, 1f);
	}

	///<summary> yaw / pitch: camera angle around the toy (degrees), zoom: bigger = closer </summary>
	public Texture2D Shoot(GameObject prefab, int size, bool convertMaterials, bool turnAround, float yaw, float pitch, float zoom) {
		var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
		instance.transform.position = Vector3.zero;
		if(turnAround)
			instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * instance.transform.rotation;
		foreach(var rigid in instance.GetComponentsInChildren<Rigidbody>(true))
			rigid.isKinematic = true;

		var has = false;
		var bounds = new Bounds();
		foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true)) {
			if(!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) {
				renderer.enabled = false;
				continue;
			}
			if(convertMaterials) {
				var mats = renderer.sharedMaterials;
				for(int m = 0; m < mats.Length; m++)
					mats[m] = TempUrp(mats[m]);
				renderer.sharedMaterials = mats;
			}
			if(!has) {
				bounds = renderer.bounds;
				has = true;
			}
			else
				bounds.Encapsulate(renderer.bounds);
		}

		// front view, a bit from above (like the toy thumbnails already in the game)
		var radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
		var dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
		var distance = radius / Mathf.Sin(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f / Mathf.Max(0.2f, zoom);
		_camera.transform.position = bounds.center + dir * distance;
		_camera.transform.LookAt(bounds.center);
		_camera.nearClipPlane = Mathf.Max(0.001f, distance - radius * 1.5f);
		_camera.farClipPlane = distance + radius * 1.5f;

		var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
		rt.antiAliasing = 4;
		_camera.targetTexture = rt;
		_camera.Render();
		var previous = RenderTexture.active;
		RenderTexture.active = rt;
		var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
		tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
		tex.Apply();
		RenderTexture.active = previous;
		_camera.targetTexture = null;
		RenderTexture.ReleaseTemporary(rt);
		Object.DestroyImmediate(instance);
		return tex;
	}

	Material TempUrp(Material source) {
		if(source == null || IsUrp(source))
			return source;
		if(!_tempMaterials.TryGetValue(source, out var converted)) {
			converted = ConvertToUrp(source);
			converted.hideFlags = HideFlags.HideAndDontSave;
			_tempMaterials[source] = converted;
		}
		return converted;
	}

	public static bool IsUrp(Material material) {
		var name = material.shader != null ? material.shader.name : "";
		return name.StartsWith("Universal Render Pipeline") || name.StartsWith("Shader Graphs") || name.StartsWith("Hidden/Universal");
	}

	///<summary> Copy of the game's toy material (URP Simple Lit) with the source texture/color </summary>
	public static Material ConvertToUrp(Material source) {
		if(_referenceMaterial == null)
			_referenceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Match3D/Model/Adorable 3D/Adorable 3D Item_Atlas/Materials/01.mat");
		var result = _referenceMaterial != null ? new Material(_referenceMaterial) : new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
		result.name = source.name + "_URP";
		var tex = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
		if(tex == null && source.HasProperty("_BaseMap"))
			tex = source.GetTexture("_BaseMap");
		var color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
		result.SetTexture("_BaseMap", tex);
		result.SetTexture("_MainTex", tex);
		if(tex != null && source.HasProperty("_MainTex")) {
			result.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
			result.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
		}
		result.SetColor("_BaseColor", color);
		result.SetColor("_Color", color);
		// Standard shader "Fade"/"Transparent" (glass) -> URP transparent
		var transparent = (source.HasProperty("_Mode") && source.GetFloat("_Mode") >= 2f) || source.renderQueue >= 3000;
		if(transparent) {
			result.SetFloat("_Surface", 1f);
			result.SetFloat("_Blend", 0f);
			result.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			result.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			result.SetFloat("_ZWrite", 0f);
			result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			result.SetOverrideTag("RenderType", "Transparent");
			result.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		}
		return result;
	}

	public void Dispose() {
		foreach(var mat in _tempMaterials.Values)
			Object.DestroyImmediate(mat);
		_tempMaterials.Clear();
		if(_scene.IsValid())
			EditorSceneManager.ClosePreviewScene(_scene);
	}
}
