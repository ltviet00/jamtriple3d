using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///<summary> Simply fire event when Cheat enable or not </summary>
public class LogStatusEvent : MonoBehaviour {
	[Header("Event out")]
	[SerializeField] UnityEvent OnLogEnable;
	[SerializeField] UnityEvent OnLogDisable;

	private void OnEnable() {
#if CHEAT
		OnLogEnable.Invoke();
#else
		OnLogDisable.Invoke();
#endif
	}
}
