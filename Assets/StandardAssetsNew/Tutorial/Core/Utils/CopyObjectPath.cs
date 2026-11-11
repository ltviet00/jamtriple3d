#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CopyObjectPath {
	/// <summary>
	/// For tutorial, we need to copy object path.
	/// https://forum.unity.com/threads/please-include-a-copy-path-when-right-clicking-a-game-object.429480/
	/// </summary>
	public static class CopyGameObjectPathFromHierarchy {
		[MenuItem("GameObject/Tutorial/Copy Path", false, 11)]
		static void CopyPath() {
			// no select gameobject, then ignore
			GameObject currentGameObject = Selection.activeGameObject;
			if(currentGameObject == null)
				return;
			// go up to parents
			string path = currentGameObject.name;
			while(currentGameObject.transform.parent != null) {
				currentGameObject = currentGameObject.transform.parent.gameObject;
				path = $"{currentGameObject.name}/{path}";
			}
			// save to copy buffer for paste later
			EditorGUIUtility.systemCopyBuffer = path;
		}

		/// <summary>
		/// Only allow path copying if 1 object is selected.
		/// </summary>
		[MenuItem("GameObject/Tutorial/Copy Path", true)]
		static bool CopyPathValidation() => Selection.gameObjects.Length == 1;
	}
}
#endif//UNITY_EDITOR