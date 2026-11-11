using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// After baking lot of raw textures into one atlas, we save some information to a file,
/// to know the results (atlas + material), also references to the textures was used to build up this atlas,
/// also there're updated uv in this atlas.
/// </summary>
public class AtlasBakedResult : ScriptableObject {
	public Texture2D atlas;
	public Material material;
	public UVMap[] uvMaps;

	///<summary> Call when create this file </summary>
	public void FillUVMap(Texture2D[] originalTextures, Rect[] UVs) {
		uvMaps = new UVMap[originalTextures.Length];
		for(int n = 0; n < uvMaps.Length; n++) {
			uvMaps[n].texture = originalTextures[n];
			uvMaps[n].rect = UVs[n];
		}
	}

	///<summary> Check if the input texture was used to build up this atlas (contain in the UVMaps)</summary>
	public bool ContainTexture(Texture2D texture) {
		for(int n = 0; n < uvMaps.Length; n++) {
			if(uvMaps[n].texture == texture)
				return true;
		}
		// reach here, mean not found
		return false;
	}

	///<summary> If the texture is inside atlas, then this function return the Rect of that texture in atlas.
	///NOTES : check ContainTexture() first</summary>
	public Rect GetRectForSubTexture(Texture2D texture) {
		for(int n = 0; n < uvMaps.Length; n++) {
			if(uvMaps[n].texture == texture)
				return uvMaps[n].rect;
		}
		// reach here mean not found the texture
		Debug.LogWarning(string.Format("Not found texture [{0}] in atlas [{1}]", texture.name, this.name), this);
		return new Rect();
	}

	///<summary> Small textures after packing to atlas, will have new UV, save them here </summary>
	[Serializable]
	public struct UVMap {
		public Texture2D texture;
		public Rect rect;
	}
}
