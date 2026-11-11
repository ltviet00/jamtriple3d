using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for conditions for task. Task need condition to be execute, eg: task1 need user pass level 10 ...
/// You can create other inherited class such as PassUserLevelCondition , override IsPassed() to do checking
/// </summary>
public class TutorialConditionBase : ScriptableObject {
	public virtual bool IsPass() {
		return true;
	}
}
