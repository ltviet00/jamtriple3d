using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector;
#endif//UNITY_EDITOR

/// <summary>
/// Group toy prefab into groups, for unlocking later.
/// ToyStorage will only spawn toy that in "default" + unlocked groups.
/// </summary>
[CreateAssetMenu(fileName = "ToyPrefabGroup", menuName = "Match3D/Toys/ToyPrefabGroup")]
public class ToyPrefabGroup : ScriptableObject {
	[Header("Main data")]
	public string groupName;
	///<summary> GroupID for toys in thsi group : house, fruit ... </summary>
	public ToyGroupID groupID;
	///<summary> Toy in this group </summary>
	public List<ToyView> toyList;

#if UNITY_EDITOR
	///<summary> The toyList default is ref to ToyView on scene, we must replace by rootPrefab in project </summary>
	[Button]
	public void RelaceListByRootPrefab() {
		CheckAndReplceRootPrefabForList(toyList);
		//CheckAndReplceRootPrefabForList(showCaseToyList);
	}

	void CheckAndReplceRootPrefabForList(List<ToyView> list) {
		for(int n = 0; n < list.Count; n++) {
			var toy = list[n];
			/// https://forum.unity.com/threads/get-prefab-from-gameobject-of-that-type.714092/
			/// https://docs.unity3d.com/ScriptReference/PrefabUtility.GetCorrespondingObjectFromSource.html
			var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(toy.gameObject);
			if(rootPrefab != null) {
				// alreay root prefab?
				if(rootPrefab == toy.gameObject) {
					continue;
				}
				// carefully check, if we call GetCorrespondingObjectFromSource() it will return the object without the TOyView component
				var viewer = rootPrefab.GetComponent<ToyView>();
				if(viewer != null) {
					list[n] = viewer;
				}
				else {
					Log.Warning(false, rootPrefab, $"Not found ToyView component on sourcePrefab [{viewer.name}]");
				}
			}
			else {
				Log.Warning(false, toy, $"NOt found rootPrefab of [{toy.name}]");
			}
		}
		EditorUtility.SetDirty(this);
	}
#endif//UNITY_EDITOR
}
