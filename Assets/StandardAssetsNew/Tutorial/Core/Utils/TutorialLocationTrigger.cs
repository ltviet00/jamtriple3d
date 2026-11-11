using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto trigger Tutorial location (at OnEanble, Start ...), or you can manuall trigger via some public func
/// </summary>
public class TutorialLocationTrigger : MonoBehaviour {
	#region Init, config
	[Header("Config")]
	[SerializeField] StringVariable locationVar;
	[SerializeField] TriggerType triggerType;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		if(triggerType == TriggerType.OnEnable)
			TriggerLocationTutorial();
	}

	void Start() {
		if(isFailedConfig)
			return;
		if(triggerType == TriggerType.OnStart)
			TriggerLocationTutorial();
	}
	#endregion// Init config


	#region UI invoke
	///<summary> UI invoke this to trigger config-ed location </summary>
	public void UI_Trigger() {
		if(isFailedConfig)
			return;
		TriggerLocationTutorial();
	}
	#endregion//UI invoke


	#region Private
	void TriggerLocationTutorial() {
		if(TutorialManager.HasInstance()) {
			TutorialManager.Instance.TriggerLocation(locationVar);
		}
	}
	#endregion//Private


	#region Classes
	public enum TriggerType {
		Manually, OnEnable, OnStart, 
	}
	#endregion//Classes
}
