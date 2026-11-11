using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

//[RequireComponent(typeof(UIElementTransition), typeof(Canvas), typeof(GraphicRaycaster))]
[RequireComponent(typeof(UIElementTransition))]
public class UIElement : MonoBehaviour {
	#region Config
	UIElementTransition _cacheTranstion = null;
	UIElementTransition CacheTransition {
		get {
			if(_cacheTranstion == null) _cacheTranstion = GetComponent<UIElementTransition>();
			return _cacheTranstion;
		}
	}
	#endregion// Config


	#region Public
	/// <summary> Let the Transition component cache default moving transform, for doing animation later </summary>
	public void CacheDefaultTransform() {
		CacheTransition.CacheDefaultLocalTransform();
	}

	public void PrepareForShowAnimation() {
		CacheTransition.PrepareForShowAnimation();
	}

	public void PrepareForHideAnimation() {
		CacheTransition.PrepareForHideAnimation();
	}

	public void Show() {
		CheckToActiveGameObject();
		CacheTransition.Show();
		OnShowStartHandler.Invoke();
		// invoke finished event
		var showDuration = CacheTransition.GetShowDuration();
		if(showDuration > 0f) {
			LeanTween.delayedCall(showDuration, _ => OnShowFinishedHander.Invoke());
		}
		else {
			OnShowFinishedHander.Invoke();
		}
	}

	public void Hide() {
		CheckToActiveGameObject();
		CacheTransition.Hide();
		OnHideStartHandler.Invoke();
		// check to deactive gameobject
		//if(CacheTransition.IsEmptyHideTransition())
			//gameObject.SetActive(false);
			//CacheCanvas.enabled = false;
		// invoke finished event
		var hideDuration = CacheTransition.GetHideDuration();
		if(hideDuration > 0f) {
			LeanTween.delayedCall(hideDuration, _ => OnHideFinishedHander.Invoke());
		}
		else {
			OnHideFinishedHander.Invoke();
		}
	}

	public void ShowWithoutAnimation() {
		CheckToActiveGameObject();
		// two event at sametime
		//OnShowStartHandler.Invoke();
		//OnShowFinishedHander.Invoke();
	}

	public void HideWithoutAnimation() {

	}

	public float GetShowDuration() {
		return CacheTransition.GetShowDuration();
	}

	public float GetHideDuration() {
		return CacheTransition.GetHideDuration();
	}
	#endregion//Public

#if UNITY_EDITOR
	public void Editor_CacheNeededComponents() {
		// var canvas = gameObject.GetOrAddComponent<GraphicRaycaster>();
		// var raycaster = gameObject.GetOrAddComponent<GraphicRaycaster>();
		CacheDefaultTransform();
	}
#endif//UNITY_EDITOR


	#region Private
	/// <summary> We refactoring the  </summary>
	void CheckToActiveGameObject() {
		if(!gameObject.activeSelf)
			gameObject.SetActive(true);
	}
	#endregion//Private


	#region Events
	[Header("Event on Show")]
	public UnityEvent OnShowStartHandler;
	public UnityEvent OnShowFinishedHander;

	[Header("Event on Hide")]
	public UnityEvent OnHideStartHandler;
	public UnityEvent OnHideFinishedHander;
	#endregion// Events
}