#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
#endif//UNITY_EDITO

/// <summary>
/// Use in editor to trigger render for all toy
/// </summary>
#if UNITY_EDITOR
public class ToyThumbnailRenderer : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] ToyStorage toyStorage;
	[SerializeField] ToyThumbnailStorage thumbnailStorage;
	[SerializeField] Object targetFolder;
	[SerializeField] Camera snapshotCamera;
	[SerializeField] Transform rootToySnapshot;

	[Header("Config")]
	[SerializeField] Color outlineColor;
	[SerializeField] float outlineThick;
	[SerializeField] Color backgroundColor;
	[SerializeField] int thumbnailSize = 128;
	[SerializeField] float cameraDistance = 2.5f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Private
	Vector3 _defaultCameraPosition;
	[Button]
	void RenderThumbnailAndSaveToStorage() {
		_defaultCameraPosition = snapshotCamera.transform.position;
		// clear before render
		thumbnailStorage.thumbnailMap.Clear();
		for(int n = 0; n < toyStorage.AllToyGroup.Count; n++) {
			RenderForToyGroup(toyStorage.AllToyGroup[n]);
		}
		// set dirty
		UnityEditor.EditorUtility.SetDirty(thumbnailStorage);
		// refresh to let it reload thumbnail textures
		AssetDatabase.Refresh();
	}

	void RenderForToyGroup(ToyPrefabGroup group) {
		var thumnailMap = thumbnailStorage.thumbnailMap;
		var toys = group.toyList;
		for(int n = 0; n < toys.Count; n++) {
			var toy = toys[n];
			if(thumnailMap.ContainsKey(toy.rootPrefab)) {
				Log.Warning(false, this, $"Exist thumbnail for prefab [{toy.rootPrefab}]");
				continue;
			}
			// instantiate to take snapshot (destroy later)
			var instance = Instantiate(toy.rootPrefab, rootToySnapshot);
			instance.transform.localPosition = Vector3.zero;
			//AddOutlineToSubject(instance);

			// snapshot and save to target folder
			var snapshot = TakeSnapshot(instance);
			SaveSnapshotTexture(snapshot, toy.rootPrefab.name);

			// take sprite. Load the sprite at same path. we set spriteType = single
			var sprite = AssetDatabase.LoadAssetAtPath(PathForSpriteLoadingOrImport(toy.rootPrefab.name), typeof(Sprite)) as Sprite;
			thumnailMap.Add(toy.rootPrefab, sprite);

			// destroy the instance at last
			DestroyImmediate(instance);

			// TEMP: just create 1 texture for testing only
			//return;
		}
	}
	#endregion//Private


	#region Render async
	[Button]
	void RenderThumbnailWithOutlineAsync() {
		if(!EditorApplication.isPlaying) {
			Log.Warning(false, this, $"This function work when playing only. Play editor first");
			return;
			//EditorApplication.isPlaying = true;
		}
		_defaultCameraPosition = snapshotCamera.transform.position;
		// clear before render
		thumbnailStorage.thumbnailMap.Clear();
		RenderForToyGroupAsync().Forget();
	}

	async UniTask RenderForToyGroupAsync() {
		for(int n = 0; n < toyStorage.AllToyGroup.Count; n++) {
			var group = toyStorage.AllToyGroup[n];
			var thumnailMap = thumbnailStorage.thumbnailMap;
			var toys = group.toyList;

			for(int m = 0; m < toys.Count; m++) {
				var toy = toys[m];
				if(thumnailMap.ContainsKey(toy.rootPrefab)) {
					Log.Warning(false, this, $"Exist thumbnail for prefab [{toy.rootPrefab}]");
					continue;
				}
				// instantiate to take snapshot (destroy later)
				var instance = Instantiate(toy.rootPrefab, rootToySnapshot);
				instance.transform.localPosition = Vector3.zero;
				// disable rigidbody, cause we tend to delay taking snapshot, rigidbody will let toy falling
				var rigid = instance.GetComponent<Rigidbody>();
				rigid.isKinematic = true;
				//instance.SetActive(false);

				await UniTask.DelayFrame(2);
				//await UniTask.Delay(500);
				AddOutlineToSubject(instance);
				//await UniTask.Delay(500);
				await UniTask.DelayFrame(2);
				//instance.SetActive(true);
				//await UniTask.DelayFrame(2);

				// snapshot and save to target folder
				var snapshot = TakeSnapshot(instance);
				SaveSnapshotTexture(snapshot, toy.rootPrefab.name);

				// take sprite. Load the sprite at same path. we set spriteType = single
				var sprite = AssetDatabase.LoadAssetAtPath(PathForSpriteLoadingOrImport(toy.rootPrefab.name), typeof(Sprite)) as Sprite;
				thumnailMap.Add(toy.rootPrefab, sprite);

				// destroy the instance at last
				//Destroy(instance);
				instance.SetActive(false);

				// TEMP: just create 1 texture for testing only
				//return;
				break;
			}
		}
		// set dirty
		UnityEditor.EditorUtility.SetDirty(thumbnailStorage);
		// refresh to let it reload thumbnail textures
		AssetDatabase.Refresh();
	}
	#endregion//Render async


	#region Utils
	Bounds CalculateBoundForSubject(GameObject subject) {
		Bounds bounds;
		Renderer[] renderers = subject.GetComponentsInChildren<Renderer>();

		// If at least 1 mesh renderer was found
		if(renderers.Length > 0) {
			// Get the bounds of the first mesh renderer
			bounds = renderers[0].bounds;
			// Encapsulate "enlarge" the bounds to include all mesh renderers found 
			for(int i = 1; i < renderers.Length; ++i)
				bounds.Encapsulate(renderers[i].bounds);
		}
		// If no mesh renderers are found!?
		else {
			// Create a fake bounds at world center
			bounds = new Bounds(new Vector3(0f, 0f, 0f), Vector3.zero);
		}
		return bounds;
	}

	void FitCameraToSubject(Camera snapshotCamera, GameObject subject, Bounds bounds) {
		//float cameraDistance = 2.5f; // Constant factor
		Vector3 objectSizes = bounds.size;//bounds.max - bounds.min;
		float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
		// Visible height 1 meter in front
		float cameraView = 3.5f * Mathf.Tan(0.5f * Mathf.Deg2Rad * snapshotCamera.fieldOfView);
		// Combined wanted distance from the object
		float distance = cameraDistance * objectSize / cameraView;
		// Estimated offset from the center to the outside of the object
		distance += 0.5f * objectSize;

		var lookAtToyVector = bounds.center - _defaultCameraPosition;
		//var direction = snapshotCamera.transform.position - bounds.center;
		snapshotCamera.transform.forward = lookAtToyVector.normalized;
		snapshotCamera.transform.position = subject.transform.position - distance * snapshotCamera.transform.forward;
	}

	Texture2D CaptureThumbnailOfSubject(Camera snapshotCamera, GameObject subject) {
		var renderTarget = RenderTexture.GetTemporary(thumbnailSize, thumbnailSize);
		snapshotCamera.targetTexture = renderTarget;
		snapshotCamera.Render();

		RenderTexture.active = renderTarget;
		Texture2D snapshot = new Texture2D(renderTarget.width, renderTarget.height, TextureFormat.RGBA32, false);
		snapshot.ReadPixels(new Rect(0, 0, renderTarget.width, renderTarget.height), 0, 0);
		snapshot.Apply();

		RenderTexture.active = null;
		snapshotCamera.targetTexture = null;
		return snapshot;
	}

	void AddOutlineToSubject(GameObject subject) {
		// outline
		var outline = subject.AddComponent<Outline>();
		//outline.ForceUpdateMaterial();
		outline.OutlineColor = outlineColor;
		outline.OutlineWidth = outlineThick;
		//outline.OutlineMode = Outline.Mode.OutlineVisible;
		//outline.ForceUpdateMaterial();
	}

	///<summary> Instantiate gameobject and take a snapshot - returned as a Texture2D </summary>
	Texture2D TakeSnapshot(GameObject subject) {
		// Set prefab parent and child layers
		//SetLayerRecursively(subject.GetComponent<Transform>(), prefabLayer);
		Bounds bounds = CalculateBoundForSubject(subject);
		// Fit Camera to GameObject
		FitCameraToSubject(snapshotCamera, subject, bounds);
		// Take snapshot of GameObject
		Texture2D snapshot = CaptureThumbnailOfSubject(snapshotCamera, subject);
		return snapshot;
	}

	void SaveSnapshotTexture(Texture2D texture, string name) {
		var pathForWriteFile = PathForWritingTextureToDisk(name);
		// https://www.reddit.com/r/Unity3D/comments/oukbj8/assetdatabasecreateasset_not_working/
		// You need to use texture.EncodeToPNG or one of the other similar methods then File.WriteAllBytes to save it in a specific image format.
		// Encode this thumbnail image to PNG
		byte[] bytes = texture.EncodeToPNG();
		File.WriteAllBytes(pathForWriteFile, bytes);

		// AssetDatabase.Refresh() will reload the whole change in project, which cost time
		// Just need to call import for single file (if not the TextureImporter.GetAtPath() will be null), importto let it import the texture by default setting. 
		var pathForImport = PathForSpriteLoadingOrImport(name);
		AssetDatabase.ImportAsset(pathForImport);
		// import it. read more in https://docs.unity3d.com/ScriptReference/TextureImporterSettings.html
		// https://forum.unity.com/threads/how-to-use-textureimporter-to-change-textures-format-and-re-import-again.86177/
		var importSetting = (TextureImporter)TextureImporter.GetAtPath(pathForImport);
		importSetting.textureType = TextureImporterType.Sprite;
		importSetting.spriteImportMode = SpriteImportMode.Single;

		// reimport it after change spriteMode
		EditorUtility.SetDirty(importSetting);
		importSetting.SaveAndReimport();
	}

	string PathForWritingTextureToDisk(string prefabName) {
		// the GetAssetPath will return "Assets/ABC/XYZ". We then will combine with Application.dataPath (which is D:/ABC/Project/Assets.
		// so we need to remove the "Assets" text before combining them
		var basePath = AssetDatabase.GetAssetPath(targetFolder);
		var basePathWithoutAssetWord = basePath.Substring(basePath.IndexOf("/"));
		var result = $"{Application.dataPath}{basePathWithoutAssetWord}/{prefabName}_thumbnail.png";
		return result;
	}

	string PathForSpriteLoadingOrImport(string prefabName) {
		// for import mean we use AssetDatabase ... the path just need like this "Assets/ABC/XYZ"
		var basePath = AssetDatabase.GetAssetPath(targetFolder);
		var result = $"{basePath}/{prefabName}_thumbnail.png";
		return result;
	}
	#endregion//Utils
}
#endif//UNITY_EDITOR
