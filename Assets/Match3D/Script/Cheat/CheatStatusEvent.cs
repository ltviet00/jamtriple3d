using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Simply fire event when Cheat enable or not </summary>
public class CheatStatusEvent : MonoBehaviour {
	[Header("Event out")]
	[SerializeField] UnityEvent OnCheatEnable;
	[SerializeField] UnityEvent OnCheatDisable;

	private void OnEnable() {
#if CHEAT
		OnCheatEnable.Invoke();
#else
		OnCheatDisable.Invoke();
#endif
	}
}
