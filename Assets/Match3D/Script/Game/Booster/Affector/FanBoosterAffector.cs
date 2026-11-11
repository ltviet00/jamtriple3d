using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Handle function for Fan booster
/// </summary>
public class FanBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Event in")]
	[SerializeField] GameEvent onUserRequestFanEvent;

	[Header("ShareData, config")]
	[SerializeField] LevelToyStatistics toyStats;

	[Header("Config - push toy")]
	[SerializeField] Vector2Variable pushToyVelocityVar;
	[SerializeField] Vector2Variable pushToyXrandomVar;
	[SerializeField] Vector2Variable pushToyYrandomVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		onUserRequestFanEvent.Subscribe(RequestFan);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		onUserRequestFanEvent.Unsubscribe(RequestFan);
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private
	void RequestFan() {
		AddVelocityOnAllToys();
	}

	void AddVelocityOnAllToys() {
		var allToyMap = toyStats.AllMaps;
		for(int n = 0; n < allToyMap.Count; n++) {
			var map = allToyMap[n];
			foreach(var list in map.Values) {
				for(int m = 0; m < list.Count; m++) {
					var toy = list[m];
					var velo = Vector3.zero;
					// we changed gravity coordinate to Z, and -Z mean push toy up
					velo.z = -1f * pushToyVelocityVar.RandomBetweenXY();
					// random x, y
					velo.x = pushToyXrandomVar.RandomBetweenXY();
					velo.y = pushToyYrandomVar.RandomBetweenXY();
					toy.mainRigid.velocity = velo;
				}
			}
		}
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
