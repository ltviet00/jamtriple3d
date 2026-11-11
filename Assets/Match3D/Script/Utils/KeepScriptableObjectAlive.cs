using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> ScriptableObject (SO) need "some one" to reference to it to keep it alive, cause in runtime, on device, SO are normal C# object like others.
/// And will be GC clear if no object reference to them </summary>
public class KeepScriptableObjectAlive : MonoBehaviour {
	[SerializeField] ScriptableObject[] needToAlive;

	void Awake() {
		DontDestroyOnLoad(gameObject);
	}
}
