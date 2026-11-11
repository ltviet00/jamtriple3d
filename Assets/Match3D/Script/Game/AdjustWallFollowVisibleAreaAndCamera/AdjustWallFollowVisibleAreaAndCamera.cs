using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// Adjust the wall position so it match visible area on UI.
/// NOTES: we change global Gravity so gravity direction is in Z coord (instead of default Ycoord)
/// So just need 1 camera.
/// </summary>
public class AdjustWallFollowVisibleAreaAndCamera : MonoBehaviour {
	#region Init, config
	[Header("ShareData")]
	[SerializeField] TransformVariable leftTopCornerOnUIVar;
	[SerializeField] TransformVariable rightBottomCornerOnUIVar;

	[Header("Wall refs")]
	[SerializeField] Transform wallLeft;
	[SerializeField] Transform wallRight;
	[SerializeField] Transform wallTop;
	[SerializeField] Transform wallBottom;

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
	public void AdjustWallPositionFollowVieaArea() {
		// we changed gravity direction, now the "Ground" will be in vertical.
		// the left-top and right-nottom on UI also the left-top and right-bottom limit for the ground
		var leftTopPos = leftTopCornerOnUIVar.Value.position;
		var rightBottomPos = rightBottomCornerOnUIVar.Value.position;
		var centerPos = Vector3.Lerp(leftTopPos, rightBottomPos, 0.5f);
		// simply take walls follow those position
		var wallLeftPos = wallLeft.position;
		wallLeftPos.x = leftTopPos.x;
		wallLeftPos.y = centerPos.y;
		wallLeft.position = wallLeftPos;

		var wallRightPos = wallRight.position;
		wallRightPos.x = rightBottomPos.x;
		wallRightPos.y = centerPos.y;
		wallRight.position = wallRightPos;

		var wallTopPos = wallTop.position;
		wallTopPos.x = centerPos.x;
		wallTopPos.y = leftTopPos.y;
		wallTop.position = wallTopPos;

		var wallBottomPos = wallBottom.position;
		wallBottomPos.x = centerPos.x;
		wallBottomPos.y = rightBottomPos.y;
		wallBottom.position = wallBottomPos;
	}

	[Button]
	void TestAdjust() {
		if(isFailedConfig)
			return;
		AdjustWallPositionFollowVieaArea();
	}
	#endregion//Public
}
