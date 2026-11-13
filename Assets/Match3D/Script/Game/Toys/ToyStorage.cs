using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
#endif//UNITY_EDITOR

/// <summary>
/// Storage all toys in the game
/// </summary>
[CreateAssetMenu(fileName = "ToyStorage", menuName = "Match3D/Toys/ToyStorage")]
public class ToyStorage : ScriptableObject {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] IntegerVariable userCurrentLevelVar;

	[Header("Support toy")]
	[SerializeField] List<SupportToyDefine> supportToyList;

	[Header("Weekly toy")]
	[SerializeField] List<WeeklyToyDefine> weeklyToyList;

	[Header("Toy groups")]
	[SerializeField] List<ToyPrefabGroup> allToyGroup;
	[Tooltip("A level mixes toys from this many random groups (more variety)")]
	[SerializeField] int groupsPerLevel = 3;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Public
	public List<ToyPrefabGroup> AllToyGroup => allToyGroup;
	public List<SupportToyDefine> SupportToyList => supportToyList;
	public List<WeeklyToyDefine> WeeklyToyList => weeklyToyList;

	///<summary> Random pick a group that suitable for the level </summary>
	public ToyPrefabGroup SelectToyPrefabGroupForLevel(LevelData levelData) {
		if(isFailedConfig)
			return null;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
		var forced = DebugLevelJump.ForcedToyGroup();
		if(!string.IsNullOrEmpty(forced)) {
			var group = allToyGroup.Find(g => g != null && g.name.Contains(forced));
			if(group != null) {
				Log.Info($"[Debug] forced toy group [{group.name}]");
				return group;
			}
		}
#endif
		// currently, only one condition : enough required toyType count
		var candidateList = new List<ToyPrefabGroup>();
		for(int n = 0; n < allToyGroup.Count; n++) {
			var group = allToyGroup[n];
			if(group.toyList.Count > levelData.totalInfo.typeUsed) {
				candidateList.Add(group);
			}
		}
		if(candidateList.Count > 0) {
			var randomIndex = Random.Range(0, candidateList.Count);
			Log.Info($"Select group [{candidateList[randomIndex].name}]");
			return candidateList[randomIndex];
		}
		Log.Error(false, this, $"Not found any toyGroup has enough [{levelData.totalInfo.typeUsed}] toy");
		return null;
	}

	///<summary> Toy kinds for a level, mixed from several random groups (groupsPerLevel), split evenly between them.
	/// Returns exactly typeUsed kinds when there are enough toys </summary>
	public List<ToyView> SelectToyPrefabsForLevel(LevelData levelData) {
		var result = new List<ToyView>();
		if(isFailedConfig)
			return result;
		var typeUsed = levelData.totalInfo.typeUsed;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
		var forced = DebugLevelJump.ForcedToyGroup();
		if(!string.IsNullOrEmpty(forced)) {
			var group = allToyGroup.Find(g => g != null && g.name.Contains(forced));
			if(group != null) {
				Log.Info($"[Debug] forced toy group [{group.name}]");
				result.AddRange(MathUtils.TakeOutSubListWithRandomElement(group.toyList.FindAll(t => t != null), Mathf.Min(typeUsed, group.toyList.Count)));
				return result;
			}
		}
#endif
		// random group order, each group's toys shuffled
		var groups = allToyGroup.FindAll(g => g != null && g.toyList != null && g.toyList.Count > 0);
		for(int n = groups.Count - 1; n > 0; n--) {
			var k = Random.Range(0, n + 1);
			(groups[n], groups[k]) = (groups[k], groups[n]);
		}
		var pools = new List<List<ToyView>>();
		foreach(var group in groups) {
			var pool = group.toyList.FindAll(t => t != null);
			for(int n = pool.Count - 1; n > 0; n--) {
				var k = Random.Range(0, n + 1);
				(pool[n], pool[k]) = (pool[k], pool[n]);
			}
			pools.Add(pool);
		}
		// take one toy from each of the first groupsPerLevel groups in turn; a group that runs out lets the next group in
		var used = new HashSet<ToyView>();
		var active = Mathf.Clamp(groupsPerLevel, 1, Mathf.Max(1, pools.Count));
		var nextGroup = active;
		var activePools = pools.GetRange(0, Mathf.Min(active, pools.Count));
		var names = new List<string>();
		for(int n = 0; n < activePools.Count; n++)
			names.Add(groups[n].name);
		var turn = 0;
		while(result.Count < typeUsed && activePools.Count > 0) {
			var index = turn % activePools.Count;
			var pool = activePools[index];
			ToyView picked = null;
			while(pool.Count > 0 && picked == null) {
				var toy = pool[pool.Count - 1];
				pool.RemoveAt(pool.Count - 1);
				if(used.Add(toy))
					picked = toy;
			}
			if(picked != null) {
				result.Add(picked);
				turn++;
			}
			else {
				activePools.RemoveAt(index);
				if(nextGroup < pools.Count) {
					activePools.Add(pools[nextGroup]);
					names.Add(groups[nextGroup].name);
					nextGroup++;
				}
			}
		}
		if(result.Count < typeUsed)
			Log.Error(false, this, $"Not enough toys for [{typeUsed}] kinds, got [{result.Count}]");
		Log.Info($"Select toys from groups [{string.Join(", ", names)}]");
		return result;
	}

	///<summary> Currently call in Editor for update supportToy list only </summary>
	public void AddSupportToyToList(SupportToyID id, ToyView toyview) {
		if(isFailedConfig)
			return;
		var record = new SupportToyDefine() { id = id, toyView = toyview };
		supportToyList.Add(record);
	}

	///<summary> Currently call in Editor for update weeklyToy list only </summary>
	public void AddWeeklyToyToList(WeeklyToyID id, ToyView toyview) {
		if(isFailedConfig)
			return;
		var record = new WeeklyToyDefine() { id = id, toyView = toyview };
		weeklyToyList.Add(record);
	}

	public ToyView GetSupportToyWithID(SupportToyID id) {
		for(int n = 0; n < supportToyList.Count; n++) {
			var item = supportToyList[n];
			if(item.id == id)
				return item.toyView;
		}
		Log.Error(false, this, $"Not found supportToy for ID [{id.name}]");
		return null;
	}
	#endregion//Public


	#region Public - weekly toy
	public WeeklyToyID GetWeeklyEventToyIdWithName(string toyName) {
		for(int n = 0; n < weeklyToyList.Count; n++) {
			var item = weeklyToyList[n];
			if(item.id.toyName == toyName)
				return item.id;
		}
		Log.Error(false, this, $"Not found weeklyToy with ToyName [{toyName}]");
		return null;
	}

	public ToyView GetWeeklyEventToyWithID(WeeklyToyID id) {
		for(int n = 0; n < weeklyToyList.Count; n++) {
			var item = weeklyToyList[n];
			if(item.id == id)
				return item.toyView;
		}
		Log.Error(false, this, $"Not found weeklyToy for ID [{id.name}]");
		return null;
	}

	public ToyView GetWeeklyEventToyWithToyName(string toyName) {
		for(int n = 0; n < weeklyToyList.Count; n++) {
			var item = weeklyToyList[n];
			if(item.id.toyName == toyName)
				return item.toyView;
		}
		Log.Error(false, this, $"Not found weeklyToy with ToyName [{toyName}]");
		return null;
	}
	#endregion//Public - weekly toy


	#region Classes
	[System.Serializable]
	public class SupportToyDefine {
		public SupportToyID id;
		public ToyView toyView;
	}

	[System.Serializable]
	public class WeeklyToyDefine {
		public WeeklyToyID id;
		public ToyView toyView;
	}
	#endregion//Classes
}
