#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ClearUserDataEditor {
	#region Constant
	const string USER_DATA_PATH = "Assets/UserData";
	const string USER_INVENTORY_PATH = "Assets/Match3D/ScriptableObject/User";
	#endregion// Constant


	#region Public statics
	[MenuItem("Utilities/ClearUserData")]
	public static void ClearUserData() {
		DeleteUserDataFileInEditorFolder();
		ResetAllInventory();
	}
	#endregion//Public statics


	#region Private
	static void DeleteUserDataFileInEditorFolder() {
		var allUserData = AssetDatabase.FindAssets(string.Empty, new string[] { USER_DATA_PATH });
		// from guid back to prefab, retrieve ToyView component
		for(int n = 0; n < allUserData.Length; n++) {
			var guid = allUserData[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			FileUtil.DeleteFileOrDirectory(toPath);
		}
	}

	static void ResetAllInventory() {
		var allSOFiles = AssetDatabase.FindAssets("t:ScriptableObject", new string[] { USER_INVENTORY_PATH });
		for(int n = 0; n < allSOFiles.Length; n++) {
			var guid = allSOFiles[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var toSO = AssetDatabase.LoadAssetAtPath(toPath, typeof(ScriptableObject)) as ScriptableObject;
			// check if it an UserDatabase
			if(toSO is IUserInventory) {
				var inventory = toSO as IUserInventory;
				inventory.ResetDataToDefault();
				//mark dirty
				EditorUtility.SetDirty(toSO);
			}
		}
	}
	#endregion//Private
}
#endif//UNITY_EDITOR