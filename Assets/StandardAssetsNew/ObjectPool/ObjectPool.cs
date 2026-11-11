using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif//UNITY_EDITOR

public class ObjectPool : MonoSingleton<ObjectPool> {
	#region Init, config
	///<summary> By default object pool only need on a scene, dont need to live all game process</summary>
	protected override bool IsDontDestroyThisObject { get { return false; } }
	//-------- Config
	[SerializeField] int extendAmountWhenStackEmpty = 3;
	[SerializeField] PreGeneratePoolConfig[] preGeneratePool;

	/// <summary> Making object is child of a parent object has it cost. We just need a
	/// tidy hierachy in Editor for easier management only, but we dont need it in release
	/// build. So, keep this in editor only, it gonna save you a lot of performance in
	/// release build </summary>
#if UNITY_EDITOR
	[Header("Editor only")]
	public bool editor_makeCloneObjectChildOfPoolTransform = false;
#endif

	//-------- Fields
	/// <summary> The main Pool, dictionary with key = GameObject prefab and Value is stack
	/// of clone object from the Key prefab. We will Pop() Idle object in Stack each time
	/// Spawn() func was called.
	/// </summary>
	Dictionary<GameObject, Stack<GameObject>> _pool =
		new Dictionary<GameObject, Stack<GameObject>>();

	void OnValidate() {
		if(preGeneratePool == null || preGeneratePool.Length == 0) {
			//			Log.Info(this, "Not config any preGeneratePrefabs");
			return;
		}
		// checking missing config
		for(int m = 0; m < preGeneratePool.Length; m++) {
			var config = preGeneratePool[m];
			Log.Warning(config.prefab != null, this, "Missing config preGeneratePrefab at : {0}", m);
			if(config.prefab != null)
				Log.Warning(IsPrefab(config.prefab), this, "<{0}> not a prefab, it's an object on scene !!", config.prefab.name);
			Log.Warning(config.count > 0, this, "PrefabCount = 0 at : {0}, name : {1}", m, config.prefab != null ? config.prefab.name : string.Empty);
		}
		// checking duplicate in preGeneratePrefabs
		for(int m = 0; m < preGeneratePool.Length - 1; m++) {
			var firstPrefab = preGeneratePool[m].prefab;
			if(firstPrefab == null)
				continue;

			for(int n = m + 1; n < preGeneratePool.Length; n++) {
				var secondPrefab = preGeneratePool[n].prefab;
				if(secondPrefab == null)
					continue;
				Log.Warning(firstPrefab != secondPrefab, this, "Duplicate prefab : {0}, at {1} and {2}", firstPrefab.name, m, n);
			}
		}
	}

	protected override void DoOnAwake() {
		base.DoOnAwake();
		/// make objectPool alway at (0,0,0). Cause if we setting makeCloneObjectChildOfPoolTransform = true
		/// then we won't want position of all child objects be effected
		RootTransform.position = Vector3.zero;

		// not config preGeneratePrefabs, then do nothing
		if(preGeneratePool == null || preGeneratePool.Length == 0)
			return;

		for(int m = 0; m < preGeneratePool.Length; m++) {
			var config = preGeneratePool[m];
			// we're alreay checking OnValidate(), check it last time here for carefully
			if(config.prefab == null || config.count <= 0)
				continue;
			ExtendPool(preGeneratePool[m].prefab, preGeneratePool[m].count);
		}
	}

	protected override void DoOnDestroy() {
		_pool.Clear();
	}
	#endregion//Init, config


	#region Public
	///<summary> Request for an Idle object in Pool</summary>
	public GameObject Spawn(GameObject keyPrefab) {
		if(keyPrefab == null) {
			Log.Error(false, this, "Spawn null prefab !!!!");
			return null;
		}

		if(!IsPrefab(keyPrefab)) {
			Log.Error(false, this, "Spawn parameter must be prefab, [{0}] is an gameobject on scene", keyPrefab.name);
			return null;
		}

		GameObject clone;
		Stack<GameObject> stack;
		var extendAmount = GetNumberOfExtendWhenPoolFull(keyPrefab);
		// if there exist the Stack for keyprefab, then retrieve it, if stack is out
		// Then expand it. If stack is not exist, then Extend the pool.
		if(!_pool.ContainsKey(keyPrefab)) {
			ExtendPool(keyPrefab, extendAmount);
			stack = _pool[keyPrefab];
		}
		else {
			stack = _pool[keyPrefab];
			if(stack.Count == 0)
				ExtendPool(keyPrefab, extendAmount);
		}
		clone = stack.Pop();
		//remember to active it before returning
		clone.SetActive(true);
		return clone;
	}

