using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UFO loading screen: night sky, a flying saucer beams toys up, progress bar "Loading xx%".
/// - At app start: a full screen overlay covers the Splash, fills the bar, then fades out when the first real scene (Home/Game) is loaded.
/// - Scene transitions: the same picture is added into the LoadingScreen "TransitionPage", so it fades in/out with the existing transition.
/// Installed automatically, no prefab/scene change needed. Art is in Resources/UfoLoading
/// </summary>
public class UfoLoadingDisplay : MonoBehaviour {
	#region Auto install
	const string SPLASH_SCENE_NAME = "Splash";
	const string TRANSITION_PAGE_NAME = "TransitionPage";
	const string DECOR_NAME = "UfoLoading";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Install() {
		var go = new GameObject(nameof(UfoLoadingDisplay));
		DontDestroyOnLoad(go);
		go.AddComponent<UfoLoadingDisplay>();
	}
	#endregion//Auto install


	#region Config
	const string RES_FOLDER = "UfoLoading/";
	static readonly string[] TOY_SPRITES = { "HomeCapsule/toy_apple", "HomeCapsule/toy_banana", "HomeCapsule/toy_cherry", "HomeCapsule/toy_duck", "HomeCapsule/toy_football", "HomeCapsule/toy_giftbox", "HomeCapsule/toy_cake", "HomeCapsule/toy_carrot", "BoxSort/crate_closed" };
	const float REF_WIDTH = 1080f;
	const float REF_HEIGHT = 1920f;
	/// <summary> Startup screen shows at least this long, so the player can see it </summary>
	const float STARTUP_MIN_TIME = 2.6f;
	const float STARTUP_MAX_TIME = 15f;
	const float STARTUP_FADE_TIME = 0.35f;
	const int STARTUP_SORTING_ORDER = 30000;
	/// <summary> Transition loading: time constant of the bar (63% after this time) </summary>
	const float TRANSITION_FILL_TIME = 1.5f;
	const int TOY_COUNT = 3;
	const float TOY_RISE_TIME = 2.1f;
	const float SEARCH_INTERVAL = 0.3f;
	const float MAX_DELTA_TIME = 0.05f;
	#endregion//Config


	#region View
	class RisingToy {
		public RectTransform rect;
		public Image image;
		public float offset;
	}

	/// <summary> One built UFO picture (startup overlay or transition page) </summary>
	class View {
		public RectTransform ufo;
		public Image beam;
		public Image fill;
		public TMP_Text label;
		public readonly List<RisingToy> toys = new List<RisingToy>();
		public float progress;
	}

	static readonly List<Sprite> _toySprites = new List<Sprite>();

	static Sprite Load(string spriteName) {
		return Resources.Load<Sprite>(RES_FOLDER + spriteName);
	}

	static Sprite NextToySprite() {
		if(_toySprites.Count == 0) {
			foreach(var path in TOY_SPRITES) {
				var sprite = Resources.Load<Sprite>(path);
				if(sprite != null)
					_toySprites.Add(sprite);
			}
		}
		return _toySprites.Count > 0 ? _toySprites[Random.Range(0, _toySprites.Count)] : null;
	}

	///<summary> Build the picture inside a full screen rect. Positions are taken from the 1080x1920 design </summary>
	static View Build(RectTransform parent, bool blockInput) {
		var view = new View();
		var bg = CreateImage("Background", parent, Load("ufo_bg"), Vector2.zero, Vector2.one);
		bg.raycastTarget = blockInput;
		var bgFitter = bg.gameObject.AddComponent<AspectRatioFitter>();
		bgFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
		bgFitter.aspectRatio = REF_WIDTH / REF_HEIGHT;

		var stage = CreateRect("Stage", parent);
		Stretch(stage, Vector2.zero, Vector2.one);
		var stageFitter = stage.gameObject.AddComponent<AspectRatioFitter>();
		stageFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
		stageFitter.aspectRatio = REF_WIDTH / REF_HEIGHT;

		view.beam = CreateImage("Beam", stage, Load("ufo_beam"), Design(290, 400), Design(790, 1160));
		var toyLayer = CreateRect("Toys", stage);
		Stretch(toyLayer, Vector2.zero, Vector2.one);
		for(int n = 0; n < TOY_COUNT; n++) {
			var toy = CreateImage($"Toy_{n}", toyLayer, NextToySprite(), Vector2.zero, Vector2.zero);
			toy.preserveAspect = true;
			view.toys.Add(new RisingToy { rect = toy.rectTransform, image = toy, offset = n / (float)TOY_COUNT });
		}
		view.ufo = CreateImage("Ufo", stage, Load("ufo"), Design(190, 1020), Design(890, 1440)).rectTransform;
		CreateImage("Title", stage, Load("ufo_title"), Design(90, 1460), Design(990, 1720)).preserveAspect = true;

		CreateImage("BarBg", stage, Load("bar_bg"), Design(220, 200), Design(860, 270));
		view.fill = CreateImage("BarFill", stage, Load("bar_fill"), Design(220, 200), Design(860, 270));
		view.fill.type = Image.Type.Filled;
		view.fill.fillMethod = Image.FillMethod.Horizontal;
		view.fill.fillOrigin = (int)Image.OriginHorizontal.Left;
		view.fill.fillAmount = 0f;

		var labelRect = CreateRect("Label", stage);
		Stretch(labelRect, Design(220, 110), Design(860, 190));
		var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
		label.alignment = TextAlignmentOptions.Center;
		label.enableAutoSizing = false;
		label.enableWordWrapping = false;
		label.overflowMode = TextOverflowModes.Overflow;
		label.fontStyle = FontStyles.Bold;
		label.color = Color.white;
		label.raycastTarget = false;
		label.text = "Loading 0%";
		view.label = label;
		return view;
	}

