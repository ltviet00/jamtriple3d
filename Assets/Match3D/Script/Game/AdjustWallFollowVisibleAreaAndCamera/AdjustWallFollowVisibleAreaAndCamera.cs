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
		ApplyBoxSortLayout();
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

	///<summary> Box sort mode (reference video): slots stay right below the boxes on top, toys use the whole area down to the footer.
	/// Move the corner transforms too, so the toy spawner (same corner variables) also use the new area </summary>
	void ApplyBoxSortLayout() {
		if(_boxSortLayoutApplied)
			return;
		var bar = FindObjectOfType<TargetUIBar>();
		var toyBoxAdjustor = FindObjectOfType<ToyBoxGroupAdjustPositionFollowScreen>();
		if(bar == null || !bar.IsBoxSortMode || toyBoxAdjustor == null)
			return;
		var leftTopTrans = leftTopCornerOnUIVar.Value;
		var rightBottomTrans = rightBottomCornerOnUIVar.Value;
		if(leftTopTrans == null || rightBottomTrans == null)
			return;
		_boxSortLayoutApplied = true;
		var (footerBottomY, slotsBottomY) = toyBoxAdjustor.MoveTopEdgeTo(leftTopTrans.position.y);
		var topDeltaWorld = slotsBottomY - leftTopTrans.position.y;
		var bottomDeltaWorld = footerBottomY - rightBottomTrans.position.y;
		// corners are children of the input panel (it also receive touch). Resize the panel so touch area = play area
		var panel = leftTopTrans.parent as RectTransform;
		if(panel != null && panel == rightBottomTrans.parent && panel.lossyScale.y > 0f) {
			var scale = panel.lossyScale.y;
			panel.offsetMax += new Vector2(0f, topDeltaWorld / scale);
			panel.offsetMin += new Vector2(0f, bottomDeltaWorld / scale);
			Canvas.ForceUpdateCanvases();
			return;
		}
		// fallback: move the corners only
		var pos = leftTopTrans.position;
		pos.y = slotsBottomY;
		leftTopTrans.position = pos;
		pos = rightBottomTrans.position;
		pos.y = footerBottomY;
		rightBottomTrans.position = pos;
	}
	bool _boxSortLayoutApplied;

	[Button]
	void TestAdjust() {
		if(isFailedConfig)
			return;
		AdjustWallPositionFollowVieaArea();
	}
	#endregion//Public
}
