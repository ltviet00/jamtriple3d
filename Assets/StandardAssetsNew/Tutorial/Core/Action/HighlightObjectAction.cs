using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Work with Tutorial canvas, bring the object want to highlight to the Tutorial canvas.
/// NOTES: to highlight object, we bring it to tutorial canvas, so the ObjectPath will changed, mind that in the actions
/// that need object path after the highlight task
/// </summary>
[CreateAssetMenu(fileName = "HighlightObjectAction", menuName = "Tutorial/Action/HighlightObjectAction")]
public class HighlightObjectAction : TutorialActionBase {
	#region Init, config
	[Header("Config")]
	[SerializeField] bool activeBlackCover = true;

	[Header("Data")]
	[SerializeField] List<string> objectPaths;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Override
	///<summary> List of Current highlight object</summary>
	List<BackupTransformnInfo> _targetList = new List<BackupTransformnInfo>();

	public override async UniTask<bool> Execute() {
		if(isFailedConfig)
			return false;
		_targetList.Clear();
		for(int n = 0; n < objectPaths.Count; n++) {
			var path = objectPaths[n];
			// find object by path
			var target = TutorialUtils.FindObjectByFullPathAndInTutorialCanvas(path);
			if(target == null) {
				return false;
			}
			// backup info first
			var backup = new BackupTransformnInfo();
			backup.Backup(target);
			_targetList.Add(backup);
			// bring it to Tutorial canvas call to TutorialCanvas 
			var tutorialCanvas = TutorialCanvas.Instance;
			tutorialCanvas.SetActiveRender(true);
			var rectTrans = target as RectTransform;
			tutorialCanvas.BringObjectToCanvasAndKeepRelativePostion(rectTrans);
			// black cover
			tutorialCanvas.BlackCoverImage.enabled = activeBlackCover;
		}
		return true;
	}

	public override void Dispose() {
		base.Dispose();
		RemoveCurrentHighlightObject();
	}
	#endregion//Override


	#region Public
	public void RemoveCurrentHighlightObject() {
		if(isFailedConfig)
			return;
		// no current highlight? then ignore
		if(_targetList.Count == 0) {
			return;
		}
		for(int n = 0; n < _targetList.Count; n++) {
			_targetList[n].Restore();

		}
		// also disable tutorial canvas
		var tutorialCanvas = TutorialCanvas.Instance;
		tutorialCanvas.SetActiveRender(false);
	}
	#endregion//public


	#region Classes
	///<summary> When highlight object, we need to save some info (Parent, size ...). If then we need to un-highlight it, then use this to revert </summary>
	public class BackupTransformnInfo {
		///<summary> The target object we higlighting </summary>
		public Transform target;
		///<summary> Parent of the target before moving to tutorial canvas </summary>
		public Transform previousParent;
		///<summary> Sibling index of the transform </summary>
		public int siblingIndex;
		///<summary> Anchor position of the RectTransform </summary>
		public Vector2 anchorPosition;

		public void Backup(Transform suspect) {
			target = suspect;
			previousParent = suspect.parent;
			siblingIndex = suspect.GetSiblingIndex();

			var rectTrans = target as RectTransform;
			anchorPosition = rectTrans.anchoredPosition;
		}

		public void Restore() {
			// currently , only parent transform
			target.SetParent(previousParent, false);
			target.SetSiblingIndex(siblingIndex);

			var rectTrans = target as RectTransform;
			rectTrans.anchoredPosition = anchorPosition;
		}
	}
	#endregion//Classes
}
