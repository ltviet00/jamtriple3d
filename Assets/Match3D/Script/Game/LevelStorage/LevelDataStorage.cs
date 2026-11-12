using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
#endif//UNITY_EDITOR

/// <summary>
/// Storage for all level in game
/// </summary>
[CreateAssetMenu(fileName = "LevelStorage", menuName = "Match3D/LevelStorage")]
public class LevelDataStorage : ScriptableObject {
	#region Init, config
	[Header("Data")]
	[SerializeField] List<LevelData> allLevelData;
	[SerializeField] ToyStorage toyStorage;

	[Header("Config repeat")]
	[SerializeField] int repeatFromLevel;

	[Header("Level time")]
	[SerializeField] FloatVariable timePerTargetToyVar;
	[SerializeField] IntegerVariable minLevelTimeVar;
	[SerializeField] SupportToyID clockSupportToyID;
	[SerializeField] IntegerVariable clockSupportTimeAddPerItemCfg;

	[Header("Thunder support")]
	[SerializeField] SupportToyID thunderSupportToyID;
	[SerializeField] IntegerVariable thunderSupportDestroyPerItemCfg;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	[SerializeField] string[] ignoreList;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields(ignoreList);
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}
	#endregion// Init config


	#region Public
	public int LevelCount => allLevelData != null ? allLevelData.Count : 0;

	public LevelData GetLevelData(int index) {
		if(isFailedConfig)
			return null;
		// check repeat
		if(index < 0) {
			index = 0;
		}
		if(index >= allLevelData.Count) {
			// a simple version of Mathf.Repeat, but for integer instead of float
			var odd = (index - repeatFromLevel) % (allLevelData.Count - repeatFromLevel);
			index = repeatFromLevel + odd;
			Log.Info($"Index out of level, repeat back to [{index}]");
			// careful clamp
			index = Mathf.Clamp(index, 0, allLevelData.Count - 1);
		}
		return allLevelData[index];
	}

	public int GetMaxLevel() {
		if(isFailedConfig)
			return 0;
		return allLevelData.Count;
	}
	#endregion//Public


