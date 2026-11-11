using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilsFuncToInvokeViaUnityEvent : MonoBehaviour {
	public void In_ApplicationOpenURL(string url) {
		Application.OpenURL(url);
	}

	public void In_ApplicationOpenUrlInStrinVariable(StringVariable var) {
		Application.OpenURL(var.Value);
	}
}
