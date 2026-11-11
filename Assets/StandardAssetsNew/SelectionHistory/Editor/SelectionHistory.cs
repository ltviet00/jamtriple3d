using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds Back and Forward items to the Edit > Selection menu to navigate between Hierarchy and Project pane selections.
/// </summary>
[InitializeOnLoad]
static class SelectionHistoryNavigator {
	#region Init
	///<summary> Main navigation history </summary>
	static LinkedList<Object> history = new LinkedList<Object>();
	///<summary> To track the node that we're navigating </summary>
	static LinkedListNode<Object> currentNodeInHistory;
	const int MAX_SIZE_HISTORY = 30;

	static SelectionHistoryNavigator() {
		// main delegate from Selection, use this to track Editor selection changed
		Selection.selectionChanged += SelectionChangedHandler;
		history.Clear();
	}

	static void SelectionChangedHandler() {
		// if user touch on nowhere, the active object  will be null, ignore this cas
		if(Selection.activeObject == null) {
			return;
		}
		// first select
		if(history.Count == 0) {
			history.AddLast(Selection.activeObject);
			currentNodeInHistory = history.Last;
			return;
		}
		// same current select? then ignore (also used for case user backward)
		if(Selection.activeObject == currentNodeInHistory.Value) {
			return;
		}
		// if user select object that not same to next one in history, then we need to clear history upfront
		if(currentNodeInHistory.Next != null && Selection.activeObject != currentNodeInHistory.Next.Value) {
			ClearForwardHistory(currentNodeInHistory);
			// add new one to the history
			history.AddLast(Selection.activeObject);
			currentNodeInHistory = history.Last;
			// check clear history when add new one
			CheckClearBackwardHistory();
			return;
		}
		// if we not in the history, then simply add new node to history
		if(currentNodeInHistory.Next == null) {
			history.AddLast(Selection.activeObject);
			currentNodeInHistory = history.Last;
			// check clear history when add new one
			CheckClearBackwardHistory();
			return;
		}
	}

	static void ClearForwardHistory(LinkedListNode<Object> fromNode) {
		while(fromNode.Next != null) {
			history.RemoveLast();
		}
	}

	static void CheckClearBackwardHistory() {
		while(history.Count > MAX_SIZE_HISTORY) {
			history.RemoveFirst();
		}
	}
	#endregion//Init


	#region Navigation
	const string BACK_MENU_LABEL = "Edit/Selection/Back &[";
	const string FORWARD_MENU_LABEL = "Edit/Selection/Forward &]";

	[MenuItem(BACK_MENU_LABEL)]
	static void Back() {
		// hightlight object in history, pick the previous of currentNodeInHistory
		if(currentNodeInHistory != null && currentNodeInHistory.Previous != null) {
			// highlight active object
			var targetSelect = currentNodeInHistory.Previous.Value;
			Selection.activeObject = targetSelect;
			// also move out "current node" backward
			currentNodeInHistory = currentNodeInHistory.Previous;
		}
	}

	[MenuItem(FORWARD_MENU_LABEL)]
	static void Forward() {
		if(currentNodeInHistory != null && currentNodeInHistory.Next != null) {
			// highlight active object
			var targetSelect = currentNodeInHistory.Next.Value;
			Selection.activeObject = targetSelect;
			// also move out "current node" backward
			currentNodeInHistory = currentNodeInHistory.Next;
		}
	}
	#endregion//Navigation
}
