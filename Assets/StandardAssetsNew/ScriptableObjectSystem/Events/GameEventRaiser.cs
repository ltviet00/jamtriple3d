using UnityEngine;
using System.Collections;

/// <summary>
/// Simple helper component. Config the GameEvent, then use UnityEvent or direct call to In_raise
/// it will simple raise the config-ed Event. Use this in case you dont want to create a coupling
/// between other comonent with GameEvent (Use UnityEvent to let this COuple be serialized)
/// </summary>
public class GameEventRaiser : MonoBehaviour {
	[SerializeField] GameEvent Event;

	void OnValidate() {
		Log.ValidateNotNull(Event, this, "Event");
	}

	bool IsFailedConfig() {
		return Event == null;
	}

	public void In_Raise() {
		if(IsFailedConfig())
			return;
		Event.Raise();
	}
}
