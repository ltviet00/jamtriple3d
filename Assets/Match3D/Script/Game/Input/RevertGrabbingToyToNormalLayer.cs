using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When toy are grabbing we set them to another layer so they can enter the CatcherPlaceHolder.
/// but when we release the toy, only allow it to stay in the grabbingToyLayer until they touch anything.
/// UserInputHandler will handle this component
/// </summary>
public class RevertGrabbingToyToNormalLayer : MonoBehaviour {
	#region Init, config
	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}
	#endregion// Init config


	#region Public
	LayerMaskVariable _normalLayerVar;
	ToyView _targetToy;

	public void SetData(ToyView targetToy, LayerMaskVariable normalLayer) {
		if(isFailedConfig)
			return;
		_targetToy = targetToy;
		_normalLayerVar = normalLayer;
	}
	#endregion//Public


	#region Collision event
	void OnCollisionEnter(Collision collision) {
		if(isFailedConfig)
			return;
		// set toy back to normal layer
		var layer = GameObjectUtils.FromLayerMaskToLayerIndexForGameObject(_normalLayerVar.Value);
		_targetToy.gameObject.layer = layer;
		// also destroy thsi component, no longer need
		Destroy(this);
	}
	#endregion//Collision event
}
