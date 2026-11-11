#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ConvertAutodeskMaterialToURP : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] string urpSimpleListShaderPath;
	[SerializeField] Object materialFolderPath;
	[SerializeField] Object textureFolderPath;
	[SerializeField] CopyTexture[] copyTextureList;
	[SerializeField] CopyTexture[] tempMapTexureNameToFileNameSuffix;

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
	[Button]
	void ConvertMaterial() {
		Log.Info("---- Start changing shader -----");
		var urpShader = Shader.Find(urpSimpleListShaderPath);
		if(urpShader == null) {
			Log.Error(false, this, $"Not found shader [{urpSimpleListShaderPath}]");
			return;
		}
		var materialList = GetMaterialListInFolder();
		for(int n = 0; n < materialList.Count; n++) {
			var mat = materialList[n];
			ProcessMaterial(mat, urpShader);
		}
		Log.Info("---- Done changing shader -----");
	}

	List<Material> GetMaterialListInFolder() {
		var result = new List<Material>();
		var path = AssetDatabase.GetAssetPath(materialFolderPath);
		var udidList = AssetDatabase.FindAssets("t:Material", new string[] { path });

		for(int n = 0; n < udidList.Length; n++) {
			var guid = udidList[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var toMaterial = AssetDatabase.LoadAssetAtPath<Material>(toPath);
			result.Add(toMaterial);
		}
		return result;
	}

	void ProcessMaterial(Material material, Shader newShader) {
		for(int n = 0; n < copyTextureList.Length; n++) {
			var copyName = copyTextureList[n];
			var fromText = material.GetTexture(copyName.sourceName);
			// if it null, try get texture at desire path
			if(fromText == null) {
				fromText = TryGetTargetTextureInFolder(copyName.sourceName, material.name);
				// still null ??
				if(fromText == null) {
					Log.Warning(false, material, $"Not found texture [{copyName.sourceName}] of [{material.name}] nor Texture in texturePath");
					continue;
				}
			}
			// copy texture to new "name", also reset old texture
			material.shader = newShader;
			material.SetTexture(copyName.targetName, fromText);
			material.SetTexture(copyName.sourceName, null);
			// dirty so it can be saved
			EditorUtility.SetDirty(material);
		}
	}

	///<summary> Thanks god, the creator use material's name as prefix for texture, so we can find texture by name </summary>
	Texture TryGetTargetTextureInFolder(string shaderTextureName, string materialName) {
		var targetTextureSuffix = FindTargetNameForSource(tempMapTexureNameToFileNameSuffix, shaderTextureName);
		if(targetTextureSuffix == null) {
			Log.Warning(false, this, $"Not setup texure suffix for shaderVar [{shaderTextureName}]");
			return null;
		}
		// assume all texture are png
		const string TEXTURE_EXTENSION = "png";
		var folderPath = AssetDatabase.GetAssetPath(textureFolderPath);
		var texturePath = $"{folderPath}/{materialName}{targetTextureSuffix}.{TEXTURE_EXTENSION}";
		var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
		if(texture == null) {
			Log.Warning(false, this, $"Not found texture for path [{texturePath}]");
		}
		return texture;
	}

	string FindTargetNameForSource(CopyTexture[] list, string source) {
		for(int n = 0; n < list.Length; n++) {
			var item = list[n];
			if(item.sourceName == source)
				return item.targetName;
		}
		return null;
	}
	#endregion//Private


	#region Classes
	///<summary> Coy texture from inside material to other, use sourceName and targetName </summary>
	[System.Serializable]
	public class CopyTexture {
		public string sourceName;
		public string targetName;
	}
	#endregion//Classes
}
#endif//UNITY_EDITOR