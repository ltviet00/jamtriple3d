using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeeklyEventRewardIconMapper", menuName = "Game/WeeklyEventReward/IconMapper")]
public class WeeklyEventRewardIconMapper : ScriptableObject {
	[Header("Data")]
	[SerializeField] List<IconMapper> iconList;

	public Sprite GetIcon(WeeklyEventRewardDefine.RewardType rewardType) {
		for(int n = 0; n < iconList.Count; n++) {
			var icon = iconList[n];
			if(icon.reward == rewardType)
				return icon.icon;
		}
		Log.Warning(false, this, $"Not found icon for rewardType [{rewardType}]");
		return null;
	}

	[System.Serializable]
	public class IconMapper {
		public WeeklyEventRewardDefine.RewardType reward;
		public Sprite icon;
	}
}
