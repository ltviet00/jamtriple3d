using UnityEngine;

/// <summary>
/// Pass when the level the user is about to play contains this support toy (lightning bolt, clock...).
/// So a "collect the bolt" tutorial only runs on a level that really has a bolt, and waits for the next one otherwise.
/// </summary>
[CreateAssetMenu(fileName = "CurrentLevelHasSupportToyTutorialCondition", menuName = "Tutorial/Condition/CurrentLevelHasSupportToyTutorialCondition")]
public class CurrentLevelHasSupportToyTutorialCondition : TutorialConditionBase {
	#region Init, config
	[Header("Data, config")]
	[SerializeField] UserLevelScoreInventory levelInventory;
	[SerializeField] LevelDataStorage levelStorage;
	[SerializeField] SupportToyID supportToyId;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	public override bool IsPass() {
		var levelData = levelStorage.GetLevelData(levelInventory.GetCurrentReachLevel());
		return levelData != null && levelData.GetAmountOfSupportToy(supportToyId) > 0;
	}
	#endregion//Public
}
