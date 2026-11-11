using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UniRx;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Receive request LoadingScreen, then play animation, Load the next scene, open up animation.
/// Block all input when it start the animation.
/// </summary>
public class LoadingScreen : MonoSingleton<LoadingScreen> {
	#region Init, config
	[Header("Configurations")]
	///<summary> Current scene will darker (or get cover), this is the animation time </summary>
	[SerializeField] float closeScreenAnimationDuration = 0.5f;
	///<summary> Next scene will open up, this is the animation time for the opening animation </summary>
	[SerializeField] float openScreenAnimationDuration = 0.5f;
	/// <summary> After finshed a Loading scene, the Awake() and Start() of the next scene's objects
	/// alway take a bit time, it may cause the FadeOut animation being laggy, so, it better to delay
	/// a bit before call FadeOut command </summary>
	[SerializeField] float delayAfterFinishedLoadScene = 0.5f;

	[Header("Refs")]
	[SerializeField] Camera loadingCamera;

	[Header("Event Out")]
	[SerializeField] GameEvent startLoadSceneEvent;
	[SerializeField] GameEvent screenCoveredEvent;
	[SerializeField] GameEvent finishedLoadSceneEvent;

	[Header("validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	protected override void DoOnStart() {
		if(isFailedConfig)
			return;
		// let say, at Start we alway play the FadeOut transition. For testing in all scene
		// cause some component need the event OnFinishedLoadScene for working
		//Observable.Timer(TimeSpan.FromSeconds(delayAfterFinishedLoadScene)).Subscribe(_ => IE_FirstShowUp()).AddTo(_destroyDispose);
		Observable.FromMicroCoroutine(IE_FirstShowUp).Subscribe().AddTo(_corountines);
	}

	IEnumerator IE_FirstShowUp() {
		// cover screen first then wait a bit
		requestCoverScreenAtStartHandler.Invoke();
		// Update for URP
		AddLoadingCemeraToStackOfCurrentBaseCamera();
		// Unity show error "output properties do not match base camera", then disable laodingCam, enable it here
		loadingCamera.enabled = true;

		// first frame often take long, so let ignore it
		yield return null;
		var timer = delayAfterFinishedLoadScene;
		while(timer > 0f) {
			yield return null;
			timer -= Time.deltaTime;
		}
		// now request open screen, and wait for animation
		requestOpenScreenHandler.Invoke();
		timer = openScreenAnimationDuration;
		while(timer > 0f) {
			yield return null;
			timer -= Time.deltaTime;
		}
		// finish. Raise event
		finishedLoadSceneEvent.Raise();
		// remove camera out of the stack list
		RemoveLoadingCameraFromCurrentStackOnBaseCamera();
	}
	#endregion// Init, config


	#region Events
	[Header("Public Events, use to control UI")]
	//--- Use those event to trigger animation event
	///<summary> Invoke when the component start, often use to show a black screen or the already-fade-in screen</summary>
	public UnityEvent requestCoverScreenAtStartHandler;
	///<summary> Invoke when receive LoadScene() request, use this to start the FadeIn() on UIPage</summary>
	public UnityEvent requestCloseScreenHandler;
	//<summary>/ Invoke when the LoadScene just finished, use this to start the FadeOut() on UIPage</summary>
	public UnityEvent requestOpenScreenHandler;
	#endregion// private


	#region Public
	///<summary> Store nextScene name each time Load() was called</summary>
	string _nextScene;

	///<summary> Start the transition</summary>
	public void Load(string nextScene) {
		if(isFailedConfig) {
			Log.Error(false, this, "LoadingScreen is failedConfig, ignore the Load() request");
			return;
		}
		// already load scene? then ignore
		if(IsAlreadyRunLoadingSceneProcess()) {
			Log.Warning(false, this, $"Already in LoadingProcess, ignore this call [{nextScene}]");
			return;
		}
		// okay, run the loading screen
		_nextScene = nextScene;
		Log.Info(this, $"Load next scene : [{nextScene}]");
		StartLoadNextSceneCoroutine();
	}
	#endregion//Public


	#region Private - LoadingNextScene
	/// <summary> The loading next scene corountine </summary>
	IDisposable _loadNextSceneCorountine = null;
	void ClearExistLoadSceneCorountine() {
		_loadNextSceneCorountine?.Dispose();
		_loadNextSceneCorountine = null;
	}

	bool IsAlreadyRunLoadingSceneProcess() {
		return _loadNextSceneCorountine != null;
	}

	void StartLoadNextSceneCoroutine() {
		ClearExistLoadSceneCorountine();
		_loadNextSceneCorountine = Observable.FromMicroCoroutine(IE_LoadSceneProcess).Subscribe();

	}

	/// <summary> After FadeInFinished was invoked, we have to do some task with delaying, such as
	/// load scene, wait some frame to make sure some heavy task in Awake, Start finished, then do the fadeOut task.</summary>
	IEnumerator IE_LoadSceneProcess() {
		// disable all input first
		SetActiveInput(false);
		// enable camera
		loadingCamera.enabled = true;
		// post global event, some object will need this to do the finish, close, clean ... tasks
		startLoadSceneEvent?.Raise();

		// Update for URP
		AddLoadingCemeraToStackOfCurrentBaseCamera();

		// request play the CloseScreen effect, then wait for it a bit
		requestCloseScreenHandler.Invoke();
		var timer = closeScreenAnimationDuration;
		while(timer > 0f) {
			yield return null;
			timer -= Time.deltaTime;
		}

		// Load scene
		SceneManager.LoadScene(_nextScene, LoadSceneMode.Single);
		// finish load scene, let loading came renderType = base, so it will cover next scene
		SetLoadingCameraIsBaseRenderType();

		// after the next scene was loaded, wait some more frames, some Start(), Awake() functions are heavy, make sure they finished.
		// If we do the StarFadeOut here, it will be a hiccup (tested on devices, with some scene that have heavy Awake funcs)
		const int FRAME_DELAY_FOR_START_AWAKE = 5;
		for(int n = 0; n < FRAME_DELAY_FOR_START_AWAKE; n++) {
			yield return null;
		}

		// Update for URP. Cause to nextscene, there're will be other maincamera base
		AddLoadingCemeraToStackOfCurrentBaseCamera();
		// raise event when screen is still covered by black loading. some module will need this
		screenCoveredEvent.Raise();
		// finished load scene, make sure we still disable input
		SetActiveInput(false);

		// request play the OpenScreen effect, then wait for it a bit
		requestOpenScreenHandler.Invoke();
		timer = openScreenAnimationDuration;
		while(timer > 0f) {
			yield return null;
			timer -= Time.deltaTime;
		}

		// finish, raise event, reactive Input
		finishedLoadSceneEvent.Raise();
		SetActiveInput(true);
		// disable camera
		loadingCamera.enabled = false;

		// remove camera out of the stack list
		RemoveLoadingCameraFromCurrentStackOnBaseCamera();
		// clear coroutine
		ClearExistLoadSceneCorountine();
	}
	#endregion//Private - LoadingNextScene


	#region Utils - URP camera
	void AddLoadingCemeraToStackOfCurrentBaseCamera() {
		// let this camera overlay to current mainCamera
		var data = loadingCamera.GetUniversalAdditionalCameraData();
		data.renderType = CameraRenderType.Overlay;
		// add to mainCamera
		var mainCam = Camera.main;
		var mainCamData = mainCam.GetUniversalAdditionalCameraData();
		if(mainCamData == null) {
			Log.Warning(false, this, $"Not found UniversalAdditionalCameraData data in MainCamera");
			return;
		}
		// check exist
		var camStack = mainCamData.cameraStack;
		if(camStack.Contains(loadingCamera)){
			Log.Warning(false, this, $"Already contain loadingCamera in baseStack list");
			return;
		}
		camStack.Add(loadingCamera);
	}

	void RemoveLoadingCameraFromCurrentStackOnBaseCamera() {
		var mainCam = Camera.main;
		var mainCamData = mainCam.GetUniversalAdditionalCameraData();
		if(mainCamData == null) {
			Log.Warning(false, this, $"Not found UniversalAdditionalCameraData data in MainCamera");
			return;
		}
		// check exist
		var camStack = mainCamData.cameraStack;
		if(!camStack.Contains(loadingCamera)) {
			Log.Warning(false, this, $"Not contain loadingCamera in baseStack list to remove");
			return;
		}
		camStack.Remove(loadingCamera);
	}

	void SetLoadingCameraIsBaseRenderType() {
		var data = loadingCamera.GetUniversalAdditionalCameraData();
		data.renderType = CameraRenderType.Base;
	}
	#endregion//Utils - URP camera


	#region Private - EventSystm
	///<summary> Store currentEventSystem each time we call it.enable=false, cause after disable currentEventSystem
	/// the next time we call EventSystem.current the resutl will be = null</summary>
	EventSystem _cacheEventSystemBeforeDisable = null;
	EventSystem CacheEventSystem {
		get {
			if(_cacheEventSystemBeforeDisable == null)
				_cacheEventSystemBeforeDisable = EventSystem.current;
			return _cacheEventSystemBeforeDisable;
		}
	}

	void SetActiveInput(bool active) {
		if(CacheEventSystem != null) {
			CacheEventSystem.enabled = active;
		}
		else {
			Log.Info($"LoadingScreen, Not found any EventSystem to active [{active}]");
		}
	}

	CompositeDisposable _corountines = new CompositeDisposable();
	protected override void DoOnDestroy() {
		ClearExistLoadSceneCorountine();
		_corountines.Clear();
	}
	#endregion//Private - Event System
}