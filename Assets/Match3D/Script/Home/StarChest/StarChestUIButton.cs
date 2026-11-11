using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Handle logic for UI button in MainPage
/// </summary>
public class StarChestUIButton : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserStarChestInventory starChestInventory;

	[Header("Refs")]
	[SerializeField] Image progressImage;
	[SerializeField] GameObject inProgressContent;
	[SerializeField] GameObject fullProgressContent;

	[Header("Ref - VFX star add more")]
	[SerializeField] Button mainButton;
	[SerializeField] UIElement addMoreStarVfx;
	[SerializeField] TMP_Text addMoreStarText;

	[Header("VFX")]
	[SerializeField] ImageFillAnimator progressFillAnimator;
	[SerializeField] DotFollowImageCircleFill dotFollowFillProgressAnimator;
	[SerializeField] float fillUpVfxDuration = 0.3f;

	[Header("Event in")]
	[SerializeField] GameEvent userClaimStarChestEvent;

	[Header("Config")]
	[SerializeField] float delayShowFillProgress = 0.5f;
	[SerializeField] float fillStarBarDuration = 1.5f;

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
		userClaimStarChestEvent.Subscribe(OnUserClaimStarChest);
		//CheckStartFillUpStarBar();
		SimpleDI.RegisterType(this);
		// disable some VFX by default
		addMoreStarVfx.gameObject.SetActive(false);
		// very first setup, direct config fill to current progress
		SetFillProgressWithoutVFX();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		userClaimStarChestEvent.Unsubscribe(OnUserClaimStarChest);
		SimpleDI.UnRegisterType(this);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> New logic, at begin of home scene, wait for levelchest, starChest to show fill up effect, allow user input later </summary>
	public async UniTask PlayFillUpEffect() {
		if(isFailedConfig)
			return;
		// already fulled, then do nothing
		if(starChestInventory.IsAlreadyFulled()) {
			inProgressContent.SetActive(false);
			fullProgressContent.SetActive(true);
			return;
		}
		// not full but no star gain from level? then just show the current progress, without any VFX
		var earnedMoreStar = starChestInventory.GetStarUserHasEarnMoreInLevels();
		var starInChest = starChestInventory.GetStarSavedInChest();
		if(earnedMoreStar == 0) {
			inProgressContent.SetActive(true);
			fullProgressContent.SetActive(false);
			var percent = (float)starInChest / (float)starChestInventory.StarChestCapacity;
			progressFillAnimator.SetProgress(percent);
			return;
		}

		// not full and earn more star? then show VFX
		// disable main button until finishing all VFX
		mainButton.interactable = false;
		inProgressContent.SetActive(true);
		fullProgressContent.SetActive(false);
		var ratioFrom = (float)starInChest / (float)starChestInventory.StarChestCapacity;
		var ratioTo = (float)(starInChest + earnedMoreStar) / (float)starChestInventory.StarChestCapacity;
		// Show the star plus first
		await ShowPlusEarnedMoreStarVfx(earnedMoreStar);
		// then fill up VFX
		progressFillAnimator.FillProgressAnimation(ratioFrom, ratioTo, fillUpVfxDuration).Forget();
		await dotFollowFillProgressAnimator.PlayEffect(ratioFrom, ratioTo, fillUpVfxDuration);
		// move star to chest, check full to active content
		starChestInventory.MoveStarEarnedToSavedAndResetEarnedStarToZero();
		if(starChestInventory.IsAlreadyFulled()) {
			inProgressContent.SetActive(false);
			fullProgressContent.SetActive(true);
		}
		// relase the main button
		mainButton.interactable = true;
	}
	#endregion//Public


	#region Private
	void OnUserClaimStarChest() {
		//ShowCurrentProgressOnUI(0, starChestInventory.StarChestCapacity);
		inProgressContent.SetActive(true);
		fullProgressContent.SetActive(false);
		progressFillAnimator.SetProgress(0f);
	}

	void SetFillProgressWithoutVFX() {
		var starInChest = starChestInventory.GetStarSavedInChest();
		inProgressContent.SetActive(true);
		fullProgressContent.SetActive(false);
		var percent = (float)starInChest / (float)starChestInventory.StarChestCapacity;
		progressFillAnimator.SetProgress(percent);
	}

	async UniTask ShowPlusEarnedMoreStarVfx(int starAddMore) {
		const string STAR_ADD_FORMAT = "+{0}";
		addMoreStarText.text = string.Format(STAR_ADD_FORMAT, starAddMore);
		addMoreStarVfx.gameObject.SetActive(true);
		// wait for show effect, delay a bit after done
		addMoreStarVfx.Show();
		await UniTask.Delay(TimeSpan.FromSeconds(addMoreStarVfx.GetShowDuration()), cancellationToken: _disableToken.Token);
		await UniTask.Delay(100, cancellationToken:_disableToken.Token);
		// hide vfx (star head to chest, config in UIElement)
		addMoreStarVfx.Hide();
		await UniTask.Delay(TimeSpan.FromSeconds(addMoreStarVfx.GetHideDuration()), cancellationToken: _disableToken.Token);
		addMoreStarVfx.gameObject.SetActive(false);
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
