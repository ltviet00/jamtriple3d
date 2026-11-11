using UnityEngine;
using System.Collections;

/// <summary>
/// Simple store Quit app function, use in UI.
/// Eg: we have Exit game button, add this component to it, and config OnClick()
/// to call QuitApp() in this component to terminate the app
/// </summary>
public class QuitApplicationFunction : MonoBehaviour {
	public void In_QuitApp() {
		Application.Quit();
	}
}
