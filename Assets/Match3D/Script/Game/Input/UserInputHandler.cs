using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

///<summary> Watch output from InputPanel, handle Behavior when receive input.
/// Take care dragging toy function </summary>
public class UserInputHandler : MonoBehaviour {
	#region Init, config
	[Header("ShareData - in")]
	[SerializeField] Vector3Variable pointerUpPositionVar;
	[SerializeField] GameObjectVariable userTouchHitVar;

	[Header("ShareData - Out")]
	[SerializeField] ToyViewVariable userPickToyVar;

	[Header("Config")]
	[SerializeField] FloatVariable pullPickToyUpDitanceCfg;
	[SerializeField] float pullingToyDuration = 0.2f;

	[Header("Hightlight config")]
	[SerializeField] Color hightlightColor = Color.green;
	[SerializeField] float hightlightThick = 5f;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	private void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	private void OnEnable() {
		if(isFailedConfig)
			return;
		userTouchHitVar.OnValueChangedHandler += OnUserTouchHitObject;
		pointerUpPositionVar.OnValueChangedHandler += OnUserReleasePointer;
	}

	private void OnDisable() {
		if(isFailedConfig)
			return;
		userTouchHitVar.OnValueChangedHandler -= OnUserTouchHitObject;
		pointerUpPositionVar.OnValueChangedHandler -= OnUserReleasePointer;
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Private - event in
	void OnUserTouchHitObject(GameObject hit) {
		// hit object: check do highligh touch object
		if(hit != null) {
			//Log.Info($"Hit [{hit.name}]");
			var toy = hit.GetComponent<ToyView>();
			if(toy == null) {
				Log.Warning(false, hit, "Not found ToyView in the hit object");
				return;
			}
			//Carefully check: same to current toy? then ignore
			if(IsSameTouchingToy(toy)) {
				return;
			}
			// if we are pulling any toy? then release it
			ClearRunningTasks();
			if(_touchingToy != null) {
				ReleasePullingToy(_touchingToy);
				UnHighlightTouchToy(_touchingToy);
			}
			//and pull the new toy up
			_touchingToy = toy;
			HighlighTouchToy(toy);
			PullUpTheHitToy(toy).Forget();
		}
		// reset highligh object and release current touching toy
		else {
			// carefully check if touching toy is null
			if(_touchingToy == null) {
				return;
			}
			UnHighlightTouchToy(_touchingToy);
			ReleasePullingToy(_touchingToy);
			// clear running task if we're still pulling any toy
			ClearRunningTasks();
			_touchingToy = null;
		}
	}

	void OnUserReleasePointer() {
		// if the touch hit var not null, then we'll pick this toy
		if(userTouchHitVar.IsValueNotNull() && _touchingToy != null) {
			//Log.Info($"Pick toy [{_touchingToy.name}]");
			ClearRunningTasks();
			// clear hight light
			UnHighlightTouchToy(_touchingToy);
			// save to shareVar before reset
			userPickToyVar.Value = _touchingToy;
			_touchingToy = null;
		}
	}
	#endregion//Private - event in


	#region Private
	///<summary> The toy that user is touching </summary>
	ToyView _touchingToy;

	bool IsSameTouchingToy(ToyView toy) {
		return _touchingToy != null && _touchingToy == toy;
	}

	void HighlighTouchToy(ToyView toy) {
		toy.ActiveOutline(hightlightColor, hightlightThick);
	}

	void UnHighlightTouchToy(ToyView toy) {
		toy.DeactiveOutline();
	}

	async UniTask PullUpTheHitToy(ToyView toy) {
		var currentToyPos = toy.transform.position;
		// notes: pull up in this case is following Z-coord (turn Editor and see for your self)
		var targetToyPos = currentToyPos;
		targetToyPos.z -= pullPickToyUpDitanceCfg.Value;
		// pulling
		var timer = 0f;
		while(timer < pullingToyDuration) {
			var pos = Vector3.Lerp(currentToyPos, targetToyPos, timer / pullingToyDuration);
			// use Rigidbody like this, if not, toys will stick together
			toy.mainRigid.MovePosition(pos);
			timer += Time.deltaTime;
			// use cancel token for release toy later
			await UniTask.Yield(cancellationToken: _token.Token);
		}
		// finish, head to target, disable the rigid to keep is stand still "in the air"
		toy.mainRigid.MovePosition(targetToyPos);
		toy.mainRigid.isKinematic = true;
	}

	void ReleasePullingToy(ToyView toy) {
		toy.mainRigid.isKinematic = false;
	}

	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private
}