	/// <summary>
	/// Should not call this func, let PoolRecycleHelper do it's job.
	/// Return an "finished working" object back to pool. Also deactive gameobject
	/// if it still active in hierachy
	/// </summary>
	/// <param name="keyPrefab">The Prefab that was used to instantiate</param>
	/// <param name="cloneObject">The instance of object in scene</param>
	public void Recycle(GameObject keyPrefab, GameObject cloneObject) {
		if(keyPrefab == null || cloneObject == null) {
			Log.Error(false, this, "Recyle(), null parameter (prefab or instance) !!!");
			return;
		}

		if(keyPrefab == cloneObject) {
			Log.Error(false, this, "Recyle(), keyPrefab and cloneObject must not the same");
			return;
		}
		// the cloneObject which was instantiate late, and have scene field = null
		// just checking keyPrefab here
		if(!IsPrefab(keyPrefab)) {
			//			Log.Error(false, this, "Recyle, <{0}> must be prefab, <{1}> must be object on scene, some parameters was wrong",
			//				keyPrefab.name, cloneObject.name);
			Log.Error(false, this, "Recyle, <{0}> must be prefab", keyPrefab.name);
			return;
		}

		if(_pool.ContainsKey(keyPrefab)) {
			var stack = _pool[keyPrefab];
			stack.Push(cloneObject);
			// make sure it been deactive before push to Stack
			if(cloneObject.activeInHierarchy)
				cloneObject.SetActive(false);
			// checking to push cloneObject to child of PoolTransform
			CheckToChangeParentTransformForCloneObject(cloneObject);
		}
		else {
			Log.Warning(false, cloneObject, "Recyle for {0}, but not found stack for {1} in pool", cloneObject.name, keyPrefab.name);
		}
	}

	Transform _myCacheTrans;
	///<summary> Prefab will go back to become child of rootTrans after finished working (disable)</summary>
	public Transform RootTransform {
		get {
			if(_myCacheTrans == null)
				_myCacheTrans = transform;
			return _myCacheTrans;
		}
	}
	#endregion//Public


	#region Private
	///<summary> Get the extendMoreWhenFull in Preconfig array (if exist). If not, then get
	///the default extend extendAmountWhenStackEmpty</summary>
	int GetNumberOfExtendWhenPoolFull(GameObject keyPrefab) {
		if(preGeneratePool == null || preGeneratePool.Length == 0)
			return extendAmountWhenStackEmpty;
		for(int n = 0; n < preGeneratePool.Length; n++) {
			var cfg = preGeneratePool[n];
			if(cfg.prefab == keyPrefab)
				return cfg.extendMoreWhenFull;
		}
		// not found? return default
		return extendAmountWhenStackEmpty;
	}

	/// <summary>
	/// Extend the pool, already take care both cases :
	/// already exist and not exising an Stack(prefab) in Pool.
	/// Default is extend 1 more object if stacks is empty </summary>
	void ExtendPool(GameObject keyPrefab, int amount = 1) {
		// prepare the prototype for cloning
		var prototype = ClonePrefabAndConfigTheInstance(keyPrefab);
		// get the gameObject stack, create new one if it not exist
		Stack<GameObject> stack;
		if(_pool.ContainsKey(keyPrefab)) {
			stack = _pool[keyPrefab];
		}
		else {
			// creat new stack, add it to pool dictionary
			stack = new Stack<GameObject>();
			_pool.Add(keyPrefab, stack);
		}
		// expand the stack
		ExpandObjectStackWithPrototype(stack, prototype, amount);
	}

	///<summary> In some cases, we need to modify, or add some components
	/// to prefab before push it to our pool</summary>
	GameObject ClonePrefabAndConfigTheInstance(GameObject prefab) {
#if UNITY_EDITOR
		/// one more carefully checking step, make sure no redundancies components
		/// was added to prefab that will be use in Pool. Wrap this checking in EDITOR,
		/// use it as "data-validate" phase
		var poolHelperComponent = prefab.GetComponent<PoolRecycleHelper>();
		if(poolHelperComponent != null) {
			Log.Warning(false, poolHelperComponent, "Should not add PoolRecycleHelper component to prefab {0}," +
			"let ObjectPool add this component automatically", prefab.name);
			DestroyImmediate(poolHelperComponent);
		}
#endif

		/// Deactive the prefab first, then clone it. This to make sure no Awake,Start are called
		/// on the clone object in this cloning process. So, we dont need to Deactive the clone
		/// object anymore, cause it already Deactive. Then, also make sure no OnDisable()
		/// will call on clone object when we push it to stack
		bool defaultPrefabActiveState = prefab.activeSelf;
		prefab.SetActive(false);
		var clone = InstantiatePrefab(prefab);
		// revert the state for prefab
		prefab.SetActive(defaultPrefabActiveState);
		// config name
		const string cloneNameFormat = "Clone_{0}";
		clone.name = string.Format(cloneNameFormat, prefab.name);
		CheckToChangeParentTransformForCloneObject(clone);
		// Add some helper components
		var helperScript = clone.GetComponent<PoolRecycleHelper>();
		if(helperScript == null)
			helperScript = clone.AddComponent<PoolRecycleHelper>();
		helperScript.keyPrefabWasUsedToClone = prefab;

		return clone;
	}

