using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Config decsciption for booster. Curerntly used for pre-booster 
/// </summary>
[CreateAssetMenu(fileName = "BoosterDescStorage", menuName = "Game/Booster/BoosterDescStorage")]
public class BoosterDescStorage : ScriptableObject {
	[SerializeField] List<Decs> decsList;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	public Decs GetDecs(BoosterID boosterID) {
		for(int n = 0; n < decsList.Count; n++) {
			var decs =	decsList[n];
			if(decs.boosterID == boosterID)
				return decs;
		}
		Log.Error(false, this, $"Not contains decs for booster [{boosterID.name}]");
		return null;
	}

	[System.Serializable]
	public class Decs {
		public BoosterID boosterID;
		public string displayName;
		public string decscription;
	}
}
