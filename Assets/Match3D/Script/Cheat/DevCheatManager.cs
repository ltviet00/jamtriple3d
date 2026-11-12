#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dev cheat menu, only compiled in Development builds and the Editor (gone in Release).
/// A small "CHEAT" button stays on screen, tap it to open the panel:
/// - Level: -10, -1, +1, +10, jump to any level (1-based)
/// - In game: Win, Lose
/// Changing the level reloads the current scene (Home or Game) so the new level is used right away.
/// </summary>
public class DevCheatManager : MonoBehaviour {
	#region Auto install
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Install() {
		if(FindObjectOfType<DevCheatManager>() != null)
			return;
		var go = new GameObject(nameof(DevCheatManager));
		DontDestroyOnLoad(go);
		go.AddComponent<DevCheatManager>();
	}
	#endregion//Auto install


	#region Config
	const string HOME_SCENE = "Home";
	const string GAME_SCENE = "Game";
	const string TUTORIAL_SCENE = "Tutorial";
	const string EVT_ALL_TOY_CLEAR = "Evt_Result_AllToyClear";
	const string EVT_GAME_WIN = "Evt_Result_GameWin";
	const string EVT_GAME_LOSE = "Evt_Result_GameLose";
	const string EVT_GAME_PAUSE = "Evt_Game_PauseOn";
	const float WIN_DELAY = 1f; // same as CheckRaiseWinGameEvent
	const int SORTING_ORDER = 29000; // under the UFO loading overlay (30000)
	static readonly Color PANEL_COLOR = new Color(0.08f, 0.09f, 0.14f, 0.94f);
	static readonly Color BUTTON_COLOR = new Color(0.25f, 0.45f, 0.85f, 1f);
	static readonly Color WIN_COLOR = new Color(0.2f, 0.65f, 0.3f, 1f);
	static readonly Color LOSE_COLOR = new Color(0.8f, 0.25f, 0.25f, 1f);
	static readonly Color TOGGLE_COLOR = new Color(0.9f, 0.3f, 0.6f, 0.75f);
	#endregion//Config


	#region Runtime
	Font _font;
	GameObject _panel;
	Text _levelLabel;
	Text _statusLabel;
	InputField _jumpInput;
	Button _winButton;
	Button _loseButton;
	bool _resultSent;

