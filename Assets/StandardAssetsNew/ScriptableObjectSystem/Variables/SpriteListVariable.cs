using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEditor.U2D;
#endif//UNITY_EDITOR

[CreateAssetMenu(menuName = "SO architect/SpriteListVariable")]
public class SpriteListVariable : ClassListVariable<Sprite> {
#if UNITY_EDITOR
	[Button("Load all Sprite in same folder")]
	void Editor_LoadAllSyncKeyInSameFolder() {
		//if(isFailedConfig)
		//	return;
		_list.Clear();
		// get the current directory path to retrieve sub folders
		var root = AssetDatabase.GetAssetPath(this);
		var folderPath = root.Substring(0, root.LastIndexOf("/"));
		var allSyncKey = AssetDatabase.FindAssets("t:Sprite", new string[] { folderPath });

		for(int n = 0; n < allSyncKey.Length; n++) {
			var guid = allSyncKey[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var toSprite = AssetDatabase.LoadAssetAtPath<Sprite>(toPath);
			if(toSprite == null) {
				Log.Error(false, this, $"Cant load Sprite at path [{toPath}]");
				continue;
			}
			// save theme
			_list.Add(toSprite);
		}
		// finish, let asset refresh
		AssetDatabase.Refresh();
		// let it dirty to be saved
		UnityEditor.EditorUtility.SetDirty(this);
		Log.Info($"------ Loaded [{_list.Count}] skin in subfolders at Path: {folderPath}");
	}
#endif//UNITY_EDITOR
}
