using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Get call from from CatchToyPlaceHolder when user catch a toy.
/// Check to see if there're any affector for that toy to trigger
/// </summary>
public class SupportToyAffectorController : MonoBehaviour {
	#region Init, config
	[Header("Refs")]
	[SerializeReference] List<BaseSupportToyAffector> affectorList;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.RegisterType(this);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		SimpleDI.UnRegisterType(this);
	}
	#endregion// Init config


	#region Public
	///<summary> Find affector for this supportToyId and trigger </summary>
	public void TriggerAffectForSupportToy(SupportToyID id) {
		if(isFailedConfig)
			return;
		for(int n = 0; n < affectorList.Count; n++) {
			var affector = affectorList[n];
			if(affector.IsSameTargetSupportToyID(id)) {
				affector.TriggerAffect();
				return;
			}
		}
		// not found affector?
		Log.Error(false, this, $"Not found affector for supportToy [{id.name}]");
	}
	#endregion//Public


	#region Classes
	public abstract class BaseSupportToyAffector : MonoBehaviour {
		public virtual bool IsSameTargetSupportToyID(SupportToyID id) {
			return false;
		}

		public virtual void TriggerAffect() {
		}
	}
	#endregion//Classes
}
