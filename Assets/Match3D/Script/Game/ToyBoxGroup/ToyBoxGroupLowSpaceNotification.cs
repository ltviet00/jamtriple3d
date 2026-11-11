using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Work with ToyBoxGroup, control the notification area when toy nearly full the boxes
/// </summary>
public class ToyBoxGroupLowSpaceNotification : MonoBehaviour
{
	#region Init, config
	[Header("Refs")]
	[SerializeField] ToyBoxGroup toyBoxGroup;
	[SerializeField] Transform rootNotification;

	[Header("ShareData")]
	[SerializeField] ToyViewClassListVariable activeToyInBoxList;

	[Header("Config")]
	[SerializeField] Transform moveFromTrans;
	[SerializeField] Transform moveToTrans;
	[SerializeField] float moveDuration = 0.3f;
	[SerializeField] float idleDuration = 1.5f;
	[SerializeField] LeanTweenType moveTweenType;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		toyBoxGroup.OnInsertToyToBoxHandler += OnInseartNewToyToBox;
		// default is deactive the notification area
		rootNotification.gameObject.SetActive(false);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		toyBoxGroup.OnInsertToyToBoxHandler -= OnInseartNewToyToBox;
	}

	void OnInseartNewToyToBox() {
		// we're showing the notification, skip this time
		if(_showNotificationTask.Status == UniTaskStatus.Pending) {
			return;
		}
		// check if we on lowSpace : 1 more toy to full
		if(activeToyInBoxList.CountNotNullItems() == toyBoxGroup.Capacity - 1) {
			_showNotificationTask = OpenNotificationAsync();
		}
	}
	#endregion// Init config


	#region Private
	///<summary> Cache the notification task, to know if we're running it </summary>
	UniTask _showNotificationTask;

	async UniTask OpenNotificationAsync() {
		// set to moveFrom position, active it
		rootNotification.gameObject.SetActive(true);
		rootNotification.position = moveFromTrans.position;

		// move to MoveTo position
		LeanTween.move(rootNotification.gameObject, moveToTrans.position, moveDuration).setEase(moveTweenType);

		// wait for move duration, then Idle there a bit
		await UniTask.Delay(TimeSpan.FromSeconds(moveDuration + idleDuration), cancellationToken: _cancelToken.Token);

		// move back to moveFrom position, 
		LeanTween.move(rootNotification.gameObject, moveFromTrans.position, moveDuration).setEase(moveTweenType);

		// wait for moving duration and deactive it
		await UniTask.Delay(TimeSpan.FromSeconds(moveDuration), cancellationToken: _cancelToken.Token);
		rootNotification.gameObject.SetActive(false);
	}
	#endregion//Priate


	#region Cancel token
	UniTaskTokenHelper _cancelToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_cancelToken.ClearRunningTasks();
	}
	#endregion//Cancel token
}
