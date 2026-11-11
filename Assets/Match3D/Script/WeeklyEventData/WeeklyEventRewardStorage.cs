using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif//UNITY_EDITOR


/// <summary>
/// Store reward for weekly events. LevelToUnlcok event ...
/// Also save to local
/// </summary>
[CreateAssetMenu(fileName = "WeeklyEventRewardStorage", menuName = "Game/WeeklyEventReward/Storage")]
public class WeeklyEventRewardStorage : ScriptableObject {
	#region Init, config
	[Header("Config")]
	///<summary> User need to pass this level to unlock weekly event data </summary>
	[SerializeField] int levelToUnlock = 10;
	///<summary> Simply random in randomSpawnToyRange. Use for ToySpawner at start ingame </summary>
	[SerializeField] Vector2Int randomSpawnToyRange;

	///<summary> List reward for a week, often 30 items. </summary>
	[Header("Reward list")]
	[SerializeField] List<WeeklyEventRewardDefine> rewardList;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	///<summary> Level to start the weekly event </summary>
	public int LevelToUnlock => levelToUnlock;
	///<summary> How many milestone of weekly event </summary>
	public int TotalMilestone => rewardList.Count;

	///<summary> currentLevel > leveltoUnlock ? </summary>
	public bool IsEventAvailable(int currentLevel) {
		if(isFailedConfig)
			return false;
		return currentLevel >= levelToUnlock;
	}

	public WeeklyEventRewardDefine GetRewardDataForMilestone(int milestoneIndex) {
		if(isFailedConfig)
			return null;
		// clamp so it won't out of range
		if(milestoneIndex < 0 || milestoneIndex >= rewardList.Count) {
			Log.Error(false, this, $"Request milestone outofrange [{milestoneIndex}], range [{rewardList.Count}]. Clamp to range");
			milestoneIndex = Mathf.Clamp(milestoneIndex, 0, rewardList.Count);
		}
		return rewardList[milestoneIndex];
	}

	///<summary> Simply random in randomSpawnToyRange. Use for ToySpawner at start ingame </summary>
	public int GetToyAmountToSpawn() {
		var result = Random.Range(randomSpawnToyRange.x, randomSpawnToyRange.y);
		return result;
	}
	#endregion//Public


#if UNITY_EDITOR
	[Header("Editor only")]
	[SerializeField] IncreaseConfig collectRequireIncreaseConfig;

	[Button("Load all RewardDefine in same/child folder")]
	void Editor_LoadAllProductIDInSameFolder() {
		rewardList.Clear();
		// get the current directory path to retrieve sub folders
		var root = AssetDatabase.GetAssetPath(this);
		var folderPath = root.Substring(0, root.LastIndexOf("/"));
		var allProduct = AssetDatabase.FindAssets("t:WeeklyEventRewardDefine", new string[] { folderPath });

		for(int n = 0; n < allProduct.Length; n++) {
			var guid = allProduct[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var toProduct = AssetDatabase.LoadAssetAtPath<WeeklyEventRewardDefine>(toPath);
			if(toProduct == null) {
				Log.Error(false, this, $"Cant load WeeklyEventRewardDefine at path [{toPath}]");
				continue;
			}
			// save to list
			rewardList.Add(toProduct);
		}
		// finish, let asset refresh
		AssetDatabase.Refresh();
		EditorUtility.SetDirty(this);
	}

	[Button("Auto increase and fill toyRequire for reward")]
	void Editor_IncreaseToyRequire() {
		for(int n = 0; n < rewardList.Count; n++) {
			var reward = rewardList[n];
			reward.toyCollectRequire = collectRequireIncreaseConfig.GetValueAtLevel(n);
			EditorUtility.SetDirty(reward);
		}
	}

	[System.Serializable]
	class IncreaseConfig {
		public int begin;
		public int increasePerLevel;
		public int max;

		public int GetValueAtLevel(int level) {
			var result = begin + increasePerLevel * level;
			result = Mathf.Clamp(result, 0, max);
			return result;
		}
	}
#endif//UNITY_EDITOR
}
