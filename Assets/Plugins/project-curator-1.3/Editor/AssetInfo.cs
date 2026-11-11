using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ogxd.ProjectCurator {
	public enum IncludedInBuild {
		Unknown = 0,
		// Not included in build
		NotIncludable = 1,
		NotIncluded = 2,
		// Included in build
		SceneInBuild = 10,
		RuntimeScript = 11,
		ResourceAsset = 12,
		Referenced = 13,
	}

	/// <summary>
	/// Store information for an asset: referencers, dependencies. Will be convert to JSON to save to disk (read ProjectCuratorData), so mark this as Serializable
	/// </summary>
	[Serializable]
	public class AssetInfo : ISerializationCallbackReceiver {
		///<summary> List of path of Assets that reference to me. Use for runtime only, so NonSerialized </summary>
		[NonSerialized] public HashSet<string> referencers = new HashSet<string>();
		///<summary> List of path of Me reference to these assets. Use for runtime only, so NonSerialized </summary>
		[NonSerialized] public HashSet<string> dependencies = new HashSet<string>();

		///<summary> List of path of Assets that reference to me. Will be converted from referencers hashset to string[] to be saved to disk, so mark serializable </summary>
		[SerializeField] string[] _references;
		///<summary> List of path of Me reference to these assets. Will be converted from referencers hashset to string[] to be saved to disk, so mark serializable </summary>
		[SerializeField] string[] _dependencies;


		#region Interface - ISerializationCallbackReceiver
		public void OnBeforeSerialize() {
			// Turn hashets to string[] to save to disk
			_references = referencers.ToArray();
			_dependencies = dependencies.ToArray();
		}

		public void OnAfterDeserialize() {
			// Turn string[] back to hashet to be used in runtime
			referencers = new HashSet<string>(_references ?? new string[0]);
			dependencies = new HashSet<string>(_dependencies ?? new string[0]);
		}
		#endregion//Interface - ISerializationCallbackReceiver


		[SerializeField]
		public string path;

		public AssetInfo(string path) {
			this.path = path;
		}

		///<summary> MAIN FEATURE IS HERE !!! Wrapper func, call to AssetDatabase.GetDependencies() to get all dependencies of this asset </summary>
		public string[] GetDependencies() {
			return AssetDatabase.GetDependencies(path);
		}

		public void ClearIncludedStatus() {
			includedStatus = IncludedInBuild.Unknown;
		}

		[NonSerialized]
		private IncludedInBuild includedStatus;

		public IncludedInBuild IncludedStatus {
			get {
				if(includedStatus != IncludedInBuild.Unknown)
					return includedStatus;
				// Avoid circular loops
				includedStatus = IncludedInBuild.NotIncluded;
				return includedStatus = CheckIncludedStatus();
			}
		}

		public bool IsIncludedInBuild => (int)IncludedStatus >= 10;

		private IncludedInBuild CheckIncludedStatus() {
			foreach(var referencer in referencers) {
				AssetInfo refInfo = ProjectCurator.GetAsset(referencer);
				if(refInfo.IsIncludedInBuild) {
					return IncludedInBuild.Referenced;
				}
			}

			bool isInEditor = false;
			string[] directories = path.ToLower().Split('/');

			for(int i = 0; i < directories.Length - 1; i++) {
				switch(directories[i]) {
				case "editor":
					isInEditor = true;
					break;
				case "resources":
					return IncludedInBuild.ResourceAsset;
				case "plugins":
					break;
				default:
					break;
				}
			}

			string extension = System.IO.Path.GetExtension(path);
			switch(extension) {
			case ".cs":
				if(isInEditor) {
					return IncludedInBuild.NotIncludable;
				}
				else {
					return IncludedInBuild.RuntimeScript;
				}
			case ".unity":
				if(EditorBuildSettings.scenes.Select(x => x.path).Contains(path))
					return IncludedInBuild.SceneInBuild;
				break;
			// Todo : Handle DLL
			// https://docs.unity3d.com/ScriptReference/Compilation.Assembly-compiledAssemblyReferences.html
			// CompilationPipeline
			// Assembly Definition
			default:
				break;
			}

			return IncludedInBuild.NotIncluded;
		}
	}
}