	void Awake() {
		_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		BuildUI();
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnDestroy() {
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		_resultSent = false;
		EnsureEventSystem();
		RefreshPanel();
	}

	void TogglePanel() {
		_panel.SetActive(!_panel.activeSelf);
		if(_panel.activeSelf) {
			_statusLabel.text = "";
			RefreshPanel();
		}
	}

	void RefreshPanel() {
		if(_panel == null || !_panel.activeSelf)
			return;
		var inventory = FindLevelInventory();
		var count = GetLevelCount();
		var level = inventory != null ? inventory.Data.reachedLevel + 1 : 0;
		_levelLabel.text = count > 0 ? $"Level {level} / {count}" : $"Level {level}";
		var inGame = IsInGame();
		_winButton.interactable = inGame && !_resultSent;
		_loseButton.interactable = inGame && !_resultSent;
	}
	#endregion//Runtime


	#region Actions
	void ChangeLevel(int delta) {
		var inventory = FindLevelInventory();
		if(inventory == null) {
			_statusLabel.text = "Level data not ready";
			return;
		}
		JumpToLevel(inventory.Data.reachedLevel + 1 + delta);
	}

	void JumpFromInput() {
		if(!int.TryParse(_jumpInput.text, out var level)) {
			_statusLabel.text = "Type a level number";
			return;
		}
		JumpToLevel(level);
	}

	///<summary> level is 1-based </summary>
	void JumpToLevel(int level) {
		var inventory = FindLevelInventory();
		if(inventory == null) {
			_statusLabel.text = "Level data not ready";
			return;
		}
		var count = GetLevelCount();
		var max = count > 0 ? count : int.MaxValue;
		level = Mathf.Clamp(level, 1, max);
		inventory.Data.reachedLevel = level - 1;
		inventory.SaveToDisk();
		RefillEnergy();
		_jumpInput.text = "";
		_statusLabel.text = $"Go to level {level}";
		_panel.SetActive(false);
		ReloadScene();
	}

	void Win() {
		if(!IsInGame() || _resultSent)
			return;
		_resultSent = true;
		_panel.SetActive(false);
		StartCoroutine(IE_Win());
	}

	IEnumerator IE_Win() {
		RaiseEvent(EVT_ALL_TOY_CLEAR);
		yield return new WaitForSecondsRealtime(WIN_DELAY);
		RaiseEvent(EVT_GAME_WIN);
	}

	void Lose() {
		if(!IsInGame() || _resultSent)
			return;
		_resultSent = true;
		_panel.SetActive(false);
		// same as the "give up" button on the TimeUp page: open LevelFailed page + raise GameLose
		RaiseEvent(EVT_GAME_PAUSE); // stop the clock
		var failedPage = FindObjectOfType<LevelFailedPage>(true);
		var page = failedPage != null ? failedPage.GetComponentInParent<UIPage>(true) : null;
		if(page != null)
			page.UI_CloseCurrentTopPageAndOpenMe();
		else
			_statusLabel.text = "LevelFailedPage not found";
		RaiseEvent(EVT_GAME_LOSE);
	}

	void ReloadScene() {
		var scene = SceneManager.GetActiveScene().name;
		var target = scene == HOME_SCENE ? HOME_SCENE : (IsInGame() ? GAME_SCENE : scene);
		var loading = FindObjectOfType<LoadingScreen>(true);
		if(loading != null)
			loading.Load(target);
		else
			SceneManager.LoadScene(target);
	}
	#endregion//Actions


	#region Helpers
	static bool IsInGame() {
		var scene = SceneManager.GetActiveScene().name;
		return scene == GAME_SCENE || scene == TUTORIAL_SCENE;
	}

	static UserLevelScoreInventory FindLevelInventory() {
		var all = Resources.FindObjectsOfTypeAll<UserLevelScoreInventory>();
		return all.Length > 0 ? all[0] : null;
	}

	static int GetLevelCount() {
		var max = 0;
		foreach(var storage in Resources.FindObjectsOfTypeAll<LevelDataStorage>())
			max = Mathf.Max(max, storage.LevelCount);
		return max;
	}

	static void RefillEnergy() {
		foreach(var energy in Resources.FindObjectsOfTypeAll<UserEnergyInventory>()) {
			for(int n = 0; n < energy.MaxEnergy && !energy.IsFull(); n++)
				energy.IncreaseOneEnergyAndSaveCurrentTime();
		}
	}

	void RaiseEvent(string eventName) {
		foreach(var evt in Resources.FindObjectsOfTypeAll<GameEvent>()) {
			if(evt.name == eventName) {
				evt.Raise();
				return;
			}
		}
		_statusLabel.text = $"Event not found: {eventName}";
		Debug.LogWarning($"[DevCheatManager] event not found {eventName}");
	}

	static void EnsureEventSystem() {
		if(EventSystem.current != null || FindObjectOfType<EventSystem>() != null)
			return;
		var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
		DontDestroyOnLoad(go);
	}
	#endregion//Helpers


	#region Build UI
	void BuildUI() {
		var canvas = gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = SORTING_ORDER;
		var scaler = gameObject.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080f, 1920f);
		scaler.matchWidthOrHeight = 0f;
		gameObject.AddComponent<GraphicRaycaster>();
		var root = (RectTransform)transform;

		// toggle button: small, left edge, a bit under the middle so it does not cover the game HUD
		var toggle = CreateButton(root, "CHEAT", TOGGLE_COLOR, TogglePanel);
		Place(toggle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, -260f), new Vector2(130f, 64f));
		toggle.GetComponentInChildren<Text>().fontSize = 26;

