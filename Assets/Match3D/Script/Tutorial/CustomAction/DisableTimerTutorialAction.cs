using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disable game timer, for some tutorial level, we need to disable timer
/// </summary>
[CreateAssetMenu(fileName = "DisableTimerTutorialAction", menuName = "Tutorial/Action/DisableTimerTutorialAction")]
public class DisableTimerTutorialAction : TutorialActionBase {
	#region Override
	public override async UniTask<bool> Execute() {
		var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
		if(levelTimer == null)
			return false;
		levelTimer.SetActiveFreeze(true);
		return true;
	}
	#endregion//Override
}
