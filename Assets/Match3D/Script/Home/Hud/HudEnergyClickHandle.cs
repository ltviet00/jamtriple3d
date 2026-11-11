using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Handle logic when user click on energy icon at Hud.
/// Currently 2 cases: open "your lives are full" or open Buy life popup.
/// The text on the heart timer already took care by UserEnergyTimerText component.
/// </summary>
public class HudEnergyClickHandle : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] UserEnergyInventory userEnergyInventory;
	[SerializeField] UserUnlimitEnergyInventory userUnlimitEnergyInventory;

	[Header("Refs")]
	[SerializeField] GameObject yourLifeFullToast;

	[Header("Config")]
	[SerializeField] float toastDuration = 2f;

	[Header("Event out")]
	[SerializeField] UnityEvent openGetMoreLifePopupHandler;

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


	#region UI callbacks
	///<summary> Config HudEnergy (Lives) button to invoke this </summary>
	public void In_OnButtonClick() {
		if(isFailedConfig)
			return;
		// full energy, open toast
		if(userUnlimitEnergyInventory.IsInUnlimitEnergyDuration() || userEnergyInventory.IsFull()) {
			ClearRunningTasks();
			yourLifeFullToast.SetActive(true);
			DelayDisableToast().Forget();
		}
		// not full, open buy energy popup
		else {
			openGetMoreLifePopupHandler.Invoke();
		}
	}
	#endregion//UI callbacks


	#region Private
	async UniTask DelayDisableToast() {
		await UniTask.Delay(TimeSpan.FromSeconds(toastDuration), cancellationToken: _disableToken.Token);
		yourLifeFullToast.SetActive(false);
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
