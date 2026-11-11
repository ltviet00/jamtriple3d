using UnityEngine;
using UnityEngine.UI;

public class AdmobPrivacyOptionsButton : MonoBehaviour {
	[SerializeField] AdmobConsentManager consentManager;
	[SerializeField] Button button;

	void OnEnable() {
		RefreshVisibility();
	}

	public void In_OpenPrivacyOptions() {
		if(consentManager == null) {
			Log.Warning(false, this, "Missing AdmobConsentManager");
			return;
		}
		consentManager.OpenPrivacyOptions(RefreshVisibility);
	}

	public void RefreshVisibility() {
		if(button != null && consentManager != null) {
			button.gameObject.SetActive(consentManager.IsPrivacyOptionsRequired);
		}
	}
}
