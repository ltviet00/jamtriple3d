using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

/// <summary>
/// Handle logic and display for NewToyPage
/// </summary>
public class NewToyPage : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] ToyStorage toyStorage;

	[Header("Refs")]
	[SerializeField] Button okayButton;

	[Header("Ref - toy placeholder")]
	[SerializeField] Transform leftTopToyPos;
	[SerializeField] Transform rightBottomToyPos;

	[Header("Config")]
	[SerializeField] int columnToShowToy = 2;
	[SerializeField] float autoRotateToySpeed;
	[SerializeField] float delayShowNextButton;

	[Header("UnityEvent")]
	[SerializeField] UnityEvent requestOpenNextLevelPageCmd;

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
		ClearShowCaseToy();
	}
	#endregion// Init config


	#region UI input
	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageStartOpen() {
		if(isFailedConfig)
			return;
		// disable okay button as first
		okayButton.gameObject.SetActive(false);
	}

	///<summary> Config UIPage to invoke this </summary>
	public void In_OnPageOpen() {
		if(isFailedConfig)
			return;
		SpawnNewToyAsync().Forget();
	}

	///<summary> Config okay button to invoke this </summary>
	public void In_OnUserPressOkayButton() {
		requestOpenNextLevelPageCmd.Invoke();
		ClearShowCaseToy();
	}
	#endregion//UI input


	#region Private
	List<ToyView> _showCaseToys = new List<ToyView>();

	async UniTaskVoid SpawnNewToyAsync() {
		Log.Error(false, $"The new toypage is nolonger function");
		//var group = toyStorage.GetCurrentGroupToUnlock();
		//if(group == null) {
		//	Log.Warning(false, this, $"No group to unlock !!!");
		//	// rare, but in case bug happen, let enable the OkayButton so the game wont stuck
		//	okayButton.gameObject.SetActive(true);
		//	return;
		//}
		//// spawn toy
		//for(int n = 0; n < group.showCaseToyList.Count; n++) {
		//	var prefab = group.showCaseToyList[n];
		//	var toy = Instantiate(prefab).GetComponent<ToyView>();
		//	// remember to add to the list to clear later
		//	_showCaseToys.Add(toy);
		//	// disable render, will zoom them later
		//	if(toy.mainRenderer != null) {
		//		toy.mainRenderer.enabled = false;
		//	}
		//	// position for toy
		//	var posForToy = CalculatePositionForToyByIndex(n, group.showCaseToyList.Count);
		//	toy.transform.position = posForToy;
		//	// disable physics
		//	toy.DisablePhysics();
		//}
		//// wait a bit, spawn toy may cost a hicup
		//await UniTask.Yield();
		//for(int n = 0; n < _showCaseToys.Count; n++) {
		//	var toy = _showCaseToys[n];
		//	if(toy.mainRenderer != null) {
		//		toy.mainRenderer.enabled = true;
		//	}
		//	// rotat them a bit
		//	var rotator = toy.gameObject.AddComponent<AutoRotateUniRx>();
		//	rotator.rotateSpeed = new Vector3(0, autoRotateToySpeed, 0);
		//}
		//// delay show okay button
		//await UniTask.Delay(1500);
		//okayButton.gameObject.SetActive(true);
	}

	Vector3 CalculatePositionForToyByIndex(int index, int totalToy) {
		var row = index / columnToShowToy;
		var column = index % columnToShowToy;
		var rowDistance = (leftTopToyPos.transform.position.y - rightBottomToyPos.transform.position.y) / (float)((totalToy - 1) /columnToShowToy);
		var columnDitance = (rightBottomToyPos.transform.position.x - leftTopToyPos.transform.position.x) / (float)(columnToShowToy - 1);

		var pos = leftTopToyPos.transform.position;;
		pos.x += column * columnDitance;
		pos.y -= row * rowDistance;
		return pos;
	}

	void ClearShowCaseToy() {
		for(int n = 0; n < _showCaseToys.Count; n++) {
			var toy = _showCaseToys[n];
			if(toy != null)
				Destroy(toy.gameObject);
		}
	}
	#endregion//Private
}
