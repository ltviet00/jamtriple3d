#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Sirenix.OdinInspector;

/// <summary>
/// Have a lot of MeshRenderer, each mesh use a separate material with separate texture?
/// Want to create a single atlas for them all, and just one material? This's your savior.
/// 
/// How to use it?
/// Create an object on scene, attach this component to it.
/// </summary>
public class TextureBaker : MonoBehaviour {
	#region Config
	[Header("Objects have material to pack")]
	public GameObject[] objectsToBake;

	[Header("Atlas config")]
	public int padding = 2;
	public AtlasSize atlasSize = AtlasSize.max4096;
	public TextureFormat atlastTextureFormat;

	public int maxAtlasSize {
		get { return (int)atlasSize; }
	}

	[Header("Save location, name")]
	public Object savePath;
	public string saveName;

	public enum AtlasSize {
		max1024 = 1024,
		max2048 = 2024,
		max4096 = 4096,
		max8192 = 8192
	}

	[Button]
	void CreateAtlasAndMaterial() {
		CreateAtlasForMeshes(objectsToBake, maxAtlasSize, padding, savePath, saveName);
	}
	#endregion//Config


	#region Private
	void CreateAtlasForMeshes(GameObject[] roots, int atlasSize, int padding, Object savePath, string saveName) {
		// create the array of texture to bake
		var texturesList = CreateListOfTextureToBake(roots);
		//	make a copy before packing. Reason? read the function's summary
		var texturesToBake = MakeCopyOfTexturesToIgnoreReadWriteSetting(texturesList);
		// pack them up. save the list UV, will write it to the ScriptableObejct
		//var atlas = new Texture2D(atlasSize, atlasSize);//, TextureFormat.ARGB32, false);
		var atlas = new Texture2D(atlasSize, atlasSize, atlastTextureFormat, false);
		var UVs = atlas.PackTextures(texturesToBake, padding, atlasSize, false);
		// save the atlas, material and the config file
		var atlastOnDisk = SaveAtlasToDisk(atlas, savePath, saveName);
		var materialToClone = FindMaterialToClone(roots);
		var savedMaterial = CreateAndSaveMaterialToDisk(atlastOnDisk, materialToClone, savePath, saveName);
		// save to disk, we use the texturesList, cause they will reference to textures on disk,
		// dont use the teturesToBake cause they're texture we generated in memory
		CreateAtlasConfigFile(atlastOnDisk, texturesList, UVs, savedMaterial, savePath, saveName);
	}

	///<summary> Create a list of mainTexture of the input Mesh array, also remove duplicate ones </summary>
	Texture2D[] CreateListOfTextureToBake(GameObject[] roots) {
		var result = new List<Texture2D>();
		// find all material in child
		var materialsInChild = new List<Material>();
		for(int n = 0; n < roots.Length; n++) {
			var root = roots[n];
			// find both MeshRenderer and SkinnedMeshRenderer. Only get enabled components (not those renderer components was disabled)
			var meshRenders = root.GetComponentsInChildren<MeshRenderer>(false);
			var skinnedMeshRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(false);
			// add material to the sum list
			for(int m = 0; m < meshRenders.Length; m++) {
				materialsInChild.Add(meshRenders[m].sharedMaterial);
			}
			for(int m = 0; m < skinnedMeshRenderer.Length; m++) {
				materialsInChild.Add(skinnedMeshRenderer[m].sharedMaterial);
			}
		}
		// extract the texture, also remove the duplicate
		for(int n = 0; n < materialsInChild.Count; n++) {
			var meshTexture = materialsInChild[n].mainTexture;
			// check duplicate
			var isDuplicated = false;
			for(int m = 0; m < result.Count; m++) {
				if(meshTexture == result[m]) {
					isDuplicated = true;
					break;
				}
			}
			// no duplicate, then add to the list. Remember to cast
			if(!isDuplicated)
				result.Add((Texture2D)meshTexture);
		}
		return result.ToArray();
	}

	///<summary> We use Unity API Texture2D.PackTextures(), which only work for texture that have mark
	/// EnableRead/Write. BUT, one thing to remind. Enable Read/Write will double memory usage (1 on CPU, one on GPU), read here
	/// https://answers.unity.com/questions/1452944/does-enabling-readwrite-on-a-texture-double-its-me.html
	/// So, it better just make copy of them when working in Editor</summary>
	Texture2D[] MakeCopyOfTexturesToIgnoreReadWriteSetting(Texture2D[] texturesOnDisks) {
		var result = new Texture2D[texturesOnDisks.Length];
		for(int n = 0; n < texturesOnDisks.Length; n++) {
			var original = texturesOnDisks[n];
			// remember to keep the same width, heigh and format to the original, to ignore some error from Graphics.CopyTexture function
			var clone = new Texture2D(original.width, original.height, original.format, false);
			// have to specific the mipmap we want to copy, just pick the index 0
			Graphics.CopyTexture(original, 0, 0, clone, 0, 0);
			result[n] = clone;
		}
		return result;
	}

	///<summary> We use Unity API Texture2D.PackTextures(), which only work for texture that have mark
	///EnableRead/Write. So should enable them first before using this API.
	///NOTES : one thing to remind. Enable Read/Write will double memory, read here
	///https://answers.unity.com/questions/1452944/does-enabling-readwrite-on-a-texture-double-its-me.html
	///So after packing those textures, you should deactive R/W, or if you only use the packed assets (texture/mesh),
	///then the old texture won't be used anymore</summary>
	bool IsAllTexturesGoodBeforePacking(Texture2D[] textures) {
		var isAllGood = true;
		for(int n = 0; n < textures.Length; n++) {
			var texture = textures[n];
			if(!texture.isReadable) {
				Debug.LogWarningFormat(texture, "[{0}] should Enable Read/Write, cause the API Texture2D.PackTexture() need it", texture.name);
				isAllGood = false;
			}
		}
		return isAllGood;
	}

