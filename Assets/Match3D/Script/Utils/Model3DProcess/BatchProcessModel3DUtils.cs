using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif//#UNITY_EDITOR

public class BatchProcessModel3DUtils : MonoBehaviour {
#if UNITY_EDITOR
	[Header("Config")]
	[SerializeField] Object targetExportFolder;

	[Header("Validate BoxCollider")]
	[SerializeField] Vector3 minSizeBoxCollider;

	[Header("Validate Capsule collider")]
	[SerializeField] float minRadiusCapsuleCollider;

	[Button]
	void AddBoxCollidersForAllChild() {
		AddCollidersForAllChild<BoxCollider>();
	}

	[Button]
	void AddSphereCollidersForAllChild() {
		AddCollidersForAllChild<SphereCollider>();
	}

	[Button]
	void AddCapsuleCollidersForAllChild() {
		AddCollidersForAllChild<CapsuleCollider>();
	}

	[Button]
	void ClearCurrentCollidersForAllChildIfExist() {
		for(int n = 0; n < transform.childCount; n++) {
			var child = transform.GetChild(n).gameObject;
			var boxCollider = child.GetComponent<Collider>();
			if(boxCollider != null) {
				Object.DestroyImmediate(boxCollider);
				EditorUtility.SetDirty(child.gameObject);
			}
		}
	}

	[Button]
	void MakeSureMinSizeOfBoxCollider() {
		ClearConsole();
		for(int n = 0; n < transform.childCount; n++) {
			var child = transform.GetChild(n).gameObject;
			var boxCollider = child.GetComponent<BoxCollider>();
			if(boxCollider != null) {
				var boxSize = boxCollider.size;
				if(boxSize.x < minSizeBoxCollider.x
					|| boxSize.y < minSizeBoxCollider.y
					|| boxSize.z < minSizeBoxCollider.z) {
					boxSize.x = boxSize.x < minSizeBoxCollider.x ? minSizeBoxCollider.x : boxSize.x;
					boxSize.y = boxSize.y < minSizeBoxCollider.y ? minSizeBoxCollider.y : boxSize.y;
					boxSize.z = boxSize.z < minSizeBoxCollider.z ? minSizeBoxCollider.z : boxSize.z;
					boxCollider.size = boxSize;
					Log.Info(child, $"Fix box collider for [{child}]");
				}
			}
		}
	}

	[Button]
	void AddRigidbodyForAllChild() {
		ClearConsole();
		for(int n = 0; n < transform.childCount; n++) {
			var child = transform.GetChild(n).gameObject;
			child.AddComponent<Rigidbody>();
		}
		EditorUtility.SetDirty(gameObject);
	}

	[Button]
	void MakeSureMinSizeOfCapsuleCollider() {
		ClearConsole();
		for(int n = 0; n < transform.childCount; n++) {
			var child = transform.GetChild(n).gameObject;
			var capsuleCollider = child.GetComponent<CapsuleCollider>();
			if(capsuleCollider != null) {
				var radius = capsuleCollider.radius;
				if(radius < minRadiusCapsuleCollider) {
					capsuleCollider.radius = minRadiusCapsuleCollider;
					Log.Info(child, $"Fix capsule collider for [{child}]");
				}
			}
		}
	}

	[Button]
	void CreatePrefabForAllChildToTargetFolder() {
		ClearConsole();
		var rootPath = AssetDatabase.GetAssetPath(targetExportFolder);
		for(int n = 0; n < transform.childCount; n++) {
			var child = transform.GetChild(n).gameObject;
			var path = $"{rootPath}/{child.name}_prefab.prefab";
			// Make sure the file name is unique, in case an existing Prefab has the same name.
			AssetDatabase.GenerateUniqueAssetPath(path);
			// save to prefab, link object as well
			PrefabUtility.SaveAsPrefabAssetAndConnect(child, path, InteractionMode.UserAction);
		}
	}

	void AddCollidersForAllChild<T>() where T : Collider {
		ClearConsole();
		for(int n = 0; n < transform.childCount; n++) {
			var child = transform.GetChild(n).gameObject;
			var existCollider = child.GetComponent<Collider>();
			if(existCollider != null) {
				Log.Info(child, $"Already contain collider in this object [{child.name}], ignore");
				continue;
			}
			child.AddComponent<T>();
		}
		EditorUtility.SetDirty(gameObject);
	}

	static void ClearConsole() {
		// This simply does "LogEntries.Clear()" the long way:
		const string LOG_ENTRIES_PATH = "UnityEditor.LogEntries, UnityEditor.dll";
		var logEntries = System.Type.GetType(LOG_ENTRIES_PATH);
		if(logEntries == null) {
			Debug.LogWarning("ReferenceFinder, cannot clear console windows");
			return;
		}
		var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
		if(clearMethod == null) {
			Debug.LogWarning("ReferenceFinder, cannot find [Clear] method in dll to invoke, console not clear");
			return;
		}
		clearMethod.Invoke(null, null);
	}
#endif//UNITY_EDITOR
}
