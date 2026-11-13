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
