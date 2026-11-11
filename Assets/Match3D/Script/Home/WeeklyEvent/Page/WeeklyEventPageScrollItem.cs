using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle logic display for item in ScrollView of the WeeklyEventInprogressPage
/// </summary>
public class WeeklyEventPageScrollItem : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] BackgroundRefs bgBarImageRefs;
	[SerializeField] TMP_Text indexNumeberText;
	[SerializeField] Image lockedIcon;
	[SerializeField] Image collectedIcon;
	[SerializeField] WeeklyEventRewardIcon rewardIcon;

	[Header("Config")]
	[SerializeField] BackgroundSprite bgBarSpriteNormal;
	[SerializeField] BackgroundSprite bgBarSpriteHighlight;

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


	#region Public
	public void ConfigContent(int index, WeeklyEventRewardDefine reward) {
		if(isFailedConfig)
			return;
		rewardIcon.ConfigDisplayForReward(reward);
		indexNumeberText.text = $"{index + 1}";
	}

	public void ActiveContentFollowState(State state) {
		if(isFailedConfig)
			return;
		// BG first, only 2 state: normal - highlight
		SetSpriteForBackgroundBar(state == State.Current);
		// lock icon, show when state = locked
		lockedIcon.enabled = state == State.Locked;
		// reward icon will show if state != collected
		rewardIcon.gameObject.SetActive(state != State.Collected);
		// check mark will show if state = collected
		collectedIcon.gameObject.SetActive(state == State.Collected);
	}
	#endregion//Public


	#region Private
	void SetSpriteForBackgroundBar(bool isHighlight) {
		var spriteToUse = isHighlight ? bgBarSpriteHighlight : bgBarSpriteNormal;
		bgBarImageRefs.left.sprite = spriteToUse.left;
		bgBarImageRefs.right.sprite = spriteToUse.right;
	}
	#endregion//Private


	#region Classes
	[System.Serializable]
	class BackgroundRefs {
		public Image left;
		public Image right;
	}

	[System.Serializable]
	class BackgroundSprite {
		public Sprite left;
		public Sprite right;
	}

	public enum State {
		Collected, Current, Locked
	}
	#endregion//Classes
}
