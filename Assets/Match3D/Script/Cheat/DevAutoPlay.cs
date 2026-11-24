#if DEVELOPMENT_BUILD || UNITY_EDITOR || CHEAT
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dev tool: plays levels by itself on a device and logs everything that looks wrong.
/// Start: create "debug_autoplay.txt" in Application.persistentDataPath (Android: /sdcard/Android/data/[package]/files/).
/// Content (optional): "[levelCount] [startLevel]", e.g. "50 1". The file is deleted when the run starts.
/// Each tap goes through the real input path (EventSystem raycast at the toy's screen position -> InputPanel pointer down/up),
/// so a toy covered by UI, or a tap that does not pick, is caught. Every few seconds all remaining toys are checked:
/// inside the play area, on screen, not under UI, rendered.
/// Log: "autoplay_log.txt" (same folder), screenshots "autoplay_*.png" for each problem.
/// </summary>
public class DevAutoPlay : MonoBehaviour {
	const string TRIGGER = "debug_autoplay.txt";
	const string LOG = "autoplay_log.txt";
	const string GAME_SCENE = "Game";
	const float TAP_INTERVAL = 0.45f;
	const float PICK_TIMEOUT = 1.2f;
	const float LEVEL_TIMEOUT = 400f;
	const float NO_PROGRESS_TIMEOUT = 25f;
	const float HEALTH_CHECK_INTERVAL = 2f;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Install() {
		var trigger = Path.Combine(Application.persistentDataPath, TRIGGER);
		if(!File.Exists(trigger))
			return;
		var args = File.ReadAllText(trigger).Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
		File.Delete(trigger);
		var go = new GameObject(nameof(DevAutoPlay));
		DontDestroyOnLoad(go);
		var auto = go.AddComponent<DevAutoPlay>();
		auto._levelCount = args.Length > 0 && int.TryParse(args[0], out var c) ? c : 50;
		auto._startLevel = args.Length > 1 && int.TryParse(args[1], out var s) ? s : 1;
	}

	int _levelCount;
	int _startLevel;
	string _logPath;
	int _shotIndex;
	bool _retried;
	readonly Dictionary<string, int> _issueCount = new Dictionary<string, int>();
	readonly List<string> _summary = new List<string>();

	// per level
	bool _ready, _win, _lose, _timeUp, _boxFull;
	readonly HashSet<ToyView> _reportedToys = new HashSet<ToyView>();

	void Start() {
		// long unattended run: keep the screen on (only while the app runs, no device setting changed)
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		_logPath = Path.Combine(Application.persistentDataPath, LOG);
		File.WriteAllText(_logPath, $"AUTOPLAY start {System.DateTime.Now} levels={_levelCount} from={_startLevel} screen={Screen.width}x{Screen.height} dpi={Screen.dpi}\n");
		SubscribeEvents();
		// game events are loaded with the Game scene: subscribe again after each load (each asset once)
		SceneManager.sceneLoaded += (scene, mode) => SubscribeEvents();
		StartCoroutine(IE_Run());
	}

	// GameEvent clears its listeners in OnDisable (asset unloaded with a scene): subscribe again often, unsubscribe first so never twice
	void SubscribeEvents() {
		Subscribe("Evt_Game_Flow_ReadyForUserToInput", OnReady);
		Subscribe("Evt_Result_GameWin", OnWin);
		Subscribe("Evt_Result_GameLose", OnLose);
		Subscribe("Evt_Clock_TimeUp", OnTimeUp);
		Subscribe("Evt_Game_ToyBox_Fulled", OnBoxFull);
	}
	void OnReady() => _ready = true;
	void OnWin() => _win = true;
	void OnLose() => _lose = true;
	void OnTimeUp() => _timeUp = true;
	void OnBoxFull() => _boxFull = true;

	static void Subscribe(string eventName, System.Action callback) {
		foreach(var evt in Resources.FindObjectsOfTypeAll<GameEvent>()) {
			if(evt.name != eventName)
				continue;
			evt.Unsubscribe(callback);
			evt.Subscribe(callback);
		}
	}

	IEnumerator IE_Run() {
		yield return new WaitForSecondsRealtime(4f);
		for(int n = 0; n < _levelCount; n++) {
			var level = _startLevel + n;
			yield return IE_LoadLevel(level);
			yield return IE_PlayLevel(level);
		}
		Write("==== SUMMARY ====");
		foreach(var line in _summary)
			Write(line);
		foreach(var pair in _issueCount)
			Write($"issue {pair.Key}: {pair.Value}");
		Write("AUTOPLAY DONE");
	}

