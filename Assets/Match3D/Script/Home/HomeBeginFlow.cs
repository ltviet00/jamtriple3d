using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// At Home, begin flow is : show LevelChest fill up, StarChest fillup, , weekly events collect ...
/// </summary>
public class HomeBeginFlow : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] UIPage mainPage;

	[Header("Event out")]
	[SerializeField] UnityEvent checkOpenRateGameHandler;
	[SerializeField] UnityEvent triggerTutorialLocationHandler;

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
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI callback
	///<summary> Config the mainPage to invoke this - notes: main callback to trigger the Begin flow </summary>
	public void In_OnMainPageFinishOpen() {
		if(isFailedConfig)
			return;
		RunBeginFlow().Forget();
	}
	#endregion//UI callback


	#region Private
	async UniTask RunBeginFlow() {
		// disable input on main page
		// UPDATE: we now not disable touch on main page
		//mainPage.CachedCanvasGroup.interactable = false;
		var levelChestUI = SimpleDI.Instance.Get<LevelChestUIButton>();
		var starChestUI = SimpleDI.Instance.Get<StarChestUIButton>();
		var weeklyEventUI = SimpleDI.Instance.Get<WeeklyEventUIBar>();

		// weekly event
		//await UniTask.Delay(200, cancellationToken: _disableToken.Token);
		await weeklyEventUI.PlayFillUpEffect();

		// level chest, delay a bit
		//await UniTask.Delay(200, cancellationToken: _disableToken.Token);
		await levelChestUI.PlayFillUpEffect();

		// star chest
		//await UniTask.Delay(200, cancellationToken: _disableToken.Token);
		await starChestUI.PlayFillUpEffect();

		// reactive input on main page
		// UPDATE: we now not disable touch on main page
		//mainPage.CachedCanvasGroup.interactable = true;
		// Trigger tutorial
		triggerTutorialLocationHandler.Invoke();
		// check open rate game
		checkOpenRateGameHandler.Invoke();
	}

	//async UniTask RunBeginFlow() {
	//	// disable input on main page
	//	mainPage.CachedCanvasGroup.interactable = false;
	//	var levelChestUI = SimpleDI.Instance.Get<LevelChestUIButton>();
	//	var starChestUI = SimpleDI.Instance.Get<StarChestUIButton>();
	//	var weeklyEventUI = SimpleDI.Instance.Get<WeeklyEventUIBar>();

	//	// weekly event
	//	await UniTask.Delay(200, cancellationToken: _disableToken.Token);
	//	await weeklyEventUI.PlayFillUpEffect();

	//	// level chest, delay a bit
	//	await UniTask.Delay(200, cancellationToken: _disableToken.Token);
	//	await levelChestUI.PlayFillUpEffect();

	//	// star chest
	//	await UniTask.Delay(200, cancellationToken: _disableToken.Token);
	//	await starChestUI.PlayFillUpEffect();

	//	// reactive input on main page
	//	mainPage.CachedCanvasGroup.interactable = true;
	//	// Trigger tutorial
	//	triggerTutorialLocationHandler.Invoke();
	//	// check open rate game
	//	checkOpenRateGameHandler.Invoke();
	//}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
