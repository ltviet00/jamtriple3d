using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// https://answers.unity.com/questions/1091618/ui-panel-without-image-component-as-raycast-target.html
/// Button with no graphics render (read above post, turn out we can just use Image with Zero alpha to get same result.
/// But this button give extra callback : tap, onclick ... (drag if need, implement later)
/// </summary>
public class NoGraphicsButton : Graphic, IPointerDownHandler, IPointerClickHandler {
	#region Event out
	public UnityEvent OnClickHandler;
	public UnityEvent OnTapHandler;

	// use for Unitask
	public UniTaskCompletionSource OnClickCompleteSource => _onClickCompleteSource;
	public UniTaskCompletionSource OnTapCompleteSource => _onTapCompleteSource;
	#endregion//Event out


	#region Pointer interfaces
	public void OnPointerClick(PointerEventData eventData) {
		OnClickHandler.Invoke();
		SetResultOnClick();
	}

	public void OnPointerDown(PointerEventData eventData) {
		OnTapHandler.Invoke();
		SetResultOnTap();
	}
	#endregion//Pointer interfaces


	#region Override
	///<summary> Simple return to let this button "no graphics" </summary>
	public override void SetMaterialDirty() {
		return;
	}
	///<summary> Simple return to let this button "no graphics" </summary>
	public override void SetVerticesDirty() {
		return;
	}
	#endregion//Override


	#region Private
	UniTaskCompletionSource _onClickCompleteSource;
	UniTaskCompletionSource _onTapCompleteSource;

	void SetResultOnClick() {
		_onClickCompleteSource.TrySetResult();
		// create new source for next await. Cause since first TrySetResult, it status turn to Succeedm and nolonger awaiter (callback right when you await it)
		_onClickCompleteSource = new UniTaskCompletionSource();
	}

	void SetResultOnTap() {
		_onTapCompleteSource.TrySetResult();
		// create new source for next await. Cause since first TrySetResult, it status turn to Succeedm and nolonger awaiter (callback right when you await it)
		_onTapCompleteSource = new UniTaskCompletionSource();
	}
	#endregion//Private
}
