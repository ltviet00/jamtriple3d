using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pick toys you don't like and remove them from the game: from every toy group, the thumbnail map
/// and the visual size table. The prefab and its thumbnail go to the OS trash (can be restored from there).
/// Menu: Tools/Toy Remover
/// </summary>
public class ToyRemoverWindow : EditorWindow {
	const string NEW_TOYS_ROOT = "Assets/Match3D/Model/NewToys";
	const string THUMBNAIL_STORAGE_PATH = "Assets/Match3D/ScriptableObject/ToyAndLevel/ToyStorage/ToyThumbnailStorage.asset";
	const string VISUAL_TABLE_PATH = "Assets/Match3D/Resources/ToyVisualSizeTable.asset";
	const string ADDED_LOG = "Tools/ToyPackPicker/added.txt";
	const float CELL = 96f;

	bool _onlyNewToys = true;
	string _search = "";
	Vector2 _scroll;
	readonly HashSet<GameObject> _marked = new HashSet<GameObject>();
	GameObject _focused;
	Editor _previewEditor;
	List<(ToyPrefabGroup group, List<GameObject> toys)> _rows;
	ToyThumbnailStorage _thumbnails;

	[MenuItem("Tools/Toy Remover")]
	static void Open() {
		var window = GetWindow<ToyRemoverWindow>("Toy Remover");
		window.minSize = new Vector2(700, 450);
		window.Reload();
	}

	void OnEnable() => Reload();

	void OnDisable() {
		if(_previewEditor != null)
			DestroyImmediate(_previewEditor);
	}

	void Reload() {
		_thumbnails = AssetDatabase.LoadAssetAtPath<ToyThumbnailStorage>(THUMBNAIL_STORAGE_PATH);
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(ToyPackData.TOY_STORAGE_PATH);
		_rows = new List<(ToyPrefabGroup, List<GameObject>)>();
		if(storage == null)
			return;
		foreach(var group in storage.AllToyGroup.Where(g => g != null)) {
			var toys = group.toyList.Where(t => t != null).Select(t => t.gameObject)
				.Where(t => !_onlyNewToys || AssetDatabase.GetAssetPath(t).StartsWith(NEW_TOYS_ROOT)).ToList();
			if(toys.Count > 0)
				_rows.Add((group, toys));
		}
		_marked.RemoveWhere(t => t == null);
	}