#if UNITY_EDITOR
	[Header("------- For Editor only")]
	[SerializeField] int generateMoreCount = 10;

	[Header("------- Auto increase difficult")]
	[SerializeField] DifficultConfig generateConfig;

	[Header("------- Support toy config")]
	[SerializeField] SupportToyConfig supportConfig;

	[Button]
	void LoadAllLevelInSubfolder() {
		if(isFailedConfig)
			return;
		allLevelData.Clear();
		// get all LevelData in the toy folder
		var myPath = AssetDatabase.GetAssetPath(this);
		var folderPath = myPath.Substring(0, myPath.LastIndexOf('/'));
		var allLevelDataGUIDInFolder = AssetDatabase.FindAssets("t:LevelData", new string[] { folderPath });
		if(allLevelDataGUIDInFolder.Length == 0) {
			Log.Error(false, this, $"Found no LevelData in path [{folderPath}]");
			return;
		}
		// from guid back to LevelData, retrieve ToyView component
		for(int n = 0; n < allLevelDataGUIDInFolder.Length; n++) {
			var guid = allLevelDataGUIDInFolder[n];
			var toPath = AssetDatabase.GUIDToAssetPath(guid);
			var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(toPath);
			allLevelData.Add(levelData);
		}
		// dirty so it can be save
		EditorUtility.SetDirty(this);
	}

	[Button]
	void GenerateRandomMoreLevel() {
		// more level from current count
		var currentLevelCount = allLevelData.Count;
		var currentFilePath = AssetDatabase.GetAssetPath(this);
		var folderPath = currentFilePath.Substring(0, currentFilePath.LastIndexOf("/"));
		for(int n = 0; n < generateMoreCount; n++) {
			var levelData = (LevelData)ScriptableObject.CreateInstance(typeof(LevelData));
			// add config to level?
			AssetDatabase.CreateAsset(levelData, $"{folderPath}/Generated/Level_{n + currentLevelCount}.asset");
			// add to list as well
			allLevelData.Add(levelData);
		}
		// dirty so it can be save
		EditorUtility.SetDirty(this);
		// also fill difficult, support toy. Level duration, total amount
		AutoFillDifficultForAllLevelData();
		AutoFillSupportToyDataForAllLevel();
		CalculateLevelDurationForAllLevel();
		UpdateTotalToyForLevelHasThunderSupport();
		// after write to disk, let refresh
		AssetDatabase.Refresh();
	}

	[Button]
	void AutoFillDifficultForAllLevelData() {
		if(isFailedConfig)
			return;
		for(int n = 0; n < allLevelData.Count; n++) {
			var level = allLevelData[n];
			// total toy 
			level.totalInfo.totalTriad = generateConfig.totalTriad.GetValueAtLevelAsInt(n);
			level.totalInfo.typeUsed = generateConfig.totalToyTypeUsed.GetValueAtLevelAsInt(n);

			// target
			level.targetInfo.totalTriad = generateConfig.targetTriad.GetValueAtLevelAsInt(n);
			level.targetInfo.typeUsed = generateConfig.targetToyTypeUsed.GetValueAtLevelAsInt(n);
			// mark dirty
			EditorUtility.SetDirty(level);
		}
	}

	[Button]
	void AutoFillSupportToyDataForAllLevel() {
		if(isFailedConfig)
			return;
		// validate
		var availableFromLevel = supportConfig.availableFromLevel;
		if(availableFromLevel >= allLevelData.Count || availableFromLevel < 0) {
			Log.Warning(false, this, $"supportToyConfig.availableFromLevel is out of range. Clamped");
			availableFromLevel = Mathf.Clamp(availableFromLevel, 0, allLevelData.Count);
			supportConfig.availableFromLevel = availableFromLevel;
		}
		// clear support toy before define level
		for(int n = 0; n < availableFromLevel; n++) {
			allLevelData[n].supportToyList.Clear();
		}
		// create support toy data from define level
		for(int n = availableFromLevel; n < allLevelData.Count; n++) {
			var levelData = allLevelData[n];
			levelData.supportToyList.Clear();
			// not all level has support toy, make it random
			var hasSupportToy = Random.Range(0, 100) < supportConfig.percentHasSupportToy;
			if(!hasSupportToy)
				continue;
			var amount = Random.Range(supportConfig.randomRange.x, supportConfig.randomRange.y);
			var supportTypes = toyStorage.SupportToyList;
			// random amount of each support toy (eg: amount = 10, then random to 3 clock , 7 thunder
			var amountList = MathUtils.CreateRandomIntegerListWhoseSumIsDefined(supportTypes.Count, amount);
			for(int m = 0; m < amountList.Length; m++) {
				var supportDefine = new LevelData.SupportToyDefine() {
					amount = amountList[m],
					id = supportTypes[m].id,
				};
				levelData.supportToyList.Add(supportDefine);
			}
			EditorUtility.SetDirty(levelData);
		}
	}

	[Button]
	void CalculateLevelDurationForAllLevel() {
		for(int n = 0; n < allLevelData.Count; n++) {
			var level = allLevelData[n];
			// duration, first calculate : by totalTargetToy * timePerToy
			var levelTime = (int)((float)level.targetInfo.totalTriad * timePerTargetToyVar.Value);
			levelTime = levelTime > minLevelTimeVar.Value ? levelTime : minLevelTimeVar.Value;
			// trick here, if we got CLock support toy, then let decrease the level time, 
			// Level not get easier with Clock support toy, we just force user to collect Clock toy (haha)
			var clockSuportAmount = level.GetAmountOfSupportToy(clockSupportToyID);
			var durationDeduct = clockSuportAmount * clockSupportTimeAddPerItemCfg.Value;
			// let humanity a bit by deduct only 70% of it
			levelTime -= (int)(durationDeduct * 0.7f);
			level.levelDuration = levelTime;
			// dirty to be save
			EditorUtility.SetDirty(level);
		}
	}

	[Button]
	void UpdateTotalToyForLevelHasThunderSupport() {
		for(int n = 0; n < allLevelData.Count; n++) {
			var level = allLevelData[n];
			// trick here, if we got thunder support toy, then let increase total, 
			// Level not get easier with support toy, we just force user to collect support toy
			var thunderSuportAmount = level.GetAmountOfSupportToy(thunderSupportToyID);
			var decoyToAddMore = thunderSuportAmount * thunderSupportDestroyPerItemCfg.Value;
			// let humanity a bit by add only 60% of it
			level.totalInfo.totalTriad += (int)(decoyToAddMore * 0.6f);
			// dirty to be save
			EditorUtility.SetDirty(level);
		}
	}

	[System.Serializable]
	class DifficultConfig {
		[Header("Repeat")]
		public int repeatFromLevel;

		[Header("Total toy")]
		public IncreaseConfig totalTriad;
		public IncreaseConfig totalToyTypeUsed;

		[Header("Target")]
		public IncreaseConfig targetTriad;
		public IncreaseConfig targetToyTypeUsed;
	}

	[System.Serializable]
	struct IncreaseConfig {
		public float begin;
		public float levelCountToReachMax;
		public float max;

		public int GetValueAtLevelAsInt(int level) {
			var increasePerLevel = (max - begin) / levelCountToReachMax;
			var result = begin + level * increasePerLevel;
			result = Mathf.Clamp(result, 0, max);
			return (int)result;
		}

		public float GetValueAtLevelAsFloat(int level) {
			var increasePerLevel = (max - begin) / levelCountToReachMax;
			var result = begin + (float)level * increasePerLevel;
			result = Mathf.Clamp(result, 0, max);
			return result;
		}
	}

	[System.Serializable]
	class SupportToyConfig {
		public int availableFromLevel;
		public int percentHasSupportToy;
		public Vector2Int randomRange;
	}
#endif//UNITY_EDITOR
}
