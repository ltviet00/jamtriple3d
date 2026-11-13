using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using System;
using UnityEngine.Events;

/// <summary>
/// Control group of UIPage object. Often put it in the mainCanvas, it need to hold reference to all sub-pages.
/// Receive request OpenPage, ClosePage ... then open, close page in sub-pages follow pageName ...
/// </summary>
public class UIPageController : MonoBehaviour {
	#region Init
	[Header("PageList, config")]
	/// <summary> The list of all sub-page. Will be cache via Editor-CacheAllUIPage() </summary>
	[SerializeField] List<UIPage> pageList;
	/// <summary> Use for debugging, set to True, it will log message for every Request (openPage, close page ...).
	/// Better set it to false in Release build for better performance. </summary>
	[SerializeField] bool logOpenClosePageProcess = false;

	[Header("Events")]
	/// <summary> Raise when Init() was called on all page.
	/// Maybe use it to open the first page of the scene </summary>
	[SerializeField] UnityEvent OnInitAllPageHandler;

	void Awake() {
		// wait some frame, make sure all action relate to UserData has done.
		// cause we will call pages.Init, they may do some action need userData
		//Observable.TimerFrame(2).Subscribe(_ => ConfigReferenceForPageListAndInitPages());
		ConfigReferenceForPageListAndInitPages();
	}

	void ConfigReferenceForPageListAndInitPages() {
		// set reference
		for(int m = 0; m < pageList.Count; m++) {
			pageList[m].controller = this;
		}
		/// call init. Why cost one more for loop? dont do it on above for loop?
		/// This to make sure all page are hold manager reference, before Init
		/// was called.
		for(int m = 0; m < pageList.Count; m++) {
			pageList[m].InitOnControllerAwake();
		}
		// event at last
		OnInitAllPageHandler.Invoke();
	}
	#endregion// Init


#if UNITY_EDITOR
	void CheckToCacheChildPagesAndSameRootPage() {
		if(pageList == null || (pageList != null && pageList.Count == 0)) {
			Editor_CacheAllUIPageInFirstLevelChild();
			Editor_CacheUIPageInSameRoot();
		}
	}

	/// <summary> Call via Editor script, to cache all UIPage component.
	/// Notes : currently, we cache only UIPage in first-level-child, the UIPage in deep child level
	/// wont be cached (some cases, we will need another UIPageController in deep child, let them control
	/// their child UIPage) </summary>
	public void Editor_CacheAllUIPageInFirstLevelChild() {
		pageList = new List<UIPage>();
		for(int childIndex = 0, childCount = transform.childCount; childIndex < childCount; childIndex++) {
			var child = transform.GetChild(childIndex);
			var pageComponent = child.GetComponent<UIPage>();
			if(pageComponent != null)
				pageList.Add(pageComponent);
		}
	}

	public void Editor_LetAllPageCacheNeedComponentsForChild() {
		for(int n = 0; n < pageList.Count; n++) {
			pageList[n].Editor_CacheNeededComponentForElement();
		}
	}

	public void Editor_CacheUIPageInSameRoot() {
		var parent = transform.parent;
		if(parent == null)
			return;
		for(int n = 0, amount = parent.childCount; n < amount; n++) {
			var sameRoot = parent.GetChild(n);
			// its me, dont need to check
			if(sameRoot == transform)
				continue;
			var pageComponent = sameRoot.gameObject.GetComponent<UIPage>();
			if(pageComponent != null)
				pageList.Add(pageComponent);
		}
	}

	public void Editor_DeactiveAllPages() {
		CheckToCacheChildPagesAndSameRootPage();
		for(int pageIndex = 0; pageIndex < pageList.Count; pageIndex++) {
			pageList[pageIndex].Editor_DeactiveAllChilds();
		}
	}

	public void Editor_ActiveAllPages() {
		CheckToCacheChildPagesAndSameRootPage();
		for(int pageIndex = 0; pageIndex < pageList.Count; pageIndex++) {
			pageList[pageIndex].Editor_ActiveAllChilds();
		}
	}
#endif //UNITY_EDITOR


