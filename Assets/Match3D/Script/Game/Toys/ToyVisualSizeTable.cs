using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visible size of each toy prefab, measured in the Editor (Tools/Toy Visual Size - Measure all toys):
/// sqrt of the silhouette area seen on its biggest face, in prefab root units.
/// A toy with holes / thin parts (letters, figures, cans) fills less of its box than a round fruit:
/// sizing by the box made it look smaller. Loaded from Resources at runtime.
/// </summary>
[CreateAssetMenu(fileName = "ToyVisualSizeTable", menuName = "Game/Toy/ToyVisualSizeTable")]
public class ToyVisualSizeTable : ScriptableObject {
	public const string RESOURCES_PATH = "ToyVisualSizeTable";

	[System.Serializable]
	public class Entry {
		public GameObject prefab;
		///<summary> sqrt(silhouette area), prefab root units </summary>
		public float visualSize;
		///<summary> visualSize / sqrt(box biggest face area): how much of the box the toy fills </summary>
		public float fill;
	}

	public List<Entry> entries = new List<Entry>();
	[Tooltip("Median fill of all toys: a toy with this fill keeps the box based size")]
	public float referenceFill = 0.8f;

	Dictionary<GameObject, Entry> _map;

	public bool TryGet(GameObject prefab, out Entry entry) {
		if(_map == null) {
			_map = new Dictionary<GameObject, Entry>();
			foreach(var e in entries) {
				if(e != null && e.prefab != null && e.visualSize > 0f)
					_map[e.prefab] = e;
			}
		}
		entry = null;
		return prefab != null && _map.TryGetValue(prefab, out entry);
	}

	static ToyVisualSizeTable _instance;
	static bool _loaded;
	public static ToyVisualSizeTable Instance {
		get {
			if(!_loaded) {
				_instance = Resources.Load<ToyVisualSizeTable>(RESOURCES_PATH);
				_loaded = true;
			}
			return _instance;
		}
	}

#if UNITY_EDITOR
	public void ClearCache() {
		_map = null;
		_loaded = false;
	}
#endif
}
