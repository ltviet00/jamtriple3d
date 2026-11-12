using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;
using Sirenix.OdinInspector;
using System.Threading;

/// <summary>
/// Spawn toys on screen, depend on current level ..
/// </summary>
public class ToySpawner : MonoBehaviour {
	#region Init, config
	[Header("ShareData - storage, stats")]
	[SerializeField] ToyStorage toyStorage;
	[SerializeField] LevelDataStorage levelStorage;
	[SerializeField] LevelToyStatistics levelToyStats;

	[Header("ShareData - weekly event")]
	[SerializeField] WeeklyEventRewardStorage weeklyEventStorage;
	[SerializeField] WeeklyEventDataSaver weeklyEventData;

	[Header("ShareData, config")]
	[SerializeField] IntegerVariable userCurrentLevelVar;
	[SerializeField] LayerMaskVariable toyLayerMaskVar;

	[Header("Config VFX")]
	[SerializeField] GameObjectVariable vfxSparkleForSpecialToyCfg;

	[Header("Event out")]
	[SerializeField] GameEvent finishSpawningEvent;

	[Header("ShareData - visible area")]
	[SerializeField] TransformVariable visibleAreaLeftTopVar;
	[SerializeField] TransformVariable visibleAreaRightBottomVar;
	[SerializeField] float marginFromEdge = 0.5f;

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
		// also clear ToyStats
		levelToyStats.ClearAllStats();
		ClearRunningTasks();
	}
	#endregion// Init config


	#region Public
	///<summary> Call from mainFlow, spawn toy </summary>
	public async UniTask SpawnToysAsync(CancellationToken cancelToken) {
		if(isFailedConfig)
			return;
		// spawn area. adjust size by margin
		var (leftTopPos, areaSize) = CalculateSpawnArea();
		// level data
		var levelData = levelStorage.GetLevelData(userCurrentLevelVar.Value);
		var toyGroup = toyStorage.SelectToyPrefabGroupForLevel(levelData);
		// decoy and target
		var (decoySpawnInfo, targetSpawnInfo) = PrepareSpawnDataForDecoyAndTarget(levelData, toyGroup);
		// box sort mode (reference video): every toy on the field belongs to a box, no decoy
		var targetBar = FindObjectOfType<TargetUIBar>();
		if(targetBar != null && targetBar.IsBoxSortMode) {
			foreach(var decoy in decoySpawnInfo) {
				if(decoy.Item2 > 0)
					targetSpawnInfo.Add(decoy);
			}
			decoySpawnInfo.Clear();
		}
		
		var supportSpawnInfo = PrepareSpawnDataForSupportToy(levelData);
		_spawnedThisLevel.Clear();

		// Spawn them on screen
		var spawnedDecoys = SpawnToysOnScene(decoySpawnInfo, leftTopPos, areaSize);
		var spawnedTargets = SpawnToysOnScene(targetSpawnInfo, leftTopPos, areaSize);
		// save to stats
		levelToyStats.SetupStats(spawnedDecoys, spawnedTargets);

		// Support toys, call after SetupStats() cause it will clear all maps
		if(supportSpawnInfo.Count > 0) {
			var spawnedSupport = SpawnToysOnScene(supportSpawnInfo, leftTopPos, areaSize);
			levelToyStats.SetSupportToyMap(spawnedSupport);
			// vfx for support toys
			SpawnSparkleVfxForSpecialToy(spawnedSupport);
		}
		// weely event toys
		if(weeklyEventStorage.IsEventAvailable(userCurrentLevelVar.Value)) {
			var eventToyPrefab = toyStorage.GetWeeklyEventToyWithToyName(weeklyEventData.Data.selectedToyName);
			// not found? then ignore. Already log error at toyStorage
			if(eventToyPrefab != null) {
				var spawnAmount = weeklyEventStorage.GetToyAmountToSpawn();
				var spawnInfo = new List<(ToyView, int)>();
				spawnInfo.Add((eventToyPrefab, spawnAmount));
				var spawnedEventToy = SpawnToysOnScene(spawnInfo, leftTopPos, areaSize);
				levelToyStats.SetWeeklyEventToyMap(spawnedEventToy);
				// vfx for support toys
				SpawnSparkleVfxForSpecialToy(spawnedEventToy);
			}
		}

		// same visual size for every kind (reference video), only a little natural variation kept
		NormalizeToySizes(_spawnedThisLevel);
		// all toys start as one pile in the center (reference video), then fall and spread by physics
		ArrangeIntoCenterPile(_spawnedThisLevel, leftTopPos, areaSize);
		_spawnedThisLevel.Clear();
		// raise event at last
		finishSpawningEvent.Raise();
	}
	#endregion//Public


	#region Private
	(Vector3 leftTopPos, Vector2 areaSize) CalculateSpawnArea() {
		// spawn area. adjust size by margin
		var leftTopPos = visibleAreaLeftTopVar.Value.position;
		leftTopPos.x += marginFromEdge;
		leftTopPos.y -= marginFromEdge;
		// reduce spawn area size 2x margin (2 edge)
		Vector2 areaSize = new Vector2();
		areaSize.x = Mathf.Abs(visibleAreaLeftTopVar.Value.position.x - visibleAreaRightBottomVar.Value.position.x) - 2f * marginFromEdge;
		areaSize.y = Mathf.Abs(visibleAreaLeftTopVar.Value.position.y - visibleAreaRightBottomVar.Value.position.y) - 2f * marginFromEdge;
		return (leftTopPos, areaSize);
	}

	(List<(ToyView, int)>, List<(ToyView, int)>) PrepareSpawnDataForDecoyAndTarget(LevelData levelData, ToyPrefabGroup toyGroup) {
		// clone the toyList of levelData
		var toyListclone = toyGroup.toyList.Clone();
		// take out enough prefab for level
		var decoyToyPrefabs = MathUtils.TakeOutSubListWithRandomElement(toyListclone, levelData.totalInfo.typeUsed);
		// take out from it to use for target
		var targetToyPrefabs = MathUtils.TakeOutSubListWithRandomElement(decoyToyPrefabs, levelData.targetInfo.typeUsed);

		// amount toy to spawn for each type (NOtes: update for triple project, this's the triad number, need to x3 before spawning on screen
		var numberOfTriadToSpawnForDecoy = MathUtils.CreateRandomIntegerListWhoseSumIsDefined(decoyToyPrefabs.Count, levelData.GetNumberOfDecoyToyTriadToSpawn());
		var numberOfTriadToSpawnForTarget = MathUtils.CreateRandomIntegerListWhoseSumIsDefined(targetToyPrefabs.Count, levelData.targetInfo.totalTriad);

		var decoyData = new List<(ToyView prefab, int amount)>();
		var targetData = new List<(ToyView prefab, int amount)>();
		for(int n = 0; n < decoyToyPrefabs.Count; n++) {
			var prefab = decoyToyPrefabs[n];
			// update for triple project, x3
			var amount = numberOfTriadToSpawnForDecoy[n] * 3;
			decoyData.Add((prefab, amount));
		}
		for(int n = 0; n < targetToyPrefabs.Count; n++) {
			var prefab = targetToyPrefabs[n];
			// update for triple project, x3
			var amount = numberOfTriadToSpawnForTarget[n] * 3;
			targetData.Add((prefab, amount));
		}
		return (decoyData, targetData);
	}

	List<(ToyView, int)> PrepareSpawnDataForSupportToy(LevelData levelData) {
		var result = new List<(ToyView, int)>();
		var supportToyList = levelData.supportToyList;
		for(int n = 0; n < supportToyList.Count; n++) {
			var define = supportToyList[n];
			var toyViewPrefab = toyStorage.GetSupportToyWithID(define.id);
			// not found? no need warning ,already warning in GetSuppportToyWithID
			if(toyViewPrefab == null)
				continue;
			result.Add((toyViewPrefab, define.amount));
		}
		return result;
	}

	Dictionary<GameObject, List<ToyView>> SpawnToysOnScene(List<(ToyView prefab, int amount)> spawnData, Vector3 leftTopPos, Vector2 spawnAreaSize) {
		var result = new Dictionary<GameObject, List<ToyView>>();
		var layerForToy = GameObjectUtils.FromLayerMaskToLayerIndexForGameObject(toyLayerMaskVar.Value);
		for(int n = 0; n < spawnData.Count; n++) {
			var data = spawnData[n];
			var spawnedList = new List<ToyView>(data.amount);

			for(int m = 0; m < data.amount; m++) {
				var prefab = data.prefab;
				var spawnPos = RandomPositionInsideArea(leftTopPos, spawnAreaSize);
				// spawn
				var toy = Instantiate(prefab.gameObject).GetComponent<ToyView>();
				toy.transform.position = spawnPos;
				// some config
				toy.gameObject.layer = layerForToy;
				toy.rootPrefab = prefab.gameObject;
				// name it to easier to count in inspector
				toy.name = $"{toy.name}_{m}";
				// add to list
				spawnedList.Add(toy);
				_spawnedThisLevel.Add(toy);
			}
			// add to result
			result.Add(data.prefab.gameObject, spawnedList);
		}
		return result;
	}

	void SpawnSparkleVfxForSpecialToy(Dictionary<GameObject, List<ToyView>> toysMap) {
		foreach(var toyList in toysMap.Values) {
			for(int n = 0; n < toyList.Count; n++) {
				var vfx = ObjectPool.Instance.Spawn(vfxSparkleForSpecialToyCfg.Value);
				var followScript = vfx.GetComponent<VfxFollowToy>();
				if(followScript == null) {
					Log.Error(false, this, $"Not found VFXFollow component in prefab [{vfxSparkleForSpecialToyCfg.Value.name}]");
					Destroy(vfx);
					return;
				}
				// let vfx follow
				var toy = toyList[n];
				followScript.FollowToy(toy);
			}
		}
	}

	#region Toy size
	[Header("Toy size (level start)")]
	[Tooltip("Visual size (world units, biggest side seen from the camera) every toy kind is scaled to")]
	[SerializeField] float toyTargetSize = 1.05f;
	[Tooltip("0 = all kinds exactly the same size, 1 = keep the original size differences")]
	[Range(0f, 1f)]
	[SerializeField] float toyKeepSizeDifference = 0.25f;

	///<summary> Scale every toy kind so they look about the same size. Small kinds stay a bit smaller (toyKeepSizeDifference) </summary>
	void NormalizeToySizes(List<ToyView> toys) {
		toys.RemoveAll(toy => toy == null);
		// measure one toy per kind
		var sizeByKind = new Dictionary<GameObject, float>();
		foreach(var toy in toys) {
			var kind = toy.rootPrefab != null ? toy.rootPrefab : toy.gameObject;
			if(sizeByKind.ContainsKey(kind))
				continue;
			var size = MeasureVisualSize(toy);
			if(size > 0.01f)
				sizeByKind.Add(kind, size);
		}
		if(sizeByKind.Count == 0)
			return;
		// median size of the kinds in this level
		var sizes = new List<float>(sizeByKind.Values);
		sizes.Sort();
		var median = sizes[sizes.Count / 2];
		foreach(var toy in toys) {
			var kind = toy.rootPrefab != null ? toy.rootPrefab : toy.gameObject;
			if(!sizeByKind.TryGetValue(kind, out var size))
				continue;
			var wantedSize = toyTargetSize * Mathf.Pow(size / median, toyKeepSizeDifference);
			toy.transform.localScale *= wantedSize / size;
		}
		Physics.SyncTransforms();
	}

	///<summary> Biggest side on the XY plane (what the camera sees), from the mesh renderers </summary>
	static float MeasureVisualSize(ToyView toy) {
		var renderers = toy.GetComponentsInChildren<Renderer>();
		var hasBounds = false;
		var bounds = new Bounds();
		foreach(var renderer in renderers) {
			if(!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
				continue;
			if(!hasBounds) {
				bounds = renderer.bounds;
				hasBounds = true;
			}
			else {
				bounds.Encapsulate(renderer.bounds);
			}
		}
		if(!hasBounds) {
			foreach(var collider in toy.GetComponentsInChildren<Collider>()) {
				if(!hasBounds) {
					bounds = collider.bounds;
					hasBounds = true;
				}
				else {
					bounds.Encapsulate(collider.bounds);
				}
			}
		}
		return hasBounds ? Mathf.Max(bounds.size.x, bounds.size.y) : 0f;
	}
	#endregion//Toy size


	#region Center pile
	[Header("Center pile (level start)")]
	[Tooltip("Max number of toy layers in the middle of the pile (more = taller, narrower pile)")]
	[SerializeField] int pileMaxLayers = 9;
	[Tooltip("Pile radius can not be bigger than this * the short side of the play area")]
	[SerializeField] float pileMaxRadiusRatio = 0.46f;
	[Tooltip("While the pile collapses, overlapping toys are pushed apart slowly (no explosion)")]
	[SerializeField] float pileDepenetrationVelocity = 1.5f;
	[SerializeField] float pileSettleDuration = 2.5f;
	[Tooltip("Sideways damping while the pile collapses (per second), so toys do not slide far and the pile keeps a mound shape")]
	[SerializeField] float pileSideDamping = 8f;
	const float GOLDEN_ANGLE = 2.39996f;
	const float LAYER_FILL = 0.85f; // how much of a layer disk is covered by toys

	readonly List<ToyView> _spawnedThisLevel = new List<ToyView>();

	///<summary> Move all toys into a dome shaped pile in the middle of the play area (gravity is +z, layers go toward the camera).
	/// Toys are shuffled so the kinds are mixed inside the pile </summary>
	void ArrangeIntoCenterPile(List<ToyView> toys, Vector3 leftTopPos, Vector2 areaSize) {
		toys.RemoveAll(toy => toy == null);
		var count = toys.Count;
		if(count == 0)
			return;
		// average toy size from colliders (fallback 1 unit)
		var sizeSum = Vector3.zero;
		var measured = 0;
		foreach(var toy in toys) {
			var colliders = toy.GetComponentsInChildren<Collider>();
			if(colliders.Length == 0)
				continue;
			var bounds = colliders[0].bounds;
			for(int n = 1; n < colliders.Length; n++)
				bounds.Encapsulate(colliders[n].bounds);
			sizeSum += bounds.size;
			measured++;
		}
		var avgSize = measured > 0 ? sizeSum / measured : Vector3.one;
		var footprint = Mathf.Max(0.3f, (avgSize.x + avgSize.y) * 0.5f);
		var layerHeight = Mathf.Max(0.3f, avgSize.z);

		// dome: layer k has radius R * sqrt(1 - k/layers), volume of all layers ~ R^2 * layers / 2
		var maxRadius = pileMaxRadiusRatio * Mathf.Min(areaSize.x, areaSize.y);
		var layers = Mathf.Max(1, pileMaxLayers);
		var cellArea = footprint * footprint / LAYER_FILL;
		var radius = Mathf.Sqrt(count * cellArea * 2f / (Mathf.PI * layers));
		if(radius > maxRadius) {
			// too many toys: keep max width, stack higher
			radius = maxRadius;
			layers = Mathf.CeilToInt(count * cellArea * 2f / (Mathf.PI * radius * radius));
		}
		radius = Mathf.Max(radius, footprint);

		var center = leftTopPos + new Vector3(areaSize.x * 0.5f, -areaSize.y * 0.5f, 0f);
		var bottomZ = transform.position.z;
		var positions = new List<Vector3>(count);
		for(int layer = 0; positions.Count < count; layer++) {
			var t = Mathf.Min(layer, layers - 1) / (float)layers;
			var layerRadius = Mathf.Max(footprint * 0.5f, radius * Mathf.Sqrt(1f - t));
			var capacity = Mathf.Max(1, Mathf.FloorToInt(Mathf.PI * layerRadius * layerRadius / cellArea));
			var angleOffset = layer * 0.9f;
			for(int n = 0; n < capacity && positions.Count < count; n++) {
				var r = capacity == 1 ? 0f : layerRadius * Mathf.Sqrt((n + 0.5f) / capacity);
				var angle = n * GOLDEN_ANGLE + angleOffset;
				var pos = center;
				pos.x += Mathf.Cos(angle) * r;
				pos.y += Mathf.Sin(angle) * r;
				pos.z = bottomZ - layer * layerHeight;
				positions.Add(pos);
			}
		}
		MathUtils.ShuffleList(toys);
		var bodies = new List<Rigidbody>(count);
		for(int n = 0; n < count; n++) {
			toys[n].transform.position = positions[n];
			// random spin so the pile looks messy (after size measure)
			toys[n].transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) * toys[n].transform.rotation;
			var body = toys[n].GetComponent<Rigidbody>();
			if(body != null) {
				body.position = positions[n];
				body.rotation = toys[n].transform.rotation;
				body.velocity = Vector3.zero;
				body.angularVelocity = Vector3.zero;
				bodies.Add(body);
			}
		}
		Physics.SyncTransforms();
		LimitDepenetrationWhileSettling(bodies).Forget();
	}

	async UniTaskVoid LimitDepenetrationWhileSettling(List<Rigidbody> bodies) {
		var original = new float[bodies.Count];
		for(int n = 0; n < bodies.Count; n++) {
			original[n] = bodies[n].maxDepenetrationVelocity;
			bodies[n].maxDepenetrationVelocity = pileDepenetrationVelocity;
		}
		// damp only the sideways motion (x, y), falling speed (z) is kept, so the pile drops fast but does not slide far
		var token = this.GetCancellationTokenOnDestroy();
		var endTime = Time.time + pileSettleDuration;
		while(Time.time < endTime && !token.IsCancellationRequested) {
			var keep = Mathf.Clamp01(1f - pileSideDamping * Time.fixedDeltaTime);
			for(int n = 0; n < bodies.Count; n++) {
				var body = bodies[n];
				if(body == null || body.isKinematic)
					continue;
				var velocity = body.velocity;
				velocity.x *= keep;
				velocity.y *= keep;
				body.velocity = velocity;
			}
			await UniTask.WaitForFixedUpdate(token).SuppressCancellationThrow();
		}
		for(int n = 0; n < bodies.Count; n++) {
			if(bodies[n] != null)
				bodies[n].maxDepenetrationVelocity = original[n];
		}
	}
	#endregion//Center pile

	Vector3 RandomPositionInsideArea(Vector3 leftTopPos, Vector2 spawnAreaSize) {
		// random pos. center point at the spawner, XY follow spawnArea. Also random a bit on Z coord
		var result = leftTopPos;
		result.z = transform.position.z;
		result.x += Random.Range(0, spawnAreaSize.x);
		result.y -= Random.Range(0, spawnAreaSize.y);
		result.z += Random.Range(-2f, 2f);
		return result;
	}


	UniTaskTokenHelper _token = new UniTaskTokenHelper();
	void ClearRunningTasks() {
		_token.ClearRunningTasks();
	}
	#endregion//Private


	#region Test
	[Button]
	void TestGenerate() {
		if(!Application.isPlaying) {
			return;
		}
		SpawnToysAsync(_token.Token).Forget();
	}
	#endregion//Test


	#region Classes
	//class SpawnInfo {
	//	public ToyView prefab;
	//	public int amount;
	//}
	#endregion//Classes
}