	IEnumerator IE_LoadLevel(int level) {
		_ready = _win = _lose = _timeUp = _boxFull = false;
		_reportedToys.Clear();
		var inventory = Find<UserLevelScoreInventory>();
		if(inventory != null) {
			inventory.Data.reachedLevel = level - 1;
			inventory.SaveToDisk();
		}
		foreach(var energy in Resources.FindObjectsOfTypeAll<UserEnergyInventory>()) {
			for(int k = 0; k < energy.MaxEnergy && !energy.IsFull(); k++)
				energy.IncreaseOneEnergyAndSaveCurrentTime();
		}
		var loading = FindObjectOfType<LoadingScreen>(true);
		if(loading != null)
			loading.Load(GAME_SCENE);
		else
			SceneManager.LoadScene(GAME_SCENE);
		var t = 0f;
		while(!_ready && t < 40f) {
			SubscribeEvents();
			// fallback if the ready event was missed: toys spawned and input open
			if(t > 12f && CountAll(Find<LevelToyStatistics>()) > 0 && IsInputEnabled(FindObjectOfType<InputPanel>())) {
				Write($"level {level}: ready event missed, start anyway");
				break;
			}
			t += 0.2f;
			yield return new WaitForSecondsRealtime(0.2f);
		}
		if(!_ready && CountAll(Find<LevelToyStatistics>()) <= 0 && !_retried) {
			_retried = true;
			Write($"level {level}: not loaded, try again");
			yield return IE_LoadLevel(level);
			yield break;
		}
		_retried = false;
		if(!_ready)
			Issue("LEVEL_NOT_READY", $"level {level}: ready event never came");
	}

	IEnumerator IE_PlayLevel(int level) {
		var stats = Find<LevelToyStatistics>();
		var cam = GetInputCamera();
		var startTime = Time.realtimeSinceStartup;
		var lastProgress = Time.realtimeSinceStartup;
		var lastRemain = CountAll(stats);
		var nextHealth = 0f;
		var taps = 0;
		var tapFails = 0;
		Write($"--- level {level} start: toys={lastRemain} target={stats?.RemainTargetcount} walls={DescribeWalls(cam)}");
		yield return new WaitForSecondsRealtime(0.5f);
		while(true) {
			if(_win || _lose || _timeUp || _boxFull)
				break;
			var now = Time.realtimeSinceStartup;
			if(now - startTime > LEVEL_TIMEOUT) {
				Issue("LEVEL_TIMEOUT", $"level {level}");
				break;
			}
			if(cam == null)
				cam = GetInputCamera();
			var remain = CountAll(stats);
			if(remain != lastRemain) {
				lastRemain = remain;
				lastProgress = now;
			}
			if(now > nextHealth) {
				nextHealth = now + HEALTH_CHECK_INTERVAL;
				SubscribeEvents();
				HealthCheck(level, stats, cam);
			}
			if(remain == 0) {
				// all picked, wait for the win
				yield return new WaitForSecondsRealtime(0.5f);
				if(Time.realtimeSinceStartup - lastProgress > 8f) {
					Issue("NO_WIN_AFTER_CLEAR", $"level {level}: field empty, no win event; boxes not done? target={stats.RemainTargetcount}");
					Shot("nowin");
					break;
				}
				continue;
			}
			if(now - lastProgress > NO_PROGRESS_TIMEOUT) {
				Issue("NO_PROGRESS", $"level {level}: {remain} toys left, nothing picked for {NO_PROGRESS_TIMEOUT}s");
				DumpToys(stats, cam);
				Shot("stuck");
				break;
			}
			var panel = FindObjectOfType<InputPanel>();
			if(panel == null || !IsInputEnabled(panel)) {
				yield return new WaitForSecondsRealtime(0.1f);
				continue;
			}
			var toy = ChooseToy(stats, cam);
			if(toy == null) {
				yield return new WaitForSecondsRealtime(0.3f);
				continue;
			}
			taps++;
			var picked = false;
			yield return IE_Tap(level, toy, panel, cam, ok => picked = ok);
			if(!picked)
				tapFails++;
			yield return new WaitForSecondsRealtime(TAP_INTERVAL);
		}
		var result = _win ? "WIN" : _lose ? "LOSE" : _timeUp ? "TIME_UP" : _boxFull ? "SLOTS_FULL" : "ABORT";
		var duration = Time.realtimeSinceStartup - startTime;
		var line = $"level {level}: {result} in {duration:0}s, taps={taps}, tapFails={tapFails}, left={CountAll(stats)}";
		_summary.Add(line);
		Write("=== " + line);
		if(!_win)
			Shot("end_" + result);
		yield return new WaitForSecondsRealtime(2.5f);
	}

