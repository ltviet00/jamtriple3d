using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Some utils function for Tutorial actions
/// </summary>
public class TutorialUtils {
	#region Find object
	const string SPLITTER = "/";
	///<summary> The path should like this "Canvas/Root1/Root2/Target" </summary>
	public static Transform FindObjectByPath(string path) {
		if(string.IsNullOrEmpty(path)) {
			Log.Warning(false, $"Null object path");
			return null;
		}

		string[] splittedPath = path.Split(SPLITTER);
		// find by root object is more efficient than GameObject.Find
		// find in all current active scenes
		int count = SceneManager.sceneCount;
		var roots = new List<GameObject>();
		for(int index = 0; index < count; index++) {
			var scene = SceneManager.GetSceneAt(index);
			if(!scene.isLoaded) continue;
			roots.AddRange(scene.GetRootGameObjects());
		}

		// start from the root
		var rootName = splittedPath[0];
		Transform parent = null;
		for(int n = 0; n < roots.Count; n++) {
			var root = roots[n];
			if(rootName.Equals(root.name)) {
				parent = root.transform;
				break;
			}
		}
		// not found from the parent?
		if(parent == null) {
			Log.Info($"Not found object for path from parent [{path}]. Wrong path or object has moved?");
			return null;
		}
		// recursive to the child, now start from 1
		for(int n = 1; n < splittedPath.Length; n++) {
			var childCount = parent.childCount;
			var objectName = splittedPath[n];
			// no child but not reach the end of path, mean not found
			if(childCount == 0 && n < splittedPath.Length) {
				Log.Info($"Not found object for path, noChild from [{objectName}] ,[{path}]");
				return null;
			}
			// found in child
			Transform nextParentFound = null;
			for(int m = 0; m < childCount; m++) {
				var child = parent.GetChild(m);
				if(child.name.Equals(objectName)) {
					nextParentFound = child.transform;
					break;
				}
			}
			// not found any child same name? break
			if(nextParentFound == null) {
				Log.Info($"Not found object for path, from [{objectName}] ,[{path}]. Wrong path or object has moved?");
				return null;
			}
			// assign nextParent to parent, for the next loop
			parent = nextParentFound;
			//// found object and reach the end of path?
			//if(n == splittedPath.Length - 1) {
			//	return parent;
			//}
		}
		// reach the end
		if(parent == null) {
			Log.Info($"Not found object for path [{path}]. Wrong path or object has moved?");
		}
		return parent;
	}

	///<summary> Find object with same name on the rootHightlight item.
	/// When we highligh object, its parent change (to rootHightligh), so path change. Use this to try find object in TutorialCanvas.
	/// Return null if not found </summary>
	public static Transform FindObjectInTutorialCanvas(string path) {
		if(!TutorialCanvas.HasInstance()) {
			Log.Info($"No tutorialCanvas alive, no search");
			return null;
		}
		// take the name of object only, last word in the path (next to the splitter 1 character, so +1)
		var lastIndexOfSplitter = path.LastIndexOf(SPLITTER);
		var objectName = path.Substring(lastIndexOfSplitter + 1, path.Length - lastIndexOfSplitter - 1);

		var tutorialCanvas = TutorialCanvas.Instance;
		var result = tutorialCanvas.FindHighlightObject(objectName);
		return result;
	}

	///<summary> Try find object by full path, if not found, then find in Tutorial canvas </summary>
	public static Transform FindObjectByFullPathAndInTutorialCanvas(string path) {
		var target = FindObjectByPath(path);
		if(target == null) {
			target = FindObjectInTutorialCanvas(path);
		}
		return target;
	}
	#endregion// Find object
}