	void OnGUI() {
		if(_rows == null)
			Reload();
		using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
			var only = GUILayout.Toggle(_onlyNewToys, "Chỉ đồ chơi mới (NewToys)", EditorStyles.toolbarButton, GUILayout.Width(180));
			if(only != _onlyNewToys) {
				_onlyNewToys = only;
				Reload();
			}
			GUILayout.Label("Tìm:", GUILayout.Width(30));
			_search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(160));
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Bỏ chọn hết", EditorStyles.toolbarButton))
				_marked.Clear();
			if(GUILayout.Button("Tải lại", EditorStyles.toolbarButton))
				Reload();
		}

		using(new EditorGUILayout.HorizontalScope()) {
			// toy grid
			using(var scroll = new EditorGUILayout.ScrollViewScope(_scroll)) {
				_scroll = scroll.scrollPosition;
				var perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 300f - 30f) / (CELL + 4f)));
				foreach(var (group, toys) in _rows) {
					var shown = toys.Where(t => string.IsNullOrEmpty(_search) || t.name.ToLower().Contains(_search.ToLower())).ToList();
					if(shown.Count == 0)
						continue;
					var total = group.toyList.Count(t => t != null);
					var removing = group.toyList.Count(t => t != null && _marked.Contains(t.gameObject));
					GUILayout.Label($"{group.name.Replace("Group_", "")}  —  {total} món{(removing > 0 ? $", còn {total - removing} sau khi xoá" : "")}", EditorStyles.boldLabel);
					for(int i = 0; i < shown.Count; i += perRow) {
						using(new EditorGUILayout.HorizontalScope()) {
							for(int j = i; j < Mathf.Min(i + perRow, shown.Count); j++)
								DrawCell(shown[j]);
							GUILayout.FlexibleSpace();
						}
					}
					GUILayout.Space(8);
				}
			}
			// preview + actions
			using(new EditorGUILayout.VerticalScope(GUILayout.Width(300))) {
				if(_focused != null) {
					GUILayout.Label(_focused.name, EditorStyles.boldLabel);
					GUILayout.Label(AssetDatabase.GetAssetPath(_focused), EditorStyles.miniLabel);
					if(_previewEditor == null || _previewEditor.target != _focused) {
						if(_previewEditor != null)
							DestroyImmediate(_previewEditor);
						_previewEditor = Editor.CreateEditor(_focused);
					}
					var rect = GUILayoutUtility.GetRect(290, 290);
					_previewEditor.OnInteractivePreviewGUI(rect, EditorStyles.helpBox);
					GUILayout.Label("Kéo chuột trong khung để xoay", EditorStyles.centeredGreyMiniLabel);
				}
				else {
					GUILayout.Label("Bấm vào một món để xem 3D.\nBấm lần nữa (hoặc nút X) để đánh dấu xoá.", EditorStyles.wordWrappedLabel);
				}
				GUILayout.FlexibleSpace();
				if(_marked.Count > 0) {
					GUILayout.Label("Sẽ xoá:", EditorStyles.boldLabel);
					GUILayout.Label(string.Join(", ", _marked.Where(t => t != null).Select(t => t.name)), EditorStyles.wordWrappedMiniLabel);
				}
				GUI.enabled = _marked.Count > 0;
				GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
				if(GUILayout.Button($"Xoá {_marked.Count} món khỏi game", GUILayout.Height(36))) {
					if(EditorUtility.DisplayDialog("Xoá đồ chơi",
						$"Xoá {_marked.Count} món khỏi mọi group, thumbnail và bảng size?\nPrefab và ảnh thumbnail sẽ vào Thùng rác (khôi phục được).",
						"Xoá", "Huỷ")) {
						var log = Remove(_marked.ToList());
						Debug.Log(log);
						_marked.Clear();
						_focused = null;
						Reload();
					}
				}
				GUI.backgroundColor = Color.white;
				GUI.enabled = true;
			}
		}
	}

	void DrawCell(GameObject toy) {
		var rect = GUILayoutUtility.GetRect(CELL, CELL + 16f, GUILayout.Width(CELL));
		var imageRect = new Rect(rect.x, rect.y, CELL, CELL);
		var marked = _marked.Contains(toy);
		EditorGUI.DrawRect(imageRect, toy == _focused ? new Color(0.25f, 0.45f, 0.8f, 0.5f) : new Color(0f, 0f, 0f, 0.2f));
		Texture image = null;
		if(_thumbnails != null && _thumbnails.thumbnailMap.TryGetValue(toy, out var sprite) && sprite != null)
			image = sprite.texture;
		if(image == null)
			image = AssetPreview.GetAssetPreview(toy);
		if(image != null)
			GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit);
		else
			Repaint();
		if(marked) {
			EditorGUI.DrawRect(imageRect, new Color(1f, 0f, 0f, 0.35f));
			GUI.Label(imageRect, "XOÁ", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 18, normal = { textColor = Color.white } });
		}
		var xRect = new Rect(imageRect.xMax - 20, imageRect.y + 2, 18, 18);
		if(GUI.Button(xRect, marked ? "↺" : "X")) {
			if(marked) _marked.Remove(toy); else _marked.Add(toy);
		}
		else if(Event.current.type == EventType.MouseDown && imageRect.Contains(Event.current.mousePosition)) {
			if(_focused == toy) {
				if(marked) _marked.Remove(toy); else _marked.Add(toy);
			}
			_focused = toy;
			Event.current.Use();
			Repaint();
		}
		GUI.Label(new Rect(rect.x, rect.y + CELL, CELL, 16), toy.name, EditorStyles.miniLabel);
	}

	///<summary> Remove the toys from groups, thumbnail map, visual size table and the picker log; prefab + thumbnail to trash </summary>
	public static string Remove(List<GameObject> toys) {
		var lines = new List<string>();
		var storage = AssetDatabase.LoadAssetAtPath<ToyStorage>(ToyPackData.TOY_STORAGE_PATH);
		var thumbnails = AssetDatabase.LoadAssetAtPath<ToyThumbnailStorage>(THUMBNAIL_STORAGE_PATH);
		var table = AssetDatabase.LoadAssetAtPath<ToyVisualSizeTable>(VISUAL_TABLE_PATH);
		var trash = new List<string>();
		var names = new HashSet<string>();
		foreach(var toy in toys.Where(t => t != null)) {
			names.Add(toy.name);
			foreach(var group in storage.AllToyGroup.Where(g => g != null)) {
				if(group.toyList.RemoveAll(t => t != null && t.gameObject == toy) > 0) {
					EditorUtility.SetDirty(group);
					lines.Add($"{toy.name}: removed from {group.name}");
				}
			}
			if(thumbnails != null && thumbnails.thumbnailMap.TryGetValue(toy, out var sprite)) {
				thumbnails.thumbnailMap.Remove(toy);
				var spritePath = sprite != null ? AssetDatabase.GetAssetPath(sprite) : null;
				// only trash thumbnails made for the new toys, old ones may be shared
				if(!string.IsNullOrEmpty(spritePath) && Path.GetFileName(spritePath).StartsWith("NewToys_"))
					trash.Add(spritePath);
			}
			if(table != null && table.entries.RemoveAll(e => e.prefab == toy) > 0)
				table.ClearCache();
			var prefabPath = AssetDatabase.GetAssetPath(toy);
			// prefabs made by the Toy Pack Picker can go; old toys only leave the groups (their models may be used elsewhere)
			if(prefabPath.StartsWith(NEW_TOYS_ROOT))
				trash.Add(prefabPath);
		}
		if(thumbnails != null)
			EditorUtility.SetDirty(thumbnails);
		if(table != null)
			EditorUtility.SetDirty(table);
		AssetDatabase.SaveAssets();
		var failed = new List<string>();
		if(trash.Count > 0)
			AssetDatabase.MoveAssetsToTrash(trash.ToArray(), failed);
		lines.Add($"trashed {trash.Count - failed.Count} files{(failed.Count > 0 ? $", failed: {string.Join(", ", failed)}" : "")}");
		// the picker won't list them again (rejected.txt)
		if(File.Exists(ADDED_LOG)) {
			var all = File.ReadAllLines(ADDED_LOG);
			var removed = all.Where(l => names.Contains(Path.GetFileNameWithoutExtension(l))).ToArray();
			File.WriteAllLines(ADDED_LOG, all.Except(removed).ToArray());
			if(removed.Length > 0)
				File.AppendAllLines(ToyPackData.REJECTED_LOG, removed);
		}
		ToyPackImporter.RemoveMissingReferences();
		return "[ToyRemover]\n" + string.Join("\n", lines);
	}
}
