using UnityEngine;
using System.Collections;

/// <summary>
/// Same with GaemEventRaiser, but it have an array of Event,
/// when invoke In_Raise(int) you must sent an index to let it know which event we need to raise
/// </summary>
public class MultiGameEventRaiser : MonoBehaviour {
	[SerializeField] GameEvent[] Events;

	void OnValidate() {
		if(Events == null) {
			Log.Warning(false, this, "Empty Events list");
			return;
		}
		for(int n = 0; n < Events.Length; n++) {
			if(Events[n] == null)
				Log.Warning(false, this, "Missing Event at index [{0}]", n);
		}
	}

	bool IsFailedConfig() {
		return Events == null
			|| (Events != null && Events.Length == 0);
	}

	public void In_Raise(int index) {
		if(IsFailedConfig())
			return;
		if(index < 0 || index >= Events.Length) {
			Log.Warning(false, this, "Raise with index out of Events array length");
			return;
		}
		var evt = Events[index];
		if(evt != null) {
			evt.Raise();
		}
		else {
			Log.Warning(false, this, "Call Raise() with index [] but null Event at that index", index);
		}
	}
}
