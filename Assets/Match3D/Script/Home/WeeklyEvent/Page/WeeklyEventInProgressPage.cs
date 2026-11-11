using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle content for WeeklyEvent InProgress page
/// </summary>
public class WeeklyEventInProgressPage : MonoBehaviour {
	#region Init, config
	[Header("Data")]
	[SerializeField] WeeklyEventRewardStorage rewardStorage;
	[SerializeField] UserWeeklyToyInventory userInventory;

	[Header("Refs")]
	[SerializeField] GameObject itemPrefab;
	[SerializeField] ScrollRect scrollView;
	[SerializeField] Transform rootScrollItem;
	[SerializeField] List<WeeklyEventPageScrollItem> itemList = new List<WeeklyEventPageScrollItem>();

	[Header("Config for snap")]
	[SerializeField] float itemHeigh;
	[SerializeField] float marginBetweenItem;
	[SerializeField] int offsetItemCount;

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
	}
	#endregion// Init config


	#region UI callbacks
	///<summary> Config UIPage component to call this </summary>
	public void In_OnPageStartShow() {
		if(isFailedConfig)
			return;
		// re-enable scrollview
		scrollView.enabled = true;
		ConfigContent();
	}

	///<summary> Config UIPage component to call this </summary>
	public void In_OnPageFinishHide() {
		if(isFailedConfig)
			return;
		// disable scrollview for better performance
		scrollView.enabled = false;
	}

	///<summary> Config UIPage component to call this </summary>
	public void In_OnPageFinishShow() {
		if(isFailedConfig)
			return;
		SnapToCurrentMilestone();
	}
	#endregion//UI callbacks


	#region Private
	void ConfigContent() {
		var userCurrentMilestone = userInventory.Data.currentMilestone;
		for(int n = 0; n < itemList.Count; n++) {
			var reward = rewardStorage.GetRewardDataForMilestone(n);
			var item = itemList[n];
			item.ConfigContent(n, reward);
			// state
			WeeklyEventPageScrollItem.State stateForItem = WeeklyEventPageScrollItem.State.Locked;
			if(n == userCurrentMilestone) {
				stateForItem = WeeklyEventPageScrollItem.State.Current;
			}
			else if (n > userCurrentMilestone) {
				stateForItem = WeeklyEventPageScrollItem.State.Locked;
			}
			else {
				stateForItem = WeeklyEventPageScrollItem.State.Collected;
			}
			item.ActiveContentFollowState(stateForItem);
		}
	}

	void SnapToCurrentMilestone() {
		// var targetPos
		var currentMilestone = userInventory.Data.currentMilestone;
		var targetY = (rewardStorage.TotalMilestone - currentMilestone - offsetItemCount) * (itemHeigh + marginBetweenItem);

		var rectTrans = rootScrollItem.transform as RectTransform;
		var pos = rectTrans.anchoredPosition;
		pos.y = targetY;
		rectTrans.anchoredPosition = pos;

		// this list for vertical, so affect Y coord only

		// followed this but not work
		// https://stackoverflow.com/questions/30766020/how-to-scroll-to-a-specific-element-in-scrollrect-with-unity-ui
		// snap to current milestone
		//var contentRectTrans = rootScrollItem.transform as RectTransform;
		//var currentMilestone = userInventory.Data.currentMilestone;
		//var itemToScollTo = itemList[currentMilestone].transform as RectTransform;

		//var anchorPos = scrollView.transform.InverseTransformPoint(contentRectTrans.position) - scrollView.transform.InverseTransformPoint(itemToScollTo.localPosition);
		//anchorPos.x = contentRectTrans.position.x;
		//contentRectTrans.position = anchorPos;
	}
	#endregion//Private


#if UNITY_EDITOR
	[Button]
	void GenerateItemForScrollview() {
		// clear child first
		itemList.Clear();
		var childCount = rootScrollItem.childCount;
		for(int i = childCount - 1; i >= 0; i--) {
			var child = rootScrollItem.GetChild(i);
			DestroyImmediate(child.gameObject);
		}
		// generate item
		var totalMileStone = rewardStorage.TotalMilestone;
		for(int n = 0; n < totalMileStone; n++) {
			var item = UnityEditor.PrefabUtility.InstantiatePrefab(itemPrefab, rootScrollItem) as GameObject;
			var script = item.GetComponent<WeeklyEventPageScrollItem>();
			// cause the milestone 0 item will be put at last in the list, need to set it as first sibling
			item.transform.SetAsFirstSibling();
			itemList.Add(script);
			// set a name
			item.name = $"Milestone_{n}";
		}
	}
#endif//UNITY_EDITOR
}
