using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace Ogxd.ProjectCurator {
	/// <summary>
	/// Main class, Store the map of all asset , receive calls from AssetProcessor when project asset changed (move, create, delete ..) to update database.
	/// All function and field are static.
	/// </summary>
	public static class ProjectCurator {
		///<summary> Main map, key path , value = assetInfo </summary>
		[NonSerialized]
		private static Dictionary<string, AssetInfo> pathToAssetMap;

		static ProjectCurator() {
			pathToAssetMap = new Dictionary<string, AssetInfo>();
			var assetInfos = ProjectCuratorData.AssetInfos;
			for(int i = 0; i < assetInfos.Length; i++) {
				pathToAssetMap.Add(assetInfos[i].path, assetInfos[i]);
			}
		}

		public static AssetInfo GetAsset(string path) {
			AssetInfo assetInfo = null;
			pathToAssetMap.TryGetValue(path, out assetInfo);
			return assetInfo;
		}

		public static void AddAssetToDatabase(string assetPath) {
			AssetInfo assetInfo;
			// check exist first. if not, add new one
			if(!pathToAssetMap.TryGetValue(assetPath, out assetInfo)) {
				pathToAssetMap.Add(assetPath, assetInfo = new AssetInfo(assetPath));
			}
			// get dependencies of this asset
			var dependencyPathList = assetInfo.GetDependencies();
			foreach(string path in dependencyPathList) {
				// the asset also dependen itself, so ignore this case
				if(path == assetInfo.path)
					continue;
				if(pathToAssetMap.TryGetValue(path, out AssetInfo dependencyInfo)) {
					// update the dependencies (hashset) of the current assetinfo
					assetInfo.dependencies.Add(path);
					// and also add it as referencer to the target dependencer
					dependencyInfo.referencers.Add(assetInfo.path);
					// Included status may have changed and need to be recomputed
					dependencyInfo.ClearIncludedStatus();
				}
			}
		}

		public static void RemoveAssetFromDatabase(string asset) {
			if(pathToAssetMap.TryGetValue(asset, out AssetInfo assetInfo)) {
				// update info for Assets that reference to this one (referencers)
				foreach(string referencer in assetInfo.referencers) {
					if(pathToAssetMap.TryGetValue(referencer, out AssetInfo referencerAssetInfo)) {
						if(referencerAssetInfo.dependencies.Remove(asset)) {
							referencerAssetInfo.ClearIncludedStatus();
						}
						else {
							// Non-Reciprocity Error
							Debug.LogWarning($"Asset '{referencer}' that depends on '{asset}' doesn't have it as a dependency");
						}
					}
					else {
						Debug.LogWarning($"Asset '{referencer}' that depends on '{asset}' is not present in the database");
					}
				}
				// Update info for Dependenciy of this asset as well
				foreach(string dependency in assetInfo.dependencies) {
					if(pathToAssetMap.TryGetValue(dependency, out AssetInfo dependencyAssetInfo)) {
						if(dependencyAssetInfo.referencers.Remove(asset)) {
							dependencyAssetInfo.ClearIncludedStatus();
						}
						else {
							// Non-Reciprocity Error
							Debug.LogWarning($"Asset '{dependency}' that is referenced by '{asset}' doesn't have it as a referencer");
						}
					}
					else {
						Debug.LogWarning($"Asset '{dependency}' that is referenced by '{asset}' is not present in the database");
					}
				}
				pathToAssetMap.Remove(asset);
			}
			else {
				Debug.LogWarning($"Asset '{asset}' is not present in the database");
			}
		}

		public static void ClearDatabase() {
			pathToAssetMap.Clear();
		}

		public static void RebuildDatabase() {
			pathToAssetMap = new Dictionary<string, AssetInfo>();
			var allAssetPaths = AssetDatabase.GetAllAssetPaths();

			// Ignore non-assets (package folder for instance) and directories
			allAssetPaths = allAssetPaths.Where(x => x.StartsWith("Assets/") && !Directory.Exists(x)).ToArray();

			EditorUtility.DisplayProgressBar("Building Dependency Database", "Gathering All Assets...", 0f);
			// Gather all assets
			for(int p = 0; p < allAssetPaths.Length; p++) {
				AssetInfo assetInfo = new AssetInfo(allAssetPaths[p]);
				pathToAssetMap.Add(assetInfo.path, assetInfo);
			}

			// Find links between assets
			for(int n = 0; n < allAssetPaths.Length; n++) {
				// display cancel button on progress bar
				if(n % 10 == 0) {
					var cancel = EditorUtility.DisplayCancelableProgressBar("Building Dependency Database", allAssetPaths[n], (float)n / allAssetPaths.Length);
					if(cancel) {
						pathToAssetMap = null;
						break;
					}
				}
				AddAssetToDatabase(allAssetPaths[n]);
			}
			// done, clean and save to database
			EditorUtility.ClearProgressBar();
			ProjectCuratorData.IsUpToDate = true;
			SaveDatabaseToDisk();
		}

		public static void SaveDatabaseToDisk() {
			if(pathToAssetMap == null)
				return;
			var assetInfos = new AssetInfo[pathToAssetMap.Count];
			int i = 0;
			foreach(var pair in pathToAssetMap) {
				assetInfos[i] = pair.Value;
				i++;
			}
			ProjectCuratorData.AssetInfos = assetInfos;
			ProjectCuratorData.Save();
		}
	}
}