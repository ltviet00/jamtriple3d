using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepScriptableObjectAlive : MonoSingleton<KeepScriptableObjectAlive> {
	[SerializeField] ScriptableObject[] needAliveSOList;

	protected override void DoOnStart() {
		DontDestroyOnLoad(gameObject);
	}
}
