using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to Toy object on scene
/// </summary>
public class ToyView : MonoBehaviour {
	#region Init, config
	[Header("Tag, Labels")]
	///<summary> Use for filtering, grouping toy later </summary>
	public ToyGroupID groupID;

	[Header("Refs")]
	public Rigidbody mainRigid;
	public Collider mainCollider;
	public MeshRenderer mainRenderer;

	[Header("ShareData")]
	/////<summary> Will remove from this when destroy </summary>
	//public ToyViewClassListVariable activeToyListVar;
	///<summary> Otfen itself, will be set by spawner, use to group in activeToyLIst </summary>
	public GameObject rootPrefab;

	[Header("VFX")]
	///<summary> Variable, store refs to explode VFX at toyCatcher </summary>
	public GameObjectVariable explodeVfxVar;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		CacheComponents();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void CacheComponents() {
		//if(toyTag == ToyTags.UnCategory) {
		//	toyTag = tag;
		//	UnityEditor.EditorUtility.SetDirty(gameObject);
		//}
		if(mainRigid == null) {
			mainRigid = GetComponent<Rigidbody>();
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
		}
		if(mainCollider == null) {
			mainCollider = GetComponent<Collider>();
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
		}
		if(mainRenderer == null) {
			mainRenderer = GetComponent<MeshRenderer>();
			if(mainRenderer == null) {
				mainRenderer = GetComponentInChildren<MeshRenderer>();
			}
			// still null ?!?!
			if(mainRenderer == null) {
				Log.Error(false, this, $"Not found any render in this prefab");
			}
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
		}
		// by default, this validate will be call in editor only, so on prefab,
		// then the rootPrefab is itself
		if(rootPrefab == null) {
			rootPrefab = gameObject;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}

	void Start() {
		if(isFailedConfig)
			return;
		//activeToyListVar.Add(this);
	}

	void OnDestroy() {
		if(isFailedConfig)
			return;
		//activeToyListVar.Remove(this);
		OnDestroyHandler?.Invoke();
		ClearRunningTasks();
	}

	//void OnDisable() {
	//	if(isFailedConfig)
	//		return;
	//}
	#endregion// Init config


	#region Public
	///<summary> Raised when toy destroy, some VFX need this </summary>
	public event Action OnDestroyHandler;

	public void SetActivePhysics(bool active) {
		mainCollider.enabled = active;
		mainRigid.isKinematic = !active;
		mainRigid.detectCollisions = active;
	}

	public void SetActiveCollider(bool active) {
		if(isFailedConfig)
			return;
		mainCollider.enabled = active;
	}

	public void AutoRotate(Vector3 rotateSpeed) {
		if(isFailedConfig)
			return;
		var rotator = this.GetOrAddComponent<AutoRotateUniRx>();
		rotator.rotateSpeed = rotateSpeed;
	}

	public void DisableAutoRotate() {
		if(isFailedConfig)
			return;
		this.RemoveComponent<AutoRotateUniRx>();
	}

	public void ActiveOutline(Color color, float thick) {
		if(isFailedConfig)
			return;
		// clear running tasks first. Already use token in Outline async
		_disableToken.ClearRunningTasks();
		ActiveOutlineAsync(color, thick).Forget();
	}

	public void DeactiveOutline() {
		if(isFailedConfig)
			return;
		// clear running tasks first. Already use token in Outline async
		_disableToken.ClearRunningTasks();
		DeactiveOutlineAsync().Forget();
	}
	#endregion//Public


	#region Private - Outline
	async UniTask ActiveOutlineAsync(Color color, float thick) {
		const float INCREASE_OUTLINE_DURATION = 0.1f;
		var outliner = this.GetOrAddComponent<Outline>();
		// default setting, thick = 0, increasing effect below
		outliner.OutlineColor = color;
		outliner.OutlineMode = Outline.Mode.OutlineVisible;
		outliner.OutlineWidth = 0f;

		var timer = 0f;
		while(timer < INCREASE_OUTLINE_DURATION) {
			await UniTask.Yield(PlayerLoopTiming.Update, _disableToken.Token);
			timer += Time.deltaTime;
			var targetThick = Mathf.Lerp(0, thick, timer / INCREASE_OUTLINE_DURATION);
			outliner.OutlineWidth = targetThick;
		}
		outliner.OutlineWidth = thick;
	}

	async UniTask DeactiveOutlineAsync() {
		const float DECREASE_OUTLINE_DURATION = 0.1f;
		var outliner = this.GetComponent<Outline>();
		// not found ? mean we are not activating outline effect yet, nothing to do here
		if(outliner == null) {
			return;
		}
		// decrease effect
		var timer = DECREASE_OUTLINE_DURATION;
		var currentThick = outliner.OutlineWidth;
		while(timer > 0f) {
			await UniTask.Yield(PlayerLoopTiming.Update, _disableToken.Token);
			timer -= Time.deltaTime;
			var targetThick = Mathf.Lerp(0f, currentThick, timer / DECREASE_OUTLINE_DURATION);
			outliner.OutlineWidth = targetThick;
		}
		// also destroy outliner component, nolonger need
		Destroy(outliner);
	}


	UniTaskTokenHelper _disableToken = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_disableToken.ClearRunningTasks();
	}
	#endregion//Private - Outline
}
