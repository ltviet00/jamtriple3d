#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorToyGroupVisualize : MonoBehaviour {
	#region Init, config
	[Header("Visualize Config")]
	[SerializeField] float distanceBetweenGroup = 1f;
	[SerializeField] float distanceBetweenToyInGroup = 0.5f;

	[Header("Toylist")]
	[SerializeField] List<Group> groupList;

	[Header("Generate")]
	[SerializeField] ToyStorage toyStorage;
	[SerializeField] Object genPath;

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


	#region Editor
	[Button("Collect toys, create Groups, split support/event toys")]
	void ArrangeToyByGroups() {
		CreateToyGroupByTag();
		VisualizeGroupOnScene();
	}

	[Button]
	void SaveToStorage() {
		GenerateToyGroupSOInTargetFolderAndSaveToStorage();
	}

	[Button]
	void ApplyChangeToRootPrefabs() {
		var allToy = FindObjectsOfType<ToyView>();
		for(int n = 0; n < allToy.Length; n++) {
			var toy = allToy[n];
			PrefabUtility.ApplyPrefabInstance(toy.gameObject, InteractionMode.UserAction);
		}
	}
	#endregion//Editor


	#region Private
	void CreateToyGroupByTag() {
		// clear first, in toyStorage
		groupList.Clear();
		toyStorage.SupportToyList.Clear();
		toyStorage.WeeklyToyList.Clear();

		// find toy and grouping
		var groupMap = new Dictionary<ToyGroupID, List<ToyView>>();
		var allToy = FindObjectsOfType<ToyView>();

		for(int n = 0; n < allToy.Length; n++) {
			var toy = allToy[n];
			// check special toy first
			if(CheckHandleForSpecialToy(toy)) {
				continue;
			}
			// normal toy
			if(groupMap.TryGetValue(toy.groupID, out var list)) {
				list.Add(toy);
			}
			else {
				var newList = new List<ToyView>();
				newList.Add(toy);
				groupMap.Add(toy.groupID, newList);
			}
		}
		// save to list
		foreach(var pair in groupMap) {
			var group = new Group();
			group.id = pair.Key;
			group.toyList = pair.Value;
			groupList.Add(group);
		}
		EditorUtility.SetDirty(gameObject);
	}

	void VisualizeGroupOnScene() {
		var rootPos = Vector3.zero;
		for(int n = 0; n < groupList.Count; n++) {
			var group = groupList[n];
			var pos = rootPos;
			pos.x += n * distanceBetweenGroup;
			group.ArrangeToys(pos, distanceBetweenToyInGroup);
		}
	}

	void GenerateToyGroupSOInTargetFolderAndSaveToStorage() {
		// clear first
		toyStorage.AllToyGroup.Clear();
		// generate
		var rootPath = AssetDatabase.GetAssetPath(genPath);
		for(int n = 0; n < groupList.Count; n++) {
			var group = groupList[n];
			var nameForSO = $"Group_{group.id.groupName}.asset";
			var pathForFile = $"{rootPath}/{nameForSO}";
			// load first, not exist? then create new one
			var groupSOfile = AssetDatabase.LoadAssetAtPath<ToyPrefabGroup>(pathForFile);
			if(groupSOfile == null) {
				groupSOfile = ScriptableObject.CreateInstance<ToyPrefabGroup>();
				AssetDatabase.CreateAsset(groupSOfile, pathForFile);
			}
			// update data
			//groupSOfile.toyTag = group.tag;
			groupSOfile.groupID = group.id;
			groupSOfile.toyList = group.toyList;
			groupSOfile.RelaceListByRootPrefab();
			// save to storage
			toyStorage.AllToyGroup.Add(groupSOfile);
		}
		// refresh
		EditorUtility.SetDirty(toyStorage);
		AssetDatabase.SaveAssets();
	}

	GameObject GetSourcePrefabForToy(ToyView toy) {
		/// https://forum.unity.com/threads/get-prefab-from-gameobject-of-that-type.714092/
		/// https://docs.unity3d.com/ScriptReference/PrefabUtility.GetCorrespondingObjectFromSource.html
		var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(toy.gameObject);
		if(rootPrefab != null) {
			// alreay root prefab?
			if(rootPrefab == toy.gameObject) {
				return toy.gameObject;
			}
			// carefully check, if we call GetCorrespondingObjectFromSource() it will return the object without the TOyView component
			var viewer = rootPrefab.GetComponent<ToyView>();
			if(viewer == null) {
				Log.Warning(false, viewer, $"Not found ToyView component on sourcePrefab [{viewer.name}]");
				return null;
			}
			// okay now
			return rootPrefab;
		}
		else {
			Log.Warning(false, toy, $"NOt found rootPrefab of [{toy.name}]");
			return null;
		}
	}

	///<summary> check special toy first: supportToy, event toys. Return true if found the marker for special toy</summary>
	bool CheckHandleForSpecialToy(ToyView toy) {
		// check special toy first: supportToy, event toys
		var supportMarker = toy.GetComponent<SupportToyMarker>();
		if(supportMarker != null) {
			var sourcePrefab = GetSourcePrefabForToy(toy);
			if(sourcePrefab == null) {
				return false;
			}
			toyStorage.AddSupportToyToList(supportMarker.supportToyID, sourcePrefab.GetComponent<ToyView>());
			return true;
		}
		// weekly event toy
		var weeklyMarker = toy.GetComponent<WeeklyToyMarker>();
		if(weeklyMarker != null) {
			var sourcePrefab = GetSourcePrefabForToy(toy);
			if(sourcePrefab == null) {
				return false;
			}
			toyStorage.AddWeeklyToyToList(weeklyMarker.weeklyToyID, sourcePrefab.GetComponent<ToyView>());
			return true;
		}
		// not found any special marker
		return false;
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	public class Group {
		//public ToyTags tag;
		public ToyGroupID id;
		public List<ToyView> toyList;

		public void ArrangeToys(Vector3 rootPostion, float distanceBetweenToy) {
			for(int n = 0; n < toyList.Count; n++) {
				var toy = toyList[n];
				var pos = rootPostion;
				pos.y += n * distanceBetweenToy;
				toy.transform.position = pos;
			}
		}
	}
	#endregion//Classes
}
#endif//UNITY_EDITOR