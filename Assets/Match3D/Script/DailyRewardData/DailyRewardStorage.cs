using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Contains list daily reward, often 7 items
/// </summary>
[CreateAssetMenu(fileName = "DailyRewardStorage", menuName = "Game/DailyReward/Storage")]
public class DailyRewardStorage : ScriptableObject {
	///<summary> List reward for a week, 7 items. </summary>
	[Header("Reward list")]
	[SerializeField] List<DailyRewardDefine> rewardList;

	///<summary> Day index start from 0, often 0-6 </summary>
	public DailyRewardDefine GetRewardForDay(int dayIndex) {
		// carefully claim
		if(dayIndex < 0 || dayIndex >= rewardList.Count) {
			dayIndex = Mathf.Clamp(dayIndex, 0, rewardList.Count - 1);
			Log.Warning(false, this, $"request value [{dayIndex}] outof range [{rewardList.Count}], clamped");
		}
		return rewardList[dayIndex];
	}

#if UNITY_EDITOR
	[Button("Load all RewardDefine in same/child folder")]
	void Editor_LoadAllProductIDInSameFolder() {
		rewardList.Clear();
		// get the current directory path to retrieve sub folders
		var root = AssetDatabase.GetAssetPath(this);
		var folderPath = root.Substring(0, root.LastIndexOf("/"));
		var allProduct = AssetDatabase.FindAssets("t:DailyRewardDefine", new string[] { folderPath });

		for(int n = 0; n < allProduct.Length; n++) {
			var guid = allProduct[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var toReward = AssetDatabase.LoadAssetAtPath<DailyRewardDefine>(toPath);
			if(toReward == null) {
				Log.Error(false, this, $"Cant load DailyRewardDefine at path [{toPath}]");
				continue;
			}
			// save to list
			rewardList.Add(toReward);
		}
		// finish, let asset refresh
		AssetDatabase.Refresh();
		EditorUtility.SetDirty(this);
	}
#endif//UNITY_EDITOR
}