	Texture2D SaveAtlasToDisk(Texture2D atlas, Object directory, string name) {
		// check directory and create if it not exist
		var path = AssetDatabase.GetAssetPath(directory);
		// the above path will include that "Assets/" at last, we need to remove it to be used for Fie.WriteAllBytes
		var pathForFileWriter = path.Substring(path.IndexOf("/") + 1);
		//var directoryPath = string.Format("{0}/{1}", Application.dataPath, path);
		//if(!Directory.Exists(directoryPath)) {
		//	Directory.CreateDirectory(directoryPath);
		//}
		// with PNG, we have to encode to bytes[], then write to disk using File.
		byte[] encodeToPng = EncodeTextureToPngToSaveToDisk(atlas);
		var savePath = $"{Application.dataPath}/{pathForFileWriter}/{name}_Atlas.png";
		File.WriteAllBytes(savePath, encodeToPng);
		//var folderPath = AssetDatabase.GetAssetPath(directory);
		//var pathToSave = $"{folderPath}/{name}_Atlas.bmp";
		//AssetDatabase.CreateAsset(atlas, pathToSave);

		// and we have to re-import the file we just created
		var pathToImport = $"{path}/{name}_Atlas.png";
		AssetDatabase.ImportAsset(pathToImport);
		// reload it to return
		var textureOnDisk = AssetDatabase.LoadAssetAtPath<Texture2D>(pathToImport);
		return textureOnDisk;
	}

	///<summary> Simple pick the first one on the list we found </summary>
	Material FindMaterialToClone(GameObject[] roots) {
		// check if we're working on MeshRenderer first
		var allMeshRender = roots[0].GetComponentsInChildren<MeshRenderer>(true);
		if(allMeshRender.Length > 0) {
			return allMeshRender[0].sharedMaterial;
		}
		// so we working on Skinmeshrenderer
		var allSkinnedMesh = roots[0].GetComponentsInChildren<SkinnedMeshRenderer>(true);
		if(allSkinnedMesh.Length > 0) {
			return allSkinnedMesh[0].sharedMaterial;
		}
		// oh, not found anything? then fallback to default
		Debug.LogWarning($"Not found shader, fallback to Standard");
		return new Material(Shader.Find("Standard"));
	}

	Material CreateAndSaveMaterialToDisk(Texture2D atlas, Material originalMaterial, Object directory, string saveName) {
		var savePath = AssetDatabase.GetAssetPath(directory);
		//var materialSavePath = string.Format("Assets/{0}/{1}_Material.mat", savePath, saveName);
		var materialSavePath = $"{savePath}/{saveName}_Material.mat";
		var saveMaterial = new Material(originalMaterial);
		saveMaterial.mainTexture = atlas;
		AssetDatabase.CreateAsset(saveMaterial, materialSavePath);
		return saveMaterial;
	}

	void CreateAtlasConfigFile(Texture2D atlas, Texture2D[] originalTextures, Rect[] UVs, Material material, Object directory, string saveName) {
		// create and save the file
		var cfg = (AtlasBakedResult)ScriptableObject.CreateInstance(typeof(AtlasBakedResult));
		// config it
		cfg.atlas = atlas;
		cfg.material = material;
		cfg.FillUVMap(originalTextures, UVs);
		// save to disk
		//var pathToSave = string.Format("Assets/{0}/{1}_AtlasConfig.asset", directory, saveName);
		var pathToSave = $"{AssetDatabase.GetAssetPath(directory)}/{saveName}_AtlasConfig.asset";
		AssetDatabase.CreateAsset(cfg, pathToSave);
	}

	///<summary> The compress format DXT1/DXT5 cannot be use with Texture2D.EncodeToPNG, so we have to use some
	///way to encode it to a byte[]</summary>
	byte[] EncodeTextureToPngToSaveToDisk(Texture2D texture) {
		// check if it have the compress format
		//if(!IsTextureInCompressFormat(texture.format)) {
		//	//return texture.EncodeToPNG();
		//	return ImageConversion.EncodeToPNG(texture);
		//}
		//else {
			/// Read post here to know how to deal with writing compressed Texture2D to disk
			/// https://stackoverflow.com/questions/51315918/how-to-encodetopng-compressed-textures-in-unity
			//save here to restore later
			var previousRenderTexture = RenderTexture.active;
			// copy to a render texture
			var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(texture, renderTexture);
			RenderTexture.active = renderTexture;
			// try read it back to a temp texture
			var tempText = new Texture2D(texture.width, texture.height);//, TextureFormat.ARGB32, false);
			tempText.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
			tempText.Apply();
			// finally, Encode that TempTemptexture
			var result = ImageConversion.EncodeToPNG(tempText);
			// release everything
			RenderTexture.ReleaseTemporary(renderTexture);
			//restore the rendertexture
			RenderTexture.active = previousRenderTexture;
			// return
			return result;
		//}
	}

	bool IsTextureInCompressFormat(TextureFormat format) {
		for(int n = 0; n < COMPRESS_TEXTURE_FORMAT_LIST.Length; n++) {
			if(format == COMPRESS_TEXTURE_FORMAT_LIST[n])
				return true;
		}
		return false;
	}

	static TextureFormat[] COMPRESS_TEXTURE_FORMAT_LIST = {
		TextureFormat.DXT1,
		TextureFormat.DXT5
	};
	#endregion//Private
}
#endif//UNITY_EDITOR