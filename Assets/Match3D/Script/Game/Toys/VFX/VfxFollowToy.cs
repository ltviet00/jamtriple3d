using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For hint object, we need the VFX to follow the toy until it destroy
/// </summary>
public class VfxFollowToy : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	[SerializeField] Vector3 offsetToTarget;

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
		ClearCallbackByTarget();
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Pubilc
	public void FollowToy(ToyView toy) {
		if(isFailedConfig)
			return;
		_target = toy;
		_target.OnDestroyHandler += OnTargetDestroy;
	}
	#endregion//Public


	#region Private
	ToyView _target;

	void Update() {
		if(_target == null)
			return;
		// simple hard follow
		var pos = _target.transform.position;
		pos += offsetToTarget;
		transform.position = pos;
	}

	void OnTargetDestroy() {
		ClearCallbackByTarget();
		// we will add VFX in Pool, so just need to disable it to recycle. In case they all destroy, check null here
		if(gameObject != null) {
			gameObject.SetActive(false);
		}
	}

	void ClearCallbackByTarget() {
		if(_target != null) {
			_target.OnDestroyHandler -= OnTargetDestroy;
		}
	}

	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private
}
