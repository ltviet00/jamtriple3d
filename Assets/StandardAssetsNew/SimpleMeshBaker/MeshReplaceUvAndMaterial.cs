#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Sirenix.OdinInspector;

/// <summary>
/// The same with THB_SkinnedMeshReplaceUvAndMaterial, but working on MeshRenderer instead of SkinnedMeshRenderer.
/// SkinnedMesh have the Mesh filed in it, but the MeshRenderer isn't, instead it have a separate MeshFilter component
/// to store the mesh.
/// </summary>
public class MeshReplaceUvAndMaterial : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	public AtlasBakedResult atlasConfig;
	public GameObject[] objectsToUpdate;
	public Object exportDirectory;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Private
	[Button("Bake mesh")]
	void OnInspectorGUI() {
		if(isFailedConfig)
			return;
		Debug.Log("--- Start update UV ---");
		// okay, run now. Clone the original objects, and modify on clone ones, not the originals
		var clones = CloneOriginalObjects(objectsToUpdate);
		for(int n = 0; n < clones.Count; n++) {
			var cloneName = clones[n].name;
			var meshes = FindAllMeshRenderer(clones[n]);
			// update UV for each Mesh
			for(int m = 0; m < meshes.Count; m++) {
				UpdateUVForMeshAndExportModel(atlasConfig, meshes[m], exportDirectory, cloneName);
			}
		}
		Debug.Log("--- Finished update UV ---");
	}

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

	void UpdateUVForMeshAndExportModel(AtlasBakedResult atlasConfig, MeshRenderAndModel meshRender, Object exportPath, string rootObjectName) {
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

	void ExportGeneratedMesh(Mesh mesh, Object directory, string rootObjectName) {
		// check export path
		//var directoryPath = string.Format("{0}/{1}/{2}", Application.dataPath, directory, rootObjectName);
		var folderPath = $"{AssetDatabase.GetAssetPath(directory)}/{rootObjectName}";
		if(!AssetDatabase.IsValidFolder(folderPath)) {
			AssetDatabase.CreateFolder(AssetDatabase.GetAssetPath(directory), rootObjectName);
		}
		//if(!Directory.Exists(directoryPath)) {
		//	Directory.CreateDirectory(directoryPath);
		//}
		// save model
		var savePath = $"{folderPath}/{mesh.name}_CloneMesh.asset";
		AssetDatabase.CreateAsset(mesh, savePath);
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
	#endregion//Private
}
#endif//UNITY_EDITOR