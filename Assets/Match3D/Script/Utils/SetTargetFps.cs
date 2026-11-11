using UnityEngine;
using System.Collections;

/// <summary>
/// Simple component to cap fps on devices.
/// It will call Application.targetFps ...
/// </summary>
public class SetTargetFps : MonoBehaviour {
	[SerializeField] bool activeCustomFps = true;
	[SerializeField] int targetFPS = 60;

	void Start() {
		if(activeCustomFps) {
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = targetFPS;
		}
	}
}