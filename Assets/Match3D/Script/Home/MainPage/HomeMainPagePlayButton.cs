using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Attach to Play button at Home </summary>
public class HomeMainPagePlayButton : MonoBehaviour {
	#region Init, config
	[Header("User, ShareData")]
	[SerializeField] UserLevelScoreInventory userLevel;
	[SerializeField] UserEnergyInventory userEnergyInventory;
	[SerializeField] UserUnlimitEnergyInventory unlimitEnergyInventory;

	[Header("Unity event out")]
	[SerializeField] UnityEvent loadTutorialCmd;
	[SerializeField] UnityEvent loadGamePageCmd;
	[SerializeField] UnityEvent loadOutOfLifePopup;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Public - UI invoke
	///<summary> Config Play UI buton to invoke this </summary>
	public void In_OnClick() {
		if(isFailedConfig)
			return;
		// check unlimit energy
		if(unlimitEnergyInventory.IsInUnlimitEnergyDuration()) {
			CheckLoadTutorialOrOpenGamePage();
			return;
		}
		// check out of energy
		if(userEnergyInventory.IsEmpty()) {
			loadOutOfLifePopup.Invoke();
			return;
		}
		// check if this's first launch. For this game, simple check current level > 0
		CheckLoadTutorialOrOpenGamePage();
	}

	void CheckLoadTutorialOrOpenGamePage() {
		//var currentLevel = userLevel.GetCurrentReachLevel();
		//var passedTutorial = currentLevel > 0;
		//var eventToRaise = passedTutorial ? loadGamePageCmd : loadTutorialCmd;
		//eventToRaise.Invoke();
		// TEMP for now, bypass tutorial
		loadGamePageCmd.Invoke();
	}
	#endregion//Public - UI invoke
}
