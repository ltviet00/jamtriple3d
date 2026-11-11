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
		
		// Spawn them on screen
		var spawnedDecoys = SpawnToysOnScene(decoySpawnInfo, leftTopPos, areaSize);
		var spawnedTargets = SpawnToysOnScene(targetSpawnInfo, leftTopPos, areaSize);
		// save to stats
		levelToyStats.SetupStats(spawnedDecoys, spawnedTargets);

		// Support toys, call after SetupStats() cause it will clear all maps
		if(levelData.supportToyList.Count > 0) {
			var supportSpawnInfo = PrepareSpawnDataForSupportToy(levelData);
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
				var randomPos = RandomPositionInsideArea(leftTopPos, spawnAreaSize);
				// spawn
				var toy = Instantiate(prefab.gameObject).GetComponent<ToyView>();
				toy.transform.position = randomPos;
				// some config
				toy.gameObject.layer = layerForToy;
				toy.rootPrefab = prefab.gameObject;
				// name it to easier to count in inspector
				toy.name = $"{toy.name}_{m}";
				// add to list
				spawnedList.Add(toy);
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
