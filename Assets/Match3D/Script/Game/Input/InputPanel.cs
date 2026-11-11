using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

///<summary> Attach in canvas, check input from user, do raycasting to see if it hit any tile </summary>
public class InputPanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerUpHandler {
	#region Init, config
	[Header("Config")]
	[SerializeField] LayerMaskVariable targetCheckLayerVar;
	[SerializeField] CameraVariable viewCameraVar;

	[Header("ShareData")]
	[SerializeField] GameObjectVariable hitObjectVar;
	//[SerializeField] Vector3Variable draggingPositionVar;
	[SerializeField] Vector3Variable pointerUpPositionVar;
	//[SerializeField] Vector3Variable endDragPositionVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	private void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	private void OnEnable() {
		if(isFailedConfig)
			return;
	}

	private void OnDisable() {
		if(isFailedConfig)
			return;
		// reset vars
		hitObjectVar.Value = null;
	}
	#endregion// Init config


	#region Events
	public void OnPointerDown(PointerEventData eventData) {
		if(isFailedConfig)
			return;
		var screenToWorldPos = ScreenPointToWorldPosition(eventData.position);
		var hitTarget = FindHitTarget(screenToWorldPos);
		if(hitTarget != null) {
			//Log.Info(hitTarget, $"Hit [{hitTarget.name}]");
			hitObjectVar.Value = hitTarget.gameObject;
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if(isFailedConfig)
			return;
		var hitTarget = FindHitObjectAtPointerPosition(eventData.position);
		if(hitTarget != null) {
			//Log.Info(hitTarget, $"Hit [{hitTarget.name}]");
			// only assign new value if hit object was different from the last hit one
			if(hitTarget != hitObjectVar.Value) {
				hitObjectVar.Value = hitTarget.gameObject;
			}
		}
		// null, set to var to let input handler do resetting task.
		// if it already null, then do not call null again, to ignore reduntdant call
		else {
			if(hitObjectVar.IsValueNotNull())
				hitObjectVar.Value = null;
		}
	}

	public void OnEndDrag(PointerEventData eventData) {
		if(isFailedConfig)
			return;
		//var screenToWorldPos = ScreenPointToWorldPosition(eventData.position);
		//endDragPositionVar.Value = screenToWorldPos;
	}

	public void OnPointerUp(PointerEventData eventData) {
		if(isFailedConfig)
			return;
		// write to pointer up as "raise event". Dont need to write to hitObjectVar. cause it already get done in OnDrag()
		var screenToWorldPos = ScreenPointToWorldPosition(eventData.position);
		pointerUpPositionVar.Value = screenToWorldPos;
	}
	#endregion//Events


	#region Private
	GameObject FindHitObjectAtPointerPosition(Vector2 pointerPos) {
		var screenToWorldPos = ScreenPointToWorldPosition(pointerPos);
		var hitTarget = FindHitTarget(screenToWorldPos);
		if(hitTarget != null) {
			return hitTarget.gameObject;
		}
		return null;
	}

	Vector3 ScreenPointToWorldPosition(Vector2 screenPos) {
		var camera = viewCameraVar.Value;
		if(camera == null) {
			Log.Warning(false, this, "Null camera in variable");
			return Vector3.zero;
		}
		return camera.ScreenToWorldPoint(screenPos);
	}

	Collider FindHitTarget(Vector3 worldPos) {
		/// we now in 3D game, use camera2D (orthorgraphic), so make a ray from worldPos but with 
		/// viewCamera.z pos, and head to Z coord as well (vector3.forward)
		var viewCamera = viewCameraVar.Value;
		if(viewCamera == null) {
			Log.Warning(false, this, $"Null camera in variable");
			return null;
		}
		var castFrom = worldPos;
		castFrom.z = viewCamera.transform.position.z;
		if(Physics.Raycast(castFrom, Vector3.forward, out RaycastHit hit, Mathf.Infinity, targetCheckLayerVar.Value.value)) {
			return hit.collider;
		}
		return null;
	}
	#endregion//Private
}
