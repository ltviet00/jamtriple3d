using UnityEditor;
using System.Collections.Generic;
using System;

namespace Ogxd.ProjectCurator {
	/// <summary>
	/// The purpose of this class is to try detecting asset changes to automatically update the ProjectCurator database.
	/// Read more about support static functions here https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.html
	/// </summary>
	public class AssetProcessor : UnityEditor.AssetModificationProcessor {
		[InitializeOnLoadMethod]
		public static void Init() {
			EditorApplication.update += OnUpdate;
		}

		/// <summary>
		/// Some callbacks must be delayed on next frame
		/// </summary>
		private static void OnUpdate() {
			while(Actions.Count > 0) {
				Actions.Dequeue()?.Invoke();
			}
		}

		///<summary> Store all action relate to asset, will be process in Update, one per (editor) frame  </summary>
		private static Queue<Action> Actions = new Queue<Action>();

		/////<summary> https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.OnWillSaveAssets.html </summary>
		//static string[] OnWillSaveAssets(string[] paths) {
		//	if(ProjectCuratorData.IsUpToDate) {
		//		Actions.Enqueue(() => {
		//			foreach(string path in paths) {
		//				ProjectCurator.RemoveAssetFromDatabase(path);
		//				ProjectCurator.AddAssetToDatabase(path);
		//			}
		//			ProjectCurator.SaveDatabaseToDisk();
		//		});
		//	}
		//	return paths;
		//}

		/////<summary> https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.OnWillCreateAsset.html </summary>
		//static void OnWillCreateAsset(string assetName) {
		//	if(ProjectCuratorData.IsUpToDate) {
		//		Actions.Enqueue(() => {
		//			ProjectCurator.AddAssetToDatabase(assetName);
		//			ProjectCurator.SaveDatabaseToDisk();
		//		});
		//	}
		//}

		/////<summary> https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.OnWillDeleteAsset.html </summary>
		//static AssetDeleteResult OnWillDeleteAsset(string assetName, RemoveAssetOptions removeAssetOptions) {
		//	if(ProjectCuratorData.IsUpToDate) {
		//		ProjectCurator.RemoveAssetFromDatabase(assetName);
		//		ProjectCurator.SaveDatabaseToDisk();
		//	}
		//	// we won't touch asset, just update database
		//	return AssetDeleteResult.DidNotDelete;
		//}

		/////<summary> https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.OnWillMoveAsset.html </summary>
		//static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath) {
		//	if(ProjectCuratorData.IsUpToDate) {
		//		Actions.Enqueue(() => {
		//			ProjectCurator.RemoveAssetFromDatabase(sourcePath);
		//			ProjectCurator.AddAssetToDatabase(destinationPath);
		//			ProjectCurator.SaveDatabaseToDisk();
		//		});
		//	}
		//	// we won't touch asset, just update database
		//	return AssetMoveResult.DidNotMove;
		//}
	}
}