using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Utils component, attach to some child of UIPage, it will look for parent UIPage, and register common events (showStart, end ...). Use thoise event to config content follow UIPage 
/// </summary>
public class UIPageEventListener : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeField] UIPage rootPage;

	[Header("--- Focus/LostFocus, by PageManager, when transition between pages")]
	///<summary> Will be call when returning from another page</summary>
	public UnityEvent OnPageFocusHandler;
	///<summary> Will be call when another page open and cover this page</summary>
	public UnityEvent OnPageLostFocusHandler;

	[Header("--- Event for Showing")]
	///<summary> Invoke right before transition elements. Use this to disable Touch on page or something</summary>
	public UnityEvent OnPageShowStartHander;
	///<summary> Invoked when all element in this page was finished Show up</summary>
	public UnityEvent OnPageShowFinishedHander;

	[Header("--- Event for Hiding")]
	///<summary> Invoke right before transition elements. Use this to disable Touch on page or something</summary>
	public UnityEvent OnPageHideStartHander;
	///<summary> Invoked when all element in this page was finished Hide out</summary>
	public UnityEvent OnPageHideFinishedHander;

	[Header("OnEanble/Disable")]
	public UnityEvent OnEnableHandler;
	public UnityEvent OnDisableHandler;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		TryCacheRootPage();
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		RegisterEventWithUIPage();
		OnEnableHandler.Invoke();
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		UnRegisterEventWithUIPage();
		OnDisableHandler.Invoke();
	}

	void TryCacheRootPage() {
		if(rootPage == null) {
			rootPage = gameObject.GetComponentInParent(typeof(UIPage), true) as UIPage;
			if(rootPage != null) {
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif//UNITY_EDITOR
			}
		}
	}
	#endregion// Init config


	#region Events
	void RegisterEventWithUIPage() {
		rootPage.OnFocusHandler.AddListener(OnPageFocus);
		rootPage.OnLostFocusHandler.AddListener(OnPageLostFocus);

		rootPage.OnShowStart.AddListener(OnPageShowStart);
		rootPage.OnShowFinshed.AddListener(OnPageShowFinish);

		rootPage.OnHideStart.AddListener(OnPageHideStart);
		rootPage.OnHideFinished.AddListener(OnPageHideFinish);
	}

	void UnRegisterEventWithUIPage() {
		// check null cause in somse case, the page was destroyed
		if(rootPage != null) {
			rootPage.OnFocusHandler.RemoveListener(OnPageFocus);
			rootPage.OnLostFocusHandler.RemoveListener(OnPageLostFocus);

			rootPage.OnShowStart.RemoveListener(OnPageShowStart);
			rootPage.OnShowFinshed.RemoveListener(OnPageShowFinish);

			rootPage.OnHideStart.RemoveListener(OnPageHideStart);
			rootPage.OnHideFinished.RemoveListener(OnPageHideFinish);
		}
	}

	void OnPageFocus() {
		OnPageFocusHandler.Invoke();
	}

	void OnPageLostFocus() {
		OnPageLostFocusHandler.Invoke();
	}

	void OnPageShowStart() {
		OnPageShowStartHander.Invoke();
	}

	void OnPageShowFinish() {
		OnPageShowFinishedHander.Invoke();
	}

	void OnPageHideStart() {
		OnPageHideStartHander.Invoke();
	}

	void OnPageHideFinish() {
		OnPageHideFinishedHander.Invoke();
	}
	#endregion//Events
}