		// panel
		_panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
		var panel = (RectTransform)_panel.transform;
		panel.SetParent(root, false);
		Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 860f));
		_panel.GetComponent<Image>().color = PANEL_COLOR;

		var title = CreateText(panel, "DEV CHEAT", 44, Color.white);
		Place(title.rectTransform, Top(), Top(), new Vector2(0f, -55f), new Vector2(840f, 70f));
		var close = CreateButton(panel, "X", LOSE_COLOR, TogglePanel);
		Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-55f, -55f), new Vector2(80f, 80f));

		_levelLabel = CreateText(panel, "Level", 52, new Color(1f, 0.86f, 0.4f));
		Place(_levelLabel.rectTransform, Top(), Top(), new Vector2(0f, -150f), new Vector2(840f, 80f));

		// -10 -1 +1 +10
		string[] labels = { "-10", "-1", "+1", "+10" };
		int[] deltas = { -10, -1, 1, 10 };
		for(int n = 0; n < labels.Length; n++) {
			var delta = deltas[n];
			var button = CreateButton(panel, labels[n], BUTTON_COLOR, () => ChangeLevel(delta));
			Place(button, Top(), Top(), new Vector2(-315f + n * 210f, -270f), new Vector2(190f, 110f));
		}

		// jump to level
		_jumpInput = CreateInput(panel, "Level...");
		Place((RectTransform)_jumpInput.transform, Top(), Top(), new Vector2(-110f, -410f), new Vector2(600f, 110f));
		var go = CreateButton(panel, "GO", BUTTON_COLOR, JumpFromInput);
		Place(go, Top(), Top(), new Vector2(305f, -410f), new Vector2(200f, 110f));

		// win, lose
		_winButton = CreateButton(panel, "WIN", WIN_COLOR, Win).GetComponent<Button>();
		Place((RectTransform)_winButton.transform, Top(), Top(), new Vector2(-210f, -560f), new Vector2(400f, 120f));
		_loseButton = CreateButton(panel, "LOSE", LOSE_COLOR, Lose).GetComponent<Button>();
		Place((RectTransform)_loseButton.transform, Top(), Top(), new Vector2(210f, -560f), new Vector2(400f, 120f));

		_statusLabel = CreateText(panel, "", 34, new Color(0.8f, 0.85f, 1f));
		Place(_statusLabel.rectTransform, Top(), Top(), new Vector2(0f, -690f), new Vector2(840f, 60f));
		var hint = CreateText(panel, "Win / Lose only work in game", 28, new Color(1f, 1f, 1f, 0.5f));
		Place(hint.rectTransform, Top(), Top(), new Vector2(0f, -770f), new Vector2(840f, 50f));

		_panel.SetActive(false);
	}

	static Vector2 Top() => new Vector2(0.5f, 1f);

	static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size) {
		rect.anchorMin = rect.anchorMax = anchor;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	Text CreateText(Transform parent, string content, int size, Color color) {
		var go = new GameObject("Text", typeof(RectTransform));
		go.transform.SetParent(parent, false);
		var text = go.AddComponent<Text>();
		text.font = _font;
		text.text = content;
		text.fontSize = size;
		text.fontStyle = FontStyle.Bold;
		text.color = color;
		text.alignment = TextAnchor.MiddleCenter;
		text.raycastTarget = false;
		return text;
	}

	RectTransform CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick) {
		var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
		go.transform.SetParent(parent, false);
		go.GetComponent<Image>().color = color;
		go.GetComponent<Button>().onClick.AddListener(onClick);
		var text = CreateText(go.transform, label, 46, Color.white);
		Stretch(text.rectTransform);
		return (RectTransform)go.transform;
	}

	InputField CreateInput(Transform parent, string placeholder) {
		var go = new GameObject("JumpInput", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(parent, false);
		go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
		var text = CreateText(go.transform, "", 50, Color.black);
		Stretch(text.rectTransform);
		text.supportRichText = false;
		var hint = CreateText(go.transform, placeholder, 44, new Color(0f, 0f, 0f, 0.35f));
		Stretch(hint.rectTransform);
		hint.fontStyle = FontStyle.Italic;
		var input = go.AddComponent<InputField>();
		input.textComponent = text;
		input.placeholder = hint;
		input.contentType = InputField.ContentType.IntegerNumber;
		input.characterLimit = 5;
		// Done / Enter on the keyboard also jumps (Back cancels)
		input.onEndEdit.AddListener(value => {
			if(!input.wasCanceled && !string.IsNullOrEmpty(value) && _panel.activeSelf)
				JumpFromInput();
		});
		return input;
	}

	static void Stretch(RectTransform rect) {
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}
	#endregion//Build UI
}
#endif