	///<summary> Pick a toy a player would pick: visible, top of the pile, prefer one that matches an open box </summary>
	ToyView ChooseToy(LevelToyStatistics stats, Camera cam) {
		if(stats == null)
			return null;
		// a toy that is hit by the same ray the InputPanel uses, matching an open box first
		ToyView fallback = null;
		foreach(var list in stats.TargetMap.Values) {
			foreach(var toy in list) {
				if(toy == null || !IsTopVisible(toy, cam))
					continue;
				if(OpenBoxAccepts(toy))
					return toy;
				if(fallback == null)
					fallback = toy;
			}
		}
		if(fallback != null)
			return fallback;
		foreach(var map in FieldMaps(stats)) {
			foreach(var list in map.Values) {
				foreach(var toy in list) {
					if(toy != null && IsTopVisible(toy, cam))
						return toy;
				}
			}
		}
		return null;
	}

	IEnumerator IE_Tap(int level, ToyView toy, InputPanel panel, Camera cam, System.Action<bool> done) {
		var screen = (Vector2)cam.WorldToScreenPoint(toy.mainCollider.bounds.center);
		var ped = new PointerEventData(EventSystem.current) { position = screen, pressPosition = screen, button = PointerEventData.InputButton.Left };
		var hits = new List<RaycastResult>();
		EventSystem.current.RaycastAll(ped, hits);
		var topUi = hits.Count > 0 ? hits[0].gameObject : null;
		if(topUi != null && PathOf(topUi, 8).Contains("Tutorial")) {
			// tutorial overlay: do what it asks (press the highlighted button, or tap through)
			yield return IE_HandleTutorial(level, topUi);
			done(false);
			yield break;
		}
		if(topUi == null || topUi.GetComponentInParent<InputPanel>() == null) {
			Issue("TAP_BLOCKED_BY_UI", $"level {level}: {Describe(toy, cam)} -> UI on top: {PathOf(topUi)}");
			Shot("blocked");
			// keep the level going like a player would: try another toy next time
			_reportedToys.Add(toy);
			done(false);
			yield break;
		}
		ped.pointerPressRaycast = hits[0];
		ped.pointerCurrentRaycast = hits[0];
		ExecuteEvents.Execute(panel.gameObject, ped, ExecuteEvents.pointerDownHandler);
		yield return null;
		yield return null;
		ExecuteEvents.Execute(panel.gameObject, ped, ExecuteEvents.pointerUpHandler);
		var t = 0f;
		while(t < PICK_TIMEOUT) {
			if(toy == null || !IsOnField(toy)) {
				done(true);
				yield break;
			}
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		var physicsHit = PhysicsHitAt(screen, cam);
		Issue("TAP_NO_PICK", $"level {level}: {Describe(toy, cam)}; physics ray hit {physicsHit}; inputEnabled={IsInputEnabled(panel)}");
		Shot("nopick");
		done(false);
	}

	IEnumerator IE_HandleTutorial(int level, GameObject cover) {
		// a button not covered by the overlay = the one the tutorial highlights
		foreach(var selectable in FindObjectsOfType<Selectable>()) {
			if(!selectable.IsInteractable() || !selectable.isActiveAndEnabled || selectable.GetComponentInParent<DevCheatManager>() != null)
				continue;
			var rect = selectable.transform as RectTransform;
			var canvas = selectable.GetComponentInParent<Canvas>();
			if(rect == null || canvas == null)
				continue;
			var uiCam = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
			var screen = RectTransformUtility.WorldToScreenPoint(uiCam, rect.TransformPoint(rect.rect.center));
			var top = TopUiAt(screen);
			if(top == null || !top.transform.IsChildOf(selectable.transform))
				continue;
			Write($"level {level}: tutorial, press {PathOf(selectable.gameObject)}");
			yield return IE_PressUi(top, screen);
			yield return new WaitForSecondsRealtime(1f);
			yield break;
		}
		Write($"level {level}: tutorial overlay {PathOf(cover)}, tap it");
		var rt = cover.transform as RectTransform;
		yield return IE_PressUi(cover, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
		yield return new WaitForSecondsRealtime(1f);
	}

	static GameObject TopUiAt(Vector2 screen) {
		var ped = new PointerEventData(EventSystem.current) { position = screen };
		var hits = new List<RaycastResult>();
		EventSystem.current.RaycastAll(ped, hits);
		return hits.Count > 0 ? hits[0].gameObject : null;
	}

	static IEnumerator IE_PressUi(GameObject target, Vector2 screen) {
		var ped = new PointerEventData(EventSystem.current) { position = screen, pressPosition = screen, button = PointerEventData.InputButton.Left };
		var down = ExecuteEvents.ExecuteHierarchy(target, ped, ExecuteEvents.pointerDownHandler);
		ped.pointerPress = down;
		yield return null;
		yield return null;
		var up = down != null ? down : target;
		ExecuteEvents.Execute(up, ped, ExecuteEvents.pointerUpHandler);
		var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
		if(click != null)
			ExecuteEvents.Execute(click, ped, ExecuteEvents.pointerClickHandler);
	}

	void HealthCheck(int level, LevelToyStatistics stats, Camera cam) {
		if(stats == null || cam == null)
			return;
		var area = GetWallArea();
		foreach(var map in FieldMaps(stats)) {
			foreach(var list in map.Values) {
				foreach(var toy in list) {
					if(toy == null || _reportedToys.Contains(toy))
						continue;
					var problem = FindProblem(toy, cam, area);
					if(problem == null)
						continue;
					// a toy can be briefly out while bouncing, check again before reporting
					_pending.TryGetValue(toy, out var seen);
					_pending[toy] = seen + 1;
					if(seen + 1 < 2)
						continue;
					_reportedToys.Add(toy);
					var kind = problem.Split(':')[0];
					Issue("TOY_" + kind, $"level {level}: {Describe(toy, cam)} {problem}");
					Shot(kind.ToLower());
				}
			}
		}
	}
	readonly Dictionary<ToyView, int> _pending = new Dictionary<ToyView, int>();

	string FindProblem(ToyView toy, Camera cam, Rect? area) {
		if(!toy.gameObject.activeInHierarchy)
			return "INACTIVE";
		if(toy.mainRenderer != null && !toy.mainRenderer.enabled)
			return "NOT_RENDERED";
		var center = toy.mainCollider.bounds.center;
		var screen = cam.WorldToScreenPoint(center);
		if(screen.z < cam.nearClipPlane || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
			return "OFF_SCREEN";
		if(area.HasValue) {
			var a = area.Value;
			var margin = 0.3f;
			if(center.x < a.xMin - margin || center.x > a.xMax + margin || center.y < a.yMin - margin || center.y > a.yMax + margin)
				return "OUTSIDE_WALLS";
		}
		// rest on the floor: covered by UI?
		var ped = new PointerEventData(EventSystem.current) { position = screen };
		var hits = new List<RaycastResult>();
		EventSystem.current.RaycastAll(ped, hits);
		if(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<InputPanel>() == null && !PathOf(hits[0].gameObject, 8).Contains("Tutorial") && IsInputEnabled(FindObjectOfType<InputPanel>()))
			return "UNDER_UI:" + PathOf(hits[0].gameObject);
		return null;
	}

	#region Helpers
	///<summary> Every toy group on the field: target, decoy, support, weekly event (AllMaps has only the first two) </summary>
	static IEnumerable<Dictionary<GameObject, List<ToyView>>> FieldMaps(LevelToyStatistics stats) {
		yield return stats.TargetMap;
		yield return stats.DecoyMap;
		yield return stats.SupportMap;
		yield return stats.EventMap;
	}

	static bool IsOnField(ToyView toy) {
		var stats = Find<LevelToyStatistics>();
		if(stats == null)
			return false;
		foreach(var map in FieldMaps(stats)) {
			foreach(var list in map.Values) {
				if(list.Contains(toy))
					return true;
			}
		}
		return false;
	}

	static int CountAll(LevelToyStatistics stats) {
		if(stats == null)
			return -1;
		var n = 0;
		foreach(var map in FieldMaps(stats))
			foreach(var list in map.Values)
				foreach(var toy in list)
					if(toy != null)
						n++;
		return n;
	}

	///<summary> Box sort: an open box takes this toy type now (no reservation made) </summary>
	static bool OpenBoxAccepts(ToyView toy) {
		var bar = FindObjectOfType<TargetUIBar>();
		if(bar == null || !bar.IsBoxSortMode)
			return Find<LevelToyStatistics>().IsPickable(toy);
		var flags = BindingFlags.NonPublic | BindingFlags.Instance;
		var cards = typeof(TargetUIBar).GetField("_activeCards", flags)?.GetValue(bar) as IList;
		var queued = typeof(TargetUIBar).GetField("_queuedDestroyCards", flags)?.GetValue(bar) as ICollection<TargetItemUI>;
		if(cards == null)
			return false;
		foreach(TargetItemUI card in cards) {
			if(card != null && (queued == null || !queued.Contains(card)) && card.CanReserveToy(toy.rootPrefab))
				return true;
		}
		return false;
	}

	///<summary> Same ray as InputPanel.FindHitTarget </summary>
	static bool IsTopVisible(ToyView toy, Camera cam) {
		var screen = cam.WorldToScreenPoint(toy.mainCollider.bounds.center);
		if(screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
			return false;
		var hit = InputRay(screen, cam);
		return hit != null && (hit == toy.mainCollider || hit.GetComponentInParent<ToyView>() == toy);
	}

	static Collider InputRay(Vector2 screen, Camera cam) {
		var world = cam.ScreenToWorldPoint(screen);
		world.z = cam.transform.position.z;
		return Physics.Raycast(world, Vector3.forward, out var hit, Mathf.Infinity, InputLayerMask()) ? hit.collider : null;
	}

	static object PanelField(string field) {
		var panel = FindObjectOfType<InputPanel>(true);
		return panel == null ? null : typeof(InputPanel).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(panel);
	}

	static int InputLayerMask() {
		var v = PanelField("targetCheckLayerVar") as LayerMaskVariable;
		return v != null ? v.Value.value : ~0;
	}

	static Camera GetInputCamera() {
		var v = PanelField("viewCameraVar") as CameraVariable;
		return v != null && v.Value != null ? v.Value : Camera.main;
	}

	static bool IsInputEnabled(InputPanel panel) {
		if(panel == null || !panel.isActiveAndEnabled)
			return false;
		var graphic = panel.GetComponent<Graphic>();
		if(graphic != null && (!graphic.enabled || !graphic.raycastTarget))
			return false;
		foreach(var group in panel.GetComponentsInParent<CanvasGroup>()) {
			if(!group.blocksRaycasts || !group.interactable)
				return false;
		}
		return true;
	}

	static string PhysicsHitAt(Vector2 screen, Camera cam) {
		var c = InputRay(screen, cam);
		return c != null ? $"{c.name} (layer {LayerMask.LayerToName(c.gameObject.layer)})" : "nothing";
	}

	static Rect? GetWallArea() {
		var adjust = FindObjectOfType<AdjustWallFollowVisibleAreaAndCamera>();
		if(adjust == null)
			return null;
		Transform Get(string field) => typeof(AdjustWallFollowVisibleAreaAndCamera).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(adjust) as Transform;
		var l = Get("wallLeft"); var r = Get("wallRight"); var t = Get("wallTop"); var b = Get("wallBottom");
		if(l == null || r == null || t == null || b == null)
			return null;
		return Rect.MinMaxRect(l.position.x, b.position.y, r.position.x, t.position.y);
	}

	static string DescribeWalls(Camera cam) {
		var area = GetWallArea();
		if(!area.HasValue || cam == null)
			return "?";
		var a = area.Value;
		var bl = cam.WorldToScreenPoint(new Vector3(a.xMin, a.yMin, 0));
		var tr = cam.WorldToScreenPoint(new Vector3(a.xMax, a.yMax, 0));
		return $"screen x {bl.x:0}..{tr.x:0} y {bl.y:0}..{tr.y:0}; banner={BannerSpace.BottomPixels:0}px; footerTop={FooterTopY():0}";
	}

	///<summary> Top edge (screen px) of the in-game bottom bar (boosters), the UI the toys must stay above </summary>
	static float FooterTopY() {
		var best = -1f;
		foreach(var adjuster in FindObjectsOfType<SafeAreaAdjuster>()) {
			foreach(var rect in adjuster.GetComponentsInChildren<RectTransform>()) {
				if(!rect.name.ToLower().Contains("footer"))
					continue;
				var corners = new Vector3[4];
				rect.GetWorldCorners(corners);
				var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
				var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
				best = Mathf.Max(best, RectTransformUtility.WorldToScreenPoint(cam, corners[1]).y);
			}
		}
		return best;
	}

	static string Describe(ToyView toy, Camera cam) {
		var c = toy.mainCollider.bounds.center;
		var s = cam.WorldToScreenPoint(c);
		var rb = toy.mainRigid;
		return $"toy {toy.name} world ({c.x:0.00},{c.y:0.00},{c.z:0.00}) screen ({s.x:0},{s.y:0}) layer {LayerMask.LayerToName(toy.gameObject.layer)} " +
			$"kinematic={(rb != null && rb.isKinematic)} collider={toy.mainCollider.enabled} rendered={(toy.mainRenderer == null || toy.mainRenderer.enabled)} size={toy.mainCollider.bounds.size.magnitude:0.00}";
	}

	void DumpToys(LevelToyStatistics stats, Camera cam) {
		if(stats == null)
			return;
		foreach(var map in FieldMaps(stats))
			foreach(var list in map.Values)
				foreach(var toy in list)
					if(toy != null)
						Write($"    left: {Describe(toy, cam)} topVisible={IsTopVisible(toy, cam)} acceptNow={OpenBoxAccepts(toy)} renderers={DescribeRenderers(toy)} scale={toy.transform.lossyScale.x:0.00} ray: {DescribeRay(cam.WorldToScreenPoint(toy.mainCollider.bounds.center), cam)}");
	}

	static string DescribeRenderers(ToyView toy) {
		var all = toy.GetComponentsInChildren<Renderer>(true);
		var on = 0;
		foreach(var r in all)
			if(r.enabled && r.gameObject.activeInHierarchy)
				on++;
		return $"{on}/{all.Length}";
	}

	///<summary> Everything the InputPanel ray crosses at this screen point, nearest first </summary>
	static string DescribeRay(Vector2 screen, Camera cam) {
		var world = cam.ScreenToWorldPoint(screen);
		world.z = cam.transform.position.z;
		var hits = Physics.RaycastAll(world, Vector3.forward, Mathf.Infinity, InputLayerMask());
		System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
		var stats = Find<LevelToyStatistics>();
		var sb = new StringBuilder();
		for(int n = 0; n < hits.Length && n < 4; n++) {
			var c = hits[n].collider;
			var toy = c.GetComponentInParent<ToyView>();
			var state = toy == null ? "not a toy" : IsOnField(toy) ? "on field" : "NOT on field";
			var rb = c.attachedRigidbody;
			sb.Append($"[{c.name} z={c.bounds.center.z:0.00} {state} kinematic={(rb != null && rb.isKinematic)} trigger={c.isTrigger} active={c.gameObject.activeInHierarchy} renderers={(toy != null ? DescribeRenderers(toy) : "-")}] ");
		}
		return hits.Length == 0 ? "nothing" : sb.ToString();
	}

	static string PathOf(GameObject go, int depth = 4) {
		if(go == null)
			return "nothing";
		var sb = new StringBuilder(go.name);
		var t = go.transform.parent;
		for(int n = 0; n < depth && t != null; n++, t = t.parent)
			sb.Insert(0, t.name + "/");
		return sb.ToString();
	}

	void Issue(string kind, string detail) {
		_issueCount.TryGetValue(kind, out var n);
		_issueCount[kind] = n + 1;
		Write($"!! {kind} {detail}");
	}

	void Shot(string tag) {
		if(_shotIndex >= 60)
			return;
		_shotIndex++;
		// on Android the path is relative to persistentDataPath
		var file = $"autoplay_{_shotIndex:00}_{tag}.png";
		ScreenCapture.CaptureScreenshot(Application.isEditor ? Path.Combine(Application.persistentDataPath, file) : file);
	}

	void Write(string line) {
		Debug.Log("[AutoPlay] " + line);
		File.AppendAllText(_logPath, $"{Time.realtimeSinceStartup:0.0} {line}\n");
	}

	static T Find<T>() where T : Object {
		var all = Resources.FindObjectsOfTypeAll<T>();
		return all.Length > 0 ? all[0] : null;
	}
	#endregion
}
#endif
