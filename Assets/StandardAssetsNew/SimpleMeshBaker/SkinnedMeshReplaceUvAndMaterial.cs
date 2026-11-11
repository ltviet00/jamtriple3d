using UnityEngine;
using System.Collections;

/// <summary>
/// Already create an Atlas and material for many SkinnedMesh via TextureBaker component?
/// Now use this to update all separate SkinnedMeshRenderer to use the same atlas (also modify UV map),
/// so we will need 1 drawcall for them all
/// </summary>
public class SkinnedMeshReplaceUvAndMaterial : MonoBehaviour {
	public AtlasBakedResult atlasConfig;
	public GameObject[] objectsToUpdate;
	public string exportPath;

	void OnValidate() {
		if(atlasConfig == null) {
			Debug.LogWarning("Missing atlasConfig", this);
		}
		if(objectsToUpdate == null || (objectsToUpdate != null && objectsToUpdate.Length == 0)) {
			Debug.LogWarning("Missing meshesToUpdate", this);
		}
	}

	public bool IsFailedConfig() {
		return atlasConfig == null
			|| objectsToUpdate == null
			|| (objectsToUpdate != null && objectsToUpdate.Length == 0);
	}
}