	/// <summary> Simple Expands the objectStack with prototype.
	/// Also push the prototype to the Stack </summary>
	void ExpandObjectStackWithPrototype(Stack<GameObject> stack, GameObject prototype, int amount = 1) {
		// push prototype to stack first
		stack.Push(prototype);
		// extend stack. use amount-1, cause we already push one (the prototype)
		for(int m = 0; m < amount - 1; m++) {
			var clone = InstantiatePrefab(prototype);
			CheckToChangeParentTransformForCloneObject(clone);
			stack.Push(clone);
		}
	}

	bool IsPrefab(GameObject checkObject) {
		return checkObject.scene.name == null;
	}

	/// <summary> In Editor, we will use PrefabUtils to instantiate prefab, instead of Object.Instantiate(),
	/// this to make clone objects in Hierachy window still "link" to root prefab in Project folder. In relase
	/// version, we dont need this feature</summary>
	GameObject InstantiatePrefab(GameObject prefab) {
#if UNITY_EDITOR
		/// In Editor, if object not a prefab (type != Prefab) it maybe PrefabInstance ...
		/// Then the func PrefabUtility.InstantiatePrefab() will return null. So, did a carefully check here
		//var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
		//if(prefabType == PrefabAssetType.NotAPrefab) {
		//	var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		//	return instance;
		//}
		//else {
		//	return Object.Instantiate(prefab);
		//}

		// UPDATE: above code nolonger work,
		// so just instance prefab as usual
		return Object.Instantiate(prefab);
#else
		return Instantiate(prefab);
#endif// UNITY_EDITOR
	}
	#endregion//Private


	#region BackToPool task
	/// <summary> Checks to change parent transform for clone object.
	/// To keep a tiny Hierachy in Editor. NOTE: Editor only </summary>
	void CheckToChangeParentTransformForCloneObject(GameObject clone) {
#if UNITY_EDITOR
		// Why don't we just SetParent() in here, why put them to a queue then
		// setParent in Update(). Read the comment for the _delayChangeParentTransformQueue
		if(editor_makeCloneObjectChildOfPoolTransform && clone.transform.parent != RootTransform)
			_delayChangeParentTransformQueue.Enqueue(clone.transform);
#endif
	}

#if UNITY_EDITOR
	/// <summary>
	/// When recyling a gameObject, if we call SetParent() in OnDisable(), which mean the same
	/// frame with SetActive(false) -> then the error "GameObject already inactive or active ..."
	/// will fire. SetParent() must be called 1 frame later than SetActive().
	/// So, solution is : in Recyle(), we put thoes objects in a queue, and in Update (sure it
	/// be executed in the next frame), we SetParent() for them.
	/// </summary>
	Queue<Transform> _delayChangeParentTransformQueue = new Queue<Transform>();
	void Update() {
		while(_delayChangeParentTransformQueue.Count > 0) {
			var trans = _delayChangeParentTransformQueue.Dequeue();
			trans.SetParent(RootTransform);
		}
	}
#endif //UNITY_EDITOR
	#endregion//BackToPool task


	#region Classes, config
	[System.Serializable]
	class PreGeneratePoolConfig {
		public GameObject prefab = null;
		public int count = 1;
		public int extendMoreWhenFull = 5;
	}
	#endregion//Classes, config


	#region Static
	///<summary> A shortcut to call ObjectPool.Instance.Spawn</summary>
	public static GameObject SpawnObject(GameObject keyPrefab) {
		return ObjectPool.Instance.Spawn(keyPrefab);
	}

	///<summary> A shortcut to recycle object in pool. Almost the time, we dont need this, the PoolRecycleHelper
	/// already take care the back-to-pool task each time object be disabled.</summary>
	public static void RecyleObject(GameObject keyPrefab, GameObject cloneObject) {
		/// Checking HasInstance to remove memory Leak when scene is close
		/// Because some object will call recyle on Destroy() - the ObjectPool may already
		/// destroyed before, call ObjectPool.Instance will create another one
		if(ObjectPool.HasInstance())
			ObjectPool.Instance.Recycle(keyPrefab, cloneObject);
	}
	#endregion//Static
}