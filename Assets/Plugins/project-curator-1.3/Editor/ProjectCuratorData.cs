using System;
using System.IO;
using UnityEngine;

namespace Ogxd.ProjectCurator {
	/// <summary>
	/// Load/Save data to json file, in ProjectSettings folder
	/// </summary>
	[Serializable]
	public class ProjectCuratorData {
		///<summary> Json data file will be save to this path </summary>
		private const string JSON_SAVE_PATH = "ProjectSettings/ProjectCuratorSettings.json";

		[SerializeField] bool isUpToDate = false;
		public static bool IsUpToDate { get { return Instance.isUpToDate; } set { Instance.isUpToDate = value; } }

		[SerializeField] AssetInfo[] assetInfos;
		public static AssetInfo[] AssetInfos {
			get => Instance.assetInfos ?? (Instance.assetInfos = new AssetInfo[0]);
			set => Instance.assetInfos = value;
		}

		///<summary> Singleton pattern </summary>
		private static ProjectCuratorData instance;
		///<summary> Singleton pattern </summary>
		public static ProjectCuratorData Instance {
			get {
				if(instance == null) {
					if(File.Exists(JSON_SAVE_PATH)) {
						instance = JsonUtility.FromJson<ProjectCuratorData>(File.ReadAllText(JSON_SAVE_PATH));
					}
					else {
						instance = new ProjectCuratorData();
						File.WriteAllText(JSON_SAVE_PATH, JsonUtility.ToJson(instance));
					}
				}
				return instance;
			}
		}

		public static void Save() {
			File.WriteAllText(JSON_SAVE_PATH, JsonUtility.ToJson(Instance));
		}
	}
}