	#region Public
	/// <summary> Keep the current open page, call LostFocus on currentPage,
	/// and open the new page with input pageName </summary>
	//public void RequestKeepCurrentAndOpenNewPage(string pageName) {
	public void RequestKeepCurrentAndOpenNewPage(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "KeepCurrentAndOpenNewPage, with empty pageName", this);
			return;
		}
		ValidatePageExistInPagelist(page);
		if(OpenedPageCount > 0) {
			// check if the request page already showup
			if(ContainInOpenPageList(page)) {
				Log.Warning(false, this, "KeepCurrentAndOpenNewPage, Page [{0}] already showup", page);
				return;
			}
			if(LastOpenPage == page) {
				Log.Warning(false, this, $"KeepCurrentAndOPenNewPage, but newPage [{page.name}] is also the current");
				return;
			}
			// call Lost focus on current top page
			else {
				var currentTopPage = LastOpenPage;
				currentTopPage.OnLostFocus();
			}
		}

		// push to stack and Show
		page.Show();
		AddToOpenPageListAtLast(page);
		// let the new page on top
		page.transform.SetAsLastSibling();
		// logging
		const string FORMAT = "KeepCurrentAndOpenNewPage : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}


	/// <summary> Close the top open page (currentPage) and open the new page (with pageName) </summary>
	public void RequestCloseCurrentAndOpenNewPage(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "CloseCurrentAndOpenNewPage, with empty pageName", this);
			return;
		}
		ValidatePageExistInPagelist(page);
		// if request close and open same page, then do nothing
		if(LastOpenPage == page) {
			Log.Warning(false, this, $"RequestClose and open same page [{page.name} !!]");
			return;
		}
		// if it already in openPageList, then ignore
		if(ContainInOpenPageList(page)) {
			Log.Warning(false, this, $"RequestCloseAndOpen but page [{page.name}] already opened. Is it behind others?");
			return;
		}

		// close current page, popup it out of the openPageList too
		UIPage topPage = null;
		if(OpenedPageCount > 0) {
			topPage = LastOpenPage;
			topPage.Hide();
			RemoveLastOpenPage();
		}
		else {
			Log.Warning(false, this, "OpenPageStack is empty but request CloseCurrentAndOpenNewPage");
		}
		// let the new page on top render, also add it to last in openPageStack
		page.transform.SetAsLastSibling();
		AddToOpenPageListAtLast(page);
		// if currentPage = null, then just simple show new page
		if(topPage == null) {
			page.Show();
		}
		// else, wait a little for currentPage to Hide, then show newPage
		else {
			Observable.Timer(TimeSpan.FromSeconds(topPage.HideDuration)).Subscribe(_ => page.Show()).AddTo(_destroyDispose);
		}
		// logging
		const string FORMAT = "CloseCurrentAndOpenNewPage : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}


	/// <summary> Close the top page, call OnFocus on previous page (if any) </summary>
	public void RequestCloseCurrentPageOnly(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "CloseCurrentPageOnly, with null page", this);
			return;
		}
		ValidatePageExistInPagelist(page);
		if(OpenedPageCount == 0) {
			Log.Warning(false, this, "CloseCurrentPageOnly, but openStack is empty");
			return;
		}

		var topPage = LastOpenPage;
		if(topPage != page) {
			Log.Warning(false, this, "CloseCurrentPageOnly [{0}], but it not the current topPage", page.name);
			return;
		}
		// close the top page, pop it out of stack
		topPage.Hide();
		RemoveLastOpenPage();
		// call OnFocus for preivous page
		if(OpenedPageCount > 0) {
			var previousPage = LastOpenPage;
			var currentPageHideTime = topPage.HideDuration;
			Observable.Timer(TimeSpan.FromSeconds(currentPageHideTime)).Subscribe(_ => previousPage.OnFocus()).AddTo(_destroyDispose);
		}
		// logging
		const string FORMAT = "RequestCloseCurrentPageOnly : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}


	/// <summary> Close the page event if it not on top </summary>
	public void RequestForceLosePage(UIPage page) {
		if(page == null) {
			Log.Warning(false, this, "ForceLosePage, with null page", this);
			return;
		}
		ValidatePageExistInPagelist(page);

		if(OpenedPageCount == 0) {
			Log.Warning(false, this, "ForceLosePage, but openStack is empty");
			return;
		}
		// if the request close page also the top page, then this is the same case with RequestCloseCurrentPageOnly
		if(page == LastOpenPage) {
			page.Hide();
			RemoveLastOpenPage();
			// empty ? then quit
			if(OpenedPageCount == 0) {
				return;
			}
			// call Focus for previous page
			var previousPage = LastOpenPage;
			var currentPageHideTime = page.HideDuration;
			Observable.Timer(TimeSpan.FromSeconds(currentPageHideTime)).Subscribe(_ => previousPage.OnFocus()).AddTo(_destroyDispose);
			return;
		}
		// requestClosePage in the middle of stack, then close it, no OnFocus will be call on any page
		if(ContainInOpenPageList(page)) {
			RemoveFromOpenPageList(page);
			page.Hide();
		}
		else {
			Log.Warning(false, this, "RequestForceLosePage [{0}], but not contain in OpenPageList", page);
			return;
		}
		// logging
		const string FORMAT = "RequestForceLosePage : {0}";
		LogOpenClosePageProcessToConsole(FORMAT, page.name);
	}
	#endregion//Public


	#region Private
	bool IsExistInPageList(UIPage page) {
		for(int i = 0; i < pageList.Count; i++) {
			if(pageList[i] == page)
				return true;
		}
		return false;
	}

	void ValidatePageExistInPagelist(UIPage page) {
		if(!IsExistInPageList(page)) {
			Log.Warning(false, this, "CloseCurrentAndOpenNewPage, page [{0}] not exist in pageList. Its ok, but the Init() on that page wont be invoked", page.name);
		}
	}

	void LogOpenClosePageProcessToConsole(string format, string para) {
		if(logOpenClosePageProcess) {
			Log.Info(this, format, para);
		}
	}

	CompositeDisposable _destroyDispose = new CompositeDisposable();
	void OnDestroy() {
		_destroyDispose.Clear();
	}
	#endregion//Private


	#region Private - openPageList
	///<summary> Store opened pages. Use this to call OnFocus, LoseFocus on each page.
	/// Show rule is : last-open-first-close </summary>
	List<UIPage> openPageList = new List<UIPage>(20);

	int OpenedPageCount => openPageList.Count;
	UIPage LastOpenPage => openPageList[openPageList.Count - 1];
	///<summary> Page on top of the open page stack, null if no page open </summary>
	public UIPage TopOpenPage => openPageList != null && openPageList.Count > 0 ? LastOpenPage : null;

	void AddToOpenPageListAtLast(UIPage page) => openPageList.Add(page);
	bool ContainInOpenPageList(UIPage page) => openPageList.Contains(page);
	void RemoveLastOpenPage() => openPageList.RemoveAt(OpenedPageCount - 1);
	void RemoveFromOpenPageList(UIPage page) => openPageList.Remove(page);
	#endregion//Private - openPageList
}