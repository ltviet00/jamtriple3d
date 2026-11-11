using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Home screen look (reference image): dark sci-fi room, a glowing glass capsule in the center with toys floating inside,
/// the play button shows "Level N".
/// Installed automatically when the Home scene loads, it decorates the existing MainPage (no prefab change needed):
/// - replace the "CurrentLevelInfo" circle by the capsule
/// - add the background behind the page
/// Art is in Resources/HomeCapsule
/// </summary>
public class HomeCapsuleDisplay : MonoBehaviour {
	#region Auto install
	const string HOME_SCENE_NAME = "Home";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void RegisterSceneLoaded() {
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
		InstallToScene(SceneManager.GetActiveScene());
	}

	static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		InstallToScene(scene);
	}

	static void InstallToScene(Scene scene) {
		if(!scene.IsValid() || scene.name != HOME_SCENE_NAME)
			return;
		var go = new GameObject(nameof(HomeCapsuleDisplay));
		SceneManager.MoveGameObjectToScene(go, scene);
		go.AddComponent<HomeCapsuleDisplay>();
	}
	#endregion//Auto install


	#region Config
	const string RES_FOLDER = "HomeCapsule/";
	const string LEVEL_INFO_NAME = "CurrentLevelInfo";
	const string LEVEL_NUMBER_NAME = "LevelNumerText";
	const string PLAY_BUTTON_NAME = "PlayButton";
	const string PLAY_TEXT_NAME = "LevelText";
	const string MAIN_PAGE_NAME = "MainPage";
	const string PLAQUE_TEXT = "MATCH JAM";
	static readonly string[] TOY_SPRITES = { "toy_apple", "toy_banana", "toy_cherry", "toy_duck", "toy_football", "toy_giftbox", "toy_cake", "toy_carrot" };

	// capsule art is 600x860, tube inside is x 110..490, y 200..690 (from bottom), plaque y 70..144
	static readonly Vector2 CAPSULE_SIZE = new Vector2(620f, 889f);
	static readonly Vector2 CAPSULE_OFFSET = new Vector2(0f, -30f);
	static readonly Vector2 TUBE_MIN = new Vector2(110f / 600f, 230f / 860f);
	static readonly Vector2 TUBE_MAX = new Vector2(490f / 600f, 670f / 860f);
	static readonly Vector2 PLAQUE_MIN = new Vector2(164f / 600f, 82f / 860f);
	static readonly Vector2 PLAQUE_MAX = new Vector2(436f / 600f, 132f / 860f);
	const int TOY_COUNT = 5;
	const float TOY_SIZE = 165f;
	const float BOB_AMPLITUDE = 16f;
	const float BOB_SPEED = 1.6f;
	const float SWING_ANGLE = 10f;
	const float SEARCH_INTERVAL = 0.3f;
	#endregion//Config


	#region Runtime
	bool _installed;
	float _nextSearch;
	TMP_Text _levelNumberText;
	TMP_Text _playText;
	string _lastLevelText;

	class FloatingToy {
		public RectTransform rect;
		public Vector2 basePos;
		public float phase;
		public float speed;
	}
	readonly List<FloatingToy> _toys = new List<FloatingToy>();

	void Update() {
		if(!_installed) {
			if(Time.unscaledTime >= _nextSearch) {
				_nextSearch = Time.unscaledTime + SEARCH_INTERVAL;
				TryInstall();
			}
			return;
		}
		AnimateToys();
	}

	void LateUpdate() {
		if(!_installed)
			return;
		SyncPlayButtonText();
	}

	void TryInstall() {
		var levelInfo = FindRectInScene(LEVEL_INFO_NAME);
		if(levelInfo == null)
			return;
		_installed = true;
		HideLevelCircle(levelInfo);
		CreateCapsule(levelInfo);
		CreateBackground(levelInfo);
		var playButton = FindRectInScene(PLAY_BUTTON_NAME);
		if(playButton != null) {
			var text = playButton.Find(PLAY_TEXT_NAME);
			_playText = text != null ? text.GetComponent<TMP_Text>() : null;
			if(_playText != null) {
				_playText.enableAutoSizing = true;
				_playText.fontSizeMin = 20f;
			}
		}
	}

	RectTransform FindRectInScene(string objectName) {
		var scene = gameObject.scene;
		foreach(var rect in FindObjectsOfType<RectTransform>(true)) {
			if(rect.name == objectName && rect.gameObject.scene == scene)
				return rect;
		}
		return null;
	}

	///<summary> Keep the level circle object (its scripts still update the level number), only hide its graphics </summary>
	void HideLevelCircle(RectTransform levelInfo) {
		foreach(var graphic in levelInfo.GetComponentsInChildren<Graphic>(true)) {
			graphic.enabled = false;
		}
		var number = levelInfo.Find(LEVEL_NUMBER_NAME);
		_levelNumberText = number != null ? number.GetComponent<TMP_Text>() : null;
	}

	void CreateCapsule(RectTransform levelInfo) {
		var parent = levelInfo.parent as RectTransform;
		var root = CreateRect("HomeCapsule", parent);
		root.SetSiblingIndex(levelInfo.GetSiblingIndex());
		root.anchorMin = root.anchorMax = levelInfo.anchorMin;
		root.pivot = new Vector2(0.5f, 0.5f);
		root.sizeDelta = CAPSULE_SIZE;
		root.anchoredPosition = levelInfo.anchoredPosition + CAPSULE_OFFSET;

		CreateImage("Back", root, Load("capsule_back"), Vector2.zero, Vector2.one);
		var tube = CreateRect("Toys", root);
		Stretch(tube, TUBE_MIN, TUBE_MAX);
		CreateToys(tube);
		CreateImage("Front", root, Load("capsule_front"), Vector2.zero, Vector2.one);
		CreatePlaqueText(root);
	}

	void CreateToys(RectTransform tube) {
		var sprites = new List<Sprite>();
		foreach(var spriteName in TOY_SPRITES) {
			var sprite = Load(spriteName);
			if(sprite != null)
				sprites.Add(sprite);
		}
		if(sprites.Count == 0)
			return;
		MathUtils.ShuffleList(sprites);
		var size = tube.rect.size;
		if(size.x <= 0f || size.y <= 0f)
			size = new Vector2(CAPSULE_SIZE.x * (TUBE_MAX.x - TUBE_MIN.x), CAPSULE_SIZE.y * (TUBE_MAX.y - TUBE_MIN.y));
		// 2 columns, toys stacked from the bottom, a bit messy like floating in water
		const int COLUMNS = 2;
		var rows = Mathf.CeilToInt(TOY_COUNT / (float)COLUMNS);
		for(int n = 0; n < TOY_COUNT; n++) {
			var col = n % COLUMNS;
			var row = n / COLUMNS;
			var x = (col + 0.5f) / COLUMNS + Random.Range(-0.08f, 0.08f) + (row % 2 == 0 ? -0.05f : 0.05f);
			var y = (row + 0.5f) / rows + Random.Range(-0.05f, 0.05f);
			var toyImage = CreateImage($"Toy_{n}", tube, sprites[n % sprites.Count], Vector2.zero, Vector2.zero);
			var rect = toyImage.rectTransform;
			rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
			rect.sizeDelta = Vector2.one * TOY_SIZE * Random.Range(0.85f, 1.1f);
			rect.anchoredPosition = new Vector2(x * size.x, y * size.y);
			rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
			toyImage.preserveAspect = true;
			_toys.Add(new FloatingToy {
				rect = rect,
				basePos = rect.anchoredPosition,
				phase = Random.Range(0f, Mathf.PI * 2f),
				speed = BOB_SPEED * Random.Range(0.8f, 1.2f),
			});
		}
	}

	void AnimateToys() {
		var time = Time.unscaledTime;
		for(int n = 0; n < _toys.Count; n++) {
			var toy = _toys[n];
			if(toy.rect == null)
				continue;
			var t = time * toy.speed + toy.phase;
			toy.rect.anchoredPosition = toy.basePos + new Vector2(Mathf.Sin(t * 0.7f) * BOB_AMPLITUDE * 0.4f, Mathf.Sin(t) * BOB_AMPLITUDE);
			toy.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * SWING_ANGLE);
		}
	}

	void CreatePlaqueText(RectTransform root) {
		var fontSource = FindRectInScene(PLAY_BUTTON_NAME);
		var sourceText = fontSource != null ? fontSource.GetComponentInChildren<TMP_Text>(true) : null;
		var go = new GameObject("PlaqueText", typeof(RectTransform));
		var rect = go.GetComponent<RectTransform>();
		rect.SetParent(root, false);
		Stretch(rect, PLAQUE_MIN, PLAQUE_MAX);
		var text = go.AddComponent<TextMeshProUGUI>();
		if(sourceText != null && sourceText.font != null)
			text.font = sourceText.font;
		text.text = PLAQUE_TEXT;
		text.alignment = TextAlignmentOptions.Center;
		text.enableAutoSizing = true;
		text.fontSizeMin = 10f;
		text.fontSizeMax = 44f;
		text.color = new Color(1f, 0.86f, 0.55f);
		text.raycastTarget = false;
	}

	void CreateBackground(RectTransform levelInfo) {
		var page = levelInfo;
		while(page != null && page.name != MAIN_PAGE_NAME)
			page = page.parent as RectTransform;
		if(page == null)
			return;
		var bg = CreateImage("HomeBackground", page, Load("home_bg"), Vector2.zero, Vector2.one);
		bg.transform.SetAsFirstSibling();
		bg.preserveAspect = false;
		var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
		fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
		fitter.aspectRatio = 1080f / 1920f;
	}

	void SyncPlayButtonText() {
		if(_playText == null || _levelNumberText == null)
			return;
		var number = _levelNumberText.text;
		if(number == _lastLevelText)
			return;
		_lastLevelText = number;
		if(!string.IsNullOrEmpty(number))
			_playText.text = $"Level {number}";
	}
	#endregion//Runtime


	#region Utils
	static Sprite Load(string spriteName) {
		return Resources.Load<Sprite>(RES_FOLDER + spriteName);
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