	static void Animate(View view, float time) {
		if(view.ufo == null)
			return;
		// saucer hovers and tilts
		var ufoMin = Design(190, 1020);
		var ufoMax = Design(890, 1440);
		var bob = Mathf.Sin(time * 2.2f) * 0.008f;
		view.ufo.anchorMin = ufoMin + Vector2.up * bob;
		view.ufo.anchorMax = ufoMax + Vector2.up * bob;
		view.ufo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f) * 3f);
		// beam pulses
		var beamColor = view.beam.color;
		beamColor.a = 0.75f + Mathf.Sin(time * 5f) * 0.2f;
		view.beam.color = beamColor;
		// toys rise in the beam, shrink and fade when they reach the saucer
		var bottom = Design(540, 430);
		var top = Design(540, 1080);
		for(int n = 0; n < view.toys.Count; n++) {
			var toy = view.toys[n];
			var t = time / TOY_RISE_TIME + toy.offset;
			var k = t - Mathf.Floor(t);
			var cycle = Mathf.FloorToInt(t);
			if(toy.rect.name != $"c{cycle}") {
				// new trip, new toy
				toy.rect.name = $"c{cycle}";
				toy.image.sprite = NextToySprite();
			}
			var pos = Vector2.Lerp(bottom, top, k);
			pos.x += Mathf.Sin((k + toy.offset) * Mathf.PI * 3f) * 0.04f * (1f - k);
			var size = Mathf.Lerp(0.15f, 0.06f, k);
			var half = new Vector2(size, size * REF_WIDTH / REF_HEIGHT) * 0.5f;
			toy.rect.anchorMin = pos - half;
			toy.rect.anchorMax = pos + half;
			toy.rect.offsetMin = toy.rect.offsetMax = Vector2.zero;
			toy.rect.localRotation = Quaternion.Euler(0f, 0f, (time * 90f + toy.offset * 360f) % 360f);
			var color = toy.image.color;
			color.a = Mathf.Clamp01(k * 6f) * Mathf.Clamp01((1f - k) * 5f);
			toy.image.color = color;
		}
		// progress
		view.fill.fillAmount = view.progress;
		view.label.text = $"Loading {Mathf.RoundToInt(view.progress * 100f)}%";
		// font size follows the label height (canvas may have no scaler)
		var labelHeight = view.label.rectTransform.rect.height;
		if(labelHeight > 0f)
			view.label.fontSize = labelHeight * 0.6f;
	}

	static Vector2 Design(float x, float y) {
		return new Vector2(x / REF_WIDTH, y / REF_HEIGHT);
	}
	#endregion//View


	#region Startup overlay
	View _startup;
	CanvasGroup _startupGroup;
	float _startupTime;
	float _startupFade = -1f;
	float _realSceneLoadedTime = -1f;

	void Awake() {
		var canvasGo = new GameObject("UfoLoadingStartup", typeof(RectTransform));
		canvasGo.transform.SetParent(transform, false);
		var canvas = canvasGo.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = STARTUP_SORTING_ORDER;
		canvasGo.AddComponent<GraphicRaycaster>();
		_startupGroup = canvasGo.AddComponent<CanvasGroup>();
		_startup = Build(canvasGo.GetComponent<RectTransform>(), true);
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnDestroy() {
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		if(scene.name != SPLASH_SCENE_NAME && _realSceneLoadedTime < 0f)
			_realSceneLoadedTime = _startupTime;
		_transitionSearchDone = false;
	}

	void UpdateStartup(float dt) {
		if(_startup == null)
			return;
		_startupTime += dt;
		// fill to 90% in the min time, the last 10% when the real scene is ready
		var target = Mathf.Clamp01(_startupTime / STARTUP_MIN_TIME) * 0.9f;
		// still waiting for the scene: keep creeping slowly to 99%, so the bar never looks frozen
		if(_startupTime > STARTUP_MIN_TIME)
			target = 0.99f - 0.09f * Mathf.Exp(-(_startupTime - STARTUP_MIN_TIME) / 4f);
		// the LoadingScreen still plays its open animation after the scene was loaded, wait for it
		var sceneReady = _realSceneLoadedTime >= 0f && _startupTime - _realSceneLoadedTime > 0.8f;
		if((sceneReady && _startupTime >= STARTUP_MIN_TIME) || _startupTime > STARTUP_MAX_TIME)
			target = 1f;
		_startup.progress = Mathf.MoveTowards(_startup.progress, Mathf.Max(_startup.progress, target), dt * 1.2f);
		Animate(_startup, _startupTime);
		if(_startupFade < 0f && _startup.progress >= 1f)
			_startupFade = 0f;
		if(_startupFade >= 0f) {
			_startupFade += dt;
			_startupGroup.alpha = 1f - Mathf.Clamp01(_startupFade / STARTUP_FADE_TIME);
			if(_startupFade >= STARTUP_FADE_TIME) {
				Destroy(_startupGroup.gameObject);
				_startup = null;
				_startupGroup = null;
			}
		}
	}
	#endregion//Startup overlay


	#region Transition page
	View _transition;
	CanvasGroup _transitionGroup;
	float _transitionTime;
	float _lastTransitionAlpha;
	bool _transitionSearchDone;
	float _nextSearch;

	void FindTransitionPage() {
		if(_transitionSearchDone || Time.unscaledTime < _nextSearch)
			return;
		_nextSearch = Time.unscaledTime + SEARCH_INTERVAL;
		var loading = FindObjectOfType<LoadingScreen>(true);
		if(loading == null)
			return;
		_transitionSearchDone = true;
		var page = FindChild(loading.transform, TRANSITION_PAGE_NAME) as RectTransform;
		if(page == null || page.Find(DECOR_NAME) != null)
			return;
		var root = CreateRect(DECOR_NAME, page);
		Stretch(root, Vector2.zero, Vector2.one);
		_transition = Build(root, false);
		_transitionGroup = page.GetComponentInParent<CanvasGroup>();
	}

	void UpdateTransition(float dt) {
		if(_transition == null || _transition.ufo == null) {
			_transition = null;
			return;
		}
		var visible = _transitionGroup == null || (_transitionGroup.alpha > 0.01f && _transitionGroup.gameObject.activeInHierarchy);
		if(!visible) {
			_transitionTime = 0f;
			_transition.progress = 0f;
			_lastTransitionAlpha = 0f;
			return;
		}
		_transitionTime += dt;
		// loading time is unknown: approach 100% smoothly, reach it when the page starts to fade out
		var fadingOut = _transitionGroup != null && _transitionGroup.alpha < _lastTransitionAlpha - 0.001f;
		_lastTransitionAlpha = _transitionGroup != null ? _transitionGroup.alpha : 1f;
		var target = fadingOut ? 1f : 1f - Mathf.Exp(-_transitionTime / TRANSITION_FILL_TIME);
		_transition.progress = Mathf.MoveTowards(_transition.progress, Mathf.Max(_transition.progress, target), dt * 2f);
		Animate(_transition, Time.unscaledTime);
	}
	#endregion//Transition page


	void Update() {
		// cap: loading a scene blocks for seconds, the bar should not jump because of that frame
		var dt = Mathf.Min(Time.unscaledDeltaTime, MAX_DELTA_TIME);
		UpdateStartup(dt);
		FindTransitionPage();
		UpdateTransition(dt);
	}


	#region Utils
	static Transform FindChild(Transform root, string childName) {
		if(root.name == childName)
			return root;
		for(int n = 0; n < root.childCount; n++) {
			var found = FindChild(root.GetChild(n), childName);
			if(found != null)
				return found;
		}
		return null;
	}

	static RectTransform CreateRect(string objectName, Transform parent) {
		var go = new GameObject(objectName, typeof(RectTransform));
		var rect = go.GetComponent<RectTransform>();
		rect.SetParent(parent, false);
		rect.localScale = Vector3.one;
		return rect;
	}

	static Image CreateImage(string objectName, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax) {
		var rect = CreateRect(objectName, parent);
		Stretch(rect, anchorMin, anchorMax);
		var image = rect.gameObject.AddComponent<Image>();
		image.sprite = sprite;
		image.raycastTarget = false;
		image.enabled = sprite != null;
		return image;
	}

	static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) {
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}
	#endregion//Utils
}
