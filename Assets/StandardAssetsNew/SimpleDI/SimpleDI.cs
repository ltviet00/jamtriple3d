using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Simple Dependency Inject class
/// </summary>
public class SimpleDI {
	#region Singleton
	static SimpleDI s_instance = null;
	public static SimpleDI Instance {
		get {
			if(s_instance == null) {
				s_instance = new SimpleDI();
			}
			return s_instance;
		}
	}

	public static bool HasInstance() {
		return s_instance != null;
	}
	#endregion//Singleton


	#region Public - Regsiter, unregister
	///<summary> Set instance , auto use Type of the instance at key (remember to unregister when your dependency destroy) </summary>
	public void Register(object instance) {
		if(instance == null) {
			Log.Error(false, $"Null parameter");
			return;
		}
		var typeOfInstance = instance.GetType();
		if(_dependenciesMap.ContainsKey(typeOfInstance)) {
			Log.Error(false, $"Already contain type [{typeOfInstance.Name}]");
			return;
		}
		_dependenciesMap[typeOfInstance] = instance;
	}

	///<summary> Set intance for type T, (remember to unregister when your dependency destroy) </summary>
	public void Register<T>(object instance) where T : class {
		if(instance == null) {
			Log.Error(false, $"Null parameter");
			return;
		}
		var typeOfIntance = typeof(T);
		if(_dependenciesMap.ContainsKey(typeOfIntance)) {
			Log.Error(false, $"Already contain type [{typeOfIntance.Name}], only one instance for a type is allowed");
			return;
		}
		_dependenciesMap[typeOfIntance] = instance;
	}

	///<summary> Unregister type T, often call when the instance was destroy </summary>
	public void Unregister(object instance) {
		if(instance == null) {
			Log.Error(false, $"Null parameter");
			return;
		}
		var typeOfinstance = instance.GetType();
		if(!_dependenciesMap.TryGetValue(typeOfinstance, out var existDependency)) {
			Log.Error(false, $"Not contain type [{typeOfinstance.Name}] to remove");
			return;
		}
		if(existDependency != instance) {
			Log.Error(false, $"The registed instance for [{typeOfinstance.Name}] is not same to the parameter intance, can't unregister");
			return;
		}
		_dependenciesMap.Remove(typeOfinstance);
	}

	///<summary> Unregister type T, without object instance, just clear the type</summary>
	public void Unregister(Type type) {
		if(type == null) {
			Log.Error(false, "Null parameter");
			return;
		}
		if(_dependenciesMap.ContainsKey(type)) {
			_dependenciesMap.Remove(type);
		}
	}
	#endregion// - Regsiter, unregister


	#region Public - Get, Inject
	///<summary> Get intance of type T. Will create default instance if it not get Registered yet, so never null return </summary>
	public T Get<T>() where T : class, new() {
		// return if already exit
		var typeOfT = typeof(T);
		if(_dependenciesMap.TryGetValue(typeOfT, out var result)) {
			return result as T;
		}
		// create new instance if not exist
		var instance = CreateInstanceAndAddToDependenciesMap<T>();
		return instance;
	}

	///<summary> Get intance of type T. Will create default instance if it not get Registered yet, so never null return </summary>
	public object Get(Type type) {
		if(_dependenciesMap.TryGetValue(type, out var result)) {
			return result;
		}
		// create new instance if not exist
		var instance = CreateInstanceAndAddToDependenciesMap(type);
		return instance;
	}

	///<summary> Check if type already registerd (call Get() will alway got a valid instance) </summary>
	public bool Contains(Type type) {
		return _dependenciesMap.ContainsKey(type);
	}

	///<summary> Check if type already registerd (call Get() will alway got a valid instance) </summary>
	public bool Contains<T>() {
		var type = typeof(T);
		return _dependenciesMap.ContainsKey(type);
	}

	///<summary> Auto inject all field that marked as Dependency. NOTES: should call it at Start(), after Awake, OnEnable cause Dependency will register at Awake/Oneanble, call Inject at Start to make sure all dependencies are ready. Return FALSE if missing any dependency, use that to handle your logic </summary>
	public bool Inject(object target) {
		if(target == null) {
			Log.Error(false, $"Null parameter");
			return false;
		}
		var objectType = target.GetType();
		var membersToInject = GetDependencyMembers(objectType);
		bool isMissingAnyDependency = false;
		for(int n = 0; n < membersToInject.Count; n++) {
			var member = membersToInject[n];
			var memType = member.GetMemberType();
			if(_dependenciesMap.TryGetValue(memType, out var dependency)) {
				member.SetMemberValue(target, dependency);
			}
			else {
				Log.Warning(false, $"Not found registered type [{memType}] to inject");
				isMissingAnyDependency = true;
			}
		}
		return isMissingAnyDependency;
	}
	#endregion//Public - Get, Inject


	#region Private - Auto create type
	///<summary> Main map of instance for registered types </summary>
	Dictionary<Type, object> _dependenciesMap = new Dictionary<Type, object>();

