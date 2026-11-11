using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handle affect when user catch weekly toy. Will be called from ToyCatcher.
/// Also save amount of weekly toy to inventory when user win level
/// </summary>
public class WeeklyToyAffectorController : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] IntegerVariable currentCollectedToyVar;
	[SerializeField] UserWeeklyToyInventory weeklyToyInventory;
	[SerializeField] UserDoubleEventToyInventory doubleEventToyInventory;

	[Header("Event in")]
	[SerializeField] GameEvent userWinGameEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
		// reset value at start
		currentCollectedToyVar.Value = 0;
		// events
		userWinGameEvent.SubscribleOneTimeOnly(OnUserWinLevel);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
		// reset value at destroy
		currentCollectedToyVar.Value = 0;
	}
	#endregion// Init config


	#region Public
	///<summary> Called from TOycatcher, handle when user catch a weekly event toy </summary>
	public void HandleCatchingWeeklyEventToy(ToyView toyView, WeeklyToyID id) {
		if(isFailedConfig)
			return;
		// simply increase collect toy count
		currentCollectedToyVar.Value ++;
	}
	#endregion//Public


	#region Private
	void OnUserWinLevel() {
		// save to inventory. Check doubleEventToy inventory
		currentCollectedToyVar.Value *= doubleEventToyInventory.IsInDoubleToyDuration() ? 2 : 1;
		weeklyToyInventory.Data.collectedMoreFromLevels += currentCollectedToyVar.Value;
		weeklyToyInventory.In_SaveToDisk();
	}
	#endregion//Private
}
