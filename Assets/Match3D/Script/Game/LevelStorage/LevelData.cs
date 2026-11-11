using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define dificult for a level
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Match3D/LevelData")]
public class LevelData : ScriptableObject {
	[Header("Total toy")]
	public TotalAndType totalInfo;

	[Header("Target")]
	public TotalAndType targetInfo;

	[Header("Support toys")]
	public List<SupportToyDefine> supportToyList = new List<SupportToyDefine>();

	[Header("calculated")]
	public int levelDuration;

	///<summary> Total toy - targetToy (triad, so need to x3 to get the final toy) </summary>
	public int GetNumberOfDecoyToyTriadToSpawn() {
		return totalInfo.totalTriad - targetInfo.totalTriad;
	}

	public int GetAmountOfSupportToy(SupportToyID id) {
		var result = 0;
		for(int n = 0; n < supportToyList.Count; n++) {
			if(supportToyList[n].id == id)
				result += supportToyList[n].amount;
		}
		return result;
	}

	[System.Serializable]
	public class TotalAndType {
		///<summary> Total toy triad count (now Update follow triple game, 5 triad = 5x3 = 15 toy amount) </summary>
		public int totalTriad;
		///<summary> Type count used for (eg: 100 toys, and only Gun, Fruit, Table) then typeUsed = 3 </summary>
		public int typeUsed;
	}

	[System.Serializable]
	public class SupportToyDefine {
		public SupportToyID id;
		public int amount;
	}
}
