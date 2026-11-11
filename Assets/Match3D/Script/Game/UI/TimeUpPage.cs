using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimeUpPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserCoinInventory userCoinInventory;
	[SerializeField] UserWatchAdsToReviveInventory userWatchAdsReviveInventory;
	[SerializeField] IntegerVariable reviveCountingVar;

	[Header("Revive data")]
	[SerializeField] IntegerVariable coinToReviveVar;
	[SerializeField] IntegerVariable reviveExtraTimeByAdsCfg;
	[SerializeField] IntegerVariable reviveExtraTimeByCoinCfg;

	[Header("BoosterID")]
	[SerializeField] SupportToyID lightingStormSupportToyID;

	[Header("Refs")]
	[SerializeField] GameObject thunderBoosterAfterReviveUI;
	//[SerializeField] GameObject watchAdsToReviveContent;
	//[SerializeField] GameObject useCoinToReviveContent;

	[Header("Event out")]
	[SerializeField] GameEvent userReviveEvent;

	[Header("Unity Event out")]
	[SerializeField] UnityEvent openShopPageCmd;
	[SerializeField] UnityEvent closeSelfCmd;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		// reset revive counting at first enter scene
		reviveCountingVar.Value = 0;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region UI input
	///<summary> Config the UIPage to invoke this </summary>
	public void In_OnPageStartOpen() {
		if(isFailedConfig)
			return;
		// UPDATE logic: nolonger need to check ReachFreeAds, allow user to watch ads to die
		//bool stillAdsRemainToRevive = !userWatchAdsReviveInventory.IsReachFreeAdsToRevivePerDay();
		//watchAdsToReviveContent.SetActive(stillAdsRemainToRevive);
		//useCoinToReviveContent.SetActive(!stillAdsRemainToRevive);
		ConfigContentWhenOpenPage();
	}

	///<summary> Config watch ads button to invoke this when user watch ads success </summary>
	public void In_WatchAdsSuccessToRevive() {
		if(isFailedConfig)
			return;
		// reduce ads watched count, increase revive count, raise event
		userWatchAdsReviveInventory.IncreaseAdsWatched();
		userReviveEvent.Raise();
		closeSelfCmd.Invoke();
		// increse revive at last
		IncreaseReviveCount();
		// reset timer
		var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
		levelTimer.ResetClockWhenUserRevive(reviveExtraTimeByAdsCfg.Value);
	}

	///<summary> Config the useCoin to revive button to invoke this </summary>
	public void In_PressUseCoinToRevive() {
		if(isFailedConfig)
			return;
		// enough coin? then reduce coin, raise event revive, close self
		if(userCoinInventory.IsEnough(coinToReviveVar.Value)) {
			userCoinInventory.DecreaseCoin(coinToReviveVar.Value);
			closeSelfCmd.Invoke();
			userReviveEvent.Raise();
			// check trigger lighting storm, from the 2nd revive, we'll give user a free thunder booster
			if(reviveCountingVar.Value > 0) {
				LightingStormAffectAfterRevive().Forget();
			}
			// reset timer
			var levelTimer = SimpleDI.Instance.Get<LevelTimer>();
			levelTimer.ResetClockWhenUserRevive(reviveExtraTimeByCoinCfg.Value);
			// increse revive at last
			IncreaseReviveCount();
		}
		// not enough coin -> open Shop
		else {
			openShopPageCmd.Invoke();
		}
	}
	#endregion//UI input


	#region Private
	///<summary> Now base on user's revive counting, we increase the price of revive </summary>
	void ConfigContentWhenOpenPage() {
		// from 2nd timeup, if user using bailout (coin or ads) we give them one lightingStorm booster affect
		thunderBoosterAfterReviveUI.SetActive(reviveCountingVar.Value > 0);
	}

	void IncreaseReviveCount() {
		reviveCountingVar.Value++;
	}

	async UniTask LightingStormAffectAfterRevive() {
		// delay abit for popup to hide out
		await UniTask.Delay(500, cancellationToken: _disableToken.Token);
		// trigger lightingStorm affect, via SupportToyAffectController
		var supportToyAffector = SimpleDI.Instance.Get<SupportToyAffectorController>();
		supportToyAffector.TriggerAffectForSupportToy(lightingStormSupportToyID);
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
