using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> Auto register target monobehavior to the SimpleDI </summary>
public class AutoRegisterDependency : MonoBehaviour {
	#region Init, config
	[Header("Target object to register")]
	[SerializeField] UnityEngine.Object targetObject;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
		SimpleDI.Instance.Register(targetObject);
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
		// check exist DI, cause we both got destroy, and the Instance() call might creat leak memory
		if(SimpleDI.HasInstance()) {
			SimpleDI.Instance.Unregister(targetObject);
		}
	}
	#endregion// Init config
}
