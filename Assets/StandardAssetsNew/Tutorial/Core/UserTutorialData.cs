using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Store info about tutorial progress of user
/// </summary>
[CreateAssetMenu(fileName = "UserTutorialData", menuName = "Game/User/UserTutorialData")]
public class UserTutorialData : BaseUserDataSO<UserTutorialData.SaveForm> {
	#region Public
	///<summary> Check if task with ID contain in doneTasks list </summary>
	public bool IsTaskDone(string taskID) {
		return Data.doneTasks.Contains(taskID);
	}

	public void AddToDoneTask(string taskID) {
		if(Data.doneTasks.Contains(taskID)) {
			Log.Error(false, this, $"Already done task [{taskID}]");
			return;
		}
		Data.doneTasks.Add(taskID);
		SaveToDisk();
	}
	#endregion//Public


	#region Classes
	[System.Serializable]
	public class SaveForm {
		public List<string> doneTasks = new List<string>();
	}
	#endregion//Classes
}
