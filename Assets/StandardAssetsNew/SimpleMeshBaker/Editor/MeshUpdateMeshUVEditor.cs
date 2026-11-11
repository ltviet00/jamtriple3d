/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(MeshReplaceUvAndMaterial))]
public class MeshUpdateMeshUVEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		var script = target as MeshReplaceUvAndMaterial;

		if(GUILayout.Button("Update UV map for selected meshes")) {
			Debug.Log("--- Start update UV ---");
			// validate first
			if(script.IsFailedConfig())
				return;
			// okay, run now. Clone the original objects, and modify on clone ones, not the originals
			var atlas = script.atlasConfig;
			var clones = CloneOriginalObjects(script.objectsToUpdate);
			for(int n = 0; n < clones.Count; n++) {
				var cloneName = clones[n].name;
				var meshes = FindAllMeshRenderer(clones[n]);
				// update UV for each Mesh
				for(int m = 0; m < meshes.Count; m++) {
					UpdateUVForMeshAndExportModel(atlas, meshes[m], script.exportPath, cloneName);
				}
			}
			Debug.Log("--- Finished update UV ---");
		}
	}

	///<summary>  </summary>
	List<GameObject> CloneOriginalObjects(GameObject[] originals) {
		var result = new List<GameObject>(originals.Length);
		for(int n = 0; n < originals.Length; n++) {
			var clone = Instantiate(originals[n]);
			result.Add(clone);
		}
		return result;
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

	void UpdateUVForMeshAndExportModel(AtlasBakedResult atlasConfig, MeshRenderAndModel meshRender, string exportPath, string rootObjectName) {
		// check if this mesh used the texture that was pack into the atlas.
		// NOTES: always use sharedMaterial. If not it will fire an error message about clone material when call mesh.materal in Editor
		var texture = (Texture2D)meshRender.renderer.sharedMaterial.mainTexture;
		if(!atlasConfig.ContainTexture(texture)) {
			Debug.LogWarning(string.Format("The texture [{0}] not contain in the atlas [{1}]", texture.name, atlasConfig.name), atlasConfig);
			return;
		}
		// ok, clone the mesh first. NOTES. again, alway use shareMesh
		var originalMesh = meshRender.model.sharedMesh;
		var updatedMesh = CloneMesh(originalMesh);
		// then update the uv for the mesh
		var rect = atlasConfig.GetRectForSubTexture(texture);
		UpdateMeshUVFollowNewRect(updatedMesh, rect);
		// reassign back to the mesh
		meshRender.model.sharedMesh = updatedMesh;
		// also update the material
		meshRender.renderer.sharedMaterial = atlasConfig.material;
		ExportGeneratedMesh(updatedMesh, exportPath, rootObjectName);
	}

	Mesh CloneMesh(Mesh original) {
		/// NOTES !!!! it took me hours to figure it out. 
		/// You alway have to use Instantiate to clone a Mesh, never do like this :
		/// Mesh clone = new Mesh(), then clone.vertices = original.vertices ...
		/// That clone way is alway broke the mesh, and the SkinnedMeshRender's animation wont work any more
		/// Read the post here : https://forum.unity.com/threads/manually-updating-a-skinned-mesh.67047/
		/// A comment from Unity member ricardo_arango
		var result = (Mesh)Instantiate(original);
		return result;
	}

	void UpdateMeshUVFollowNewRect(Mesh mesh, Rect rect) {
		var originalUvs = mesh.uv;
		var updateUVs = new Vector2[mesh.uv.Length];
		for(int n = 0; n < updateUVs.Length; n++) {
			/// copy and modify the original
			/// Remember this recipe to convert old UV to newUV.
			/// newU = rect.X + oldU * (rect.w / oldRect.w); // oldRect.w = 1
			/// newV = rect.y + oldV * (rect.h / oldRect.h); // oldRect.h = 1
			var uv = originalUvs[n];
			uv.x = rect.x + uv.x * rect.width;// should be * rect.width/atlasWidth, but atlasWidth = 1
			uv.y = rect.y + uv.y * rect.height;// should be * rect.width/atlasHeigh, but atlasHeigh = 1
			updateUVs[n] = uv;
		}
		// dont forget to reassign
		mesh.uv = updateUVs;
	}

	void ExportGeneratedMesh(Mesh mesh, string path, string rootObjectName) {
		// check export path
		var directoryPath = string.Format("{0}/{1}/{2}", Application.dataPath, path, rootObjectName);
		if(!Directory.Exists(directoryPath)) {
			Directory.CreateDirectory(directoryPath);
		}
		// save model
		AssetDatabase.CreateAsset(mesh, string.Format("Assets/{0}/{1}/{2}_CloneMesh.asset", path, rootObjectName, mesh.name));
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
*/