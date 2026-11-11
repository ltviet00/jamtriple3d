using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle logic for UI button in MainPage
/// </summary>
public class LevelChestUIButton : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] IntegerVariable userReacheLevelVar;
	[SerializeField] UserLevelChestInventory userLevelChestInventory;
	[SerializeField] UserLevelScoreInventory userScoreInventory;

	[Header("Refs")]
	[SerializeField] Button mainButton;
	[SerializeField] GameObject inProgressContent;
	[SerializeField] GameObject fullProgressContent;

	[Header("VFX")]
	[SerializeField] ImageFillAnimator progressFillAnimator;
	[SerializeField] DotFollowImageCircleFill dotFollowFillProgressAnimator;
	[SerializeField] float fillUpVfxDuration = 0.3f;

	[Header("Event in")]
	[SerializeField] GameEvent userClaimLevelChestEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponent();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void CacheComponent() {
		if(mainButton == null) {
			mainButton = GetComponent<Button>();
		}
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		userClaimLevelChestEvent.Subscribe(OnUserClaimLevelChest);
		//ConfigDisplay();
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		userClaimLevelChestEvent.Unsubscribe(OnUserClaimLevelChest);
		ClearRunningTasks();
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> New logic, at begin of home scene, wait for levelchest, starChest to show fill up effect, allow user input later </summary>
	public async UniTask PlayFillUpEffect() {
		if(isFailedConfig)
			return;
		// disable main button until finishing all VFX
		mainButton.interactable = false;

		var isClaimedPreviousBonus = userLevelChestInventory.IsClaimedPreviousBonus();
		// not claimed? then show the open button
		inProgressContent.SetActive(isClaimedPreviousBonus);
		fullProgressContent.SetActive(!isClaimedPreviousBonus);
		// already claim, so in progress for current, config content
		var levelCountToGetChest = userLevelChestInventory.LevelCountToGetChest;
		if(isClaimedPreviousBonus) {
			var previousBonusLevel = userLevelChestInventory.GetPreviousBonusLevel();
			var currentLevel = userScoreInventory.GetCurrentReachLevel();
			var levelPassed = currentLevel - previousBonusLevel;
			// passed enough level to claim
			if(levelPassed >= levelCountToGetChest) {
				fullProgressContent.SetActive(true);
				inProgressContent.SetActive(false);
			}
			// not enough, config content
			else {
				var progressInPercent = (float)levelPassed / (float)levelCountToGetChest;
				// let just say we show progress from previous level, mean -1
				var fakeVfxFrom = (float)(levelPassed - 1) / (float)levelCountToGetChest;
				// VFX
				var fillProgres = progressFillAnimator.FillProgressAnimation(fakeVfxFrom, progressInPercent, fillUpVfxDuration);
				var dotFollowProgress = dotFollowFillProgressAnimator.PlayEffect(fakeVfxFrom, progressInPercent, fillUpVfxDuration);
				await UniTask.WhenAll(fillProgres, dotFollowProgress);//.AttachExternalCancellation(_disableToken.Token);
			}
		}
		// relase the main button
		mainButton.interactable = true;
	}
	#endregion//Public


	#region Private
	void OnUserClaimLevelChest() {
		// rest display
		//progressImage.fillAmount = 0f;
		//progressText.text = $"0/{userLevelChestInventory.LevelCountToGetChest}";
		ConfigDisplay();
	}

	void ConfigDisplay() {
		var isClaimedPreviousBonus = userLevelChestInventory.IsClaimedPreviousBonus();
		// not claimed? then show the open button
		inProgressContent.SetActive(isClaimedPreviousBonus);
		fullProgressContent.SetActive(!isClaimedPreviousBonus);
		// already claim, so in progress for current, config content
		var levelCountToGetChest = userLevelChestInventory.LevelCountToGetChest;
		if(isClaimedPreviousBonus) {
			var previousBonusLevel = userLevelChestInventory.GetPreviousBonusLevel();
			var currentLevel = userScoreInventory.GetCurrentReachLevel();
			var levelPassed = currentLevel - previousBonusLevel;
			// passed enough level to claim
			if(levelPassed >= levelCountToGetChest) {
				fullProgressContent.SetActive(true);
				inProgressContent.SetActive(false);
			}
			// not enough, config content
			else {
				var progressInPercent = (float)levelPassed / (float)levelCountToGetChest;
				progressFillAnimator.SetProgress(progressInPercent);
			}
		}
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
