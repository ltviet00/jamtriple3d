/*

using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PreGameMagnetBoosterAffector : MonoBehaviour {
	#region Init, config
	[Header("Data, config")]
	[SerializeField] BooleanVariable activeMagnetBoosterVar;
	[SerializeField] IntegerVariable takeoutPairCountVar;
	[SerializeField] ToyViewClassListVariable activeToyList;

	[Header("Config")]
	[SerializeField] GameObject magnetPrefab;
	[SerializeField] float flyInOutDuration = 1f;
	[SerializeField] float takeToyInterval = 0.1f;
	[SerializeField] float toyFlyToMagnetDuration = 0.3f;
	[SerializeField] float randomRadiusAroundMagnet = 0.5f;

	[Header("Audio")]


	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> Get call from mainFlow </summary>
	public async UniTask ProcessAffecting() {
		//// no active booster? do nothing
		//if(!activeMagnetBoosterVar.Value) {
		//	return;
		//}
		//// spawn magnet and 
		//var magnet = Instantiate(magnetPrefab);
		//var centerPos = transform.position;
		//var posForMagnet = centerPos;
		//posForMagnet.x -= 7f;
		//magnet.transform.position = posForMagnet;
		////move to center
		//LeanTween.move(magnet, centerPos, flyInOutDuration).setEase(LeanTweenType.easeOutExpo);
		//await UniTask.Delay(TimeSpan.FromSeconds(flyInOutDuration));

		//// pulling toys
		//var toyToDestroy = GetListOfToyToDestroy();
		//for(int n = 0; n < toyToDestroy.Count; n++) {
		//	var toy = toyToDestroy[n];
		//	// disable physics of toy
		//	//toy.mainCollider.enabled = false;
		//	//toy.mainRigid.useGravity = false;
		//	Destroy(toy.mainCollider);
		//	Destroy(toy.mainRigid);
		//	// move
		//	var target = centerPos + Random.insideUnitSphere * randomRadiusAroundMagnet;
		//	LeanTween.move(toy.gameObject, target, toyFlyToMagnetDuration).setEase(LeanTweenType.linear);
		//	// delay to next toy
		//	await UniTask.Delay(TimeSpan.FromSeconds(takeToyInterval));
		//}
		//// wait for last toy to come to magnet
		//await UniTask.Delay(TimeSpan.FromSeconds(toyFlyToMagnetDuration));

		//// set them all as child of magnet
		//for(int n = 0; n < toyToDestroy.Count; n++) {
		//	toyToDestroy[n].transform.SetParent(magnet.transform);
		//}

		//// move away
		//posForMagnet = centerPos;
		//posForMagnet.x += 7f;
		//LeanTween.move(magnet, posForMagnet, flyInOutDuration).setEase(LeanTweenType.easeOutExpo);
		//await UniTask.Delay(TimeSpan.FromSeconds(flyInOutDuration));
		//// destroy them
		//Destroy(magnet);
		//// finish job? don't forget to reset the magnet activate status
		//activeMagnetBoosterVar.Value = false;
	}
	#endregion//Public


	//#region Private
	//List<ToyView> GetListOfToyToDestroy() {
	//	var result = new List<ToyView>();
	//	// rare case: check if toys on scene lower than amount to take out
	//	var toyAmountToTakeOut = takeoutPairCountVar.Value * 2;
	//	if(activeToyList.Count <= toyAmountToTakeOut) {
	//		Log.Warning(false, this, $"Magnet need to take out [{toyAmountToTakeOut}], so take all current toy");
	//		result = activeToyList.Value;
	//		return result;
	//	}
	//	// simple literate in the dictionary of toys, pick 1 pair at each category,
	//	// then loop back untill we got enough toy
	//	while(result.Count < toyAmountToTakeOut) {
	//		// we take item from list at start, so if we loop more than 1 cycle, next cycle, we should skip item that already took
	//		var loopCount = 0;
	//		foreach(var item in activeToyList.ActiveToyMap) {
	//			var list = item.Value;
	//			var first = list[loopCount * 2];
	//			var second = list[loopCount * 2 + 1];
	//			result.Add(first);
	//			result.Add(second);
	//			// check break here
	//			if(result.Count >= toyAmountToTakeOut) {
	//				break;
	//			}
	//		}
	//		loopCount++;
	//	}
	//	return result;
	//}
	//#endregion//Private
}
*/