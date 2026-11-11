using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(SubMeshCombiner))]
public class SubMeshCombinerEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		var script = target as SubMeshCombiner;

		if(GUILayout.Button("Combine submesh for selected root")) {
			Debug.Log("--- Start update UV ---");
			var roots = script.roots;
			for(int n = 0; n < roots.Length; n++) {
				var root = roots[n];
				var allMeshes = FindAllMeshRenderer(root);
				// validate first
				if(!IsAllMeshRenderUseTheSameMaterial(allMeshes)) {
					Debug.LogWarningFormat(root, "Not all MeshRenderer of [{0}] use the same material, ignore");
					continue;
				}
				// okay, process
				var combinedMesh = CombinesMeshes(allMeshes);
				ExportGeneratedMesh(combinedMesh, script.exportPath, root.name);
				var commonMaterial = allMeshes[0].renderer.sharedMaterial;
				CreateCloneObjectUseTheExportModel(string.Format("{0}_Combined", root.name), commonMaterial, combinedMesh);
			}
			// validate first
			Debug.Log("--- Finished update UV ---");
		}
	}

	bool IsAllMeshRenderUseTheSameMaterial(List<MeshRenderAndModel> meshes) {
		var firstMaterial = meshes[0].renderer.sharedMaterial;
		for(int n = 1; n < meshes.Count; n++) {
			if(meshes[n].renderer.sharedMaterial != firstMaterial)
				return false;
		}
		return true;
	}

	List<MeshRenderAndModel> FindAllMeshRenderer(GameObject objectToFind) {
		var result = new List<MeshRenderAndModel>();
		var allMeshRender = objectToFind.GetComponentsInChildren<MeshRenderer>();
		for(int m = 0; m < allMeshRender.Length; m++) {
			var meshRender = allMeshRender[m];
			var meshFilter = meshRender.GetComponent<MeshFilter>();
			// have MeshRenderer but not MeshFilter? then ignore this
			if(meshFilter == null) {
				Debug.LogFormat(meshRender, "MeshRenderer [{0}] dont have MeshFilter, ignore baking", meshRender.name);
				continue;
			}
			result.Add(new MeshRenderAndModel(meshRender, meshFilter));
		}
		return result;
	}

	///<summary> Combine and return the Combined Mesh </summary>
	Mesh CombinesMeshes(List<MeshRenderAndModel> meshes) {
		var result = new Mesh();
		// create the CombineInstance[] array for combining mesh
		var combineInstances = new CombineInstance[meshes.Count];
		for(int n = 0; n < meshes.Count; n++) {
			var instance = new CombineInstance();
			instance.mesh = meshes[n].model.sharedMesh;
			instance.transform = meshes[n].renderer.transform.localToWorldMatrix;
			combineInstances[n] = instance;
		}
		// create mesh and return
		result.CombineMeshes(combineInstances);
		return result;
	}

	void ExportGeneratedMesh(Mesh mesh, string path, string rootObjectName) {
		// check export path
		var directoryPath = string.Format("{0}/{1}", Application.dataPath, path);
		if(!Directory.Exists(directoryPath)) {
			Directory.CreateDirectory(directoryPath);
		}
		// save model
		AssetDatabase.CreateAsset(mesh, string.Format("Assets/{0}/{1}_Combined.asset", path, rootObjectName));
	}

	void CreateCloneObjectUseTheExportModel(string name, Material material, Mesh mesh) {
		var clone = new GameObject(name);
		var meshFilter = clone.AddComponent<MeshFilter>();
		var meshRender = clone.AddComponent<MeshRenderer>();
		// config. NOTES : alway use sharedMesh and sharedMaterial
		meshFilter.sharedMesh = mesh;
		meshRender.sharedMaterial = material;
	}

	///<summary> Simple store the MeshRenderer and MeshFilter </summary>
	public struct MeshRenderAndModel {
		public MeshRenderer renderer;
		public MeshFilter model;

		public MeshRenderAndModel(MeshRenderer render, MeshFilter model) {
			this.renderer = render;
			this.model = model;
		}
	}
}
