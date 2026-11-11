using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for Tutorial actions (highlight object, delay, show trainer ...)
/// </summary>
public abstract class TutorialActionBase : ScriptableObject {
	#region Public
	[Header("Basic config")]
	public bool disposeOnFinish = true;
	public bool disposePreviousActions = false;

	///<summary> The task that "holding" this action. Be set from Task </summary>
	[NonSerialized] public TutorialTask parentTask;

	///<summary> Main function to execute this action. Return true if success full execute </summary>
	public virtual async UniTask<bool> Execute() {
		return true;
	}

	///<summary> Clear effect of this action (if any). Eg: incase the highlight action, the dispose will un-highlight item </summary>
	public virtual void Dispose() {
	}
	#endregion//Public
}