	///<summary> Generic function, create instance, add to our map. Cause it a generic, we can use var instance = new T() </summary>
	T CreateInstanceAndAddToDependenciesMap<T>() where T : class, new() {
		// Monobehavior? create default instance and mark it dontdestroyOnLoad
		var castedToType = typeof(T);
		if(castedToType.IsSubclassOf(typeof(MonoBehaviour))) {
			var obj = new GameObject();
			obj.name = $"Instance of [{castedToType.Name}]";
			var component = obj.AddComponent(castedToType);
			GameObject.DontDestroyOnLoad(obj);
			Log.Info($"Create instance of [{castedToType.Name}], monoBehavior, carefull if it got some SerializeField, will be null");

			var castToTarget = component as T;
			_dependenciesMap.Add(typeof(T), castToTarget);
			return castToTarget;
		}
		// SO is special asset, can't create via new(), need to use ScriptableObejct.CreateInstance(), but rather not, so warning here
		if(castedToType.IsSubclassOf(typeof(ScriptableObject))) {
			Log.Error(false, $"[{castedToType}] is ScriptableObject, should be created manually instead of using SimpleDI, return null");
			return null;
		}
		// other normal class with default construction (new())
		var instance = new T();
		_dependenciesMap.Add(typeof(T), instance);
		Log.Info($"Create instance of [{castedToType.Name}]");
		return instance;
	}

	///<summary> Non-generic function, create instance, add to our map. Cause its non-generic, use reflection to create instance, instance = Activator.Createinstance(type).
	///Notes: it has null case when Type = ScriptableObject </summary>
	object CreateInstanceAndAddToDependenciesMap(Type type) {
		// Monobehavior? create default instance and mark it dontdestroyOnLoad
		if(type.IsSubclassOf(typeof(MonoBehaviour))) {
			var obj = new GameObject();
			obj.name = $"Instance of [{type.Name}]";
			var component = obj.AddComponent(type);
			GameObject.DontDestroyOnLoad(obj);
			Log.Info($"Create instance of [{type.Name}], monoBehavior, carefull if it got some SerializeField, will be null");
			return component;
		}
		// SO is special asset, can't create via new(), need to use ScriptableObejct.CreateInstance(), but rather not, so warning here
		else if(type.IsSubclassOf(typeof(ScriptableObject))) {
			Log.Error(false, $"[{type}] is ScriptableObject, should be created manually instead of using SimpleDI, return null");
			return null;
		}
		// other normal class with default construction (new())
		else {
			var instance = Activator.CreateInstance(type);
			_dependenciesMap.Add(type, instance);
			Log.Info($"Create instance of [{type.Name}]");
			return instance;
		}
	}
	#endregion//Private - Auto create type


	#region Classes
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DependencyAttribute : Attribute {
		public DependencyAttribute() {
		}
	}
	#endregion//Classes


	#region Static
	///<summary> Call GetFields of reflection multiple time look bad, so cache them here, retrieve each time any call to Inject </summary>
	static readonly Dictionary<Type, List<MemberInfo>> DependencyMembersCache = new Dictionary<Type, List<MemberInfo>>();

	public static List<MemberInfo> GetDependencyMembers(Type type) {
		if(!DependencyMembersCache.TryGetValue(type, out List<MemberInfo> dependencyMembers)) {
			dependencyMembers = GetDependencyMembersInternal(type);
		}
		return dependencyMembers;
	}

	///<summary> Find all members of this type that mark [Dependecy]. Notes : find the current class, not finding in superType </summary>
	static List<MemberInfo> GetDependencyMembersInternal(Type type) {
		List<MemberInfo> results = new List<MemberInfo>();
		BindingFlags allMembers = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		// do we really need these?
		//// We want private fields from supertypes too, so let's walk up the type
		//// hierarchy until we hit Behaviour (DeclaredOnly guarantees we won't get duplication)
		//while(type != null && type != typeof(MonoBehaviour)) {
		//	FieldInfo[] fields = type.GetFields(allMembers);
		//	foreach(FieldInfo field in fields) {
		//		if(!field.FieldType.IsPrimitive && field.IsDefined(typeof(DependencyAttribute), inherit: true)) {
		//			result.Add(field);
		//		}
		//	}
		//	type = type.BaseType;
		//}

		// get all fields of this type, and pick fields with the Dependency Attribute
		var fields = type.GetFields(allMembers);
		for(int n = 0; n < fields.Length; n++) {
			var field = fields[n];
			if(!field.FieldType.IsPrimitive && field.IsDefined(typeof(DependencyAttribute), inherit: true)) {
				results.Add(field);
			}
		}

		// save to the cache map
		DependencyMembersCache[type] = results;
		return results;
	}
	#endregion//Static


	#region Static Utils
	///<summary> Shortcut to register type to singleton </summary>
	public static void RegisterType(object instance) {
		Instance.Register(instance);
	}

	///<summary> Shortcut to Unregister type. Include a check if SimpleDI are available </summary>
	public static void UnRegisterType(object instance) {
		if(instance == null) {
			Debug.LogError($"Null parameter");
			return;
		}
		if(HasInstance()) {
			Instance.Unregister(instance);
		}
	}

	///<summary> Unregister type T, don't care if T already register or not </summary>
	public static void UnregisterTypeWithoutInstance(Type type) {
		if(type == null) {
			Debug.LogError($"Null parameter");
			return;
		}
		if(HasInstance()) {
			Instance.Unregister(type);
		}
	}
	#endregion//Static utils
}
