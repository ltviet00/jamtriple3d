using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PassUserLevelTutorialCondition", menuName = "Tutorial/Condition/PassUserLevelTutorialCondition")]
public class PassUserLevelTutorialCondition : TutorialConditionBase {
	#region Init, config
	[Header("Data, config")]
	[SerializeField] UserLevelScoreInventory levelInventory;
	[SerializeField] int requireLevel;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public override bool IsPass() {
		var reachLevel = levelInventory.GetCurrentReachLevel();
		return reachLevel >= requireLevel;
	}
	#endregion//Public
}
